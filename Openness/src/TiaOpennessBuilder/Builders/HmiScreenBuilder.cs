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
            // Confirmed on Siemens.Engineering.HmiUnified.HmiSoftware (Unified
            // Comfort Panels): Screens sits directly on the HMI software, no
            // ScreenFolder wrapper like some Comfort/Advanced APIs use.
            dynamic screens = hmiTarget.Screens;
            DescribeMembers("Screens composition", (object)screens);

            dynamic firstScreen = null;
            foreach (dynamic s in screens)
            {
                firstScreen = s;
                break;
            }

            if (firstScreen != null)
            {
                DescribeMembers("An existing Screen instance", (object)firstScreen);
            }

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

        private static void DescribeMembers(string label, object instance)
        {
            var type = instance.GetType();
            Console.WriteLine($"{label}: {type.FullName}");
            foreach (var m in type.GetMethods())
            {
                if (m.DeclaringType == typeof(object) || m.IsSpecialName)
                {
                    continue;
                }

                var ps = string.Join(", ", Array.ConvertAll(m.GetParameters(), p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"    {m.ReturnType.Name} {m.Name}({ps})");
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
