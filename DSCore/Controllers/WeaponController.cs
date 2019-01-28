using System;
using System.Collections.Generic;
using System.Linq;
using DSCore.Ini;
using Microsoft.AspNetCore.Mvc;
using DSCore.Models;
using DSCore.Utilities;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class WeaponController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var weapons = Utils.GetDatabaseCollection<Weapon>("Weapons", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Commodities collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                List<Weapon> weaponList = new List<Weapon>();
                foreach (var i in weapons)
                {
                    Good good = goods.FirstOrDefault(x => x.Nickname == i.Nickname);
                    if (good == null)
                        continue;

                    Weapon weapon = i;
                    i.Price = good.Price;
                    i.BadBuyPrice = good.BadSellPrice;
                    i.Combinable = good.Combinable;
                    i.GoodSellPrice = good.GoodSellPrice;
                    i.BadSellPrice = good.BadSellPrice;
                    i.GoodBuyPrice = good.GoodBuyPrice;
                    weaponList.Add(weapon);
                }

                ViewBag.Infocards = infocards;
                return View(weaponList);
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
                var weapons = Utils.GetDatabaseCollection<Weapon>("Weapons", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Commodities collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                var marketEquipment = Utils.GetDatabaseCollection<Market>("MarketsEquipment", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsEquipment collection.");

                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                Dictionary<string, decimal> baseList = new Dictionary<string, decimal>();
                foreach (var i in marketEquipment)
                {
                    if (i.Good.ContainsKey(nickname))
                        baseList.Add(i.Base, i.Good.FirstOrDefault(x => x.Key == nickname).Value);
                }

                Dictionary<Base, decimal> sellpoints = new Dictionary<Base, decimal>();
                foreach (var s in baseList)
                    sellpoints[bases.First(x => x.Nickname == s.Key)] = s.Value;

                Weapon weapon = weapons.Find(x => x.Nickname == nickname);
                Good good = goods.Find(x => x.Nickname == nickname);
                weapon.Price = good.Price;
                weapon.BadBuyPrice = good.BadSellPrice;
                weapon.Combinable = good.Combinable;
                weapon.GoodSellPrice = good.GoodSellPrice;
                weapon.BadSellPrice = good.BadSellPrice;
                weapon.GoodBuyPrice = good.GoodBuyPrice;

                ViewBag.Infocards = infocards;
                ViewBag.Sellpoints = sellpoints;
                ViewBag.Systems = systems;
                ViewBag.Factions = factions;
                return View(weapon);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
