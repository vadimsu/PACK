using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProxyLib;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace PackSenderProxyEmulator
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        NotifyIcon m_NotifyIcon;
        ContextMenu m_ContextMenu;
        ulong totalReceived;
        ulong totalSent;
        ulong totalSaved;
        double savedPercentage;
        delegate void UpdateStatisticControlsCbk(object []res);
        UpdateStatisticControlsCbk m_UpdateStatisticControls;
        object m_StatisticsLock;
        public Form1()
        {
            InitializeComponent();
            radioButtonLogHigh3.Click += new EventHandler(radioButtonLogHigh3_Click);
            radioButtonLogHigh2.Click += new EventHandler(radioButtonLogHigh2_Click);
            radioButtonLogHigh.Click += new EventHandler(radioButtonLogHigh_Click);
            radioButtonLogMedium.Click += new EventHandler(radioButtonLogMedium_Click);
            radioButtonLogNone.Click += new EventHandler(radioButtonLogNone_Click);
            radioButtonHttp.Click += new EventHandler(radioButtonHttp_Click);
            radioButtonRaw.Click += new EventHandler(radioButtonRaw_Click);
            radioButtonPackHttp.Click += new EventHandler(radioButtonPackHttp_Click);
            radioButtonPackRaw.Click += new EventHandler(radioButtonPackRaw_Click);
            listenerList = new ArrayList();
            LogUtility.LogUtility.FileName = Environment.CurrentDirectory + "\\log.txt";
            LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH3);
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
            if (radioButtonHttp.Checked)
            {
                ProxyType = 1;
            }
            else if (radioButtonPackHttp.Checked)
            {
                ProxyType = 2;
            }
            else
            {
                ProxyType = 0;
            }
            statistics1.DebugMode = false/*true*/;
            m_ContextMenu = new ContextMenu();
            MenuItem []logLevelSubItems = new MenuItem[5];
            logLevelSubItems[0] = new MenuItem("None",OnSetLogLevelNone);
            logLevelSubItems[1] = new MenuItem("Medium",OnSetLogLevelMedium);
            logLevelSubItems[2] = new MenuItem("High",OnSetLogLevelHigh);
            logLevelSubItems[3] = new MenuItem("High2",OnSetLogLevelHigh2);
            logLevelSubItems[4] = new MenuItem("High3",OnSetLogLevelHigh3);
            MenuItem menuItem = new MenuItem("Log level", logLevelSubItems);
            m_ContextMenu.MenuItems.Add(menuItem);
            MenuItem[] modeSubItems = new MenuItem[4];
            modeSubItems[0] = new MenuItem("Raw", OnSetModeRaw);
            modeSubItems[1] = new MenuItem("Http", OnSetModeHttp);
            modeSubItems[2] = new MenuItem("Pack raw", OnSetModePackRaw);
            modeSubItems[3] = new MenuItem("Pack Http", OnSetModePackHttp);
            menuItem = new MenuItem("Mode", modeSubItems);
            m_ContextMenu.MenuItems.Add(menuItem);
            m_ContextMenu.MenuItems.Add("Start", buttonStart_Click);
            m_ContextMenu.MenuItems.Add("Stop", buttonStop_Click);
            m_ContextMenu.MenuItems.Add("Reset statitics", ResetStatistics);
            m_NotifyIcon = new NotifyIcon();
            m_NotifyIcon.Text = "Sender side proxy";
            m_NotifyIcon.ContextMenu = m_ContextMenu;
            m_NotifyIcon.Icon = new System.Drawing.Icon("Wi-Fi.ico");
            m_NotifyIcon.Visible = true;
            Visible = false;
            ShowInTaskbar = false;
            totalReceived = 0;
            totalSent = 0;
            totalSaved = 0;
            savedPercentage = 0;
            m_UpdateStatisticControls = new UpdateStatisticControlsCbk(ProcessStatistics);
            m_StatisticsLock = new object();
            ProxyType = 2;
            LogUtility.LogUtility.SetSilent(false);
        }
        void OnSetLogLevelNone(object sender, EventArgs e)
        {
            LogUtility.LogUtility.SetSilent(true);
        }
        void OnSetLogLevelMedium(object sender, EventArgs e)
        {
            LogUtility.LogUtility.SetSilent(false);
            LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        void OnSetLogLevelHigh(object sender, EventArgs e)
        {
            LogUtility.LogUtility.SetSilent(false);
            LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }
        void OnSetLogLevelHigh2(object sender, EventArgs e)
        {
            LogUtility.LogUtility.SetSilent(false);
            LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH2);
        }
        void OnSetLogLevelHigh3(object sender, EventArgs e)
        {
            LogUtility.LogUtility.SetSilent(false);
            LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH3);
        }
        void OnSetModeRaw(object sender, EventArgs e)
        {
            ProxyType = 0;
        }
        void OnSetModeHttp(object sender, EventArgs e)
        {
            ProxyType = 1;
        }
        void OnSetModePackRaw(object sender, EventArgs e)
        {
            ProxyType = 3;
        }
        void OnSetModePackHttp(object sender, EventArgs e)
        {
            ProxyType = 2;
        }
        void radioButtonLogHigh2_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogHigh2);
        }

        void radioButtonLogHigh3_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogHigh3);
        }

        void radioButtonPackRaw_Click(object sender, EventArgs e)
        {
            AdjustModeRadioButtons(radioButtonPackRaw);
        }

        void radioButtonPackHttp_Click(object sender, EventArgs e)
        {
            AdjustModeRadioButtons(radioButtonPackHttp);
        }

        void radioButtonRaw_Click(object sender, EventArgs e)
        {
            AdjustModeRadioButtons(radioButtonRaw);
        }

        void radioButtonHttp_Click(object sender, EventArgs e)
        {
            AdjustModeRadioButtons(radioButtonHttp);
        }
        void radioButtonLogNone_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogNone);
        }

        void radioButtonLogMedium_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogMedium);
        }

        void radioButtonLogHigh_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogHigh);
        }
        byte ProxyType;
        ArrayList listenerList;
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

        private void buttonStart_Click(object sender, EventArgs e)
        {
            Listener listener = new Listener(new IPEndPoint(GetLocalInternalIP(), UInt16.Parse(textBoxLocalPort.Text)),ProxyType);
            listener.SetRemoteEndpoint(new IPEndPoint(GetLocalInternalIP(),UInt16.Parse(textBoxRemotePort.Text)));
            listener.SetOnGotResults(new Listener.OnGotResultsCbk(OnGotResults));
            listenerList.Add(listener);
            listener.Start();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            foreach (Listener listener in listenerList)
            {
                listener.Stop();
            }
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        void AdjustModeRadioButtons(RadioButton clickedButton)
        {
            if (radioButtonHttp == clickedButton)
            {
                radioButtonHttp.Checked = true;
                radioButtonRaw.Checked = false;
                radioButtonPackHttp.Checked = false;
                radioButtonPackRaw.Checked = false;
                ProxyType = 1;
            }
            else if (radioButtonPackHttp == clickedButton)
            {
                radioButtonPackHttp.Checked = true;
                radioButtonRaw.Checked = false;
                radioButtonHttp.Checked = false;
                radioButtonPackRaw.Checked = false;
                ProxyType = 2;
            }
            else if (radioButtonPackRaw == clickedButton)
            {
                radioButtonPackRaw.Checked = true;
                radioButtonPackHttp.Checked = false;
                radioButtonRaw.Checked = false;
                radioButtonHttp.Checked = false;
                ProxyType = 3;
            }
            else
            {
                radioButtonHttp.Checked = false;
                radioButtonRaw.Checked = true;
                radioButtonPackHttp.Checked = false;
                radioButtonPackRaw.Checked = false;
                ProxyType = 0;
            }
        }

        private void radioButtonRaw_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void radioButtonHttp_CheckedChanged(object sender, EventArgs e)
        {
        }
        void AdjustLogRadioButtons(RadioButton clickedButton)
        {
            if (radioButtonLogNone == clickedButton)
            {
                radioButtonLogMedium.Checked = false;
                radioButtonLogHigh.Checked = false;
                radioButtonLogHigh2.Checked = false;
                radioButtonLogHigh3.Checked = false;
                radioButtonLogNone.Checked = true;
                LogUtility.LogUtility.SetSilent(true);
            }
            else if (radioButtonLogMedium == clickedButton)
            {
                radioButtonLogNone.Checked = false;
                radioButtonLogHigh.Checked = false;
                radioButtonLogHigh2.Checked = false;
                radioButtonLogHigh3.Checked = false;
                radioButtonLogMedium.Checked = true;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            }
            else if (radioButtonLogHigh == clickedButton)
            {
                radioButtonLogNone.Checked = false;
                radioButtonLogMedium.Checked = false;
                radioButtonLogHigh.Checked = true;
                radioButtonLogHigh2.Checked = false;
                radioButtonLogHigh3.Checked = false;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            else if (radioButtonLogHigh2 == clickedButton)
            {
                radioButtonLogNone.Checked = false;
                radioButtonLogMedium.Checked = false;
                radioButtonLogHigh.Checked = false;
                radioButtonLogHigh2.Checked = true;
                radioButtonLogHigh3.Checked = false;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH2);
            }
            else
            {
                radioButtonLogMedium.Checked = false;
                radioButtonLogNone.Checked = false;
                radioButtonLogHigh.Checked = false;
                radioButtonLogHigh2.Checked = false;
                radioButtonLogHigh3.Checked = true;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH3);
            }
        }
        private void radioButtonLogHigh_CheckedChanged(object sender, EventArgs e)
        {
            //AdjustLogRadioButtons();
        }

        private void radioButtonLogMedium_CheckedChanged(object sender, EventArgs e)
        {
            //AdjustLogRadioButtons();
        }

        private void radioButtonLogNone_CheckedChanged(object sender, EventArgs e)
        {
            //AdjustLogRadioButtons();
        }
        public void ProcessStatistics(object[] arg)
        {
            try
            {
                object[] res = (object[])arg;
                Monitor.Enter(m_StatisticsLock);
                totalReceived += (uint)res[1];
                totalSent += (uint)res[2];
                totalSaved += (uint)res[3];
                if (totalReceived > 0)
                {
                    savedPercentage = (double)totalSaved / (double)totalReceived;
                }
                //m_NotifyIcon.BalloonTipText = "Total: rx " + Convert.ToString(totalReceived) + " tx " + Convert.ToString(totalSent) + " saved " + Convert.ToString(totalSaved) + " Received " + Convert.ToString(res[1]) + " Sent " + Convert.ToString(res[2]) + " Saved " + Convert.ToString(res[3]);
                m_NotifyIcon.BalloonTipText = "Total: saved " + Convert.ToString(totalSaved) + " " + Convert.ToString(savedPercentage * 100) + "% Saved " + Convert.ToString(res[3] + " Total received " + Convert.ToString(totalReceived) + " Total sent " + Convert.ToString(totalSent));
                m_NotifyIcon.ShowBalloonTip(1000);
                Monitor.Exit(m_StatisticsLock);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        void ResetStatistics(object sender, EventArgs e)
        {
            try
            {
                Monitor.Enter(m_StatisticsLock);
                totalReceived = 0;
                totalSent = 0;
                totalSaved = 0;
                savedPercentage = 0;
                //m_NotifyIcon.BalloonTipText = "Total: rx " + Convert.ToString(totalReceived) + " tx " + Convert.ToString(totalSent) + " saved " + Convert.ToString(totalSaved) + " Received " + Convert.ToString(res[1]) + " Sent " + Convert.ToString(res[2]) + " Saved " + Convert.ToString(res[3]);
                Monitor.Exit(m_StatisticsLock);
            }
            catch(Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }
        void OnGotResults(object results)
        {
         //   statistics1.ProcessStatistics(results);
            return;
            try
            {
                object[] arg = new object[1];
                arg[0] = results;
                Invoke(m_UpdateStatisticControls, arg);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void buttonRefreshStatistics_Click(object sender, EventArgs e)
        {
            foreach (Listener l in listenerList)
            {
                StatisticsSnapShot statisticsForm = new StatisticsSnapShot();
                statisticsForm.ProcessStatistics(l.GetResults());
                statisticsForm.Show();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Shown += new EventHandler(Form1_Shown);
        }

        void Form1_Shown(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
