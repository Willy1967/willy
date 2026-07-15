using System.IO;
using Siemens.Engineering.SW;

namespace TiaOpennessBuilder.Builders
{
    /// <summary>
    /// Imports an SCL file as an "external source" and runs the equivalent of
    /// TIA Portal's "Generate blocks from source" so the UDTs/FBs/DBs it
    /// declares are created (or updated) in the PLC's program blocks.
    ///
    /// The exact external-source class names differ across Openness/TIA
    /// versions, so this deliberately never spells them out — everything
    /// is resolved through `var` and left to the compiler/runtime.
    /// </summary>
    public static class SourceImporter
    {
        public static void ImportAndGenerate(PlcSoftware plcSoftware, string sourceFilePath)
        {
            // Keep the .scl extension: TIA Portal appears to use it to
            // recognize the source content type. A bare, extension-less
            // name made CreateFromFile fail with a generic
            // "method is not supported by the current version" error.
            var name = Path.GetFileName(sourceFilePath);
            var externalSources = plcSoftware.ExternalSourceGroup.ExternalSources;

            // Re-importing under the same name keeps the workflow idempotent
            // across repeated runs of this tool.
            foreach (var existing in externalSources)
            {
                if (existing.Name == name)
                {
                    existing.Delete();
                    break;
                }
            }

            var externalSource = externalSources.CreateFromFile(name, sourceFilePath);
            externalSource.GenerateBlocksFromSource();
        }
    }
}
