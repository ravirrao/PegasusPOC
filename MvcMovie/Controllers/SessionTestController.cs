using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcMovie.Controllers
{
    public class SessionTestController : Controller
    {

       public ActionResult WriteSession(string id = "Default_Add_Route_Data")
       {
          //Session["sessionTst"] 
          System.Web.HttpContext.Current.Session["sessionTst"] 
             =  HttpUtility.HtmlEncode(id) + " From PID: " 
             + Process.GetCurrentProcess().Id.ToString();

          ViewBag.InfoMsg = "\"" + Session["sessionTst"] + "\" written to session.";
          return View("Info");
       }

       public ActionResult ReadSession()
       {
          ViewBag.InfoMsg = "Read from Session: \"" + 
             System.Web.HttpContext.Current.Session["sessionTst"] 
             //Session["sessionTst"] 
             + "\" PID: "
                            + Process.GetCurrentProcess().Id.ToString();
          return View("Info");
       }
    }
}