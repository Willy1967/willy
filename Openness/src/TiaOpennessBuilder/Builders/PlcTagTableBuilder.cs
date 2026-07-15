using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Tags;
using TiaOpennessBuilder.Config;

namespace TiaOpennessBuilder.Builders
{
    public static class PlcTagTableBuilder
    {
        private const string TableName = "Tank_IO";

        public static void BuildTankTags(PlcSoftware plcSoftware, ProjectConfig config)
        {
            var tables = plcSoftware.TagTableGroup.TagTables;
            PlcTagTable table = null;
            foreach (PlcTagTable t in tables)
            {
                if (t.Name == TableName)
                {
                    table = t;
                    break;
                }
            }

            table ??= tables.Create(TableName);

            foreach (var tank in config.Tanks)
            {
                if (string.IsNullOrWhiteSpace(tank.SoundingRawAddress))
                {
                    continue;
                }

                var tagName = $"{tank.Name}_Sounding_Raw";
                if (TagExists(table, tagName))
                {
                    continue;
                }

                table.Tags.Create(tagName, "Real", tank.SoundingRawAddress);
            }
        }

        private static bool TagExists(PlcTagTable table, string name)
        {
            foreach (PlcTag tag in table.Tags)
            {
                if (tag.Name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
