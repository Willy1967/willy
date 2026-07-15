using System;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using TiaOpennessBuilder.Config;

namespace TiaOpennessBuilder.Builders
{
    /// <summary>
    /// Best-effort HMI automation.
    ///
    /// IMPORTANT LIMITATION: the public Openness API exposes far less of the
    /// HMI object model than the PLC side, and the exact screen-object model
    /// (property/method names for the screen collection) differs across TIA
    /// versions and Comfort vs. Unified panels. This class deliberately uses
    /// `dynamic` for the screen collection instead of a hard-coded type/method
    /// name, so a wrong guess fails at runtime (caught and reported per tank)
    /// instead of blocking the whole build. Confirm the real members via
    /// IntelliSense against your installed Siemens.Engineering.Hmi.dll before
    /// relying on this, or build the screens by hand as a starting point.
    /// </summary>
    public static class HmiScreenBuilder
    {
        public static object GetOrCreateHmiSoftware(Project project, string orderNumber, string deviceName)
        {
            Device device = null;
            foreach (Device d in project.Devices)
            {
                if (string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    device = d;
                    break;
                }
            }

            device ??= project.Devices.CreateWithItem(orderNumber, deviceName, deviceName);

            foreach (DeviceItem item in device.DeviceItems)
            {
                var container = item.GetService<SoftwareContainer>();
                if (container?.Software != null)
                {
                    return container.Software;
                }
            }

            throw new InvalidOperationException(
                $"No HMI software found on device '{deviceName}'. Check that HmiOrderNumber ('{orderNumber}') " +
                "matches a valid HMI catalog string for your panel (e.g. TP1200 Comfort or a Unified panel).");
        }

        public static void EnsureTankScreens(dynamic hmiTarget, ProjectConfig config)
        {
            Console.WriteLine($"HMI software runtime type: {((object)hmiTarget).GetType().FullName}");
            Console.WriteLine("Public properties on that type:");
            foreach (var prop in ((object)hmiTarget).GetType().GetProperties())
            {
                Console.WriteLine($"  - {prop.PropertyType.Name} {prop.Name}");
            }

            dynamic screens = hmiTarget.ScreenFolder.Screens;

            foreach (var tank in config.Tanks)
            {
                var screenName = $"Tank{tank.Index}_Overview";

                try
                {
                    if (ScreenExists(screens, screenName))
                    {
                        continue;
                    }

                    screens.Create(screenName);
                    Console.WriteLine($"  Created empty HMI screen '{screenName}' — add the tank gauge/faceplate and tag bindings by hand.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Could not create HMI screen '{screenName}': {ex.Message}");
                }
            }
        }

        private static bool ScreenExists(dynamic screens, string name)
        {
            foreach (dynamic s in screens)
            {
                if (s.Name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
