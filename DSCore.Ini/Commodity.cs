using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class Commodity : Good
    {
        public int DecayRate { get; set; }
        public float Hitpoints { get; set; }
        public float CargoSpaceRequired { get; set; }
    }

    public class Good
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public string Powerplant { get; set; }
        public bool Combinable { get; set; }
        public float Price { get; set; }
        public float BadBuyPrice { get; set; }
        public float GoodBuyPrice { get; set; }
        public float BadSellPrice { get; set; }
        public float GoodSellPrice { get; set; }
        public bool BaseSells { get; set; }
    }
}
