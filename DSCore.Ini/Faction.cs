using System;
using System.Collections.Generic;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class Faction
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint ShortName { get; set; }
        public uint Infocard { get; set; }
        public Dictionary<string, float> FeelingsTowards { get; set; }
    }
}
