using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
  public static  class FromConfig
    {

        public static string IP { get { return System.Configuration.ConfigurationManager.AppSettings["ip"].ToString();  } }

        public static string Username { get { return System.Configuration.ConfigurationManager.AppSettings["user"].ToString(); } }
        public static string Password { get { return System.Configuration.ConfigurationManager.AppSettings["password"].ToString(); } }
        public static string Path { get { return System.Configuration.ConfigurationManager.AppSettings["path"].ToString(); } }
    }
}
