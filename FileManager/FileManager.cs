using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ChunkChainDataTypes;
using System.Threading;

namespace FileManager
{
    public enum FileTypes
    {
        DUMMY_FILE_TYPE,
        CHUNK_CTRL_FILE_TYPE,
        CHUNK_DATA_FILE_TYPE,
        CHAIN_FILE_TYPE
    };
    public abstract class FileManager
    {
        protected static Dictionary<long, byte[]> chunkCBfileMap;
        protected static Dictionary<long, byte []> chainfileMap;
        protected static long LastFileId;
        protected static List<string> allChunkDataFiles;
        protected static List<string> allChunkCtrlFiles;
        protected static List<string> allChainFiles;
        protected const long FileMaxSize = 1024 * 1000;
        protected static bool GlobalsInitiated = false;
        protected static uint RefCount = 0;

        protected static object chunkDataFilesMutex;
        protected static object chunkCtrlFilesMutex;
        protected static object chainFilesMutex;
        protected static string WorkingDirectory;

        protected static void InitGlobals(string workingDirectory)
        {
            chunkCtrlFilesMutex = new object();
            chunkDataFilesMutex = new object();
            chainFilesMutex = new object();
            allChunkDataFiles = new List<string>();
            allChunkCtrlFiles = new List<string>();
            allChainFiles = new List<string>();
            WorkingDirectory = workingDirectory;
            LastFileId = 0;
            GlobalsInitiated = true;
            chunkCBfileMap = new Dictionary<long, byte[]>();
            chainfileMap = new Dictionary<long, byte []>();
        }
        public FileManager()
        {
        }
        public string[] GetAllChunkDataFiles()
        {
            return allChunkDataFiles.ToArray();
        }

        public string[] GetAllChunkCtrlFiles()
        {
            return allChunkCtrlFiles.ToArray();
        }
        protected string GetFilePathById(long fileId)
        {
            string filePath;
#if false
            if (fileMap.TryGetValue(fileId, out filePath))
            {
                return filePath;
            }
#endif
            return null;
        }

        public abstract byte[] GetChunk(FileAndOffset fo, uint size);
        public abstract void AddNewChunk(byte[] data, uint offset, uint size, long chunkId, out FileAndOffset fo);
        public abstract long AddUpdateChainFile(byte[] data, long chainId);
        public abstract void AddUpdateChunkCtrlFile(long chunkId, byte[] buf, uint offset, uint size);
        public abstract bool ReadChainFile(long chainId, out byte[] buff);
        public abstract byte[] ReadFile(long chunkId);
        
        public static string FileType2Extension(byte fileType)
        {
            string ext;
            switch (fileType)
            {
                case (byte)FileTypes.CHAIN_FILE_TYPE:
                    ext = ".chain";
                    break;
                case (byte)FileTypes.CHUNK_CTRL_FILE_TYPE:
                    ext = ".chunkctrl";
                    break;
                case (byte)FileTypes.CHUNK_DATA_FILE_TYPE:
                    ext = ".chunkdata";
                    break;
                default:
                    ext = "";
                    break;
            }
            return ext;
        }

        public static string LongName2StringName(long fileId,byte fileType)
        {
            return Convert.ToString(fileId) + FileType2Extension(fileType);
        }
        public static long StringName2LongName(string FileName)
        {
            int idx = FileName.IndexOf(".");
            if (idx == -1)
            {
                return 0;
            }
            return Convert.ToInt64(FileName.Substring(0,idx));
        }
        public static byte GetFileType(string FileName)
        {
            int idx = FileName.IndexOf(".");
            if (idx == -1)
            {
                return (byte)FileTypes.DUMMY_FILE_TYPE;
            }
            switch (FileName.Substring(idx))
            {
                case "chunkdata":
                    return (byte)FileTypes.CHUNK_DATA_FILE_TYPE;
                case "chunkctrl":
                    return (byte)FileTypes.CHUNK_CTRL_FILE_TYPE;
                case "chain":
                    return (byte)FileTypes.CHAIN_FILE_TYPE;
                default:
                    return (byte)FileTypes.DUMMY_FILE_TYPE;
            }
        }
    }
}
