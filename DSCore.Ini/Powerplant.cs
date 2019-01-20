using System;
using System.Collections.Generic;
using System.Text;

namespace DSCore.Ini
{
    public sealed class Powerplant
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public int Capacity { get; set; }
        public int ChargeRate { get; set; }
    }
}
