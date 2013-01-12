using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PackReceiverProxyEmulator
{
    public partial class SetRemoteIP : Form
    {
        public SetRemoteIP()
        {
            InitializeComponent();
        }
        public void SetRemoteIp(string ip)
        {
            textBoxRemoteIp.Text = ip;
        }
        public string GetRemoteIp()
        {
            return textBoxRemoteIp.Text;
        }
    }
}
