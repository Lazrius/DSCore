using System;
using System.Collections.Generic;
using System.Text;

namespace DSCore.Ini
{
    public static class TypeFunctions
    {
        public static ShieldType GetShieldType(string value)
        {
            value = value.ToLower();
            if (value.Contains("graviton"))
                return ShieldType.Graviton;
            else if (value.Contains("molecular"))
                return ShieldType.Molecular;
            else if (value.Contains("positron"))
                return ShieldType.Positron;
            else if (value.Contains("nomad"))
                return ShieldType.Nomad;
            else if (value.Contains("drone"))
                return ShieldType.Drone;
            else return ShieldType.Unknown;
        }

        public static WeaponType GetWeaponType(string value)
        {
            value = value.ToLower();
            if (value.Contains("resisted"))
                return WeaponType.Resisted;
            else if (value.Contains("plasma"))
                return WeaponType.Plasma;
            else if (value.Contains("pulse"))
                return WeaponType.Pulse;
            else if (value.Contains("photon"))
                return WeaponType.Photon;
            else if (value.Contains("particle"))
                return WeaponType.Particle;
            else if (value.Contains("laser"))
                return WeaponType.Laser;
            else if (value.Contains("tachyon"))
                return WeaponType.Tachyon;
            else if (value.Contains("piercing"))
                return WeaponType.Piercing;
            else if (value.Contains("neutron"))
                return WeaponType.Neutron;
            else if (value.Contains("healing"))
                return WeaponType.Healing;
            return WeaponType.Neutral;
        }

        public static string EnumToString(Enum e)
        {
            return e.ToString();
        }
    }
}
