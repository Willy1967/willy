using System.Collections.Generic;

namespace TiaOpennessBuilder.Config
{
    public sealed class TankConfig
    {
        public int Index { get; set; }
        public string Name { get; set; }

        // Raw sounding -> centimeter scaling (mirrors FB_TankInterpolation inputs)
        public string SoundingRawAddress { get; set; }
        public double RawMin { get; set; } = 4.0;
        public double RawMax { get; set; } = 20.0;
        public double CmAtRawMin { get; set; } = 0.0;
        public double CmAtRawMax { get; set; } = 250.0;

        public List<AlarmSetpointConfig> Alarms { get; set; } = new List<AlarmSetpointConfig>();
    }

    public sealed class AlarmSetpointConfig
    {
        // 1=HH 2=H 3=L 4=LL 5=SensorFailure 6=Spare1 7=Spare2
        public int ChannelIndex { get; set; }
        public double Setpoint { get; set; }
        public double Hysteresis { get; set; } = 0.5;
        public string DelayTime { get; set; } = "T#3S";
        public bool IsHighAlarm { get; set; } = true;
    }
}
