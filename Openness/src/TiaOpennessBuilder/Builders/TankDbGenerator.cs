using System.Globalization;
using System.IO;
using System.Text;
using TiaOpennessBuilder.Config;

namespace TiaOpennessBuilder.Builders
{
    /// <summary>
    /// Generates a DB_Tanks.scl source, pre-filled with tank names and per-alarm
    /// setpoints from the JSON config, so DB_Tanks doesn't need to be configured
    /// by hand after import.
    /// </summary>
    public static class TankDbGenerator
    {
        public static string GenerateDbTanksSource(ProjectConfig config, string outputDirectory)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DATA_BLOCK \"DB_Tanks\"");
            sb.AppendLine("{ S7_Optimized_Access := 'TRUE' }");
            sb.AppendLine("VERSION : 0.1");
            sb.AppendLine("NON_RETAIN");
            sb.AppendLine();
            sb.AppendLine("VAR");
            sb.AppendLine($"    Tank : Array[1..{config.Tanks.Count}] of \"UDT_Tank\";");
            sb.AppendLine("END_VAR");
            sb.AppendLine();
            sb.AppendLine("BEGIN");

            foreach (var tank in config.Tanks)
            {
                sb.AppendLine($"    Tank[{tank.Index}].TankName := '{EscapeString(tank.Name)}';");

                foreach (var alarm in tank.Alarms)
                {
                    var path = $"Tank[{tank.Index}].Alarms[{alarm.ChannelIndex}]";
                    sb.AppendLine($"    {path}.Enabled := TRUE;");
                    sb.AppendLine($"    {path}.IsHighAlarm := {(alarm.IsHighAlarm ? "TRUE" : "FALSE")};");
                    sb.AppendLine($"    {path}.Setpoint := {Fmt(alarm.Setpoint)};");
                    sb.AppendLine($"    {path}.Hysteresis := {Fmt(alarm.Hysteresis)};");
                    sb.AppendLine($"    {path}.DelayTime := {alarm.DelayTime};");
                }
            }

            sb.AppendLine("END_DATA_BLOCK");

            var outputPath = Path.Combine(outputDirectory, "DB_Tanks.scl");
            File.WriteAllText(outputPath, sb.ToString());
            return outputPath;
        }

        private static string Fmt(double value) => value.ToString(CultureInfo.InvariantCulture);

        private static string EscapeString(string value) => value?.Replace("'", "''") ?? string.Empty;
    }
}
