using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LiteDB;
using DSCore.Ini;
using System.Reflection;
using Newtonsoft.Json;

namespace DSCore.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        // GET api/
        [HttpGet]
        public ActionResult<string> Get(string nickname)
        {
            this.HttpContext.Response.StatusCode = 200;
            return "Hi. This is a test page!";
        }
    }
}