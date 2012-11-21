using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ChunkChainDataTypes;
using ReceiverPackLib;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace PackReceiverTest
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        public Form1()
        {
            InitializeComponent();
        }

        ReceiverPackLib.ReceiverPackLib receiverPackLib;
        String FileName;
        bool StopFlag;
        UpdateStatus updateStatus;
        UpdateControls updateControls;
        bool TransactionEnded;
        uint BytesSent;
        uint BytesReceived;
        uint DataWritten;

        void OnMsgRead4Tx(object param, byte[] msg)
        {
            Socket sock = (Socket)param;
            object []args;

            try
            {
                int sent = sock.Send(msg);
                BytesSent += (uint)sent;
                LogUtility.LogUtility.LogFile("Sending PRED " + Convert.ToString(msg.Length), ModuleLogLevel);
            }
            catch (SocketException se)
            {
                args = new object[1];
                args[0] = se.Message;
                Invoke(updateStatus, args);
            }
        }

        void OnDataReceived(byte[] data,int offset,int length)
        {
            object[] args;
            try
            {
                FileStream fs = File.Open(FileName, FileMode.Append, FileAccess.Write);
                fs.Write(data, offset, length);
                DataWritten += (uint)data.Length;
                fs.Flush();
                fs.Close();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
                MessageBox.Show(exc.Message);
            }
            args = new object[1];
            args[0] = "receiver: block written " + Convert.ToString(data.Length);
            Invoke(updateStatus, args);
            LogUtility.LogUtility.LogFile("OnDataReceived " + Convert.ToString(data.Length) + " overall " + Convert.ToString(DataWritten), ModuleLogLevel);
        }
        void OnTransationEnd(object param)
        {
            LogUtility.LogUtility.LogFile("OnTransactionEnd " + Convert.ToString(DataWritten), ModuleLogLevel);
            TransactionEnded = true;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            FileName = "";
            StopFlag = false;
            OnData onData = new OnData(OnDataReceived);
            OnMessageReadyToTx onMsgRead4Tx = new OnMessageReadyToTx(OnMsgRead4Tx);
            OnEnd onEnd = new OnEnd(OnTransationEnd);
            try
            {
                receiverPackLib = new ReceiverPackLib.ReceiverPackLib(onData, onEnd,null,onMsgRead4Tx);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Fatal");
            }
            updateStatus = new UpdateStatus(Update_Status);
            updateControls = new UpdateControls(Update_Controls);
            Disposed += new EventHandler(Form1_Disposed);
        }

        void Form1_Disposed(object sender, EventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        struct ReceiverThreadParam
        {
            public IPAddress remoteIp;
            public UInt16 remotePort;
        };
        delegate void UpdateStatus(string status);
        void Update_Status(string status)
        {
            labelStatus.Text = status;
        }
        delegate void UpdateControls(bool first_iteration, ulong ticks, ulong iterations,uint bytes_received,uint bytes_sent);
        void Update_Controls(bool first_iteration, ulong ticks, ulong iterations, uint bytes_received, uint bytes_sent)
        {
            if (first_iteration)
            {
                labelFirstAttempt.Text = Convert.ToString(ticks);
                labelFirstAttemptReceived.Text = Convert.ToString(bytes_received);
                labelFirstAttemptSent.Text = Convert.ToString(bytes_sent);
            }
            else
            {
                labelAverage.Text = Convert.ToString(ticks);
                labelAverageReceived.Text = Convert.ToString(bytes_received);
                labelAverageSent.Text = Convert.ToString(bytes_sent);
            }
            labelAttempts.Text = Convert.ToString(iterations);
        }
        void PackMode(Socket sock)
        {
            int dummy_iterations = 0;
            object[] args;
            byte[] buff;

            TransactionEnded = false;
            DataWritten = 0;
            while (sock.Connected && (!TransactionEnded))
            {
#if false
                if (sock.Available == 0)
                {
                    if (dummy_iterations == 500)
                    {
                        args = new object[1];
                        args[0] = "dummy";
                        Invoke(updateStatus, args);
                        LogUtility.LogUtility.LogFile("dummy");
                        break;
                    }
                    dummy_iterations++;
                    Thread.Sleep(100);
                    continue;
                }
                dummy_iterations = 0;
#endif
                buff = new byte[sock.Available];
                try
                {
                    LogUtility.LogUtility.LogFile("Available " + Convert.ToString(sock.Available), ModuleLogLevel);
                    int received = sock.Receive(buff);
                    BytesReceived += (uint)received;
                    if (received > 0)
                    {
                        LogUtility.LogUtility.LogFile("Received " + Convert.ToString(received), ModuleLogLevel);
                        receiverPackLib.OnDataByteMode(buff, 0);
                    }
                }
                catch (SocketException se)
                {
                    args = new object[1];
                    args[0] = se.Message;
                    Invoke(updateStatus, args);
                    break;
                }
            }
        }
        
        void RawMode(Socket sock)
        {
            int dummy_delay = 0;
            while (sock.Connected)
            {
                byte[] buff;
                if (sock.Available > 0)
                {
                    dummy_delay = 0;
                    buff = new byte[sock.Available];
                    int received = sock.Receive(buff);
                    BytesReceived += (uint)received;
                    if (received == buff.Length)
                    {
                        OnDataReceived(buff,0,buff.Length);
                        LogUtility.LogUtility.LogFile("Received in raw mode " + Convert.ToString(buff.Length), ModuleLogLevel);
                    }
                    else
                    {
                        LogUtility.LogUtility.LogFile("Broken", ModuleLogLevel);
                        break;
                    }
                }
                else
                {
                    Thread.Sleep(1);
                    dummy_delay++;
                }
                if (dummy_delay > 10)
                {
                    break;
                }
            }
        }
        void ReceiverThread(object obj)
        {
            ReceiverThreadParam param = (ReceiverThreadParam)obj;
            ulong iterations_counter = 0;
            ulong ticks_sum = 0;
            object[] args;

            BytesReceived = 0;
            BytesSent = 0;

            while (!StopFlag)
            {
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
                byte []buff;
                
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (sock == null)
                {
                    return;
                }
                receiverPackLib.SetOnMsgReady4TxParam(sock);
                try
                {
                    sock.Connect(param.remoteIp, param.remotePort);
                    LogUtility.LogUtility.LogFile("Connected", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    buff = new byte[1];
                    buff[0] = (checkBoxPackMode.Checked) ? (byte)1 : (byte)0;
                    sock.Send(buff);
                    LogUtility.LogUtility.LogFile("Sent " + Convert.ToString(buff.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                catch (SocketException se)
                {
                    args = new object[1];
                    args[0] = se.Message;
                    Invoke(updateStatus, args);
                    continue;
                }
                DateTime startTime = DateTime.Now;
                if (checkBoxPackMode.Checked)
                {
                    PackMode(sock);
                }
                else
                {
                    RawMode(sock);
                }
                DateTime finishTime = DateTime.Now;
                LogUtility.LogUtility.LogFile("Finished " + Convert.ToString(finishTime.Ticks - startTime.Ticks), ModuleLogLevel);
                args = new object[5];
                
                if (iterations_counter == 0)
                {
                    args[0] = true;
                    args[1] = (ulong)(finishTime.Ticks - startTime.Ticks);
                    args[3] = (uint)BytesReceived;
                    args[4] = (uint)BytesSent;
                    BytesReceived = 0;
                    BytesSent = 0;
                }
                else
                {
                    args[0] = false;
                    ticks_sum += (ulong)(finishTime.Ticks - startTime.Ticks);
                    args[1] = ticks_sum / (iterations_counter + 1);
                    args[3] = (uint)(BytesReceived / iterations_counter);
                    args[4] = (uint)(BytesSent / iterations_counter);
                }
                args[2] = iterations_counter + 1;
                Invoke(updateControls, args);
                iterations_counter++;
                sock.Close();
                sock = null;
                buff = null;
                receiverPackLib.Reset();
            }
        }
        private void buttonChooseFile_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            FileName = saveFileDialog1.FileName;
            ParameterizedThreadStart pts = new ParameterizedThreadStart(ReceiverThread);
            Thread thread = new Thread(pts);
            ReceiverThreadParam param = new ReceiverThreadParam();
            param.remoteIp = IPAddress.Parse(textBoxRemoteIp.Text);
            param.remotePort = UInt16.Parse(textBoxRemotePort.Text);
            thread.Start(param);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            StopFlag = true;
        }

        private void buttonClearChunks_Click(object sender, EventArgs e)
        {
            string []files = Directory.GetFiles("datafiles");
            if (files == null)
            {
                return;
            }
            foreach (string file in files)
            {
                File.Delete(file);
            }
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
        }
    }
}
