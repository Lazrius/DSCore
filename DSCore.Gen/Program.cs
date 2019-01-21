using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LibreLancer.Data;
using LiteDB;
using DSCore.Ini;
using MadMilkman.Ini;
using System.Reflection;

namespace DSCore.Gen
{
    class Program
    {
        static void Main(string[] args)
        {
            string root = Directory.GetCurrentDirectory();
            if (!File.Exists(root + @"\EXE\Freelancer.exe"))
            {
                Console.WriteLine("Freelancer.exe not found. Assuming not in the correct directory. Please relocate to the root of your Freelancer install.");
                Console.ReadLine();
                return;
            }

            List<Armour> armours = new List<Armour>(); //
            List<CloakingDevice> cloaks = new List<CloakingDevice>(); 
            List<CloakDisrupter> disrupters = new List<CloakDisrupter>();
            List<Commodity> commodities = new List<Commodity>(); //
            List<CountermeasureDropper> droppers = new List<CountermeasureDropper>(); //
            List<Faction> factions = new List<Faction>(); //
            List<Good> goods = new List<Good>(); //
            List<Powerplant> powerplants = new List<Powerplant>(); //
            List<Scanner> scanners = new List<Scanner>(); //
            List<Shield> shields = new List<Shield>(); // 
            List<Weapon> weapons = new List<Weapon>();
            List<Thruster> thrusters = new List<Thruster>(); //
            Infocard infocard = new Infocard();

            infocard.Infocards = SetupInfocards(root);

            string data = root + @"\DATA";
            string equip = data + @"\EQUIPMENT";
            IniOptions iniOptions = new IniOptions { KeyDuplicate = IniDuplication.Allowed, SectionDuplicate = IniDuplication.Allowed };

            // Commodities and Armour
            IniFile ini = new IniFile(iniOptions);
            ini.Load(equip + @"\select_equip.ini");
            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "armor":
                        Armour armour = new Armour();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    armour.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    armour.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    armour.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "volume":
                                    armour.CargoSpaceRequired = Convert.ToSingle(ii.Value);
                                    break;
                                case "hit_pts_scale":
                                    armour.DamageResistance = Convert.ToSingle(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(armour))
                            armours.Add(armour);
                        break;

                    case "commodity":
                        Commodity commodity = new Commodity();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    commodity.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    commodity.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    commodity.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "volume":
                                    commodity.CargoSpaceRequired = Convert.ToSingle(ii.Value);
                                    break;
                                case "hit_pts":
                                    commodity.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                                case "decay_per_second":
                                    commodity.DecayRate = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(commodity))
                            commodities.Add(commodity);
                        break;
                }
            }

            #region Goods
            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\goods.ini");
            foreach (IniSection i in ini.Sections)
            {
                Good good = new Good();
                foreach (IniKey ii in i.Keys)
                {
                    if (ii.Name == "category" && ii.Value.Contains("ship"))
                        break;

                    switch (ii.Name.ToLower())
                    {
                        case "nickname":
                            good.Nickname = ii.Value;
                            break;

                        case "equipment":
                            good.Equipment = ii.Value;
                            break;

                        case "combinable":
                            good.Combinable = Boolean.Parse(ii.Value);
                            break;

                        case "price":
                            good.Price = Convert.ToSingle(ii.Value);
                            break;

                        case "good_sell_price":
                            good.GoodSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_sell_price":
                            good.BadSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "good_buy_price":
                            good.GoodBuyPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_buy_price":
                            good.BadBuyPrice = Convert.ToSingle(ii.Value);
                            break;
                    }

                    if (!IsAnyNullOrEmpty(good))
                        goods.Add(good);
                }
            }

            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\engine_good.ini");
            foreach (IniSection i in ini.Sections)
            {
                Good good = new Good();
                foreach (IniKey ii in i.Keys)
                {
                    if (ii.Name == "category" && ii.Value.Contains("ship"))
                        break;

                    switch (ii.Name.ToLower())
                    {
                        case "nickname":
                            good.Nickname = ii.Value;
                            break;

                        case "equipment":
                            good.Equipment = ii.Value;
                            break;

                        case "combinable":
                            good.Combinable = Boolean.Parse(ii.Value);
                            break;

                        case "price":
                            good.Price = Convert.ToSingle(ii.Value);
                            break;

                        case "good_sell_price":
                            good.GoodSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_sell_price":
                            good.BadSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "good_buy_price":
                            good.GoodBuyPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_buy_price":
                            good.BadBuyPrice = Convert.ToSingle(ii.Value);
                            break;
                    }

                    if (!IsAnyNullOrEmpty(good))
                        goods.Add(good);
                }
            }

            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\weapon_good.ini");
            foreach (IniSection i in ini.Sections)
            {
                Good good = new Good();
                foreach (IniKey ii in i.Keys)
                {
                    if (ii.Name == "category" && ii.Value.Contains("ship"))
                        break;

                    switch (ii.Name.ToLower())
                    {
                        case "nickname":
                            good.Nickname = ii.Value;
                            break;

                        case "equipment":
                            good.Equipment = ii.Value;
                            break;

                        case "combinable":
                            good.Combinable = Boolean.Parse(ii.Value);
                            break;

                        case "price":
                            good.Price = Convert.ToSingle(ii.Value);
                            break;

                        case "good_sell_price":
                            good.GoodSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_sell_price":
                            good.BadSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "good_buy_price":
                            good.GoodBuyPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_buy_price":
                            good.BadBuyPrice = Convert.ToSingle(ii.Value);
                            break;
                    }

                    if (!IsAnyNullOrEmpty(good))
                        goods.Add(good);
                }
            }

            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\misc_good.ini");
            foreach (IniSection i in ini.Sections)
            {
                Good good = new Good();
                foreach (IniKey ii in i.Keys)
                {
                    if (ii.Name == "category" && ii.Value.Contains("ship"))
                        break;

                    switch (ii.Name.ToLower())
                    {
                        case "nickname":
                            good.Nickname = ii.Value;
                            break;

                        case "equipment":
                            good.Equipment = ii.Value;
                            break;

                        case "combinable":
                            good.Combinable = Boolean.Parse(ii.Value);
                            break;

                        case "price":
                            good.Price = Convert.ToSingle(ii.Value);
                            break;

                        case "good_sell_price":
                            good.GoodSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_sell_price":
                            good.BadSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "good_buy_price":
                            good.GoodBuyPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_buy_price":
                            good.BadBuyPrice = Convert.ToSingle(ii.Value);
                            break;
                    }

                    if (!IsAnyNullOrEmpty(good))
                        goods.Add(good);
                }
            }

            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\st_good.ini");
            foreach (IniSection i in ini.Sections)
            {
                Good good = new Good();
                foreach (IniKey ii in i.Keys)
                {
                    if (ii.Name == "category" && ii.Value.Contains("ship"))
                        break;

                    switch (ii.Name.ToLower())
                    {
                        case "nickname":
                            good.Nickname = ii.Value;
                            break;

                        case "equipment":
                            good.Equipment = ii.Value;
                            break;

                        case "combinable":
                            good.Combinable = Boolean.Parse(ii.Value);
                            break;

                        case "price":
                            good.Price = Convert.ToSingle(ii.Value);
                            break;

                        case "good_sell_price":
                            good.GoodSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_sell_price":
                            good.BadSellPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "good_buy_price":
                            good.GoodBuyPrice = Convert.ToSingle(ii.Value);
                            break;

                        case "bad_buy_price":
                            good.BadBuyPrice = Convert.ToSingle(ii.Value);
                            break;
                    }

                    if (!IsAnyNullOrEmpty(good))
                        goods.Add(good);
                }
            }

            #endregion

            // Thrusters and Shields
            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\st_equip.ini");
            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "thruster":
                        Thruster thruster = new Thruster();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    thruster.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    thruster.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    thruster.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "max_force":
                                    thruster.MaxForce = Convert.ToSingle(ii.Value);
                                    break;
                                case "power_usage":
                                    thruster.PowerUsage = Convert.ToSingle(ii.Value);
                                    break;
                                case "hit_pts":
                                    thruster.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(thruster))
                            thrusters.Add(thruster);
                        break;

                    case "shieldgenerator":
                        Shield shield = new Shield();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    shield.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    shield.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    shield.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "shield_type":
                                    shield.ShieldType = GetShieldType(ii.Value);
                                    break;
                                case "hit_pts":
                                    shield.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                                case "offline_rebuild_time":
                                    shield.OfflineTime = Convert.ToInt32(ii.Value);
                                    break;
                                case "constant_power_draw":
                                    shield.PowerDraw = Convert.ToInt32(ii.Value);
                                    break;
                                case "max_capacity":
                                    shield.Capacity = Convert.ToInt32(ii.Value);
                                    break;
                                case "regeneration_rate":
                                    shield.RegenRate = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(shield))
                            shields.Add(shield);
                        break;
                }
            }

            // Scanners and Powerplants. and CMs (this file also does IDs, but we're ignoging them for now)
            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\misc_equip.ini");
            var tempcmlist = new List<Countermeasure>();
            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "power":
                        Powerplant power = new Powerplant();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    power.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    power.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    power.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "charge_rate":
                                    power.ChargeRate = Convert.ToInt32(ii.Value);
                                    break;
                                case "capacity":
                                    power.Capacity = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(power))
                            powerplants.Add(power);
                        break;

                    case "scanner":
                        Scanner scanner = new Scanner();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    scanner.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    scanner.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    scanner.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "range":
                                    scanner.Range = Convert.ToUInt32(ii.Value);
                                    break;
                                case "cargo_scan_range":
                                    scanner.CargoRange = Convert.ToUInt32(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(scanner))
                            scanners.Add(scanner);
                        break;

                    case "countermeasuredropper":
                        CountermeasureDropper dropper = new CountermeasureDropper();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name.ToLower())
                            {
                                case "nickname":
                                    dropper.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    dropper.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    dropper.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ai_range":
                                    dropper.AutoDeploymentRange = Convert.ToInt32(ii.Value);
                                    break;
                                case "hit_pts":
                                    dropper.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                                case "projectile_archtype":
                                    dropper.ArchtypeName = ii.Value;
                                    break;
                            }
                        }
                        if (!string.IsNullOrEmpty(dropper.ArchtypeName))
                            droppers.Add(dropper);
                        break;
                    case "countermeasure":
                        Countermeasure cm = new Countermeasure();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name.ToLower())
                            {
                                case "nickname":
                                    cm.Nickname = ii.Value;
                                    break;
                                case "range":
                                    cm.EffectiveRange = Convert.ToInt32(ii.Value);
                                    break;
                                case "ammo_limit":
                                    cm.AmmoLimit = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }
                        if (!IsAnyNullOrEmpty(cm))
                            tempcmlist.Add(cm);
                        break;
                }
            }

            // We need to iterate over the list and assign the subclasses
            foreach (Countermeasure i in tempcmlist)
                droppers = droppers.Where(x => x.ArchtypeName == i.Nickname).Select(x => { x.Ammo = i; return x; }).ToList();
            tempcmlist = null;

            // Factions
            ini = new IniFile(iniOptions);
            ini.Load(data + @"\initialworld.ini");
            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "group":
                        Faction faction = new Faction();
                        faction.FeelingsTowards = new Dictionary<string, float>();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    faction.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    faction.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    faction.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_short_name":
                                    faction.ShortName = Convert.ToUInt32(ii.Value);
                                    break;
                                case "rep":
                                    var pair = ii.Value.Split(',');
                                    faction.FeelingsTowards[pair[1]] = Convert.ToSingle(pair[0]);
                                    break;
                            }
                        }

                        if (!IsAnyNullOrEmpty(faction))
                            factions.Add(faction);
                        break;
                }
            }


            /*using (var db = new LiteDatabase(root + @"FLData.db"))
            {

            }*/
        }



        static Dictionary<int, string> SetupInfocards(string root)
        {
            VFS.Init(root);
            InfocardManager info = new InfocardManager((new FreelancerIni()).Resources);
            info.ExportStrings(root + @"\Infonames.json");
            info.ExportInfocards(root + @"\Infocards.json");

            var infocards = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(root + @"\Infocards.json"));
            var infonames = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText(root + @"\Infonames.json"));
            return infocards.Concat(infonames).GroupBy(d => d.Key).OrderBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
        }

        static bool IsAnyNullOrEmpty(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return false;

            return obj.GetType().GetProperties().Any(x => IsNullOrEmpty(x.GetValue(obj)));
        }

        static bool IsNullOrEmpty(object value)
        {
            if (Object.ReferenceEquals(value, null))
                return false;

            var type = value.GetType();
            return type.IsValueType && Object.Equals(value, Activator.CreateInstance(type));
        }

        static ShieldType GetShieldType(string value)
        {
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
    }
}
