using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ChunkChainDataTypes;

namespace FileManager
{
    public class PhoneFileManager : FileManager
    {
        public PhoneFileManager(string workingDirectory)
        {
            throw new NotImplementedException();
        }
        public override byte[] GetChunk(FileAndOffset fo, uint size)
        {
            throw new NotImplementedException();
            return null;
        }
        public override void AddNewChunk(byte[] data, uint offset, uint size, long chunkId, out FileAndOffset fo)
        {
            throw new NotImplementedException();
            fo.file = 0;
            fo.offset = 0;
        }
        public override long AddUpdateChainFile(byte[] data, long chainId)
        {
            throw new NotImplementedException();
            return 0;
        }
        public override void AddUpdateChunkCtrlFile(long chunkId, byte[] buf, uint offset, uint size)
        {
        }
        public override bool ReadChainFile(long chainId, out byte[] buff)
        {
            throw new NotImplementedException();
            buff = null;
            return false;
        }
        public override byte[] ReadFile(string file)
        {
            throw new NotImplementedException();
            return null;
        }
    }
}
