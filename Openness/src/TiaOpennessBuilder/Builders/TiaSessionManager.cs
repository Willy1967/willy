using System;
using System.IO;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;

namespace TiaOpennessBuilder.Builders
{
    /// <summary>
    /// Owns the TiaPortal process and the open Project. Dispose closes the
    /// project and shuts down the Openness session.
    /// </summary>
    public sealed class TiaSessionManager : IDisposable
    {
        public TiaPortal Tia { get; }
        public Project Project { get; private set; }

        public TiaSessionManager(bool withUserInterface = true)
        {
            Tia = new TiaPortal(withUserInterface ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface);
        }

        public void OpenOrCreateProject(Config.ProjectConfig config)
        {
            if (config.CreateNewProject)
            {
                var dir = new DirectoryInfo(config.TiaProjectDirectory);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                Project = Tia.Projects.Create(dir, config.TiaProjectName);
            }
            else
            {
                Project = Tia.Projects.Open(new FileInfo(config.TiaProjectPath));
            }
        }

        public PlcSoftware GetOrCreatePlcSoftware(string orderNumber, string deviceName)
        {
            Console.WriteLine("Devices found in project:");
            foreach (Device d in Project.Devices)
            {
                Console.WriteLine($"  - '{d.Name}'");
            }

            var device = FindDevice(deviceName) ?? Project.Devices.CreateWithItem(orderNumber, deviceName, deviceName);

            foreach (DeviceItem item in device.DeviceItems)
            {
                var container = item.GetService<SoftwareContainer>();
                if (container?.Software is PlcSoftware plcSoftware)
                {
                    return plcSoftware;
                }
            }

            throw new InvalidOperationException(
                $"No PLC software found on device '{deviceName}'. Check that PlcOrderNumber ('{orderNumber}') " +
                "matches a valid CPU catalog string, e.g. \"OrderNumber:6ES7 515-2AM02-0AB0/V3.0\".");
        }

        public Device FindDevice(string name)
        {
            foreach (Device d in Project.Devices)
            {
                if (string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return d;
                }
            }

            return null;
        }

        public void Save() => Project.Save();

        public void Dispose()
        {
            Project?.Close();
            Tia?.Dispose();
        }
    }
}
