namespace DSCore.Ini
{
    public sealed class Faction
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public string ShortName { get; set; }
        public uint Infocard { get; set; }
        public float RepChangeObjDest { get; set; }
        public float RepChangeMisnSuccess { get; set; }
        public float RepChangeMisnFailure { get; set; }
        public float RepChangeMisnAbort { get; set; }
    }
}
