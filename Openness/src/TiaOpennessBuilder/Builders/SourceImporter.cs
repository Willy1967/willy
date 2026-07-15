using System.IO;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.ExternalSources;

namespace TiaOpennessBuilder.Builders
{
    /// <summary>
    /// Imports an SCL file as an "external source" and runs the equivalent of
    /// TIA Portal's "Generate blocks from source" so the UDTs/FBs/DBs it
    /// declares are created (or updated) in the PLC's program blocks.
    /// </summary>
    public static class SourceImporter
    {
        public static void ImportAndGenerate(PlcSoftware plcSoftware, string sourceFilePath)
        {
            var name = Path.GetFileNameWithoutExtension(sourceFilePath);
            var group = plcSoftware.ExternalSourceGroup;

            // Re-importing under the same name keeps the workflow idempotent
            // across repeated runs of this tool.
            FindExisting(group, name)?.Delete();

            var externalSource = group.ExternalSources.CreateFromFile(name, sourceFilePath);
            externalSource.GenerateBlocksFromSource();
        }

        private static ExternalSource FindExisting(ExternalSourceGroup group, string name)
        {
            foreach (ExternalSource source in group.ExternalSources)
            {
                if (source.Name == name)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
