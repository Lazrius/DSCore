using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class Armour : Good
    {
        public float CargoSpaceRequired { get; set; }
        public float DamageResistance { get; set; }
    }
}
