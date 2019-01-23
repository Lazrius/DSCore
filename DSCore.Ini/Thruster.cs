using LiteDB;

namespace DSCore.Ini
{
    public sealed class Thruster : Good
    {
        public float MaxForce { get; set; }
        public float PowerUsage { get; set; }
        public float Hitpoints { get; set; }
    }
}
