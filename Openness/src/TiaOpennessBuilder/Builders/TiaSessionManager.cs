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
    /// project and shuts down the Openness session — unless this manager
    /// attached to an already-running TIA Portal instance, in which case
    /// Dispose leaves that instance and its open project alone.
    /// </summary>
    public sealed class TiaSessionManager : IDisposable
    {
        public TiaPortal Tia { get; private set; }
        public Project Project { get; private set; }

        private readonly bool _attached;

        public TiaSessionManager(bool withUserInterface = true)
        {
            Tia = new TiaPortal(withUserInterface ? TiaPortalMode.WithUserInterface : TiaPortalMode.WithoutUserInterface);
            _attached = false;
        }

        private TiaSessionManager(TiaPortal tia)
        {
            Tia = tia;
            _attached = true;
        }

        /// <summary>
        /// Attaches to an already-running TIA Portal process instead of
        /// starting a new one, so you never need to close the project in
        /// the GUI before running this tool. Requires TIA Portal to already
        /// be open (with your project loaded).
        /// </summary>
        public static TiaSessionManager AttachToRunning()
        {
            TiaPortalProcess selected = null;
            var count = 0;
            foreach (TiaPortalProcess p in TiaPortal.GetProcesses())
            {
                count++;
                selected ??= p;
            }

            if (selected == null)
            {
                throw new InvalidOperationException(
                    "No running TIA Portal process found to attach to. Open TIA Portal with your " +
                    "project loaded first, or set \"AttachToRunningInstance\": false to let this tool open it itself.");
            }

            if (count > 1)
            {
                Console.WriteLine($"Note: {count} running TIA Portal processes found; attaching to the first one enumerated.");
            }

            return new TiaSessionManager(selected.Attach());
        }

        public void OpenOrCreateProject(Config.ProjectConfig config)
        {
            if (_attached)
            {
                Project found = null;
                foreach (Project p in Tia.Projects)
                {
                    if (string.IsNullOrWhiteSpace(config.TiaProjectName) ||
                        string.Equals(p.Name, config.TiaProjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = p;
                        break;
                    }
                }

                Project = found ?? throw new InvalidOperationException(
                    "Attached to a running TIA Portal instance, but no matching project is open in it. " +
                    "Make sure the project is open in that TIA Portal window.");
                return;
            }

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

                // The station (Device) name and the CPU/HMI module's own name
                // (DeviceItem) can differ, e.g. a station called
                // "S7-1500/ET200MP station_1" containing a CPU item named
                // "PLC_1". Match on either.
                foreach (DeviceItem item in d.DeviceItems)
                {
                    if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return d;
                    }
                }
            }

            return null;
        }

        public void Save() => Project.Save();

        public void Dispose()
        {
            // Never close/dispose an attached session — that's the user's
            // own TIA Portal window, not something this tool owns.
            if (_attached)
            {
                return;
            }

            Project?.Close();
            Tia?.Dispose();
        }
    }
}
