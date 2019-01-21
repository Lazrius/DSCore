using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace DSCore.Ini
{
    public sealed class Ship
    {
        [BsonId]
        public string Nickname { get; set; }
        public uint Name { get; set; }
        public uint Infocard { get; set; }
        public int CargoSize { get; set; }
        public int Nanobots { get; set; }
        public int ShieldBats { get; set; }
        public float NudgeForce { get; set; }
        public float StrafeForce { get; set; }
        public float Hitpoints { get; set; }
        public float Price { get; set; }
        public Powerplant Powerplant { get; set; }
        public ShipClass ShipClass { get; set; }
    }

    public enum ShipClass
    {
        LightFighter = 0,
        HeavyFighter = 1,
        Freighter = 2,
        VeryHeavyFighter = 3,
        SuperHeavyFighter = 4,
        Bomber = 5,
        Transport = 6,
        Train = 7,
        HeavyTransport = 8,
        SuperTrain = 9,
        Liner = 10,
        Gunship = 11,
        Gunboat = 12,
        Destroyer = 13,
        Cruiser = 14,
        Battlecruiser = 15,
        Battleship = 16,
        Carrier = 17,
        Dreadnought = 18,
        RepairShip = 19
    }
}
