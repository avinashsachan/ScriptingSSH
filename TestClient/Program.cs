using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {

            var session = new ScriptingSSH.ScriptingSSH(FromConfig.IP, FromConfig.Username, "", 22, ScriptingSSH.Authenticationtype.Key);
            session.DebugMode = true;
            //session.KeyboardAuthPrompts = new System.Collections.Hashtable();
            //session.KeyboardAuthPrompts.Add("neId", );

            session.setKeyString(FromConfig.Path);

            try
            {
                var s = session.Connect();
                session.WaitFor("$");
                System.Threading.Thread.Sleep(5);
                //session.SendAndWait("hostname\n", "~$", false);
                Console.WriteLine(session.SessionLog);
            }
            catch (Exception)
            {


            }
            
            session.Disconnect();
            //session.Dispose();

        }
    }
}
