using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PackReceiverProxyEmulator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Form1());
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message + " " + exc.StackTrace);
            }
        }
    }
}
