using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LibreLancer.Data;
using LiteDB;
using DSCore.Ini;
using MadMilkman.Ini;
using System.Text.RegularExpressions;

namespace DSCore.Gen
{
    class Program
    {
        static void Main(string[] args)
        {
            string root = Directory.GetCurrentDirectory();
            string output = root + @"\Output";
            
            if (!File.Exists(root + @"\EXE\Freelancer.exe"))
            {
                Console.WriteLine("Freelancer.exe not found. Assuming not in the correct directory. Please relocate to the root of your Freelancer install.");
                Console.ReadLine();
                return;
            }

            List<Armour> armours = new List<Armour>();
            List<CloakingDevice> cloaks = new List<CloakingDevice>(); 
            List<CloakDisrupter> disrupters = new List<CloakDisrupter>();
            List<Commodity> commodities = new List<Commodity>();
            List<CountermeasureDropper> droppers = new List<CountermeasureDropper>();
            List<Faction> factions = new List<Faction>();
            List<Good> goods = new List<Good>();
            List<Powerplant> powerplants = new List<Powerplant>();
            List<Scanner> scanners = new List<Scanner>();
            List<Shield> shields = new List<Shield>();
            List<Ship> ships = new List<Ship>();
            List<Weapon> weapons = new List<Weapon>();
            List<Thruster> thrusters = new List<Thruster>();
            List<Infocard> infocards = SetupInfocards(root);

            string data = root + @"\DATA";
            string equip = data + @"\EQUIPMENT";
            IniOptions iniOptions = new IniOptions
            {
                KeyDuplicate = IniDuplication.Allowed,
                SectionDuplicate = IniDuplication.Allowed
            };

            // Markets, Bases, Systems
            SetupBasesAndSystems(iniOptions, data, out var marketCommodities, out var marketEquipment, out var marketShips, out var bases, out var systems);

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
                                    armour.Nickname = ii.Value.ToLower();
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

                        armours.Add(armour);
                        break;

                    case "commodity":
                        Commodity commodity = new Commodity();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    commodity.Nickname = ii.Value.ToLower();
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

                        commodities.Add(commodity);
                        break;
                }
            }

            Console.WriteLine("Processed Commodities and Armour.");

