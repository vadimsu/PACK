using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Threading;
using System.IO;

namespace PackReceiverProxyEmulator
{
    public partial class Statistics : UserControl
    {
        ulong totalReceived;
        ulong totalSent;
        ulong totalSaved;
        double savedPercentage;
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
            debugInfo = new ArrayList();
            statisticsLock = new object();
            isDebugMode = true;
        }
        void UpdateCounters(object[] res)
        {
            totalSent += (uint)Convert.ToUInt32(res[0]);
            totalSaved += (uint)res[1];
            if (isDebugMode)
            {
                debugInfo.Add(res[2]);
            }
        }
        void UpdateStatisticControls()
        {
            labelTotalReceived.Text = Convert.ToString(totalReceived);
            labelTotalSent.Text = Convert.ToString(totalSent);
            labelTotalSaved.Text = Convert.ToString(totalSaved);
            labelPercentage.Text = Convert.ToString(savedPercentage);
            if (isDebugMode)
            {
                listBoxDebugInfo.Items.Clear();
                foreach (string s in debugInfo)
                {
                    listBoxDebugInfo.Items.Add(s);
                }
            }
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
            Monitor.Enter(statisticsLock);
            UpdateCounters((object [])results);
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
                if ((isDebugMode) && (!value))
                {
                    //this.Width -= listBoxDebugInfo.Width;
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
            byte[] bytes;
            int idx;
            line = "Totals: ";
            line += " Received " + Convert.ToString(totalReceived) + " Sent " + Convert.ToString(totalSent) + " saved " + Convert.ToString(totalSaved) + Environment.NewLine;
            bytes = Encoding.ASCII.GetBytes(line);
            fs.Write(bytes, 0, bytes.Length);
            if (isDebugMode)
            {
                for (idx = 0; idx < debugInfo.Count; idx++)
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
