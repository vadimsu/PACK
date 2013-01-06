using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChunkChainDataTypes
{
    public struct ChunkCB
    {
        public uint size;
        public long sha1;
        public byte hint;
        public FileAndOffset fo;
        public uint ChainsListSize;
        public long[] chains;
        public static uint GetSize() { return sizeof(uint) + sizeof(long) + sizeof(byte) + + FileAndOffset.GetSize() + sizeof(uint); }
        public static uint GetSize(uint ChainEntriesNumber) { return GetSize() + sizeof(long) * ChainEntriesNumber; }
    };
    public struct ChunkMetaData
    {
        public long chunk;
        public byte hint;
        public static uint GetSize() { return (sizeof(long) + sizeof(byte)); }
    };

    public struct FileAndOffset
    {
        public long file;
        public long offset;
        public static uint GetSize() { return sizeof(long) + sizeof(long); }
    };

    public struct Chain
    {
        public uint NumberOfEntries;
        public long[] chunkIds;
    };

    public struct ChunkListAndChainId
    {
        public long[] chunks;
        public long chainId;
        public uint firstChunkIdx;
    }
    public delegate void OnData(byte []data,int offset,int length);
    public delegate void OnMessageReadyToTx(object param,byte []msg);
    public delegate byte []OnMessageReceived(byte []buffer,int offset,byte Flags,int room_space);
    public delegate void OnEnd(object param);
}
