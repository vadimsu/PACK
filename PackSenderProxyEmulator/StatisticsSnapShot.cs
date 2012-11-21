using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace PackSenderProxyEmulator
{
    public partial class StatisticsSnapShot : Form
    {
        public StatisticsSnapShot()
        {
            InitializeComponent();
        }

        public void ProcessStatistics(ArrayList al)
        {
            statistics1.ProcessStatistics(al);
        }
    }
}
