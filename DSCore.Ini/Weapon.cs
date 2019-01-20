using System;
using System.Collections.Generic;
using System.Text;

namespace DSCore.Ini
{
    public sealed class Weapon
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public float Hitpoints { get; set; }
        public float PowerUsage { get; set; }
        public float RefireDelay { get; set; }
        public float TurnRate { get; set; }
        public WeaponType WeaponType { get; set; }
        public Munition Munition { get; set; }
    }

    public sealed class Munition
    {
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public bool IsSeeking { get; set; }
        public float TimeToLock { get; set; }
        public float SeekerRange { get; set; }
        public float Lifetime { get; set; }
        public float HullDamage { get; set; }
        public float ShieldDamage { get; set; }
        public int AmmoLimit { get; set; }
        public bool RequiresAmmo { get; set; }
        public float Hitpoints { get; set; }
        public bool HasForcedOrientation { get; set; }
        public Explosion Explosion { get; set; }
    }

    public sealed class Explosion
    {
        public string Nickname { get; set; }
        public float Radius { get; set; }
        public float Strength { get; set; }
        public float HullDamage { get; set; }
        public float EnergyDamage { get; set; }
    }

    public enum WeaponType
    {
        Laser,
        Plasma,
        Tachyon,
        Neutron,
        Particle,
        Photon,
        Pulse,
        Healing,
        Neutral,
        Piercing,
        Resisted
    }
}
