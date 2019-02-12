using System;
using System.Collections.Generic;
using DSCore.Ini;
using DSCore.Models;
using Microsoft.AspNetCore.Mvc;
using DSCore.Utilities;

namespace DSCore.Controllers
{
    [Route("[controller]")]
    public class SystemController : InheritController
    {
        public IActionResult Index()
        {
            Errors error = Errors.Null;
            try
            {
                var systems = Utils.GetDatabaseCollection<Ini.System>("Systems", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Systems collection.");

                var infocards = Utils.GetDatabaseCollection<Infocard>("Infocards", ref error);
                if (error != Errors.Null)
                    throw new InvalidOperationException("The database was unable to access the Infocards collection.");

                ViewBag.Infocards = infocards;
                return View(systems);
            }
            catch (Exception ex)
            {
                return View("DatabaseError", new PageException(ex, error));
            }
        }
    }
}
