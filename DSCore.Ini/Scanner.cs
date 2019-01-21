using LiteDB;

namespace DSCore.Ini
{
    public sealed class Scanner
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public uint Range { get; set; }
        public uint CargoRange { get; set; }
    }
}
