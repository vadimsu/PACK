using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace TestServerEmulator
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        public Form1()
        {
            InitializeComponent();
            LogUtility.LogUtility.FileName = Environment.CurrentDirectory + "\\log.txt";
            LogUtility.LogUtility.SetLevel(ModuleLogLevel);
            sent = 0;
            transmitPending = false;
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
        }

        byte[] fileBuff;
        Thread thread;
        Socket clientSocket;
        Socket serverSocket;
        int sent;
        bool transmitPending;

        void WorkerThread(object param)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(GetLocalInternalIP(), 8888));
            serverSocket.Listen(50);
            while (true)
            {
                Socket clientSock = serverSocket.Accept();
                LogUtility.LogUtility.LogFile("accepted", ModuleLogLevel);
                sent = 0;
                byte[] rbuff = new byte[1];
                clientSock.Receive(rbuff, 0, 1, SocketFlags.None);
                LogUtility.LogUtility.LogFile("received", ModuleLogLevel);
                while (sent < fileBuff.Length)
                {
                    int bytes2sent;
                    Random r = new Random(DateTime.Now.Millisecond);
                    bytes2sent = r.Next(1, fileBuff.Length - sent);
                    if (bytes2sent > clientSock.SendBufferSize)
                    {
                        clientSock.SendBufferSize = bytes2sent;
                    }
                    int sent2 = clientSock.Send(fileBuff, sent, bytes2sent, SocketFlags.None);
                    if (sent2 > 0)
                    {
                        LogUtility.LogUtility.LogFile("sent " + Convert.ToString(sent2) + " overall " + Convert.ToString(sent), ModuleLogLevel);
                        sent += sent2;
                    }
                }
                LogUtility.LogUtility.LogFile("shutting down", ModuleLogLevel);
                SocketAsyncEventArgs se = new SocketAsyncEventArgs();
                try
                {
                    //clientSock.DisconnectAsync(se);
                    clientSock.Shutdown(SocketShutdown.Both);
                    //clientSock.Disconnect(false);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                }
                //clientSock.Close();
                LogUtility.LogUtility.LogFile("closed", ModuleLogLevel);
            }
        }
        void OnDisconnected(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndDisconnect(ar);
                clientSocket.Close();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
            }
            LogUtility.LogUtility.LogFile("disconnected", ModuleLogLevel);
        }
        void Transmit()
        {
            try
            {
                lock (this)
                {
                    if (transmitPending)
                    {
                        LogUtility.LogUtility.LogFile("transmitPending", ModuleLogLevel);
                        return;
                    }
                    transmitPending = false;
                }

                int bytes2sent;
                Random r = new Random(DateTime.Now.Millisecond);
                bytes2sent = r.Next(1, fileBuff.Length - sent);
                try
                {
                    if (bytes2sent > clientSocket.SendBufferSize)
                    {
                        clientSocket.SendBufferSize = bytes2sent;
                    }
                    clientSocket.BeginSend(fileBuff, sent, bytes2sent, SocketFlags.None, new AsyncCallback(OnTransmitted), null);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("Transmit " + exc.Message, ModuleLogLevel);
                }
                transmitPending = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("Transmit " + exc.Message, ModuleLogLevel);
            }
            LogUtility.LogUtility.LogFile("transmit began", ModuleLogLevel);
        }
        void OnTransmitted(IAsyncResult ar)
        {
            try
            {
                int sent2 = 0;

                try
                {
                    sent2 = clientSocket.EndSend(ar);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                }

                LogUtility.LogUtility.LogFile("sent " + Convert.ToString(sent2) + " overall " + Convert.ToString(sent), ModuleLogLevel);
                sent += sent2;
                lock (this)
                {
                    transmitPending = false;
                }
                if (sent >= fileBuff.Length)
                {
                    try
                    {
                        LogUtility.LogUtility.LogFile("shutting down the socket", ModuleLogLevel);
                        clientSocket.Shutdown(SocketShutdown.Both);
                        //clientSocket.Disconnect(false);
                        clientSocket.Close();
                        //sent = 0;
                        LogUtility.LogUtility.LogFile("Done", ModuleLogLevel);
                        //clientSocket.BeginDisconnect(false, new AsyncCallback(OnDisconnected), null);
                    }
                    catch (Exception exc)
                    {
                        LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                    }
                    LogUtility.LogUtility.LogFile("return", ModuleLogLevel);
                    return;
                }
                Transmit();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("OnTransmitted " + exc.Message, ModuleLogLevel);
            }
        }
        void OnReceive(IAsyncResult ar)
        {
            try
            {
                int received = clientSocket.EndReceive(ar);
                //            Transmit();
                byte[] buffer = (byte[])ar.AsyncState;
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), buffer);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("OnReceive " + exc.Message, ModuleLogLevel);
                try
                {
                    LogUtility.LogUtility.LogFile("shutting down the socket", ModuleLogLevel);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    sent = 0;
                    LogUtility.LogUtility.LogFile("Done", ModuleLogLevel);
                    //clientSocket.BeginDisconnect(false, new AsyncCallback(OnDisconnected), null);
                }
                catch (Exception exc2)
                {
                    LogUtility.LogUtility.LogFile(exc2.Message, ModuleLogLevel);
                }
            }
        }
        void OnAccept(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile("accepted", ModuleLogLevel);
            clientSocket = serverSocket.EndAccept(ar);
            byte []buffer = new byte[1024];
            LogUtility.LogUtility.LogFile("BeginReceive", ModuleLogLevel);
            //clientSocket.LingerState.LingerTime = 5;
            //clientSocket.LingerState.Enabled = true;
            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(OnReceive), buffer);
            LogUtility.LogUtility.LogFile("calling transmit", ModuleLogLevel);
            transmitPending = false;
            sent = 0;
            Transmit();
            LogUtility.LogUtility.LogFile("BeginAccept", ModuleLogLevel);
            try
            {
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile("return", ModuleLogLevel);
        }
        private void buttonSelectFile_Click(object sender, EventArgs e)
        {
            fileBuff = null;
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            //LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH);
            FileStream fs = File.Open(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);
            if (fs == null)
            {
                return;
            }
            if (thread != null)
            {
                thread.Abort();
            }
            fileBuff = new byte[fs.Length];
            fs.Read(fileBuff, 0, fileBuff.Length);
            fs.Close();
            //ParameterizedThreadStart pts = new ParameterizedThreadStart(WorkerThread);
            //thread = new Thread(pts);
            //thread.Start();
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(GetLocalInternalIP(), 8888));
            serverSocket.Listen(50);
            serverSocket.BeginAccept(new AsyncCallback(OnAccept), serverSocket);
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
            serverSocket.Disconnect(true);
            serverSocket.Close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
