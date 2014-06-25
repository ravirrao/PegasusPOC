using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcMovie
{
   public static class Keys
   {
       public static string URL = "sagescacache.redis.cache.windows.net";
       public static string passwd = "Xtg8XNtLwBOYF9Ls3VRqGr3qlR2OhEp8rAxI/9hNF3M=";

      public static string conStr
      {
         get
         {
            return URL + ",ssl=true,password=" + passwd;
         }
      }

   }
}