using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ProxyLib;
using System.Threading;
using System.Collections;
using System.IO;

namespace PackSenderProxyEmulator
{
    public partial class Statistics : UserControl
    {
        ulong totalReceived;
        ulong totalSent;
        ulong totalSaved;
        double savedPercentage;
        ArrayList urls;
        ArrayList debugInfo;
        object statisticsLock;
        bool isDebugMode;

        public Statistics()
        {
            InitializeComponent();
            totalReceived = 0;
            totalSent = 0;
            totalSaved = 0;
            savedPercentage = 0;
            statisticsLock = new object();
            urls = new ArrayList();
            debugInfo = new ArrayList();
            isDebugMode = true;
        }
        void UpdateCounters(object[] res)
        {
            urls.Add(res[0]);
            totalReceived += (uint)res[1];
            totalSent += (uint)res[2];
            totalSaved += (uint)res[3];
            if (isDebugMode)
            {
                debugInfo.Add(res[4]);
            }
        }
        void UpdateStatisticControls()
        {
            listBoxDebugInfo.Items.Clear();
            int idx = 0;
            foreach (string s in urls)
            {
                listBoxDebugInfo.Items.Add(s + debugInfo[idx++]);
            }
            labelTotalReceived.Text = Convert.ToString(totalReceived);
            labelTotalSent.Text = Convert.ToString(totalSent);
            labelTotalSaved.Text = Convert.ToString(totalSaved);
            labelPercentage.Text = Convert.ToString(savedPercentage);
            /*if (isDebugMode)
            {
                listBoxDebugInfo.Items.Clear();
                foreach (string d in debugInfo)
                {
                    listBoxDebugInfo.Items.Add(d);
                }
            }*/
        }

        void UpdatePercentage()
        {
            if (totalSent > 0)
            {
                savedPercentage = (double)totalSaved / (double)totalSent;
            }
        }

        delegate void UpdateStatisticControlsCbk();

        public void ProcessStatistics(object results)
        {
            object[] res = (Object[])results;
            Monitor.Enter(statisticsLock);
            UpdateCounters(res);
            UpdatePercentage();
            UpdateStatisticControlsCbk updateStatisticControls = new UpdateStatisticControlsCbk(UpdateStatisticControls);
            Invoke(updateStatisticControls);
            Monitor.Exit(statisticsLock);
        }
        public void ProcessStatistics(ArrayList al)
        {
            Monitor.Enter(statisticsLock);
            totalReceived = 0;
            totalSent = 0;
            totalSaved = 0;
            savedPercentage = 0;
            urls.Clear();
            if (isDebugMode)
            {
                debugInfo.Clear();
            }
            
            foreach (object[] res in al)
            {
                UpdateCounters(res);
            }
            UpdatePercentage();
            UpdateStatisticControls();
            Monitor.Exit(statisticsLock);
        }

        public bool DebugMode
        {
            set
            {
                if ((isDebugMode)&&(!value))
                {
                    //listBoxDebugInfo.Width = listBoxDebugInfo.Right - listBoxOutStandingUrls.Left;
                    listBoxDebugInfo.Visible = false;
                }
                isDebugMode = value;
            }
            get
            {
                return isDebugMode;
            }
        }

        void SaveToFile(string FileName)
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
            FileStream fs = File.OpenWrite(FileName);
            if (fs == null)
            {
                return;
            }
            string line;
            byte []bytes;
            int idx;
            for (idx = 0; idx < urls.Count; idx++)
            {
                line = "URL: " + urls[idx] + Environment.NewLine;
                bytes = Encoding.ASCII.GetBytes(line);
                fs.Write(bytes, 0, bytes.Length);
                line = "Totals: ";
                line += " Received " + Convert.ToString(totalReceived) + " Sent " + Convert.ToString(totalSent) + " saved " + Convert.ToString(totalSaved) + Environment.NewLine;
                bytes = Encoding.ASCII.GetBytes(line);
                fs.Write(bytes, 0, bytes.Length);
                if (isDebugMode)
                {
                    line = "Debug info: " + debugInfo[idx] + Environment.NewLine;
                    bytes = Encoding.ASCII.GetBytes(line);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            fs.Close();
        }

        private void buttonSaveAs_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            SaveToFile(saveFileDialog1.FileName);
        }
    }
}




