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
    public class ThrusterController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var thrusters = Utils.GetDatabaseCollection<Thruster>("Thrusters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Thrusters collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                List<Thruster> thrusterList = new List<Thruster>();
                foreach (var i in thrusters)
                {
                    Good good = goods.FirstOrDefault(x => x.Nickname == i.Nickname);
                    if (good == null)
                        continue;

                    Thruster thruster = i;
                    thruster.Price = good.Price;
                    thrusterList.Add(thruster);
                }

                ViewBag.Infocards = infocards;
                return View(thrusterList);
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
                var thrusters = Utils.GetDatabaseCollection<Thruster>("Thrusters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Thrusters collection.");

                var goods = Utils.GetDatabaseCollection<Good>("Goods", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Goods collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                var bases = Utils.GetDatabaseCollection<Base>("Bases", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Bases collection.");

                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                var marketEquipment = Utils.GetDatabaseCollection<Market>("MarketsEquipment", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the MarketsEquipment collection.");

                var factions = Utils.GetDatabaseCollection<Faction>("Factions", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Factions collection.");

                Thruster thruster = thrusters.Find(x => x.Nickname == nickname);
                Good good = goods.Find(x => x.Nickname == nickname);
                thruster.Price = good.Price;

                Dictionary<string, decimal> baseList = new Dictionary<string, decimal>();
                foreach (var i in marketEquipment)
                {
                    if (i.Good.ContainsKey(nickname))
                        baseList.Add(i.Base, i.Good.FirstOrDefault(x => x.Key == nickname).Value);
                }

                Dictionary<Base, decimal> sellpoints = new Dictionary<Base, decimal>();
                foreach (var s in baseList)
                    sellpoints[bases.First(x => x.Nickname == s.Key)] = s.Value;

                ViewBag.Infocards = infocards;
                ViewBag.Factions = factions;
                ViewBag.Sellpoints = sellpoints;
                ViewBag.Systems = systems;
                return View(thruster);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
