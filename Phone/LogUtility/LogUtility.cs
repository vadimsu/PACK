using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Threading;

namespace LogUtility
{
    public enum LogLevels
    {
        LEVEL_LOG_NONE,
        LEVEL_LOG_MEDIUM,
        LEVEL_LOG_HIGH,
        LEVEL_LOG_HIGH2,
        LEVEL_LOG_HIGH3
    };
    public class LogUtility
    {
        public static string FileName = "log.txt";
        public static bool IsSilent = false;
        static Queue<string> logEntriesList = new Queue<string>();
        static Thread logThread = null;
        private static Object thisLock = new Object();
        private static bool Silent = false;
        private static byte CurrentLevel = (byte)LogLevels.LEVEL_LOG_HIGH3;
        private static byte[] GetBytes(string str, out int reallLength)
        {
            char[] charr = (str + Environment.NewLine).ToCharArray();
            byte[] buff = new byte[charr.Length * 2];
            reallLength = 0;

            foreach (char c in charr)
            {
                byte[] temp = BitConverter.GetBytes(c);
                temp.CopyTo(buff, reallLength);
                reallLength += temp.Length;
            }
            return buff;
        }
        static void LogThread()
        {
            while (true)
            {
                RetrieveEntryAndWrite();
            }
        }
        public static void SetSilent(bool isSilent)
        {
            Silent = isSilent;
        }
        public static void SetLevel(LogLevels level)
        {
            CurrentLevel = (byte)level;
        }
        public static void Stop()
        {
            if (logThread != null)
            {
                logThread.Abort();
            }
        }
        public static void LogFile(string entry, LogLevels level)
        {
            if (Silent)
            {
                return;
            }
            if (CurrentLevel > (byte)level)
            {
                return;
            }
            entry = DateTime.Now.TimeOfDay.ToString() + " " + Convert.ToString(Thread.CurrentThread.ManagedThreadId) + " " + entry;
            //Queue<string>.Synchronized(logEntriesList).Enqueue(entry);
            if (logThread == null)
            {
                ThreadStart ts = new ThreadStart(LogThread);
                logThread = new Thread(ts);
                logThread.Start();
            }
        }
        static void RetrieveEntryAndWrite()
        {
            string entry = null;

            //if (Queue.Synchronized(logEntriesList).Count != 0)
            {
              //  entry = (string)Queue.Synchronized(logEntriesList).Dequeue();
            }
            if (entry != null)
            {
                FileStream fs = File.Open(FileName, FileMode.Append, FileAccess.Write);
                if (fs == null)
                {
                    return;
                }
                int reallLength;
                byte[] buff = GetBytes(entry, out reallLength);
                fs.Write(buff, 0, reallLength);
                fs.Flush();
                fs.Close();
                entry = null;
            }
            else
            {
                Thread.Sleep(1);
            }
        }
    }
}
