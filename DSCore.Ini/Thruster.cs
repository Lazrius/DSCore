using LiteDB;

namespace DSCore.Ini
{
    public sealed class Thruster
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public float MaxForce { get; set; }
        public float PowerUsage { get; set; }
        public float Hitpoints { get; set; }
    }
}
