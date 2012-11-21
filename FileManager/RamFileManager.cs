using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ChunkChainDataTypes;
using System.Threading;

namespace FileManager
{
    public class RamFileManager : FileManager
    {
        static private Dictionary<string, MemoryStream> RamFileSystem;

        public RamFileManager(string workingDirectory)
        {
            Init(workingDirectory);
        }
        protected static void Init(string workingDirectory)
        {
            if (!GlobalsInitiated)
            {
                InitGlobals(workingDirectory);
                RamFileSystem = new Dictionary<string, MemoryStream>();
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
                MemoryStream stream;
                if (RamFileSystem.TryGetValue(file, out stream))
                {
                    LogUtility.LogUtility.LogFile("GetChunk: found " + file,LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    long pos = stream.Position;
                    stream.Position = fo.offset;
                    buf = new byte[size];
                    stream.Read(buf, 0, (int)size);
                    stream.Position = pos;
                    Monitor.Exit(chunkDataFilesMutex);
                    return buf;
                }
                Monitor.Exit(chunkDataFilesMutex);
                return null;
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
            MemoryStream stream = null;

            try
            {
                Monitor.Enter(chunkDataFilesMutex);
                if (allChunkDataFiles.Count > 0)
                {
                    int idx;
                    for (idx = 0; idx < allChunkDataFiles.Count; idx++)
                    {
                        if (RamFileSystem.TryGetValue(allChunkDataFiles[idx], out stream))
                        {
                            LogUtility.LogUtility.LogFile("AddNewChunk: found " + allChunkDataFiles[idx],LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                            if ((stream.Length + size) < FileMaxSize)
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
                    stream = new MemoryStream();
                    RamFileSystem.Add(file, stream);
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
                    LogUtility.LogUtility.LogFile("AddNewChunk2: found " + file + " " + filename2, LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    fo.file = StringName2LongName(filename2);
                    fo.offset = stream.Position;
                }
                stream.Write(data,(int)offset,(int)size);
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
            string file;
            long chainId2Return;

            try
            {
                Monitor.Enter(chainFilesMutex);
                if (chainId != 0)
                {
                    file = Convert.ToString(chainId);
                    chainId2Return = chainId;
                }
                else
                {
                    file = Convert.ToString(++LastFileId);
                    chainId2Return = LastFileId;
                }
                file += FileType2Extension((byte)FileTypes.CHAIN_FILE_TYPE);
                file = WorkingDirectory + "\\" + file;
                MemoryStream stream;
                if (RamFileSystem.TryGetValue(file, out stream))
                {
                    stream.Position = 0;
                    stream.Write(data, 0, data.Length);
                }
                else
                {
                    allChainFiles.Add(file);
                    stream = new MemoryStream();
                    stream.Write(data, 0, data.Length);
                    RamFileSystem.Add(file, stream);
                }
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
            string file;
            file = Convert.ToString(chunkId);
            file += FileType2Extension((byte)FileTypes.CHUNK_CTRL_FILE_TYPE);
            file = WorkingDirectory + "\\" + file;
            try
            {
                Monitor.Enter(chunkCtrlFilesMutex);
                MemoryStream stream;
                if (!RamFileSystem.TryGetValue(file, out stream))
                {
                    LogUtility.LogUtility.LogFile("AddNewChunkCtrlFile: NOT found " + file,LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    allChunkCtrlFiles.Add(file);
                    stream = new MemoryStream();
                    stream.Write(buf, (int)offset, (int)size);
                    RamFileSystem.Add(file, stream);
                }
                else
                {
                    LogUtility.LogUtility.LogFile("AddNewChunkCtrlFile: found " + file,LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    stream.Position = 0;
                    stream.Write(buf, (int)offset, (int)size);
                }
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
                MemoryStream stream;
                if (!RamFileSystem.TryGetValue(file, out stream))
                {
                    buff = null;
                    Monitor.Exit(chainFilesMutex);
                    return false;
                }
                long pos = stream.Position;
                stream.Position = 0;
                buff = new byte[stream.Length];
                stream.Read(buff, 0, (int)stream.Length);
                stream.Position = pos;
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
            string file = "";
            try
            {
                Monitor.Enter(chunkCtrlFilesMutex);
                MemoryStream stream;
                if (!RamFileSystem.TryGetValue(file, out stream))
                {
                    Monitor.Exit(chunkCtrlFilesMutex);
                    return null;
                }
                long pos = stream.Position;
                stream.Position = 0;
                byte[] buff = new byte[stream.Length];
                stream.Read(buff, 0, buff.Length);
                stream.Position = pos;
                Monitor.Exit(chunkCtrlFilesMutex);
                return buff;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return null;
            }
        }
    }
}
