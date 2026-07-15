using System.Collections.Generic;

namespace TiaOpennessBuilder.Config
{
    public sealed class ProjectConfig
    {
        // Project
        public bool AttachToRunningInstance { get; set; }
        public bool CreateNewProject { get; set; }
        public string TiaProjectPath { get; set; }
        public string TiaProjectDirectory { get; set; }
        public string TiaProjectName { get; set; }
        public bool WithUserInterface { get; set; } = true;

        // PLC
        public string PlcOrderNumber { get; set; }
        public string PlcDeviceName { get; set; } = "PLC_1";

        // PLC tags
        public bool BuildTagTables { get; set; } = true;

        // HMI (optional)
        public bool BuildHmiScreens { get; set; }
        public string HmiOrderNumber { get; set; }
        public string HmiDeviceName { get; set; } = "HMI_1";

        // SCL sources
        public bool ImportPlcSources { get; set; } = true;
        public string SclSourceDirectory { get; set; }
        public List<string> SclImportOrder { get; set; } = new List<string>();

        // Tank data
        public List<TankConfig> Tanks { get; set; } = new List<TankConfig>();
    }
}
