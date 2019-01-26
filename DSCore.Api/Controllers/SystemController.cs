using Microsoft.AspNetCore.Mvc;
using LiteDB;
using DSCore.Ini;
using System = DSCore.Ini.System;

namespace DSCore.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Ini.System>("Systems");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                return Utils.ReturnJson(collection.FindAll(), Errors.Null);
            }
        }

        [HttpGet("{nickname}")]
        public ActionResult<string> Get(string nickname)
        {
            if (!Utils.CheckDatabase())
                return Utils.ReturnJson(null, Errors.DatabaseNotFound);

            using (var db = new LiteDatabase(Utils.GetDatabase()))
            {
                var collection = db.GetCollection<Ini.System>("Systems");
                if (collection.Count() == 0)
                    return Utils.ReturnJson(null, Errors.InvalidDatabaseStructure);

                var find = collection.FindById(nickname);
                if (find == null)
                    return Utils.ReturnJson(null, Errors.ValueNotFound);
                else
                {
                    return Utils.ReturnJson(find, Errors.Null);
                }
            }
        }
    }
}