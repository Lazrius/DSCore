using System;
using System.Collections.Generic;
using System.Linq;
using DSCore.Ini;
using DSCore.Models;
using Microsoft.AspNetCore.Mvc;
using DSCore.Utilities;
using System = DSCore.Ini.System;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class BaseController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Factions collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                

                ViewBag.Infocards = infocards;
                ViewBag.Factions = factions;
                ViewBag.Systems = systems;
                return View(bases);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }

        [HttpGet("{nickname}")]
        public IActionResult Individual(string nickname)
        {
            Errors error = Errors.Null;
            try
            {
                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Factions collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                var marketCommodities = Utils.GetDatabaseCollection<Market>("MarketsCommodities", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsCommodities collection.");

                var marketShips = Utils.GetDatabaseCollection<Market>("MarketsShips", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsShips collection.");

                var marketEquipment = Utils.GetDatabaseCollection<Market>("MarketsEquipment", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsEquipment collection.");

                var commodities = Utils.GetDatabaseCollection<Commodity>("Commodities", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Commodities collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var weapons = Utils.GetDatabaseCollection<Weapon>("Weapons", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Weapons collection.");

                var shields = Utils.GetDatabaseCollection<Shield>("Shields", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Shields collection.");

                var ships = Utils.GetDatabaseCollection<Ship>("Ships", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Ships collection.");

                var thrusters = Utils.GetDatabaseCollection<Thruster>("Thrusters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Thrusters collection.");

                var cms = Utils.GetDatabaseCollection<CountermeasureDropper>("CMs", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Thrusters collection.");

                Base b = bases.Find(x => x.Nickname == nickname);
                Ini.System system = systems.Find(x => x.Nickname == b.System);
                var cList = new List<Commodity>();
                var sList = new List<Ship>();
                var eList = new Dictionary<string, List<object>>(); // Equipment can be of multiple types
                eList.Add("Shield", new List<object>());
                eList.Add("Thruster", new List<object>());
                eList.Add("Weapon", new List<object>());
                eList.Add("CM", new List<object>());

                // Load in all the commodities for this base
                foreach (var i in marketCommodities.Find(x => x.Base == nickname).Goods)
                {
                    Commodity commodity = commodities.First(x => x.Nickname == i.Nickname);
                    Good good = goods.First(x => x.Nickname == i.Nickname);
                    commodity.Price = good.Price * ((float)i.PriceModifier == 0 ? 1 : (float)i.PriceModifier);
                    if (i.StockA == 150 && i.StockB == 500) // If the base sells the item, I have literally no idea why FL is like this
                        commodity.BaseSells = true;
                    cList.Add(commodity);
                }

                // Load in all the ships for this base, if any
                var baseShips = marketShips.Find(x => x.Base == nickname);
                if (baseShips != null)
                    foreach (var i in baseShips.Goods)
                    {
                        Ship ship = ships.FirstOrDefault(x => x.Nickname == i.Nickname.Replace("_package", ""));
                        if (ship == null)
                            continue;
                        Good good = goods.First(x => x.Nickname == i.Nickname.Replace("_package", ""));
                        ship.Price = good.Price * ((float)i.PriceModifier == 0 ? 1 : (float)i.PriceModifier);
                        sList.Add(ship);
                    }

                // Load in all the equipment for the base
                foreach (var i in marketEquipment.Find(x => x.Base == nickname).Goods)
                {
                    try
                    {
                        string equipType;
                        dynamic equip = weapons.FirstOrDefault(x => x.Nickname == i.Nickname);
                        equipType = "Weapon";

                        if (equip == null)
                        {
                            equip = shields.FirstOrDefault(x => x.Nickname == i.Nickname);
                            equipType = "Shield";
                            if (equip != null)
                                if (((Shield)equip).Nickname.Contains("pod")) continue;
                        }

                        if (equip == null)
                        {
                            equipType = "Thruster";
                            equip = thrusters.FirstOrDefault(x => x.Nickname == i.Nickname);
                        }
                            

                        if (equip == null)
                        {
                            equipType = "CM";
                            equip = cms.FirstOrDefault(x => x.Nickname == i.Nickname);
                        }
                            

                        if (equip == null)
                            continue;

                        Good good = goods.First(x => x.Nickname == i.Nickname);
                        equip.Price = good.Price * ((float)i.PriceModifier == 0 ? 1 : (float)i.PriceModifier);
                        eList[equipType].Add((object)equip);
                    } catch { continue;  }
                }

                string faction = infocards.First(x => x.Key == factions.First(y => y.Nickname.Trim() == b.OwnerFaction.Trim()).Name).Value;

                ViewBag.System = system;
                ViewBag.Infocards = infocards;
                ViewBag.Faction = faction;
                ViewBag.Ships = sList;
                ViewBag.Commodities = cList;
                ViewBag.Equipment = eList;
                return View(b);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
