using System;
using System.Collections.Generic;
using System.Text;

namespace DSCore.Ini
{
    public sealed class CloakingDevice
    {
        public string Nickname { get; set; }
        public uint Infocard { get; set; }
        public uint Name { get; set; }
        public int CargoRequirement { get; set; }
        public float PowerUsage { get; set; }
        public int TimeToCloak { get; set; }
        public int DisruptionCooldown { get; set; }
        public int MaxiumCargoSize { get; set; }
        public int CloakTime { get; set; }
        public bool DropsShields { get; set; }
        public Dictionary<string, int> FuelRequirements { get; set; }
    }

    public sealed class CloakDisrupter
    {
        public string Nickname { get; set; }
        public uint Infocard { get; set; }
        public uint Name { get; set; }
        public int Range { get; set; }
        public int CooldownTime { get; set; }
        public Dictionary<string, int> AmmoRequirements { get; set; }
    }
}
