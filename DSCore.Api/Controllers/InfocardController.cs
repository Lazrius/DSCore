using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LiteDB;
using DSCore.Ini;
using Newtonsoft.Json;

namespace DSCore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfocardController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Infocard>("Infocards");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                return Utils.ReturnJson(collection.FindAll(), Errors.Null);
            }
        }

        // GET api/infocard/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Infocard>("Infocards");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                var find = collection.FindById(id);
                if (find == null)
                    return Utils.ReturnJson(null, Errors.ValueNotFound);
                else
                {
                    if (find.Value == null)
                        find.Value = "Warning: String is null or empty.";

                    return Utils.ReturnJson(find, Errors.Null);
                }
            }
        }
    }
}
