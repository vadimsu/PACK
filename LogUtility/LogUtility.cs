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
        static Queue logEntriesList = new Queue();
        static Thread logThread =  new Thread(new ThreadStart(LogThread));
        private static bool Silent = false;
        private static byte CurrentLevel = (byte)LogLevels.LEVEL_LOG_HIGH3;
        static Semaphore sm = new Semaphore(0,1);
        static Dictionary<string, Queue> separateLogQueues = new Dictionary<string,Queue>();
        static object separateQueuesLock = new object();

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
                sm.WaitOne();
                while (RetrieveEntryAndWrite(logEntriesList, FileName)) ;
                Monitor.Enter(separateQueuesLock);
                foreach (KeyValuePair<string, Queue> pair in separateLogQueues)
                {
                    while (RetrieveEntryAndWriteBinary(pair.Value, pair.Key)) ;
                }
                Monitor.Exit(separateQueuesLock);
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
            Queue.Synchronized(logEntriesList).Enqueue(entry);
            Monitor.Enter(logThread);
            if(!logThread.IsAlive)
            {
                logThread.Start();
            }
            Monitor.Exit(logThread);
            try
            {
                sm.Release();
            }
            catch
            {
            }
        }
        static bool RetrieveEntryAndWrite(Queue entriesList,string filename)
        {
            string entry = null;

            if (Queue.Synchronized(entriesList).Count != 0)
            {
                entry = (string)Queue.Synchronized(entriesList).Dequeue();
            }
            if (entry != null)
            {
                FileStream fs = File.Open(filename, FileMode.Append, FileAccess.Write);
                if (fs == null)
                {
                    return false;
                }
                int reallLength;
                byte[] buff = GetBytes(entry, out reallLength);
                fs.Write(buff, 0, reallLength);
                fs.Flush();
                fs.Close();
                entry = null;
                return true;
            }
            return false;
        }
        static bool RetrieveEntryAndWriteBinary(Queue entriesList, string filename)
        {
            byte []entry = null;

            if (Queue.Synchronized(entriesList).Count != 0)
            {
                entry = (byte [])Queue.Synchronized(entriesList).Dequeue();
            }
            if (entry != null)
            {
                FileStream fs = File.Open(filename, FileMode.Append, FileAccess.Write);
                if (fs == null)
                {
                    return false;
                }
                fs.Write(entry, 0, entry.Length);
                fs.Flush();
                fs.Close();
                entry = null;
                return true;
            }
            return false;
        }
        public static void LogBinary(string filename, byte[] data)
        {
            Queue queue;
            Monitor.Enter(separateQueuesLock);
            if (!separateLogQueues.TryGetValue(filename, out queue))
            {
                queue = new Queue();
                separateLogQueues.Add(filename, queue);
            }
            queue.Enqueue(data);
            Monitor.Exit(separateQueuesLock);
            try
            {
                sm.Release();
            }
            catch
            {
            }
        }
    }
}
