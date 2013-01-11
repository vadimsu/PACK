using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProxyLib;
using System.IO;
using System.Collections;

namespace PackReceiverProxyEmulator
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        NotifyIcon m_NotifyIcon;
        ContextMenu m_ContextMenu;
        ulong total;
        ulong totalSaved;
        double savedPercentage;
        delegate void UpdateStatisticControlsCbk(object[] res);
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
            ProxyMode = 0;
            listenerList = new ArrayList();
            textBoxRemoteIp.Text = Convert.ToString(GetLocalInternalIP());
            LogUtility.LogUtility.FileName = Environment.CurrentDirectory + "\\log.txt";
            LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH3);
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
            Listener.InitGlobalObjects();
            statistics1.DebugMode = false/*true*/;
            m_ContextMenu = new ContextMenu();
            MenuItem[] logLevelSubItems = new MenuItem[5];
            logLevelSubItems[0] = new MenuItem("None", OnSetLogLevelNone);
            logLevelSubItems[1] = new MenuItem("Medium", OnSetLogLevelMedium);
            logLevelSubItems[2] = new MenuItem("High", OnSetLogLevelHigh);
            logLevelSubItems[3] = new MenuItem("High2", OnSetLogLevelHigh2);
            logLevelSubItems[4] = new MenuItem("High3", OnSetLogLevelHigh3);
            MenuItem menuItem = new MenuItem("Log level", logLevelSubItems);
            m_ContextMenu.MenuItems.Add(menuItem);
            MenuItem[] modeSubItems = new MenuItem[2];
            modeSubItems[0] = new MenuItem("Raw", OnSetModeRaw);
            modeSubItems[1] = new MenuItem("Http", OnSetModePack);
            menuItem = new MenuItem("Mode", modeSubItems);
            m_ContextMenu.MenuItems.Add(menuItem);
            m_ContextMenu.MenuItems.Add("Start", buttonStart_Click);
            m_ContextMenu.MenuItems.Add("Stop", buttonStop_Click);
            m_ContextMenu.MenuItems.Add("Flush", buttonFlush_Click);
            m_ContextMenu.MenuItems.Add("Reset statistics", ResetStatistics);
            m_NotifyIcon = new NotifyIcon();
            m_NotifyIcon.Text = "Receiver side proxy";
            m_NotifyIcon.ContextMenu = m_ContextMenu;
            m_NotifyIcon.Icon = new System.Drawing.Icon("BlackBerry-8830.ico");
            m_NotifyIcon.Visible = true;
            Visible = false;
            ShowInTaskbar = false;
            total = 0;
            totalSaved = 0;
            savedPercentage = 0;
            m_UpdateStatisticControls = new UpdateStatisticControlsCbk(ProcessStatistics);
            m_StatisticsLock = new object();
            LogUtility.LogUtility.SetSilent(false);
            ProxyMode = 1;
            this.Shown += new EventHandler(Form1_Shown);
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
            ProxyMode = 0;
        }
        void OnSetModePack(object sender, EventArgs e)
        {
            ProxyMode = 1;
        }

        void radioButtonLogHigh2_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogHigh2);
        }

        void radioButtonLogHigh3_Click(object sender, EventArgs e)
        {
            AdjustLogRadioButtons(radioButtonLogHigh3);
        }
        byte ProxyMode;
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
            ClientListener listener;

            listener = new ClientListener(new IPEndPoint(GetLocalInternalIP(), UInt16.Parse(textBoxLocalPort.Text)), new IPEndPoint(IPAddress.Parse(textBoxRemoteIp.Text), UInt16.Parse(textBoxPort.Text)), ProxyMode);
            Listener.OnGotResultsCbk onGotRes = new Listener.OnGotResultsCbk(OnGotResults);
            listener.SetOnGotResults(onGotRes);
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
            else if (radioButtonLogHigh2 == clickedButton)
            {
                radioButtonLogNone.Checked = false;
                radioButtonLogHigh.Checked = false;
                radioButtonLogHigh3.Checked = false;
                radioButtonLogMedium.Checked = false;
                radioButtonLogHigh2.Checked = true;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH2);
            }
            else if (radioButtonLogHigh3 == clickedButton)
            {
                radioButtonLogNone.Checked = false;
                radioButtonLogHigh.Checked = false;
                radioButtonLogHigh2.Checked = false;
                radioButtonLogMedium.Checked = false;
                radioButtonLogHigh3.Checked = true;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH3);
            }
            else
            {
                radioButtonLogMedium.Checked = false;
                radioButtonLogNone.Checked = false;
                radioButtonLogHigh.Checked = true;
                radioButtonLogHigh2.Checked = false;
                radioButtonLogHigh3.Checked = false;
                LogUtility.LogUtility.SetSilent(false);
                LogUtility.LogUtility.SetLevel(LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        private void radioButtonLogNone_CheckedChanged(object sender, EventArgs e)
        {
            //AdjustLogRadioButtons();
        }

        private void radioButtonLogMedium_CheckedChanged(object sender, EventArgs e)
        {
            //AdjustLogRadioButtons();
        }

        private void radioButtonLogHigh_CheckedChanged(object sender, EventArgs e)
        {
            //AdjustLogRadioButtons();
        }

        private void checkBoxPackMode_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPackMode.Checked)
            {
                ProxyMode = 1;
            }
            else
            {
                ProxyMode = 0;
            }
        }
        public void ProcessStatistics(object[] arg)
        {
            object[] res = (object[])arg;
            Monitor.Enter(m_StatisticsLock);
            total += (uint)res[0];
            totalSaved += (uint)res[1];
            if (total > 0)
            {
                savedPercentage = (double)totalSaved / (double)total;
            }
            //m_NotifyIcon.BalloonTipText = "Total " + Convert.ToString(total) + " tx " + Convert.ToString(total) + " saved " + Convert.ToString(totalSaved) + " Sent " + Convert.ToString(res[0]) + " Saved " + Convert.ToString(res[1]);
            //m_NotifyIcon.ShowBalloonTip(1000);
            Monitor.Exit(m_StatisticsLock);
        }
        void OnGotResults(object results)
        {
            //statistics1.ProcessStatistics(results);
            //object[] arg = new object[1];
            //arg[0] = results;
            //Invoke(m_UpdateStatisticControls, arg);
        }

        private void ResetStatistics(object sender, EventArgs e)
        {
            Monitor.Enter(m_StatisticsLock);
            total = 0;
            totalSaved =0;
            savedPercentage = 0;
            //m_NotifyIcon.BalloonTipText = "Total " + Convert.ToString(total) + " tx " + Convert.ToString(total) + " saved " + Convert.ToString(totalSaved) + " Sent " + Convert.ToString(res[0]) + " Saved " + Convert.ToString(res[1]);
            //m_NotifyIcon.ShowBalloonTip(1000);
            Monitor.Exit(m_StatisticsLock);
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

        private void buttonFlush_Click(object sender, EventArgs e)
        {
            ProxyLib.Proxy.Flush();
        }
        void Form1_Shown(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
