using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TestClient
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        public Form1()
        {
            InitializeComponent();
            iterations = 0;
            received_overall = 0;
            received_overall_this_time = 0;
            initialStamp = 0;
            waiting4Receive = false;
            fileWriteLock = new object();
            LogUtility.LogUtility.FileName = Environment.CurrentDirectory + "\\log.txt";
            LogUtility.LogUtility.SetLevel(ModuleLogLevel);
            updateStatistics = new UpdateStatistics(Update_Statistics);
            updateStatistics2 = new UpdateStatistics2(Update_Statistics2);
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
            requestToConnectSent = false;
        }
        Thread thread;
        uint iterations;
        uint received_overall;
        Socket sock;
        uint received_overall_this_time;
        long initialStamp;
        bool waiting4Receive;
        object fileWriteLock;
        bool requestToConnectSent;

        delegate void UpdateStatistics(uint iter,uint time, uint bytes);
        UpdateStatistics updateStatistics;
        delegate void UpdateStatistics2(uint bytes,uint overall_bytes);
        UpdateStatistics2 updateStatistics2;

        void Update_Statistics(uint iter,uint time, uint bytes)
        {
            if (iter == 1)
            {
                labelfirstIterationTime.Text = Convert.ToString(time);
                labelfirstRxBytes.Text = Convert.ToString(bytes);
            }
            else
            {
                labelAverageTime.Text = Convert.ToString(time);
                labelAverageRxBytes.Text = Convert.ToString(bytes);
            }
        }

        void Update_Statistics2(uint bytes,uint overall_bytes)
        {
            labelReceivedBytes.Text = Convert.ToString(bytes);
            labelReceivedOverallBytes.Text = Convert.ToString(overall_bytes);
        }

        void WorkerThread(object param)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(GetLocalInternalIP(),6666));
            LogUtility.LogUtility.LogFile("connected", ModuleLogLevel);
            byte[] buff = new byte[1024];
            sock.Send(buff, 0, 1, SocketFlags.None);
            LogUtility.LogUtility.LogFile("sent", ModuleLogLevel);
            initialStamp = DateTime.Now.Ticks;
            received_overall_this_time = 0;
            object[] args;
            FileStream fs;
            fs = File.Open(openFileDialog1.FileName, FileMode.Create, FileAccess.Write);
            fs.Close();
            while(sock.Connected)
            {
                try
                {
                    int received = sock.Receive(buff);
                    if (received <= 0)
                    {
                        break;
                    }
                    received_overall_this_time += (uint)received;
                    fs = File.Open(openFileDialog1.FileName, FileMode.Append, FileAccess.Write);
                    fs.Write(buff, 0, received);
                    fs.Flush();
                    fs.Close();
                    args = new object[2];
                    received_overall += (uint)received;
                    args[0] = received_overall_this_time;
                    args[1] = received_overall;
                    Invoke(updateStatistics2, args);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                    break;
                }
            }
            LogUtility.LogUtility.LogFile("done", ModuleLogLevel);
            iterations++;
            received_overall += received_overall_this_time;
            args = new object[3];
            args[0] = iterations;
            args[1] = DateTime.Now.Ticks - initialStamp;
            args[2] = received_overall_this_time;
            if (iterations == 1)
            {
                args[1] = DateTime.Now.Ticks - initialStamp;
                args[2] = received_overall_this_time;
            }
            else
            {
                args[1] = (DateTime.Now.Ticks - initialStamp) / iterations;
                args[2] = received_overall / iterations;
            }
            Invoke(updateStatistics, args);
        }
        string fileName;
        string GetNewFileName()
        {
            int idx = fileName.IndexOf(".");
            String str = fileName.Substring(0,idx) + Convert.ToString(iterations) + fileName.Substring(idx);
            LogUtility.LogUtility.LogFile("setting new file name " + str, ModuleLogLevel);
            return str;
        }
        void FinalizeStatistics()
        {
            try
            {
                iterations++;
                //received_overall += received_overall_this_time;
                object[] args = new object[3];
                args[0] = iterations;
                args[1] = DateTime.Now.Ticks - initialStamp;
                args[2] = received_overall_this_time;
                if (iterations == 1)
                {
                    args[1] = (uint)(DateTime.Now.Ticks - initialStamp);
                    args[2] = received_overall_this_time;
                }
                else
                {
                    args[1] = ((uint)(DateTime.Now.Ticks - initialStamp)) / iterations;
                    args[2] = received_overall / iterations;
                }
                received_overall_this_time = 0;
                Invoke(updateStatistics, args);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("Finalize " + exc.Message, ModuleLogLevel);
            }
        }
        void Done()
        {
            if (requestToConnectSent)
            {
                return;
            }
            requestToConnectSent = true;
            try
            {
                LogUtility.LogUtility.LogFile("done", ModuleLogLevel);
                FinalizeStatistics();
                fileName = GetNewFileName();
                lock (this)
                {
                    waiting4Receive = false;
                }
                try
                {
                    ////sock.Shutdown(SocketShutdown.Both);
                    ////sock.Close();
                    sock.Disconnect(true);
                    sock.Close();
                    //sock.BeginConnect(new IPEndPoint(GetLocalInternalIP(), 6666), new AsyncCallback(OnConnected), null);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("Done " + exc.Message, ModuleLogLevel);
                }
                try
                {
                    ////sock.Shutdown(SocketShutdown.Both);
                    ////sock.Close();
                    //sock.Disconnect(true);
                    sock = null;
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("Done " + exc.Message, ModuleLogLevel);
                }
                lock (this)
                {
                    waiting4Receive = false;
                }
                try
                {
                    ////sock.Shutdown(SocketShutdown.Both);
                    ////sock.Close();
                    //sock.Disconnect(true);
                    sock.BeginConnect(new IPEndPoint(GetLocalInternalIP(), 6666), new AsyncCallback(OnConnected), null);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("Done " + exc.Message, ModuleLogLevel);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
            }
        }
        void OnSent(IAsyncResult ar)
        {
            try
            {
                int sent2 = 0;
                byte[] buffer = null;

                //LogUtility.LogUtility.LogFile("OnSent");
                try
                {
                    buffer = new byte[1024];
                    sent2 = sock.EndSend(ar);
                    Thread.Sleep(1000);
                    sock.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnSent), buffer);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("OnSent" + exc.Message, ModuleLogLevel);
                    //FinalizeStatistics();
                    //Done();
                    buffer = null;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
            }
        }
        void OnReceived(IAsyncResult ar)
        {
            try
            {
                int received = 0;
                byte[] buff = (byte[])ar.AsyncState;

                lock (this)
                {
                    if (!waiting4Receive)
                    {
                        LogUtility.LogUtility.LogFile("not waiting", ModuleLogLevel);
                        sock.EndReceive(ar);
                        return;
                    }
                    waiting4Receive = false;
                }
                try
                {
                    received = sock.EndReceive(ar);
                    if (received <= 0)
                    {
                        LogUtility.LogUtility.LogFile("receiver error", ModuleLogLevel);
                        if (sock.Poll(1, SelectMode.SelectRead) && sock.Available == 0)
                        {
                            Done();
                            return;
                        }
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("OnReceived " + exc.Message, ModuleLogLevel);
                    Done();
                    return;
                }
                received_overall_this_time += (uint)received;
                LogUtility.LogUtility.LogFile("OnReceived now " + Convert.ToString(received) + " overall this time " + Convert.ToString(received_overall_this_time) + " overall " + Convert.ToString(received_overall), ModuleLogLevel);
                FileStream fs;
                lock (fileWriteLock)
                {
                    try
                    {
                        fs = File.Open(fileName, FileMode.Append, FileAccess.Write);
                        fs.Write(buff, 0, received);
                        fs.Flush();
                        fs.Close();
                        LogUtility.LogUtility.LogFile("written " + Convert.ToString(received) + " to " + fileName, ModuleLogLevel);
                    }
                    catch (Exception exc)
                    {
                        LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                    }
                }
                object[] args = new object[2];
                received_overall += (uint)received;
                args[0] = received_overall_this_time;
                args[1] = received_overall;
                Invoke(updateStatistics2, args);
                byte[] buffer = new byte[1024];
                lock (this)
                {
                    waiting4Receive = true;
                }
                try
                {
                    sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceived), buffer);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                    buffer = null;
                    //FinalizeStatistics();
                    Done();
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
            }
        }
        void OnConnected(IAsyncResult ar)
        {
            try
            {
                sock.EndConnect(ar);
                byte[] buffer = new byte[1];
                LogUtility.LogUtility.LogFile("OnConnected", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                requestToConnectSent = false;
                FileStream fs;
                fs = File.Open(fileName, FileMode.Create, FileAccess.Write);
                fs.Close();
                initialStamp = DateTime.Now.Ticks;
                try
                {
                    sock.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnSent), buffer);
                    LogUtility.LogUtility.LogFile("Sending", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    buffer = new byte[1024];
                    lock (this)
                    {
                        waiting4Receive = true;
                    }
                    sock.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceived), buffer);
                }
                catch (Exception exc)
                {
                    Done();
                    LogUtility.LogUtility.LogFile("OnConnected: either BeginSend or BeginReceive " + exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                Done();
            }
        }
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            //LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH);
            fileName = openFileDialog1.FileName;
