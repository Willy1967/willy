using System;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Hmi.Screen;
using TiaOpennessBuilder.Config;

namespace TiaOpennessBuilder.Builders
{
    /// <summary>
    /// Best-effort HMI automation.
    ///
    /// IMPORTANT LIMITATION: the public Openness API exposes far less of the
    /// HMI object model than the PLC side. Creating/renaming screens and
    /// screen folders is supported, but placing and dynamizing individual
    /// graphic objects (IO fields, the tank-level gauge, HH/H/L/LL markers,
    /// faceplate instances) is generally NOT exposed for WinCC
    /// Comfort/Advanced, and only partially available for WinCC Unified.
    ///
    /// This builder therefore only creates one empty screen per tank (if it
    /// doesn't already exist). Build the actual screen content once by hand
    /// (or as a library master copy / screen template) and either copy it
    /// manually per tank, or extend this class after confirming which
    /// Hmi.Screen APIs your installed Openness version actually exposes.
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
            ScreenComposition screens = hmiTarget.ScreenFolder.Screens;

            foreach (var tank in config.Tanks)
            {
                var screenName = $"Tank{tank.Index}_Overview";
                if (ScreenExists(screens, screenName))
                {
                    continue;
                }

                try
                {
                    screens.Create(screenName);
                    Console.WriteLine($"  Created empty HMI screen '{screenName}' — add the tank gauge/faceplate and tag bindings by hand.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Could not create HMI screen '{screenName}': {ex.Message}");
                }
            }
        }

        private static bool ScreenExists(ScreenComposition screens, string name)
        {
            foreach (Screen s in screens)
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
