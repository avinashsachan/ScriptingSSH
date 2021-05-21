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

            var session = new SSHLib.ScriptingSSH(FromConfig.IP, FromConfig.Username, FromConfig.Password, 22);
            session.DebugMode = true;
            //session.KeyboardAuthPrompts = new System.Collections.Hashtable();
            //session.KeyboardAuthPrompts.Add("neId", );
            //session.setKeyString(FromConfig.Path);

            try
            {
                var s = session.Connect();
                session.WaitFor("$");
                System.Threading.Thread.Sleep(5);
              
                session.SendAndWait(". .profile\n", "~$", true);

                session.SendAndWait(". .bashrc\n", "~$", true);

                session.SendAndWait("echo $PS1\n", "~$", true);
                //Console.WriteLine(session.SessionLog);
                Console.WriteLine("");
            }
            catch (Exception)
            {


            }
            
            session.Disconnect();
            //session.Dispose();

        }
    }
}
