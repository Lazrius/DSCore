namespace DSCore.Ini
{
    public sealed class Shield
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public float Hitpoints { get; set; }
        public ShieldType ShieldType { get; set; }
        public int RegenRate { get; set; }
        public int PowerDraw { get; set; }
        public int OfflineTime { get; set; }
        public int Capacity { get; set; }
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
