using System;
using System.IO;
using Newtonsoft.Json;
using TiaOpennessBuilder.Builders;
using TiaOpennessBuilder.Config;

namespace TiaOpennessBuilder
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var configPath = args.Length > 0 ? args[0] : "project-config.json";
            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Config file not found: {configPath}");
                Console.Error.WriteLine("Usage: TiaOpennessBuilder.exe <path-to-project-config.json>");
                return 1;
            }

            var config = JsonConvert.DeserializeObject<ProjectConfig>(File.ReadAllText(configPath));
            if (config == null)
            {
                Console.Error.WriteLine("Failed to parse config file.");
                return 1;
            }

            using var session = config.AttachToRunningInstance
                ? TiaSessionManager.AttachToRunning()
                : new TiaSessionManager(config.WithUserInterface);

            Console.WriteLine(config.AttachToRunningInstance
                ? "Attaching to running TIA Portal instance..."
                : "Opening/creating TIA Portal project...");
            session.OpenOrCreateProject(config);

            var plcSoftware = session.GetOrCreatePlcSoftware(config.PlcOrderNumber, config.PlcDeviceName);

            if (config.ImportPlcSources)
            {
                Console.WriteLine("Importing SCL sources (UDTs and function blocks)...");
                foreach (var fileName in config.SclImportOrder)
                {
                    var path = Path.Combine(config.SclSourceDirectory, fileName);
                    Console.WriteLine($"  -> {fileName}");
                    SourceImporter.ImportAndGenerate(plcSoftware, path);
                }

                Console.WriteLine("Generating DB_Tanks from config and importing...");
                var tempDir = Path.Combine(Path.GetTempPath(), "TiaOpennessBuilder");
                Directory.CreateDirectory(tempDir);
                var dbTanksSource = TankDbGenerator.GenerateDbTanksSource(config, tempDir);
                SourceImporter.ImportAndGenerate(plcSoftware, dbTanksSource);
            }
            else
            {
                Console.WriteLine("Skipping SCL/DB_Tanks import (ImportPlcSources = false) — assuming these were already imported by hand.");
            }

            if (config.BuildTagTables)
            {
                Console.WriteLine("Building PLC tag tables...");
                PlcTagTableBuilder.BuildTankTags(plcSoftware, config);
            }
            else
            {
                Console.WriteLine("Skipping PLC tag tables (BuildTagTables = false).");
            }

            if (config.BuildHmiScreens && !string.IsNullOrWhiteSpace(config.HmiDeviceName))
            {
                Console.WriteLine("Building HMI screens (best-effort, see HmiScreenBuilder for limitations)...");
                var hmiTarget = HmiScreenBuilder.GetOrCreateHmiSoftware(session.Project, config.HmiOrderNumber, config.HmiDeviceName);
                HmiScreenBuilder.EnsureTankScreens(hmiTarget, config);
            }

            Console.WriteLine("Saving project...");
            session.Save();

            Console.WriteLine("Done.");
            return 0;
        }
    }
}
