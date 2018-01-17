using Renci.SshNet;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ScriptingSSH
{
    class ScriptingSSH
    {
        private string IP;
        private int _port = 22;
        private string Username;
        private string Password;
        private int _Timeout = 30;

        private StreamReader reader = null;
        private StreamWriter writer = null;
        private ShellStream stream = null;
        private SshClient ssh = null;
       

        private char[] _mByBuff = new char[32767];
        private StringBuilder _strFullLog = new StringBuilder();
        private StringBuilder _strWorkingData = new StringBuilder();

        private delegate void rdr();
        private readonly object _messagesLockWorkingData = new object();


        public ScriptingSSH(string ip, string username, string password)
        {
            this.IP = ip;
            this.Username = username;
            this.Password = password;
        }


        public bool Connect()
        {

            ssh = new SshClient(this.IP, this.Username, this.Password);
            try { ssh.Connect(); }
            catch (Exception ex)
            {
                return false;
            }

            stream = ssh.CreateShellStream("dumb", 160, 200, 1600, 1200, 1024);
            this.reader = new StreamReader(stream);
            this.writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            AsyncCallback recieveData = new AsyncCallback(OnRecievedData);

            rdr r = new rdr(ReadStream);
            r.BeginInvoke(recieveData, r);

            return true;
        }

        private void OnRecievedData(IAsyncResult ar)
        {
            try
            {
                if (ssh == null || !ssh.IsConnected) return;

                Thread.Sleep(25);
                //AsyncCallback recieveData = new AsyncCallback(OnRecievedData);
                rdr r = new rdr(ReadStream);
                r.BeginInvoke(new AsyncCallback(OnRecievedData), r);
            }
            catch (Exception ex)
            {

            }

        }

        public string RunCommand(string commandText, Int32 timeout = 30)
        {
            var s = ssh.RunCommand(commandText);
            s.CommandTimeout = DateTime.Now.AddSeconds(timeout) - DateTime.Now;
            return s.Execute();
        }

        public void Disconnect()
        {
            this.stream.Dispose();
            this.ssh.Disconnect();
            this.ssh.Dispose();
            this.ssh = null;
        }


        private void ReadStream()
        {
            var ch = reader.Read(_mByBuff, 0, _mByBuff.Length);
            if (ch == 0) return;
            lock (_messagesLockWorkingData)
            {
                for (Int32 i = 0; i < ch; i++)
                {
                    //Console.Write(_mByBuff[i]);
                    _strFullLog.Append(_mByBuff[i]);
                    _strWorkingData.Append(_mByBuff[i]);
                }
            }
        }


        public int SendAndWait(string message, string waitFor, bool suppressCarriegeReturn = false)
        {
            lock (_messagesLockWorkingData)
            {
                _strWorkingData.Length = 0;
            }

            SendMessage(message, suppressCarriegeReturn);
            this.WaitFor(waitFor);
            return 0;
        }
        public int SendAndWait(string message, string waitFor, string breakCharacter, bool suppressCarriegeReturn = false)
        {
            lock (_messagesLockWorkingData)
            {
                _strWorkingData.Length = 0;
            }
            SendMessage(message, suppressCarriegeReturn);
            int t = this.WaitFor(waitFor, breakCharacter);
            return t;
        }



        public int WaitFor(string dataToWaitFor)
        {

            // Get the starting time
            long lngStart = System.DateTime.Now.AddSeconds(_Timeout).Ticks;
            long lngCurTime = 0;
            //  Dim start_index As UInt64 = 0
            //  Dim End_index As UInt64 = 0
            string ln = "";
            // Dim L As Integer = 0
            while (ln.IndexOf(dataToWaitFor, StringComparison.OrdinalIgnoreCase) == -1)
            {
                // Timeout logic
                lngCurTime = System.DateTime.Now.Ticks;
                if (lngCurTime > lngStart)
                {
                    throw new Exception("Timeout waiting for : " + dataToWaitFor);
                }
                Thread.Sleep(5);

                if ((ln.IndexOf("Idle too long; timed out", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    //intReturn = -2
                    if (ln.IndexOf(dataToWaitFor, StringComparison.OrdinalIgnoreCase) != -1)
                        return 0;
                    lock (_messagesLockWorkingData)
                    {
                        _strWorkingData.Clear();
                    }
                    throw new Exception("Connection Terminated forcefully");
                }

                //  L = strWorkingData.Length
                lock (_messagesLockWorkingData)
                {
                    ln = _strWorkingData.ToString(0, _strWorkingData.Length);
                    _strWorkingData.Remove(0, ln.Length < 50 ? 0 : ln.Length - 50);
                    //CLIPPING OF LN FROM WORKING DATA
                }
            }
            lock (_messagesLockWorkingData)
            {
                _strWorkingData.Length = 0;
            }

            return 0;
        }
        public int WaitFor(string dataToWaitFor, string breakCharacter)
        {
            // Get the starting time
            long lngStart = System.DateTime.Now.AddSeconds(_Timeout).Ticks;
            long lngCurTime = 0;
            //  Dim start_index As UInt64 = 0
            //  Dim End_index As UInt64 = 0
            string ln = "";

            string[] breaks = dataToWaitFor.Split(breakCharacter.ToCharArray());
            int intReturn = -1;

            while (intReturn == -1)
            {
                // Timeout logic
                lngCurTime = System.DateTime.Now.Ticks;
                if (lngCurTime > lngStart)
                {
                    throw new Exception("Timeout waiting for : " + dataToWaitFor);
                }
                Thread.Sleep(5);
                for (int i = 0; i <= breaks.Length - 1; i++)
                {
                    if (ln.IndexOf(breaks[i], StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        intReturn = i;
                    }
                }
                if ((ln.IndexOf("Idle too long; timed out", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    for (int i = 0; i <= breaks.Length - 1; i++)
                    {
                        lock (_messagesLockWorkingData)
                        {
                            if (_strWorkingData.ToString().IndexOf(breaks[i].ToLower(), StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                return i;
                            }
                        }
                    }
                    throw new Exception("Connection Terminated forcefully");
                }
                lock (_messagesLockWorkingData)
                {
                    ln = _strWorkingData.ToString(0, _strWorkingData.Length);
                    _strWorkingData.Remove(0, ln.Length < 50 ? 0 : ln.Length - 50);
                    //CLIPPING OF LN FROM WORKING DATA
                }

            }
            lock (_messagesLockWorkingData)
            {
                _strWorkingData.Length = 0;
            }
            return intReturn;

        }




        public void SendMessage(string message, bool suppressCarriegeReturn)
        {
            if (suppressCarriegeReturn)
                this.writer.Write(message);
            else
                this.writer.WriteLine(message);

        }


        public string SessionLog
        {
            get
            {
                lock (_messagesLockWorkingData)
                {
                    return _strFullLog.ToString();
                }
            }
        }

        public Int32 Timeout
        {
            get { return _Timeout; }
            set { _Timeout = Math.Max(value, 0); }
        }

        /// <summary>
        /// Clears all data in the session log
        /// </summary>
        public void ClearSessionLog()
        {
            lock (_messagesLockWorkingData)
            {
                _strFullLog.Clear();
            }
        }
    }
}
