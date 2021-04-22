﻿using Renci.SshNet;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace ScriptingSSH
{
    class ScriptingSSH
    {
        private string IP;
        private string Username;
        private string Password;
        private StreamReader reader = null;
        private StreamWriter writer = null;
        private ShellStream stream = null;
        private SshClient client = null;
        private int _Timeout = 30;

        private char[] _mByBuff = new char[32767];
        private StringBuilder _strFullLog = new StringBuilder();
        private StringBuilder _strWorkingData = new StringBuilder();


        private int _port = 22;

        private delegate void rdr();
        private readonly object _messagesLockWorkingData = new object();


        public ScriptingSSH(string ip, string username, string password, int port = 22)
        {
            this.IP = ip;
            this.Username = username;
            this.Password = password;
            this._port = port;
        }

        public string RunCommand(string commandText, Int32 timeout = 10)
        {
            var s = client.RunCommand(commandText);
            s.CommandTimeout = DateTime.Now.AddSeconds(timeout) - DateTime.Now;
            return s.Execute();
        }


        public bool Connect(bool KeyBoardInteractive = false)
        {
            client = new SshClient(this.IP, this._port, this.Username, this.Password);
            try
            {
                if (!KeyBoardInteractive)
                {
                    client.Connect();
                }
                else
                {
                    KeyboardInteractiveAuthenticationMethod kMethod = new KeyboardInteractiveAuthenticationMethod(this.Username);
                    kMethod.AuthenticationPrompt += KMethod_AuthenticationPrompt;
                    var cInfo = new ConnectionInfo(this.IP, this._port, this.Username, kMethod);
                    client = new SshClient(cInfo);
                    client.Connect();
                }
            }
            catch { return false; }

            stream = client.CreateShellStream("XTERM", 160, 200, 1600, 1200, 1024);
            this.reader = new StreamReader(stream);
            this.writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            AsyncCallback recieveData = new AsyncCallback(OnRecievedData);

            rdr r = new rdr(ReadStream);
            r.BeginInvoke(recieveData, r);

            return true;
        }

        private void KMethod_AuthenticationPrompt(object sender, Renci.SshNet.Common.AuthenticationPromptEventArgs e)
        {
            //throw new NotImplementedException();
            foreach (var prompt in e.Prompts)
            {
                if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = this.Password;
                }
            }
        }

        private void OnRecievedData(IAsyncResult ar)
        {
            try
            {
                if (client == null || !client.IsConnected) return;
                Thread.Sleep(10);
                //AsyncCallback recieveData = new AsyncCallback(OnRecievedData);
                rdr r = new rdr(ReadStream);
                r.BeginInvoke(new AsyncCallback(OnRecievedData), r);
            }
            catch //(Exception ex)
            {
                //TODO: Need capture here
            }

        }

        public void Disconnect()
        {
            this.stream.Dispose();
            this.client.Disconnect();
            this.client.Dispose();
            this.client = null;
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
            string ln = "";

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
                _strWorkingData.Clear();
            }
        }


        public void Dispose()
        {
            try { reader.Dispose(); }
            catch { }

            try { writer.Dispose(); }
            catch { }

            try { client.Dispose(); }
            catch { }

        }


    }
}
