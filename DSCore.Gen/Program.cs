using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using LibreLancer.Data;
using LiteDB;
using DSCore.Ini;
using MadMilkman.Ini;

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

                        commodities.Add(commodity);
                        break;
                }
            }

            Console.WriteLine("Processed Commodities and Armour.");

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

                    
                }
                if (good.GoodBuyPrice == 0 || string.IsNullOrEmpty(good.Equipment) || good.GoodSellPrice == 0)
                    continue;
                goods.Add(good);
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
                                case "projectile_archetype":
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

                        tempcmlist.Add(cm);
                        break;
                }
            }

            Console.WriteLine("Processed Scanners, Powerplants, and CMs.");

            foreach (Countermeasure i in tempcmlist)
            {
                try
                {
                    int index = droppers.FindIndex(m => m.ArchtypeName == i.Nickname);
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
                                        cloak.Nickname = ii.Value;
                                        break;
                                    case "warmpup_time":
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
                                        disrupter.Nickname = ii.Value;
                                        break;
                                    case "range":
                                        disrupter.Range = Convert.ToInt32(ii.Value);
                                        break;
                                    case "cooldown_time":
                                        disrupter.CooldownTime = Convert.ToInt32(ii.Value);
                                        break;
                                    case "fuel":
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
                            switch (ii.Name)
                            {
                                case "nickname":
                                    ship.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    ship.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
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
                                    ship.ShipClass = (ShipClass) Convert.ToInt32(ii.Value);
                                    break;
                                case "nanobot_limit":
                                    ship.Nanobots = Convert.ToInt32(ii.Value);
                                    break;
                                case "shield_battery_limit":
                                    ship.ShieldBats = Convert.ToInt32(ii.Value);
                                    break;
                            }
                        }

                        ships.Add(ship);
                        break;
                }
            }

            Console.WriteLine("Processed Ships.");

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
                                    tempCloakingDevice.Nickname = ii.Value;
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
                            int index = cloaks.FindIndex(m => m.Nickname == tempCloakingDevice.Nickname);
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
                                    weapon.Nickname = ii.Value;
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
                                    weapon.MunitionArchtype = ii.Value;
                                    break;
                                case "refire_delay":
                                    weapon.RefireDelay = Convert.ToSingle(ii.Value);
                                    break;
                                case "turn_rate":
                                    weapon.TurnRate = Convert.ToSingle(ii.Value);
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
                                    munition.Nickname = ii.Value;
                                    break;
                                case "ids_name":
                                    munition.Name = Convert.ToUInt32(ii.Value);
                                    break;
                                case "ids_info":
                                    munition.Infocard = Convert.ToUInt32(ii.Value);
                                    break;
                                case "explosion_arch":
                                    munition.WeaponType = WeaponType.Neutral;
                                    munition.ExplosionArchtype = ii.Value;
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
                                    break;
                                case "energy_damage":
                                    munition.ShieldDamage = Convert.ToSingle(ii.Value);
                                    break;
                                case "lifetime":
                                    munition.Lifetime = Convert.ToSingle(ii.Value);
                                    break;
                                case "weapon_type":
                                    munition.WeaponType = GetWeaponType(ii.Value);
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
                                    explosion.Nickname = ii.Value;
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
                    int index = tempMunitions.FindIndex(m => m.ExplosionArchtype == i.Nickname);
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
                    int index = weapons.FindIndex(m => m.MunitionArchtype == i.Nickname);
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
            if (File.Exists(root + @"\FLData.db"))
            {
                if (File.Exists(root + @"\FLData.db.BACKUP"))
                    File.Delete(root + @"\FLData.db.BACKUP");
                File.Move(root + @"\FLData.db", root + @"\FLData.db.BACKUP");
            }

            using (var db = new LiteDatabase(root + @"\FLData.db"))
            {
                // First we create them!
                var dbArmours = db.GetCollection<Armour>("Armours");
                var dbCloaks = db.GetCollection<CloakingDevice>("Cloaks");
                var dbDisrupters = db.GetCollection<CloakDisrupter>("Disrupters");
                var dbCommodities = db.GetCollection<Commodity>("Commodities");
                var dbCMs = db.GetCollection<CountermeasureDropper>("CMs");
                var dbFactions = db.GetCollection<Faction>("Factions");
                var dbGoods = db.GetCollection<Good>("Goods");
                var dbPowerplants = db.GetCollection<Powerplant>("Powerplants");
                var dbScanners = db.GetCollection<Scanner>("Scanners");
                var dbShields = db.GetCollection<Shield>("Shields");
                var dbShips = db.GetCollection<Ship>("Ships");
                var dbWeapons = db.GetCollection<Weapon>("Weapons");
                var dbThrusers = db.GetCollection<Thruster>("Thrusters");
                var dbInfocards = db.GetCollection<Infocard>("Infocards");

                // Then we populate them
                Console.WriteLine();
                InsertToDatabase(dbArmours, armours, "Armours");
                InsertToDatabase(dbCloaks, cloaks, "Cloaks");
                InsertToDatabase(dbDisrupters, disrupters, "Disrupters");
                InsertToDatabase(dbCommodities, commodities, "Commodities");
                InsertToDatabase(dbCMs, droppers, "CMs");
                InsertToDatabase(dbFactions, factions, "Factions");
                InsertToDatabase(dbGoods, goods, "Goods");
                InsertToDatabase(dbPowerplants, powerplants, "Powerplants");
                InsertToDatabase(dbScanners, scanners, "Scanners");
                InsertToDatabase(dbShields, shields, "Shields");
                InsertToDatabase(dbShips, ships, "Ships");
                InsertToDatabase(dbWeapons, weapons, "Weapons");
                InsertToDatabase(dbThrusers, thrusters, "Thrusters");
                InsertToDatabase(dbInfocards, infocards, "Infocards");
            }

            // Time to check that it worked!
            using (var db = new LiteDatabase(root + @"\FLData.db"))
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

        static ShieldType GetShieldType(string value)
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

        static WeaponType GetWeaponType(string value)
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
    }
}
