using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class CloakingDevice : Good
    {
        public int CargoRequirement { get; set; }
        public float PowerUsage { get; set; }
        public int CloakChargeTime { get; set; }
        public int DisruptionCooldown { get; set; }
        public int MaxiumCargoSize { get; set; }
        public Dictionary<string, int> FuelRequirements { get; set; }
    }

    public sealed class CloakDisrupter : Good
    {
        public int Range { get; set; }
        public int CooldownTime { get; set; }
        public Dictionary<string, int> AmmoRequirements { get; set; }
    }
}
