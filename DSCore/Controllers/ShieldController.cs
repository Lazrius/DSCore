using System;
using System.Collections.Generic;
using System.Linq;
using DSCore.Ini;
using DSCore.Models;
using Microsoft.AspNetCore.Mvc;
using DSCore.Utilities;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class ShieldController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var shields = Utils.GetDatabaseCollection<Shield>("Shields", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Shields collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                List<Shield> shieldList = new List<Shield>();
                foreach (var i in shields)
                {
                    Good good = goods.FirstOrDefault(x => x.Nickname == i.Nickname);
                    if (good == null)
                        continue;

                    i.Price = good.Price;
                    Shield shield = i;
                    shieldList.Add(shield);
                }

                ViewBag.Infocards = infocards;
                return View(shieldList);
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
                var shields = Utils.GetDatabaseCollection<Shield>("Shields", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Shields collection.");

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
                    throw new InvalidOperationException("The database was unable to access the Factions collection.");

                Dictionary<string, decimal> baseList = new Dictionary<string, decimal>();
                foreach (var i in marketEquipment)
                {
                    if (i.Goods.FirstOrDefault(x => x.Nickname == nickname) != null)
                        baseList.Add(i.Base, i.Goods.FirstOrDefault(x => x.Nickname == nickname).PriceModifier);
                }

                Dictionary<Base, decimal> sellpoints = new Dictionary<Base, decimal>();
                foreach (var s in baseList)
                    sellpoints[bases.First(x => x.Nickname == s.Key)] = s.Value;

                Shield shield = shields.Find(x => x.Nickname == nickname);
                Good good = goods.Find(x => x.Nickname == nickname);
                shield.Price = good.Price;

                ViewBag.Infocards = infocards;
                ViewBag.Systems = systems;
                ViewBag.Sellpoints = sellpoints;
                ViewBag.Factions = factions;
                return View(shield);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
