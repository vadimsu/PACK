using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SenderPackLib;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ChunkChainDataTypes;

namespace PackSenderTest
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        public Form1()
        {
            InitializeComponent();
        }
        struct SenderThreadParam
        {
            public IPAddress myIp;
            public UInt16 myPort;
        };
        SenderPackLib.SenderPackLib senderPackLib;
        UpdateControls updateControls;
        byte []fileBuff;
        int current_offset;
        private void Form1_Load(object sender, EventArgs e)
        {
            updateControls = new UpdateControls(Update_Controls);
            Disposed += new EventHandler(Form1_Disposed);
        }

        void Form1_Disposed(object sender, EventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        void Copybytes(byte[] src_buff, byte[] dst_buff, int size)
        {
            int idx;
            for (idx = 0; idx < size; idx++)
            {
                dst_buff[idx] = src_buff[idx];
            }
        }
        delegate void UpdateControls(string status);
        void Update_Controls(string status)
        {
            labelStatus.Text = status;
        }
        void OnMsgRead4Tx(object param, byte[] msg)
        {
            Socket sock = (Socket)param;
            object[] args;
            int sent;

            try
            {
                LogUtility.LogUtility.LogFile("Sending PRED ACK " + Convert.ToString(msg.Length), ModuleLogLevel);
                sent = sock.Send(msg);
            }
            catch (SocketException se)
            {
                args = new object[1];
                args[0] = se.Message;
                Invoke(updateControls, args);
                LogUtility.LogUtility.LogFile(se.Message, ModuleLogLevel);
                return;
            }
            LogUtility.LogUtility.LogFile("Sent", ModuleLogLevel);
            args = new object[1];
            args[0] = "sent " + Convert.ToString(sent);
            Invoke(updateControls, args);
        }

        bool AddMoreBytes()
        {
            if (current_offset == fileBuff.Length)
            {
                LogUtility.LogUtility.LogFile("already added all", ModuleLogLevel);
                return /*senderPackLib.AddLast()*/false;
            }
            Random rand = new Random(DateTime.Now.Millisecond);
#if true
            int number_of_bytes_to_add = rand.Next(1, (fileBuff.Length - current_offset));
#else
            int number_of_bytes_to_add = rand.Next(fileBuff.Length, (fileBuff.Length - current_offset));
#endif
            byte[] buff = new byte[number_of_bytes_to_add];
            for (int idx = 0; idx < number_of_bytes_to_add; idx++)
            {
                buff[idx] = fileBuff[current_offset + idx];
            }
            LogUtility.LogUtility.LogFile("Add bytes " + Convert.ToString(number_of_bytes_to_add), ModuleLogLevel);
            current_offset += number_of_bytes_to_add;
            senderPackLib.AddData(buff);
            return true;
        }

        void InitiateTransfer()
        {
            OnMessageReadyToTx onMessageReadyToTx = new OnMessageReadyToTx(OnMsgRead4Tx);
            senderPackLib = new SenderPackLib.SenderPackLib(fileBuff, onMessageReadyToTx);
            AddMoreBytes();
        }

        void PackMode(Socket clientSock)
        {
            byte[] buff;
            object[] args;
            int received;

            InitiateTransfer();
            senderPackLib.SetOnMsgReady4TxParam(clientSock);
            while (true)
            {
                try
                {
                    bool added = AddMoreBytes();
                    
                    if (!added)
                    {
                        clientSock.Disconnect(true);
                        clientSock.Close();
                        LogUtility.LogUtility.LogFile("Client socket is closed", ModuleLogLevel);
                        break;
                    }
                    

                    buff = new byte[1024];
                    try
                    {
                        received = clientSock.Receive(buff);
                    }
                    catch (SocketException se)
                    {
                        args = new object[1];
                        args[0] = se.Message;
                        Invoke(updateControls, args);
                        LogUtility.LogUtility.LogFile(se.Message, ModuleLogLevel);
                        break;
                    }
                    if (received > 0)
                    {
                        args = new object[1];
                        args[0] = "received " + Convert.ToString(received);
                        Invoke(updateControls, args);
                        byte[] receved_buff = new byte[received];
                        Copybytes(buff, receved_buff, received);
                        LogUtility.LogUtility.LogFile("PRED received " + Convert.ToString(received), ModuleLogLevel);
                        senderPackLib.OnDataByteMode(receved_buff, 0);
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                    break;
                }
            }
            senderPackLib.ClearData();
            current_offset = 0;
        }

        void RawMode(Socket clientSock)
        {
            int sent = 0;
            while (true)
            {
                try
                {
                    sent += clientSock.Send(fileBuff,sent,fileBuff.Length-sent,SocketFlags.None);
                    if (sent == fileBuff.Length)
                    {
                        clientSock.Disconnect(false);
                        clientSock.Close();
                        clientSock = null;
                        LogUtility.LogUtility.LogFile("Client socket is closed", ModuleLogLevel);
                        break;
                    }
                }
                catch (SocketException se)
                {
                    MessageBox.Show(se.Message);
                    break;
                }
            }
        }

        void SenderThread(object obj)
        {
            SenderThreadParam param = (SenderThreadParam)obj;
            Socket sock = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            if (sock == null)
            {
                return;
            }
            object[] args;
            IPEndPoint ipEndPoint = new IPEndPoint(param.myIp, param.myPort);
            sock.Bind(ipEndPoint);
            sock.Listen(1024);
            while (true)
            {
                Socket clientSock = sock.Accept();
                byte[] buff = new byte[1];
                int received = clientSock.Receive(buff, 1, SocketFlags.None);
                if (received > 0)
                {
                    LogUtility.LogUtility.LogFile("received " + Convert.ToString(received), ModuleLogLevel);
                }
                if (buff[0] == 1)
                {
                    PackMode(clientSock);
                }
                else
                {
                    RawMode(clientSock);
                }
                LogUtility.LogUtility.LogFile("finished", ModuleLogLevel);
                args = new object[1];
                args[0] = "finished";
                Invoke(updateControls, args);
            }
        }
        private void buttonGo_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            FileStream fs = File.OpenRead(openFileDialog1.FileName);
            if (fs == null)
            {
                return;
            }
            fileBuff = new byte[fs.Length];
            fs.Read(fileBuff, 0, fileBuff.Length);
            fs.Close();
            current_offset = 0;
            
            ParameterizedThreadStart pts = new ParameterizedThreadStart(SenderThread);
            Thread thread = new Thread(pts);
            SenderThreadParam param = new SenderThreadParam();
            param.myIp = IPAddress.Parse(textBoxMyIp.Text);
            param.myPort = UInt16.Parse(textBoxMyPort.Text);
            thread.Start(param);
        }

        private void buttonClearChunks_Click(object sender, EventArgs e)
        {
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
        }
    }
}
