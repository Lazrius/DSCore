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
    public class SpecialEquipmentController : InheritController
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("cloaks")]
        public IActionResult Cloaks()
        {
            Errors error = Errors.Null;
            try
            {
                var cloaks = Utils.GetDatabaseCollection<CloakingDevice>("Cloaks", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Cloaks collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                ViewBag.Infocards = infocards;
                return View("Cloak", cloaks);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }

        [HttpGet("disrupters")]
        public IActionResult Disrupters()
        {
            Errors error = Errors.Null;
            try
            {
                var droppers = Utils.GetDatabaseCollection<CountermeasureDropper>("CMs", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the CMs collection.");

                var disrupters = Utils.GetDatabaseCollection<CloakDisrupter>("Disrupters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Disrupters collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                for (int i = 0; i < disrupters.Count; i++)
                {
                    CloakDisrupter dis = disrupters[i];
                    var dropper = droppers.FirstOrDefault(x => x.Nickname == dis.Nickname);
                    if (dropper != null)
                    {
                        dis.Name = dropper.Name;
                        disrupters[i] = dis;
                    }
                }

                ViewBag.Infocards = infocards;
                return View("Disrupter", disrupters);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
