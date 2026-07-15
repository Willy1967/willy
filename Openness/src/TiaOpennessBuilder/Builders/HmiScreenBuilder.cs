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
    /// CONFIRMED LIMITATION (via reflection against a real Unified Comfort
    /// Panel project): Siemens.Engineering.HmiUnified.UI.Screens.HmiScreenComposition
    /// only exposes Create/Find/Contains/IndexOf — no Import or Export.
    /// HmiScreen itself only exposes Delete/ResizeScreen and generic
    /// Get/SetAttribute (screen-level attributes like size/background), with
    /// no way to add child graphic objects (Bar, IO Field, Rectangle, …) or
    /// import a screen's content from XML. So this class can only create or
    /// delete empty screens; all actual screen content has to be built by
    /// hand in the WinCC Unified screen editor.
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
