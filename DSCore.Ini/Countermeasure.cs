using System;
using System.Collections.Generic;
using System.Text;

namespace DSCore.Ini
{
    public sealed class CountermeasureDropper
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public float Hitpoints { get; set; }
        public float AutoDeploymentRange { get; set; }
        public string ArchtypeName { get; set; }
        public Countermeasure Ammo { get; set; }
    }

    public sealed class Countermeasure
    {
        public string Nickname { get; set; }
        public int AmmoLimit { get; set; }
        public int EffectiveRange { get; set; }
    }
}
