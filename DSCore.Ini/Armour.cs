using System;
using System.Collections.Generic;
using System.Text;

namespace DSCore.Ini
{
    public sealed class Armour
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public float CargoSpaceRequired { get; set; }
        public float DamageResistance { get; set; }
    }
}
