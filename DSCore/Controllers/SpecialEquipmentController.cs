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

        [HttpGet("disrupter")]
        public IActionResult Disrupters()
        {
            Errors error = Errors.Null;
            try
            {
                var disrupters = Utils.GetDatabaseCollection<CloakDisrupter>("Disrupters", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Cloaks collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

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