#if false
            if (thread != null)
            {
                thread.Abort();
            }
            ParameterizedThreadStart pts = new ParameterizedThreadStart(WorkerThread);
            thread = new Thread(pts);
            thread.Start();
#else
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            requestToConnectSent = true;
            sock.BeginConnect(new IPEndPoint(GetLocalInternalIP(), 6666), new AsyncCallback(OnConnected),null);
#endif
        }
        protected static bool IsLocalIP(IPAddress IP)
        {
            byte First = (byte)Math.Floor((decimal)(IP.Address % 256));
            byte Second = (byte)Math.Floor((decimal)((IP.Address % 65536)) / 256);
            //10.x.x.x Or 172.16.x.x <-> 172.31.x.x Or 192.168.x.x
            return (First == 10) ||
                (First == 172 && (Second >= 16 && Second <= 31)) ||
                (First == 192 && Second == 168);
        }
        public static IPAddress GetLocalInternalIP()
        {
            try
            {
                IPHostEntry he = Dns.Resolve(Dns.GetHostName());
                for (int Cnt = 0; Cnt < he.AddressList.Length; Cnt++)
                {
                    if (IsLocalIP(he.AddressList[Cnt]))
                        return he.AddressList[Cnt];
                }
                return he.AddressList[0];
            }
            catch
            {
                return IPAddress.Any;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            sock.Disconnect(true);
            sock.Close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
