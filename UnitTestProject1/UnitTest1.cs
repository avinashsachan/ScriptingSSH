using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void CleanupFile()
        {
            var s = new SSHLib.ScriptingSSH("", "", "");
            var inputText = System.IO.File.ReadAllText(@"C:\D\Codes\Dish-5G-RAN-HelthCheck\Dish-5G-RAN-HelthCheck\bin\Debug\SessionLog.log");

            //change
            inputText = s.CleanSessionOutput(inputText);

            System.IO.File.WriteAllText(@"C:\D\Codes\Dish-5G-RAN-HelthCheck\Dish-5G-RAN-HelthCheck\bin\Debug\SessionLog1.log", inputText);

            
        }

        [TestMethod]
        public void CleanUpClearCode()
        {


            var a = "drwxr-xr-x   1 root root 4096 May 18 14:22 \u001B[0m\u001B[01;32m.\u001B[0m/";

            Regex r = new Regex(@"\u001B\[(0?)[0-4];3[0-7]m");
            var b = r.Replace(a, ""); //a.Replace("\u001B[01;34m", "");
            var rCheck = "drwxr-xr-x   1 root root 4096 May 18 14:22 \u001B[0m.\u001B[0m/";
            Assert.AreEqual(b, rCheck);


            Regex r1 = new Regex(@"\u001B\[0m");
            var c = r1.Replace(a, "");
            var r1Check = "drwxr-xr-x   1 root root 4096 May 18 14:22 \u001B[01;32m./";
            Assert.AreEqual(c, r1Check);

        }
    }
}
