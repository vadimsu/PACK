using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace BrowerPackTest
{
    public partial class Form1 : Form
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_NONE;
        System.Timers.Timer m_ReloadTimer;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogUtility.LogUtility.FileName = Environment.CurrentDirectory + "\\log.txt";
            LogUtility.LogUtility.SetLevel(ModuleLogLevel);
            if (File.Exists(LogUtility.LogUtility.FileName))
            {
                File.Delete(LogUtility.LogUtility.FileName);
            }
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
            webBrowser1.DocumentTitleChanged += new EventHandler(webBrowser1_DocumentTitleChanged);
            webBrowser1.FileDownload += new EventHandler(webBrowser1_FileDownload);
            webBrowser1.Navigated += new WebBrowserNavigatedEventHandler(webBrowser1_Navigated);
            webBrowser1.Navigating += new WebBrowserNavigatingEventHandler(webBrowser1_Navigating);
            webBrowser1.ProgressChanged += new WebBrowserProgressChangedEventHandler(webBrowser1_ProgressChanged);
            m_ReloadTimer = new System.Timers.Timer(30000);
            m_ReloadTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_ReloadTimer_Elapsed);
            this.SetDesktopLocation(0,0);
            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            this.Width = Screen.PrimaryScreen.WorkingArea.Width;
            textBox1.Top = 0;
            textBox1.Left = 0;
            textBox1.Width = (this.Width / 4) * 3;
            textBox1.Height = this.Height / 20;
            buttonGo.Left = textBox1.Right;
            buttonGo.Top = textBox1.Top;
            buttonGo.Width = (this.Width / 8);
            buttonGo.Height = textBox1.Height;
            buttonDump.Left = buttonGo.Right;
            buttonDump.Top = textBox1.Top;
            buttonDump.Width = buttonGo.Width;
            buttonDump.Height = buttonGo.Height;
            webBrowser1.Top = textBox1.Bottom;
            webBrowser1.Left = 0;
            webBrowser1.Width = this.Width;
            webBrowser1.Height = this.Height - textBox1.Height;
            m_ReloadTimer.Start();
            HttpWebRequest.DefaultWebProxy = new WebProxy(GetLocalInternalIP().ToString(), 6666);
        }

        void m_ReloadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            GoToAddress();
            //m_ReloadTimer.Stop();
        }

        void webBrowser1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            LogUtility.LogUtility.LogFile("Current " + Convert.ToString(e.CurrentProgress) + " overall " + Convert.ToString(e.MaximumProgress), ModuleLogLevel);
        }

        void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            LogUtility.LogUtility.LogFile("navigating " + e.ToString() + " " + e.TargetFrameName + " " + e.Url.OriginalString, ModuleLogLevel);
        }

        void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            LogUtility.LogUtility.LogFile("navigated " + e.ToString() + " " + e.Url.OriginalString, ModuleLogLevel);
        }

        void webBrowser1_FileDownload(object sender, EventArgs e)
        {
            LogUtility.LogUtility.LogFile("file download " + e.ToString(), ModuleLogLevel);
        }

        void webBrowser1_DocumentTitleChanged(object sender, EventArgs e)
        {
            LogUtility.LogUtility.LogFile("doc title changed " + e.ToString(), ModuleLogLevel);
        }

        void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            LogUtility.LogUtility.LogFile(" doc completed " + e.ToString() + " " + e.Url.OriginalString, ModuleLogLevel);
            if (e.Url.OriginalString == textBox1.Text)
            {
                GoToAddress();
            }
        }
        void GoToAddress()
        {
            try
            {
#if false
                HttpWebRequest httpWebReq = (HttpWebRequest)HttpWebRequest.Create(textBox1.Text);
                HttpWebRequest.DefaultWebProxy = new WebProxy(GetLocalInternalIP().ToString(), 6666);
                HttpWebResponse webResponse = (HttpWebResponse)httpWebReq.GetResponse();
                //textBox1.Text = "Got response";
                webBrowser1.DocumentStream = webResponse.GetResponseStream();
                LogUtility.LogUtility.LogFile(webBrowser1.DocumentText, LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
#else
                try
                {
                    Uri uri = new Uri(textBox1.Text);
                    webBrowser1.Navigate(uri);
                }
                catch (Exception excs)
                {
                    MessageBox.Show(excs.Message);
                }
#endif
            }
            catch (WebException exc)
            {
                //textBox1.Text = exc.Message;
            }
        }
        private void buttonGo_Click(object sender, EventArgs e)
        {
            GoToAddress();
#if false
            try
            {
                webBrowser1.Navigate(textBox1.Text);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(exc.Message, ModuleLogLevel);
            }
#endif
        }

        private void buttonDump_Click(object sender, EventArgs e)
        {
            string str = webBrowser1.DocumentText;
            LogUtility.LogUtility.LogFile(str, LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
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
    }
}
