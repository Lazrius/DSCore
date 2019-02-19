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
    public class CMController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var countermeasureDroppers = Utils.GetDatabaseCollection<CountermeasureDropper>("CMs", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the CMs collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                List<CountermeasureDropper> droppers = new List<CountermeasureDropper>();
                foreach (var i in countermeasureDroppers)
                {
                    Good good = goods.FirstOrDefault(x => x.Nickname == i.Nickname);
                    if (good == null)
                        continue;

                    CountermeasureDropper dropper = i;
                    dropper.Price = good.Price;
                    droppers.Add(dropper);
                }

                ViewBag.Infocards = infocards;
                return View(droppers);
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
                var countermeasureDroppers = Utils.GetDatabaseCollection<CountermeasureDropper>("CMs", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the CMs collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var marketEquipment = Utils.GetDatabaseCollection<Market>("MarketsEquipment", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsEquipment collection.");

                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Factions collection.");

                CountermeasureDropper dropper = countermeasureDroppers.Find(x => x.Nickname == nickname);
                Good good = goods.Find(x => x.Nickname == nickname);
                dropper.Price = good.Price;

                Dictionary<string, decimal> baseList = new Dictionary<string, decimal>();
                foreach (var i in marketEquipment)
                {
                    if (i.Goods.FirstOrDefault(x => x.Nickname == nickname) != null)
                        baseList.Add(i.Base, i.Goods.FirstOrDefault(x => x.Nickname == nickname).PriceModifier);
                }

                Dictionary<Base, decimal> sellpoints = new Dictionary<Base, decimal>();
                foreach (var s in baseList)
                    sellpoints[bases.First(x => x.Nickname == s.Key)] = s.Value;

                ViewBag.Infocards = infocards;
                ViewBag.Sellpoints = sellpoints;
                ViewBag.Factions = factions;
                ViewBag.Systems = systems;
                return View(dropper);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
