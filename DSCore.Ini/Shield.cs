using LiteDB;

namespace DSCore.Ini
{
    public sealed class Shield : Good
    {
        public float Hitpoints { get; set; }
        public ShieldType ShieldType { get; set; }
        public float RegenRate { get; set; }
        public int PowerDraw { get; set; }
        public int OfflineTime { get; set; }
        public float Capacity { get; set; }
    }

    public enum ShieldType
    {
        Positron,
        Graviton,
        Molecular,
        Drone,
        Nomad,
        Unknown
    }
}