            // Ships
            ini = new IniFile(iniOptions);
            ini.Load(data + @"\SHIPS\shiparch.ini");
            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "ship":
                        Ship ship = new Ship();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name.ToLower())
                            {
                                case "nickname":
                                    ship.Nickname = ii.Value.ToLower();
                                    break;
                                case "ids_name":
                                    ship.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info1":
                                    ship.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "hold_size":
                                    ship.CargoSize = Convert.ToInt32(ii.Value);
                                    break;
                                case "strafe_force":
                                    ship.StrafeForce = Convert.ToSingle(ii.Value);
                                    break;
                                case "nudge_force":
                                    ship.NudgeForce = Convert.ToSingle(ii.Value);
                                    break;
                                case "hit_pts":
                                    ship.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                                case "ship_class":
                                    ship.ShipClass = (ShipClass)Convert.ToInt32(ii.Value);
                                    break;
                                case "nanobot_limit":
                                    ship.Nanobots = Convert.ToInt32(ii.Value);
                                    break;
                                case "shield_battery_limit":
                                    ship.ShieldBats = Convert.ToInt32(ii.Value);
                                    break;
                                case "da_archetype":
                                    ship.CmpFile = ii.Value;
                                    break;
                                case "material_library":
                                    if (string.IsNullOrEmpty(ship.MatFile))
                                        ship.MatFile = ii.Value;
                                    break;
                            }
                        }

                        ships.Add(ship);
                        break;
                }
            }

            Console.WriteLine("Copying Ship CMP/MAT files.");
            foreach (Ship ship in ships)
            {
                if (string.IsNullOrEmpty(ship.CmpFile) || string.IsNullOrEmpty(ship.MatFile))
                    continue;
                Directory.CreateDirectory(output + @"\DATA\" + Path.GetDirectoryName(ship.CmpFile));
                Directory.CreateDirectory(output + @"\DATA\" + Path.GetDirectoryName(ship.MatFile));
                File.Copy(data + @"\" + ship.CmpFile, output + @"\DATA\" + ship.CmpFile, true);
                File.Copy(data + @"\" + ship.MatFile, output + @"\DATA\" + ship.MatFile, true);
            }

            Console.WriteLine("Processed Ships.");

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
                                    power.Nickname = ii.Value.ToLower();
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
                        powerplants.Add(power);
                        break;

                    case "scanner":
                        Scanner scanner = new Scanner();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    scanner.Nickname = ii.Value.ToLower();
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

                        scanners.Add(scanner);
                        break;

                    case "countermeasuredropper":
                        CountermeasureDropper dropper = new CountermeasureDropper();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name.ToLower())
                            {
                                case "nickname":
                                    dropper.Nickname = ii.Value.ToLower();
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
                                case "projectile_archetype":
                                    dropper.ArchtypeName = ii.Value.ToLower();
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
                                    cm.Nickname = ii.Value.ToLower();
                                    break;
                                case "range":
                                    cm.EffectiveRange = Convert.ToInt32(ii.Value);
                                    break;
                                case "ammo_limit":
                                    cm.AmmoLimit = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }

                        tempcmlist.Add(cm);
                        break;
                }
            }

            Console.WriteLine("Processed Scanners, Powerplants, and CMs.");

            foreach (Countermeasure i in tempcmlist)
            {
                try
                {
                    int index = droppers.FindIndex(m => m.ArchtypeName.ToLower() == i.Nickname.ToLower());
                    CountermeasureDropper dropper = droppers[index];
                    dropper.Ammo = i;
                    droppers[index] = dropper;
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error trying to load CM. Message: " + ex.Message);
                }
            }
            tempcmlist = null;

            #region Goods

            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\goods.ini");
            // Ship hull nickname - price - powercore for that ship (nickname)
            Dictionary<string, float> shipHulls = new Dictionary<string, float>();
            Dictionary<string, KeyValuePair<string, Good>> shipsGoods = new Dictionary<string, KeyValuePair<string, Good>>();
            foreach (IniSection i in ini.Sections)
            {
                Good good = new Good();
                string category = "";
                string hull = "";
                string power = "";
                foreach (IniKey ii in i.Keys)
                {
                    switch (ii.Name.ToLower())
                    {
                        case "nickname":
                            // Horrible edge case right here
                            if (ii.Value.ToLower().Contains("rm_") && !ii.Value.ToLower().Contains("rm_h"))
                                break;
                            good.Nickname = ii.Value.ToLower().Trim();
                            break;

                        case "category":
                            category = ii.Value.Trim();
                            break;

                        case "hull":
                            hull = ii.Value.ToLower().Trim();
                            break;

                        case "addon":
                            if (ii.Value.ToLower().Trim().Contains("power"))
                                power = ii.Value.ToLower().Trim().Split(",").ElementAt(0);
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
                }

                if ((good.GoodBuyPrice == 0 || good.GoodSellPrice == 0) && category == "commodity")
                    continue;

                if (category == "ship")
                {
                    if (!string.IsNullOrEmpty(power))
                        shipsGoods[hull] = new KeyValuePair<string, Good>(power, good);
                    continue;
                }

                else if (category == "shiphull")
                {
                    shipHulls[good.Nickname] = good.Price;
                    continue;
                }

                if (good.Nickname != null)
                    goods.Add(good);
            }

            for (var i = 1; i <= shipsGoods.Count; i++)
            {
                try
                {
                    var pair = shipsGoods.ElementAt(i);
                    Good good = pair.Value.Value;
                    good.Nickname = good.Nickname.Replace("_package", "");
                    good.Price = shipHulls[pair.Key];
                    good.Powerplant = pair.Value.Key;
                    goods.Add(good);
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Invalid ShipGood Entry: " + ex.Message);
                }
            }

            for (var index = 1; index <= ships.Count; index++)
            {
                try
                {
                    Ship ship = ships.ElementAt(index);
                    if (marketShips.FindIndex(x => x.Good.Any(y => y.Key == ship.Nickname)) == -1)
                        continue;
                    ship.Powerplant = powerplants.Find(x => x.Nickname == goods.Find(y => y.Nickname == ship.Nickname).Powerplant);
                    ships[index] = ship;
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Unable to get powerplant: " + ex.Message);
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
                            good.Nickname = ii.Value.ToLower();
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

                    
                }
                goods.Add(good);
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
                            good.Nickname = ii.Value.ToLower();
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

                    
                }
                goods.Add(good);
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
                            good.Nickname = ii.Value.ToLower();
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

                    
                }
                goods.Add(good);
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
                            good.Nickname = ii.Value.ToLower();
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

                    
                }
                goods.Add(good);
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
                                    thruster.Nickname = ii.Value.ToLower();
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

                        thrusters.Add(thruster);
                        break;

                    case "shieldgenerator":
                        Shield shield = new Shield();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    shield.Nickname = ii.Value.ToLower();
                                    break;
                                case "ids_name":
                                    shield.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    shield.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "shield_type":
                                    shield.ShieldType = TypeFunctions.GetShieldType(ii.Value);
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
                                    shield.Capacity = Convert.ToSingle(ii.Value);
                                    break;
                                case "regeneration_rate":
                                    shield.RegenRate = Convert.ToSingle(ii.Value);
                                    break;
                            }
                        }

                        shields.Add(shield);
                        break;
                }
            }

            Console.WriteLine("Processed Thrusters and Shields.");

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
                                    faction.Nickname = ii.Value.ToLower();
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

                        factions.Add(faction);
                        break;
                }
            }

            Console.WriteLine("Processed Factions.");

            // Cloaks and Disrupters
            if (File.Exists(root + @"\cloak.cfg"))
            {
                ini = new IniFile(iniOptions);
                ini.Load(root + @"\cloak.cfg");
                foreach (IniSection i in ini.Sections)
                {
                    switch (i.Name.ToLower())
                    {
                        case "cloak":
                            CloakingDevice cloak = new CloakingDevice();
                            cloak.FuelRequirements = new Dictionary<string, int>();
                            foreach (IniKey ii in i.Keys)
                            {
                                switch (ii.Name)
                                {
                                    case "nickname":
                                        cloak.Nickname = ii.Value.ToLower();
                                        break;
                                    case "warmup_time":
                                        cloak.CloakChargeTime = Convert.ToInt32(ii.Value);
                                        break;
                                    case "cooldown_time":
                                        cloak.DisruptionCooldown = Convert.ToInt32(ii.Value);
                                        break;
                                    case "hold_size_limit":
                                        cloak.MaxiumCargoSize = Convert.ToInt32(ii.Value);
                                        break;
                                    case "fuel":
                                        var pair = ii.Value.Split(',');
                                        cloak.FuelRequirements[pair[0]] = Convert.ToInt32(pair[1]);
                                        break;
                                }
                            }

                            cloaks.Add(cloak);
                            break;

                        case "disruptor":
                            CloakDisrupter disrupter = new CloakDisrupter();
                            disrupter.AmmoRequirements = new Dictionary<string, int>();
                            foreach (IniKey ii in i.Keys)
                            {
                                switch (ii.Name)
                                {
                                    case "nickname":
                                        disrupter.Nickname = ii.Value.ToLower();
                                        break;
                                    case "range":
                                        disrupter.Range = Convert.ToInt32(ii.Value);
                                        break;
                                    case "cooldown_time":
                                        disrupter.CooldownTime = Convert.ToInt32(ii.Value);
                                        break;
                                    case "ammo":
                                        var pair = ii.Value.Split(',');
                                        disrupter.AmmoRequirements[pair[0]] = Convert.ToInt32(pair[1]);
                                        break;
                                }
                            }

                            disrupters.Add(disrupter);
                            break;
                    }
                }
                Console.WriteLine("Processed Cloaks and Disrupters.");
            }

            // Weapons
            ini = new IniFile(iniOptions);
            ini.Load(equip + @"\weapon_equip.ini");
            List<Munition> tempMunitions = new List<Munition>();
            List<Explosion> tempExplosions = new List<Explosion>();

            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "cloakingdevice":
                        if (cloaks.Count == 0) break;

                        CloakingDevice tempCloakingDevice = new CloakingDevice();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    tempCloakingDevice.Nickname = ii.Value.ToLower();
                                    break;
                                case "ids_name":
                                    tempCloakingDevice.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    tempCloakingDevice.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "power_usage":
                                    tempCloakingDevice.PowerUsage = Convert.ToUInt32(ii.Value);
                                    break;
                                case "volume":
                                    tempCloakingDevice.CargoRequirement = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }

                        try
                        {
                            int index = cloaks.FindIndex(m => m.Nickname.ToLower() == tempCloakingDevice.Nickname.ToLower());
                            CloakingDevice cloak = cloaks[index];
                            cloak.CargoRequirement = tempCloakingDevice.CargoRequirement;
                            cloak.PowerUsage = tempCloakingDevice.PowerUsage;
                            cloak.Name = tempCloakingDevice.Name;
                            cloak.Infocard = tempCloakingDevice.Infocard;
                            cloaks[index] = cloak;
                        }

                        catch (Exception ex)
                        {
                            Console.WriteLine("Error trying to load cloak. Message: " + ex.Message);
                        }

                        break;
                    case "gun":
                        Weapon weapon = new Weapon();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    weapon.Nickname = ii.Value.ToLower();
                                    break;
                                case "ids_name":
                                    weapon.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    weapon.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "power_usage":
                                    weapon.PowerUsage = Convert.ToSingle(ii.Value);
                                    break;
                                case "volume":
                                    weapon.Volume = Convert.ToSingle(ii.Value);
                                    break;
                                case "hit_pts":
                                    weapon.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                                case "projectile_archetype":
                                    weapon.MunitionArchtype = ii.Value.ToLower();
                                    break;
                                case "refire_delay":
                                    weapon.RefireDelay = Convert.ToSingle(ii.Value);
                                    break;
                                case "turn_rate":
                                    weapon.TurnRate = Convert.ToSingle(ii.Value);
                                    break;
                                case "muzzle_velocity":
                                    weapon.MuzzleVelocity = Convert.ToSingle(ii.Value);
                                    break;
                            }
                        }

                        weapons.Add(weapon);
                        break;
                    case "munition":
                        Munition munition = new Munition();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    munition.Nickname = ii.Value.ToLower();
                                    break;
                                case "ids_name":
                                    munition.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    munition.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "explosion_arch":
                                    munition.WeaponType = WeaponType.Neutral;
                                    munition.ExplosionArchtype = ii.Value.ToLower();
                                    break;
                                case "volume":
                                    try
                                    {
                                        munition.Volume = Convert.ToSingle(ii.Value);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("For some reason there is a random F in a single one of these ini values.");
                                        munition.Volume = 0;
                                    }

                                    break;
                                case "hit_pts":
                                    munition.Hitpoints = Convert.ToSingle(ii.Value);
                                    break;
                                case "force_gun_ori":
                                    munition.HasForcedOrientation = Boolean.Parse(ii.Value);
                                    break;
                                case "time_to_lock":
                                    munition.TimeToLock = Convert.ToSingle(ii.Value);
                                    break;
                                case "ammo_limit":
                                    munition.AmmoLimit = Convert.ToInt32(ii.Value);
                                    break;
                                case "requires_ammo":
                                    munition.RequiresAmmo = Boolean.Parse(ii.Value);
                                    break;
                                case "seeker":
                                    munition.IsSeeking = ii.Value == "LOCK";
                                    break;
                                case "seeker_range":
                                    munition.SeekerRange = Convert.ToSingle(ii.Value);
                                    break;
                                case "cruise_disruptor":
                                    munition.IsCruiseDistupter = Boolean.Parse(ii.Value);
                                    break;
                                case "hull_damage":
                                    munition.HullDamage = Convert.ToSingle(ii.Value);
                                    munition.ShieldDamage = Convert.ToSingle(ii.Value) * 0.5f;
                                    break;
                                case "energy_damage":
                                    munition.EnegryDamage = Convert.ToSingle(ii.Value);
                                    break;
                                case "lifetime":
                                    munition.Lifetime = Convert.ToSingle(ii.Value);
                                    break;
                                case "weapon_type":
                                    munition.WeaponType = TypeFunctions.GetWeaponType(ii.Value);
                                    break;

                            }
                        }

                        tempMunitions.Add(munition);
                        break;
                    case "explosion":
                        Explosion explosion = new Explosion();
                        foreach (var ii in i.Keys)
                        {
                            switch (ii.Name)
                            {
                                case "nickname":
                                    explosion.Nickname = ii.Value.ToLower();
                                    break;
                                case "impluse":
                                    explosion.Impulse = Convert.ToSingle(ii.Value);
                                    break;
                                case "energy_damage":
                                    explosion.EnergyDamage = Convert.ToSingle(ii.Value);
                                    break;
                                case "strength":
                                    explosion.Strength = Convert.ToSingle(ii.Value);
                                    break;
                                case "radius":
                                    explosion.Radius = Convert.ToSingle(ii.Value);
                                    break;
                                case "hull_damage":
                                    explosion.HullDamage = Convert.ToSingle(ii.Value);
                                    break;
                            }
                        }

                        tempExplosions.Add(explosion);
                        break;
                }
            }

            foreach (Explosion i in tempExplosions)
            {
                try
                {
                    int index = tempMunitions.FindIndex(m => string.Equals(m.ExplosionArchtype, i.Nickname, StringComparison.CurrentCultureIgnoreCase));
                    Munition mun = tempMunitions[index];
                    mun.Explosion = i;
                    tempMunitions[index] = mun;
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error trying to load explosions. Message: " + ex.Message);
                }
            }
                
            foreach (Munition i in tempMunitions)
            {
                try
                {
                    int index = weapons.FindIndex(m => m.MunitionArchtype == i.Nickname.ToLower());
                    Weapon weapon = weapons[index];
                    weapon.Munition = i;
                    weapons[index] = weapon;
                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error trying to load munitions. Message: " + ex.Message);
                }
            }

            Console.WriteLine("Processed Weapons.");
            Console.WriteLine();
            Console.WriteLine("Finished reading ini files. Generating Database.");

            // OK ALL DONE. Let's put this shite in a database
            // We want to start from scratch, so we rename an existing db if there is one.
            if (File.Exists(output + @"\FLData.db"))
            {
                if (File.Exists(output + @"\FLData.db.BACKUP"))
                    File.Delete(output + @"\FLData.db.BACKUP");
                File.Move(output + @"\FLData.db", output + @"\FLData.db.BACKUP");
            }

            using (var db = new LiteDatabase(output + @"\FLData.db"))
            {
                // First we create them!
                var dbArmours = db.GetCollection<Armour>("Armours");
                var dbBases = db.GetCollection<Base>("Bases");
                var dbCloaks = db.GetCollection<CloakingDevice>("Cloaks");
                var dbDisrupters = db.GetCollection<CloakDisrupter>("Disrupters");
                var dbCommodities = db.GetCollection<Commodity>("Commodities");
                var dbCMs = db.GetCollection<CountermeasureDropper>("CMs");
                var dbFactions = db.GetCollection<Faction>("Factions");
                var dbGoods = db.GetCollection<Good>("Goods");
                var dbMarketsCommodities = db.GetCollection<Market>("MarketsCommodities");
                var dbMarketsEquipment = db.GetCollection<Market>("MarketsEquipment");
                var dbMarketsShips = db.GetCollection<Market>("MarketsShips");
                var dbScanners = db.GetCollection<Scanner>("Scanners");
                var dbShields = db.GetCollection<Shield>("Shields");
                var dbShips = db.GetCollection<Ship>("Ships");
                var dbSystems = db.GetCollection<Ini.System>("Systems");
                var dbWeapons = db.GetCollection<Weapon>("Weapons");
                var dbThrusers = db.GetCollection<Thruster>("Thrusters");
                var dbInfocards = db.GetCollection<Infocard>("Infocards");

                // Then we populate them
                Console.WriteLine();
                InsertToDatabase(dbArmours, armours, "Armours");
                InsertToDatabase(dbBases, bases, "Bases");
                InsertToDatabase(dbCloaks, cloaks, "Cloaks");
                InsertToDatabase(dbDisrupters, disrupters, "Disrupters");
                InsertToDatabase(dbCommodities, commodities, "Commodities");
                InsertToDatabase(dbCMs, droppers, "CMs");
                InsertToDatabase(dbFactions, factions, "Factions");
                InsertToDatabase(dbGoods, goods, "Goods");
                InsertToDatabase(dbMarketsCommodities, marketCommodities, "MarketsCommodities");
                InsertToDatabase(dbMarketsEquipment, marketEquipment, "MarketsEquipment");
                InsertToDatabase(dbMarketsShips, marketShips, "MarketsShips");
                InsertToDatabase(dbScanners, scanners, "Scanners");
                InsertToDatabase(dbShields, shields, "Shields");
                InsertToDatabase(dbShips, ships, "Ships");
                InsertToDatabase(dbSystems, systems, "Systems");
                InsertToDatabase(dbWeapons, weapons, "Weapons");
                InsertToDatabase(dbThrusers, thrusters, "Thrusters");
                InsertToDatabase(dbInfocards, infocards, "Infocards");
            }

            // Time to check that it worked!
            using (var db = new LiteDatabase(output + @"\FLData.db"))
            {
                Console.WriteLine();
                Console.WriteLine("Running Tests:");
                var d = db.GetCollection<Commodity>("Commodities");
                Console.WriteLine($"Commodity Count: {d.Count()}");
                Console.WriteLine("Random Commodity: " + d.FindAll().Skip(new Random().Next(0, d.Count())).Take(1).First().Nickname);
                Console.WriteLine("Random Commodity: " + d.FindAll().Skip(new Random().Next(0, d.Count())).Take(1).First().Nickname);
                Console.WriteLine("Random Commodity: " + d.FindAll().Skip(new Random().Next(0, d.Count())).Take(1).First().Nickname);
                Console.WriteLine();

                var g = db.GetCollection<Good>("Goods");
                Console.WriteLine($"Good Count: {g.Count()}");
                Console.WriteLine("Random Good: " + g.FindAll().Skip(new Random().Next(0, g.Count())).Take(1).First().Nickname);
                Console.WriteLine("Random Good: " + g.FindAll().Skip(new Random().Next(0, g.Count())).Take(1).First().Nickname);
                Console.WriteLine("Random Good: " + g.FindAll().Skip(new Random().Next(0, g.Count())).Take(1).First().Nickname);
                Console.WriteLine();

                var i = db.GetCollection<Infocard>("Infocards");
                Console.WriteLine($"Infocard Count: {i.Count()}");
                Console.WriteLine("Random Infocard: " + i.FindAll().Skip(new Random().Next(0, i.Count())).Take(1).First().Value);
                Console.WriteLine("Random Infocard: " + i.FindAll().Skip(new Random().Next(0, i.Count())).Take(1).First().Value);
                Console.WriteLine("Random Infocard: " + i.FindAll().Skip(new Random().Next(0, i.Count())).Take(1).First().Value);
                Console.WriteLine();

                var w = db.GetCollection<Weapon>("Weapons");
                Console.WriteLine($"Weapon Count: {w.Count()}");
                Console.WriteLine("Random Weapon: " + w.FindAll().Skip(new Random().Next(0, w.Count())).Take(1).First().Nickname);
                Console.WriteLine("Random Weapon: " + w.FindAll().Skip(new Random().Next(0, w.Count())).Take(1).First().Nickname);
                Console.WriteLine("Random Weapon: " + w.FindAll().Skip(new Random().Next(0, w.Count())).Take(1).First().Nickname);
                Console.WriteLine();

                var mc = db.GetCollection<Market>("MarketsCommodities");
                Console.WriteLine($"MarketCommodity Count: {mc.Count()}");
                Console.WriteLine("Random MarketCommodity: " + mc.FindAll().Skip(new Random().Next(0, mc.Count())).Take(1).First().Base);
                Console.WriteLine("Random MarketCommodity: " + mc.FindAll().Skip(new Random().Next(0, mc.Count())).Take(1).First().Base);
                Console.WriteLine("Random MarketCommodity: " + mc.FindAll().Skip(new Random().Next(0, mc.Count())).Take(1).First().Base);
                Console.WriteLine();

                var me = db.GetCollection<Market>("MarketsEquipment");
                Console.WriteLine($"MarketEquipment Count: {me.Count()}");
                Console.WriteLine("Random MarketEquipment: " + me.FindAll().Skip(new Random().Next(0, me.Count())).Take(1).First().Base);
                Console.WriteLine("Random MarketEquipment: " + me.FindAll().Skip(new Random().Next(0, me.Count())).Take(1).First().Base);
                Console.WriteLine("Random MarketEquipment: " + me.FindAll().Skip(new Random().Next(0, me.Count())).Take(1).First().Base);
                Console.WriteLine();

                Console.ReadLine();
            }
        }

        static void InsertToDatabase<T>(LiteCollection<T> collection, List<T> list, string name)
        {
            int i = 0, ii = 0;
            foreach (T x in list)
            {
                try
                {
                    collection.Insert(x);
                    SetProgress(name, i - ii, list.Count);
                    i++;
                }
                catch (Exception) { ii--; }
            }
        }

        static void SetProgress(string name, int index, int count)
        {
            Console.Write($"\r|    Inserting into {name}. {100 * (index + 1) / count} % completed.");
            if (index + 1 == count)
                Console.WriteLine();
        }

        static List<Infocard> SetupInfocards(string root)
        {
            VFS.Init(root);
            InfocardManager info = new InfocardManager((new FreelancerIni()).Resources);
            info.ExportStrings(root + @"\Infonames.json");
            info.ExportInfocards(root + @"\Infocards.json");

            var infocards = JsonConvert.DeserializeObject<Dictionary<uint, string>>(File.ReadAllText(root + @"\Infocards.json"));
            var infonames = JsonConvert.DeserializeObject<Dictionary<uint, string>>(File.ReadAllText(root + @"\Infonames.json"));
            var allInfocards = infocards.Concat(infonames).GroupBy(d => d.Key).OrderBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
            List<Infocard> infoList = new List<Infocard>();
            foreach (var i in allInfocards)
                infoList.Add(new Infocard { Key = i.Key, Value = i.Value });

            return infoList;
        }

        // This stuff is so big, I'm moving it to it's own function for better readability.
        static void SetupBasesAndSystems(IniOptions o, string data, 
            out List<Market> marketCommodities, out List<Market> marketEquipment, out List<Market> marketShips,
            out List<Base> bases, out List<Ini.System> systems)
        {
            systems = new List<Ini.System>();
            bases = new List<Base>();
            marketCommodities = new List<Market>();
            marketEquipment = new List<Market>();
            marketShips = new List<Market>();
            Dictionary<string, string> nickFileDictionary = new Dictionary<string, string>();
            Dictionary<string, List<Base>> baseSystems = new Dictionary<string, List<Base>>();

            IniFile ini = new IniFile(o);
            ini.Load(data + @"\Equipment\market_misc.ini");

            foreach (IniSection i in ini.Sections)
            {
                if (i.Name.ToLower() != "basegood") continue;

                Market market = new Market();
                foreach (IniKey ii in i.Keys)
                {
                    try
                    {
                        switch (ii.Name.ToLower())
                        {

                            case "base":
                                market.Base = ii.Value.ToLower();
                                break;
                            case "marketgood":
                                if (market.Good == null)
                                    market.Good = new Dictionary<string, decimal>();

                                string[] arr = ii.Value.Split(",");
                                if (market.Good.ContainsKey(arr[0])) // Mimic how FL reads ini files
                                {
                                    market.Good[arr[0].ToLower()] = Convert.ToDecimal(arr[6]);
                                    break;
                                }

                                market.Good.Add(arr[0].ToLower(), Convert.ToDecimal(arr[6]));
                                break;
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during market_misc: {ex.Message}");
                    }
                }
                marketEquipment.Add(market);
            }

            ini = new IniFile(o);
            ini.Load(data + @"\Equipment\market_ships.ini");

            foreach (IniSection i in ini.Sections)
            {
                if (i.Name.ToLower() != "basegood") continue;

                Market market = new Market();
                foreach (IniKey ii in i.Keys)
                {
                    try
                    {
                        switch (ii.Name.ToLower())
                        {

                            case "base":
                                market.Base = ii.Value.ToLower();
                                break;
                            case "marketgood":
                                if (market.Good == null)
                                    market.Good = new Dictionary<string, decimal>();

                                string[] arr = ii.Value.Split(",");
                                if (market.Good.ContainsKey(arr[0])) // Mimic how FL reads ini files
                                {
                                    market.Good[arr[0].ToLower().Replace("_package", "")] = Convert.ToDecimal(arr[6]);
                                    break;
                                }

                                market.Good.Add(arr[0].ToLower().Replace("_package", ""), Convert.ToDecimal(arr[6]));
                                break;
                        }
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during market_ships: {ex.Message}");
                    }
                }
                marketShips.Add(market);
            }

            ini = new IniFile(o);
            ini.Load(data + @"\Equipment\market_commodities.ini");

            foreach (IniSection i in ini.Sections)
            {
                if (i.Name.ToLower() != "basegood") continue;

                Market market = new Market();
                foreach (IniKey ii in i.Keys)
                {
                    try
                    {
                        switch (ii.Name.ToLower())
                        {
                            case "base":
                                market.Base = ii.Value.ToLower();
                                break;
                            case "marketgood":
                                if (market.Good == null)
                                    market.Good = new Dictionary<string, decimal>();

                                string[] arr = ii.Value.Split(",");
                                if (market.Good.ContainsKey(arr[0])) // Mimic how FL reads ini files
                                {
                                    market.Good[arr[0].ToLower()] = Convert.ToDecimal(arr[6]);
                                    break;
                                }

                                market.Good.Add(arr[0].ToLower(), Convert.ToDecimal(arr[6]));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during market_commodities: {ex.Message}");
                    }
                }
                marketCommodities.Add(market);
            }

            ini = new IniFile(o);
            ini.Load(data + @"\UNIVERSE\universe.ini");
            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "base":
                        Base iBase = new Base();
                        string systemStr = string.Empty;
                        foreach (IniKey ii in i.Keys)
                        {
                            switch (ii.Name.ToLower())
                            {
                                case "nickname":
                                    iBase.Nickname = ii.Value.ToLower();
                                    break;
                                case "system":
                                    systemStr = ii.Value.ToLower();
                                    iBase.System = systemStr;
                                    
                                    if (!baseSystems.ContainsKey(systemStr))
                                        baseSystems[systemStr] = new List<Base>();
                                    break;
                                case "strid_name":
                                    iBase.Name = Convert.ToUInt32(ii.Value);
                                    break;
                            }
                        }

                        baseSystems[systemStr].Add(iBase);
                        bases.Add(iBase);
                        break;
                    case "system":
                        Ini.System system = new Ini.System { Region = "None", Bases = new List<Base>()};
                        string temp3 = null, temp4 = null;
                        foreach (IniKey ii in i.Keys)
                        {
                            switch (ii.Name.ToLower())
                            {
                                case "nickname":
                                    system.Nickname = ii.Value.ToLower();
                                    temp3 = ii.Value;
                                    break;
                                case "ids_info":
                                    system.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "file":
                                    temp4 = ii.Value;
                                    break;
                                case "strid_name":
                                    system.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "navmapscale":
                                    system.NavMapScale = Convert.ToDecimal(ii.Value);
                                    break;
                            }
                        }

                        if (string.IsNullOrEmpty(temp3) || string.IsNullOrEmpty(temp4))
                            break;

                        nickFileDictionary.TryAdd(temp3, temp4);
                        systems.Add(system);
                        break;
                }
            }

            foreach (var i in baseSystems)
            {
                var sys = systems.FirstOrDefault(x => x.Nickname.ToLower() == i.Key.ToLower());
                if (sys != null)
                {
                    int pos = systems.IndexOf(sys);
                    sys.Bases = i.Value;
                    systems[pos] = sys;
                }
            }

            ini = new IniFile(o);
            ini.Load(data + @"\Universe\Territory.ini");

            foreach (IniSection i in ini.Sections)
            {
                switch (i.Name.ToLower())
                {
                    case "houses":
                        string tHouse = "";
                        string tSystems = "";
                        foreach (IniKey ii in i.Keys)
                        {
                            if (!string.IsNullOrEmpty(tHouse) && !string.IsNullOrEmpty(tSystems))
                            {
                                string[] strs = tSystems.Split(",");
                                for (var index = 0; index < systems.Count; index++)
                                {
                                    Ini.System iSystem = systems[index];
                                    int pos = Array.IndexOf(strs, iSystem.Nickname);
                                    if (pos != -1)
                                    {
                                        iSystem.Region = strs[pos];
                                        systems[index] = iSystem;
                                    }
                                }
                            }

                            switch (ii.Name.ToLower())
                            {
                                case "house":
                                    tHouse = ii.Value;
                                    break;
                                case "systems":
                                    tSystems = ii.Value;
                                    break;
                            }
                        }

                        break;
                }
            }

            foreach (var s in nickFileDictionary)
            {
                ini = new IniFile(o);
                ini.Load(data + @"\Universe\" + s.Value);
                var sys = systems.FirstOrDefault(x => x.Nickname.ToLower() == s.Key.ToLower());
                if (sys == null)
                    continue;

                foreach (var i in ini.Sections)
                {
                    if (i.Name.ToLower() != "object") continue;
                    if (i.Keys.Contains("base"))
                    {
                        if (!i.Keys.Contains("dock_with"))
                            continue;

                        Base iBase = bases.FirstOrDefault(x => string.Equals(x.Nickname, i.Keys["base"].Value, StringComparison.CurrentCultureIgnoreCase));
                        if (iBase == null)
                            continue;

                        int iPos = bases.IndexOf(iBase);
                        if (!i.Keys.Contains("ids_name") || !i.Keys.Contains("reputation") ||
                            !i.Keys.Contains("archetype") || !i.Keys.Contains("pos"))
                        {
                            bases.RemoveAt(iPos);
                            try
                            {
                                marketCommodities.RemoveAt(marketCommodities.IndexOf(marketCommodities.First(x => x.Base == i.Keys["dock_with"].Value.ToLower())));
                            } catch { }

                            try
                            {
                                marketEquipment.RemoveAt(marketEquipment.IndexOf(marketEquipment.First(x => x.Base == i.Keys["dock_with"].Value.ToLower())));
                            } catch { }

                            continue;
                        }

                        try
                        { // If anything fails here, we're not interested in the base
                            iBase.Infocard = Convert.ToUInt32(i.Keys["ids_info"].Value);
                            iBase.OwnerFaction = i.Keys["reputation"].Value;
                            iBase.ArchetypeNickname = i.Keys["archetype"].Value.ToLower();
                            string[] pos = i.Keys["pos"].Value.Split(",");
                            iBase.Position = new float[3] { Convert.ToSingle(pos[0]), Convert.ToSingle(pos[1]), Convert.ToSingle(pos[2]) };
                            bases[iPos] = iBase;
                        } catch { }
                    }
                }
            }

            ini = new IniFile(o);
            ini.Load(data + @"\INTERFACE\Infocardmap.ini");
            // We sometimes have duplicates, and need to make sure those cases are handled.
            List<uint> processedNumbers = new List<uint>();
            foreach (var key in ini.Sections["InfocardMapTable"].Keys)
            {
                if (key.Name == "Map")
                {
                    uint[] arr = Array.ConvertAll(key.Value.Split(","), i => uint.Parse(i));
                    if (processedNumbers.Any(x => x == arr[0]))
                        continue; // If we've already done it, go to the next one

                    int index = bases.FindIndex(x => x.Infocard == arr[0]);
                    if (index == -1)
                    {
                        Console.WriteLine($"Base infocard missing from base list. Number: {arr[0]}");
                        continue;
                    }

                    Base iBase = bases[index];
                    iBase.Infocard = arr[1]; // Reassign the infocard
                    bases[index] = iBase; // Replace the old one
                    processedNumbers.Add(arr[0]); // Make sure we don't repeat for duplicates
                }
            }

            ini = new IniFile(o);
            ini.Load(data + @"UNIVERSE\Territory.ini");

            foreach (var section in ini.Sections)
            {
                if (section.Name.ToLower() == "houses")
                {
                    string temp = section.Keys["format"].Value;
                    bool isWorld = false;
                    if (temp == "%s System, %s Space." || temp == "% s System, % s Outer Region.")
                        isWorld = false;
                    else if (temp == "% s System, % s Worlds.")
                        isWorld = true;

                    string house = "None";
                    foreach (var key in section.Keys)
                    {
                        if (key.Name == "house")
                            house = key.Value;

                        else if (key.Name == "systems")
                        {
                            var sys = key.Value.Split(",");
                            foreach (string nick in sys)
                            {
                                var index = systems.FindIndex(x => x.Nickname == nick);
                                if (index == -1)
                                    continue;
                                var syst = systems[index];

                                if (isWorld)
                                    syst.Region = key.Value + " Worlds";

                                else
                                    syst.Region = key.Value;

                                systems[index] = syst;
                            }
                        }
                    }
                }

                else if (section.Name.ToLower() == "systems")
                {
                    string region = section.Keys["format"].Value;
                    if (region.Contains("Atmospheric"))
                        region = "Atmosphere";
                    else if (region.Contains("virtual"))
                        region = "Other";
                    else if (region.Contains("Cassiopeia"))
                        region = "Cassiopeia";

                    if (region == section.Keys["format"].Value)
                        continue;

                    List<string> sys = section.Keys["systems"].Value.Split(",").ToList();
                    foreach (string nick in sys)
                    {
                        var index = systems.FindIndex(x => x.Nickname == nick);
                        if (index == -1)
                            continue;
                        var syst = systems[index];
                        syst.Region = region;
                        systems[index] = syst;
                    }
                }
            }
        }
    }
}
