using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ChunkChainDataTypes;
using System.Threading;

namespace FileManager
{
    public class NonRamFileManager : FileManager
    {
        static byte[] GetArraySection(byte[] arr, int offset, int size)
        {
            byte[] buff = new byte[size];
            int idx;
            for (idx = 0; idx < size; idx++)
            {
                buff[idx] = arr[idx + offset];
            }
            return buff;
        }
        public override void Restart()
        {
            try
            {
                GlobalsInitiated = false;
                RefCount--;
                allChunkDataFiles.Clear();
                Monitor.Enter(chainFilesMutex);
                chainfileMap.Clear();
                Monitor.Exit(chainFilesMutex);
                Monitor.Enter(chunkCtrlFilesMutex);
                chunkCBfileMap.Clear();
                Monitor.Exit(chunkCtrlFilesMutex);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        protected static void Init(string workingDirectory)
        {
            string[] allFilePaths;
            long temp = 0;
            FileStream fs;
            long key;
            uint bufSize;
            byte[] buff;
            int idx;

            RefCount++;

            if (GlobalsInitiated)
            {
                return;
            }

            InitGlobals(workingDirectory);

            if (String.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }
            
            allFilePaths = Directory.GetFiles(WorkingDirectory);
            
            foreach (string file in allFilePaths)
            {
                string filename = file.Substring(file.LastIndexOf("\\") + "\\".Length);
                if (filename.IndexOf(".chunkdata") >= 0)
                {
                    temp = Convert.ToInt64(filename.Substring(0, filename.IndexOf(".chunkdata")));
                    allChunkDataFiles.Add(file);
                    if (temp > LastFileId)
                    {
                        LastFileId = temp;
                    }
                }
                else if (filename.IndexOf(".chunkctrl") >= 0)
                {
                    //temp = Convert.ToInt64(filename.Substring(0, filename.IndexOf(".chunkctrl")));
                    //allChunkCtrlFiles.Add(file);
                    //if (temp > LastFileId)
                    //{
                    //    LastFileId = temp;
                    //}
                    fs = File.Open(WorkingDirectory + "\\" + filename,FileMode.Open,FileAccess.Read);
                    buff = new byte[fs.Length];
                    fs.Read(buff,0,buff.Length);
                    fs.Close();
                    idx = 0;
                    while (idx < buff.Length)
                    {
                        key = BitConverter.ToInt64(buff, idx);
                        idx += 8;
                        bufSize = BitConverter.ToUInt32(buff, idx);
                        idx += 4;
                        chunkCBfileMap.Add(key,GetArraySection(buff,idx,(int)bufSize));
                        idx += (int)bufSize;
                    }
                }
                else if (filename.IndexOf(".chain") >= 0)
                {
                    //temp = Convert.ToInt64(filename.Substring(0, filename.IndexOf(".chain")));
                    //allChainFiles.Add(file);
                    //if (temp > LastFileId)
                    //{
                    //    LastFileId = temp;
                    //}
                    fs = File.Open(WorkingDirectory + "\\" +  filename, FileMode.Open, FileAccess.Read);
                    buff = new byte[fs.Length];
                    fs.Read(buff, 0, buff.Length);
                    fs.Close();
                    idx = 0;
                    while (idx < buff.Length)
                    {
                        key = BitConverter.ToInt64(buff, idx);
                        idx += 8;
                        bufSize = BitConverter.ToUInt32(buff, idx);
                        idx += 4;
                        chainfileMap.Add(key, GetArraySection(buff, idx, (int)bufSize));
                        if (key >= LastFileId)
                        {
                            LastFileId = key + 1;
                        }
                        idx += (int)bufSize;
                    }
                }
            }
        }
        public NonRamFileManager(string workingDirectory)
        {
            Init(workingDirectory);
        }

        ~NonRamFileManager()
        {
            RefCount--;
            if (RefCount == 0)
            {
                OnExiting();
            }
        }

        public override byte[] GetChunk(FileAndOffset fo, uint size)
        {
            string file;
            byte[] buf = null;
            file = Convert.ToString(fo.file);
            file += FileType2Extension((byte)FileTypes.CHUNK_DATA_FILE_TYPE);
            file = WorkingDirectory + "\\" + file;
            try
            {
                Monitor.Enter(chunkDataFilesMutex);

                FileStream fs = File.OpenRead(file);
                if (fs.Length <= fo.offset)
                {
                    fs.Close();
                    Monitor.Exit(chunkDataFilesMutex);
                    return null;
                }
                buf = new byte[size];
                fs.Position = fo.offset;
                fs.Read(buf, 0, buf.Length);
                fs.Close();
                Monitor.Exit(chunkDataFilesMutex);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            return buf;
        }

        public override void AddNewChunk(byte[] data, uint offset, uint size, long chunkId, out FileAndOffset fo)
        {
            string file = "";
            FileStream fs;

            try
            {
                Monitor.Enter(chunkDataFilesMutex);
                if (allChunkDataFiles.Count > 0)
                {
                    int idx;
                    for (idx = 0; idx < allChunkDataFiles.Count; idx++)
                    {
                        if (File.Exists(allChunkDataFiles[idx]))
                        {
                            FileInfo fileInfo = new FileInfo(allChunkDataFiles[idx]);
                            if ((fileInfo.Length + size) < FileMaxSize)
                            {
                                file = allChunkDataFiles[idx];
                                break;
                            }
                        }
                    }
                }
                if (file == "")
                {
                    file = Convert.ToString(++LastFileId);
                    file += FileType2Extension((byte)FileTypes.CHUNK_DATA_FILE_TYPE);
                    file = WorkingDirectory + "\\" + file;
                    LogUtility.LogUtility.LogFile("new file " + file, LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    allChunkDataFiles.Add(file);
                    fs = File.Create(file);
                    fo.file = LastFileId;
                    fo.offset = 0;
                }
                else
                {
                    int slash_idx;
                    string filename2 = "";

                    //slash_idx = file.IndexOf("\\");
                    slash_idx = file.LastIndexOf("\\");
                    if (slash_idx == -1)
                    {
                        filename2 = file;
                    }
                    else
                    {
                        filename2 = file.Substring(slash_idx + 1);
                    }
                    fs = File.Open(file, FileMode.Append, FileAccess.Write);
                    fo.file = StringName2LongName(filename2);
                    fo.offset = fs.Position;
                }
                fs.Write(data, (int)offset, (int)size);
                fs.Flush();
                fs.Close();
                Monitor.Exit(chunkDataFilesMutex);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION IN AddNewChunk " + file + " " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                fo.file = 0;
                fo.offset = 0;
            }
        }
        public override long AddUpdateChainFile(byte[] data, long chainId)
        {
            string file = "";
            long chainId2Return = 0;

            try
            {
                Monitor.Enter(chainFilesMutex);
                
                file += FileType2Extension((byte)FileTypes.CHAIN_FILE_TYPE);
                file = WorkingDirectory + "\\" + file;
                byte[] existingBuf;
                if((chainId != 0)&&(chainfileMap.TryGetValue(chainId, out existingBuf)))
                {
                    chainfileMap.Remove(chainId);
                    chainId2Return = chainId;
                }
                else if (chainId == 0)
                {
                    chainId2Return = ++LastFileId;
                }
                chainfileMap.Add(chainId2Return, data);
                Monitor.Exit(chainFilesMutex);
                return chainId2Return;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return 0;
            }
        }

        public override void AddUpdateChunkCtrlFile(long chunkId, byte[] buf, uint offset, uint size)
        {
            string file ="";
            //file = Convert.ToString(chunkId);
            file += FileType2Extension((byte)FileTypes.CHUNK_CTRL_FILE_TYPE);
            file = WorkingDirectory + "\\" + file;
            try
            {
                Monitor.Enter(chunkCtrlFilesMutex);
                byte[] existingBuf;
                if (chunkCBfileMap.TryGetValue(chunkId, out existingBuf))
                {
                    chunkCBfileMap.Remove(chunkId);
                }
                chunkCBfileMap.Add(chunkId, buf);
                Monitor.Exit(chunkCtrlFilesMutex);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }

        public override bool ReadChainFile(long chainId, out byte[] buff)
        {
            string file = WorkingDirectory + "\\" + LongName2StringName(chainId, (byte)FileTypes.CHAIN_FILE_TYPE);
            try
            {
                Monitor.Enter(chainFilesMutex);
                if (!chainfileMap.TryGetValue(chainId, out buff))
                {
                    Monitor.Exit(chainFilesMutex);
                    return false;
                }
                Monitor.Exit(chainFilesMutex);
                return true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                buff = null;
                return false;
            }
        }
        public override byte[] ReadFile(long chunkId)
        {
            try
            {
                Monitor.Enter(chunkCtrlFilesMutex);
                byte[] existingBuf;
                if(chunkCBfileMap.TryGetValue(chunkId, out existingBuf))
                {
                    Monitor.Exit(chunkCtrlFilesMutex);
                    return existingBuf;
                }
                Monitor.Exit(chunkCtrlFilesMutex);
                return null;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return null;
            }
        }

        public static void OnExiting()
        {
            try
            {
                LogUtility.LogUtility.LogFile("Entering OnExiting ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                string file = "";
                file += FileType2Extension((byte)FileTypes.CHAIN_FILE_TYPE);
                file = WorkingDirectory + "\\" + file;
                Monitor.Enter(chainFilesMutex);
                FileStream fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite);
                LogUtility.LogUtility.LogFile("writing chains ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                foreach (KeyValuePair<long, byte[]> kvp in chainfileMap)
                {
                    byte[] buff = BitConverter.GetBytes(kvp.Key);
                    fs.Write(buff, 0, buff.Length);
                    buff = BitConverter.GetBytes(kvp.Value.Length);
                    LogUtility.LogUtility.LogFile("key " + Convert.ToString(kvp.Key) + " len " + Convert.ToString(kvp.Value.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    fs.Write(buff, 0, buff.Length);
                    fs.Write(kvp.Value, 0, kvp.Value.Length);
                }
                fs.Close();
                Monitor.Exit(chainFilesMutex);
                file = "";
                file += FileType2Extension((byte)FileTypes.CHUNK_CTRL_FILE_TYPE);
                file = WorkingDirectory + "\\" + file;
                Monitor.Enter(chunkCtrlFilesMutex);
                fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite);
                LogUtility.LogUtility.LogFile("writing chunks ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                foreach (KeyValuePair<long, byte[]> kvp in chunkCBfileMap)
                {
                    byte[] buff = BitConverter.GetBytes(kvp.Key);
                    fs.Write(buff, 0, buff.Length);
                    buff = BitConverter.GetBytes(kvp.Value.Length);
                    LogUtility.LogUtility.LogFile("key " + Convert.ToString(kvp.Key) + " len " + Convert.ToString(kvp.Value.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    fs.Write(buff, 0, buff.Length);
                    fs.Write(kvp.Value, 0, kvp.Value.Length);
                }
                fs.Close();
                Monitor.Exit(chunkCtrlFilesMutex);
                LogUtility.LogUtility.LogFile("Leaving OnExiting ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
    }
}
