using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ChunkAndChainFileManager;
using ChunkChainDataTypes;
using StreamChunckingLib;
using System.Net;
using LogUtility;
using ByteArrayScalarTypeConversionLib;
using PackMsg;
using System.Threading;

namespace ReceiverPackLib
{
    public class ReceiverPackLib : Stream2Message.Stream2Message
    {
        class Chains2Save
        {
            List<long> m_ChunList;
            int m_FirstNonMatchingChunk;
            int m_LastNonMatchingChunk;
            byte[] m_Packet;
            int m_FirstNonMatchingChunkOffset;
            ChunkAndChainFileManager.ChunkAndChainFileManager m_ChunkAndChainFileManager;
            public Chains2Save(List<long> chunkList, int firstNonMatchingChunk, int lastNonMatchingChunk, byte[] packet, int firstNonMatchingChunkOffset, ChunkAndChainFileManager.ChunkAndChainFileManager chunkAndChainFileManager)
            {
                m_ChunList = chunkList;
                m_FirstNonMatchingChunk = firstNonMatchingChunk;
                m_LastNonMatchingChunk = lastNonMatchingChunk;
                m_Packet = packet;
                m_FirstNonMatchingChunkOffset = firstNonMatchingChunkOffset;
                m_ChunkAndChainFileManager = chunkAndChainFileManager;
            }
            public List<long> GetChunkList()
            {
                return m_ChunList;
            }
            public int GetFirstNonMatchingChunk()
            {
                return m_FirstNonMatchingChunk;
            }
            public int GetLastNonMatchingChunk()
            {
                return m_LastNonMatchingChunk;
            }
            public byte[] GetPacket()
            {
                return m_Packet;
            }
            public int GetFirstNonMatchingChunkOffset()
            {
                return m_FirstNonMatchingChunkOffset;
            }
            public ChunkAndChainFileManager.ChunkAndChainFileManager GetChunkAndChainFileManager()
            {
                return m_ChunkAndChainFileManager;
            }
        }
        static List<Chains2Save> m_Chains2Save = new List<Chains2Save>();
        static object m_Chains2SaveMutex = new object();
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        public static uint ChunksProcessed = 0;
        public static uint PredMsgSent = 0;
        public static uint PredAckMsgReceived = 0;
        static StreamChunckingLib.PackChunking packChunking = new PackChunking(8);

        //private ChunkAndChainFileManager.ChunkAndChainFileManager chunkAndChainFileManager;
        //private packChunking;
        private long CurrentOffset;
        OnData onDataReceived;
        OnEnd onTransactionEnd;
        object onTransactionEndParam;
        uint TotalSaved;
        EndPoint Id;
        MemoryStream inComingData;
        List<long> m_SentChainList;
        object libMutex;
        ChunkAndChainFileManager.ChunkAndChainFileManager chunkAndChainFileManager;
        LogUtility.LogUtility binaryLogging;
        static void AddChain2Save(Chains2Save chain2Save)
        {
            Monitor.Enter(m_Chains2SaveMutex);
            try
            {
                m_Chains2Save.Add(chain2Save);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            Monitor.Exit(m_Chains2SaveMutex);
        }
        static void WriteChunkChainsThreadProc()
        {
            int FlushSwTimer = 0;
            while (true)
            {
                try
                {
                    Monitor.Enter(m_Chains2SaveMutex);
                    Chains2Save[] chains2Save = new Chains2Save[m_Chains2Save.Count];
                    m_Chains2Save.CopyTo(chains2Save);
                    m_Chains2Save.Clear();
                    Monitor.Exit(m_Chains2SaveMutex);
                    foreach (Chains2Save chain2Save in chains2Save)
                    {
                        ChunkAndChainFileManager.ChunkAndChainFileManager.SaveChain(chain2Save.GetChunkList(), chain2Save.GetFirstNonMatchingChunk(), chain2Save.GetLastNonMatchingChunk(), chain2Save.GetPacket(), chain2Save.GetFirstNonMatchingChunkOffset(),chain2Save.GetChunkAndChainFileManager());
                        LogUtility.LogUtility.LogFile("Saved chain " + Convert.ToString(chain2Save.GetChunkList().Count) + " " + Convert.ToString(chain2Save.GetFirstNonMatchingChunk()) + " " + Convert.ToString(chain2Save.GetLastNonMatchingChunk()) + " " + Convert.ToString(chain2Save.GetFirstNonMatchingChunkOffset()), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                }
                catch(Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                try
                {
                    if (++FlushSwTimer >= 600)
                    {
                        LogUtility.LogUtility.LogFile("FLUSHING ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        Flush();
                        FlushSwTimer = 0;
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                Thread.Sleep(1000);
            }
        }
        static System.Threading.Thread PredMsgCacheTimeOutingThread = new Thread(new ThreadStart(WriteChunkChainsThreadProc));

        public static void InitGlobalObjects()
        {
            LogUtility.LogUtility.LogFile("ReceivePackLib:InitGlobalObjects", LogLevels.LEVEL_LOG_HIGH);
            ChunkAndChainFileManager.ChunkAndChainFileManager.Init();
            PredMsgCacheTimeOutingThread.Start();
        }

        public static void Flush()
        {
            ChunkAndChainFileManager.ChunkAndChainFileManager.Flush();
        }

        void InitInstance(OnData onData,OnEnd onEnd,object onEndParam)
        {
            Reset();
            onDataReceived = onData;
            onTransactionEnd = onEnd;
            onTransactionEndParam = onEndParam;
            TotalSaved = 0;
            CurrentOffset = 0;
            chunkAndChainFileManager = new ChunkAndChainFileManager.ChunkAndChainFileManager();
            inComingData = null;
            libMutex = new object();
            m_SentChainList = new List<long>();
        }
        public long GetTotalData()
        {
            return CurrentOffset;
        }
        public uint GetTotalDataSaved()
        {
            return TotalSaved;
        }
        public ReceiverPackLib(OnData onData,OnEnd onEnd,object onEndParam) : base(null)
        {
            Id = new IPEndPoint(0,0);
            InitInstance(onData,onEnd,onEndParam);
        }

        public void Reset()
        {
            CurrentOffset = 0;
        }
        
        public ReceiverPackLib(ChunkChainDataTypes.OnData onData, OnEnd onEnd,object onEndParam,OnMessageReadyToTx onMsgReadyToTx) : base(onMsgReadyToTx)
        {
            Id = new IPEndPoint(0,0);
            OnMessageReceived onDataMsg = new OnMessageReceived(ProcessDataMsg);
            OnMessageReceived onPredAckMsg = new OnMessageReceived(ProcessPredAckMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, onDataMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, onPredAckMsg);
            InitInstance(onData,onEnd,onEndParam);
        }

        public ReceiverPackLib(EndPoint id,ChunkChainDataTypes.OnData onData, OnEnd onEnd, object onEndParam, OnMessageReadyToTx onMsgReadyToTx)
            : base(onMsgReadyToTx)
        {
            Id = id;
            OnMessageReceived onDataMsg = new OnMessageReceived(ProcessDataMsg);
            OnMessageReceived onPredAckMsg = new OnMessageReceived(ProcessPredAckMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, onDataMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, onPredAckMsg);
            InitInstance(onData, onEnd, onEndParam);
        }

        void ReceiverOnPredictionConfirm(List<ChunkMetaData> chunkMetaDataAndId,uint chunksCount)
        {
            Monitor.Enter(libMutex);
            uint idx;

            idx = 0;

            foreach (ChunkMetaData chMetaData in chunkMetaDataAndId)
            {
                int ChunkLength;
                object[] o;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " writing acked " + Convert.ToString(chMetaData.chunk), ModuleLogLevel);
                ChunkLength = PackChunking.chunkToLen(chMetaData.chunk);
                byte[] buff = ChunkAndChainFileManager.ChunkAndChainFileManager.GetChunkData(chMetaData.chunk);
                onDataReceived(buff, 0, buff.Length);
                CurrentOffset += ChunkLength;
                TotalSaved += (uint)ChunkLength;
                idx++;
                if (idx == chunksCount)
                {
                    break;
                }
            }
            Monitor.Exit(libMutex);
        }

        uint GetChainsListSize(List<ChunkListAndChainId> chainChunkList)
        {
            uint size = sizeof(uint)+sizeof(uint);

            foreach (ChunkListAndChainId chunkListAndChainId in chainChunkList)
            {
                uint chunkCount = (uint)(chunkListAndChainId.chunks.Length - chunkListAndChainId.firstChunkIdx);
                //if (chunkListAndChainId.chunks.Length > 50)
                //{
                  //  if ((chunkListAndChainId.chunks.Length - chunkListAndChainId.firstChunkIdx) > 50)
                    //{
                      //  chunkCount = 50;
                    //}
                //}
                LogUtility.LogUtility.LogFile("chunkListAndChainId.chunks.Length=" + Convert.ToString(chunkListAndChainId.chunks.Length) + " chunkListAndChainId.firstChunkIdx=" + Convert.ToString(chunkListAndChainId.firstChunkIdx), ModuleLogLevel);
                size += (uint)(sizeof(long) + sizeof(uint) + (chunkCount*(sizeof(byte)+sizeof(long))));
            }

            return size;
        }

        void EncodePredictionMessage(byte[] buffer, int offset, List<ChunkListAndChainId> chainChunkList,uint chainOffset)
        {
            uint buffer_idx = (uint)offset;

            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Composing Pred msg: chainOffset " + Convert.ToString(chainOffset), ModuleLogLevel);

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, buffer_idx, chainOffset);

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, buffer_idx, (uint)chainChunkList.Count);

            foreach (ChunkListAndChainId chunkListAndChainId in chainChunkList)
            {
                buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, buffer_idx, (uint)(chunkListAndChainId.chunks.Length - chunkListAndChainId.firstChunkIdx));
                uint firstChunk = chunkListAndChainId.firstChunkIdx;
         //       if(chunkListAndChainId.chunks.Length > 50)
           //     {
             //       if(firstChunk < (chunkListAndChainId.chunks.Length - 50))
               //     {
                 //       firstChunk = (uint)(chunkListAndChainId.chunks.Length - 50);
                   // }
                //}
                for (uint idx = /*chunkListAndChainId.firstChunkIdx*/firstChunk;idx < chunkListAndChainId.chunks.Length; idx++)
                {
                    buffer[buffer_idx++] = ChunkAndChainFileManager.ChunkAndChainFileManager.GetChunkHint(chunkListAndChainId.chunks[idx]);
                    buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, buffer_idx, chunkListAndChainId.chunks[idx]);
                    //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " chunk " + Convert.ToString(chunkListAndChainId.chunks[idx]) + " len " + Convert.ToString(PackChunking.chunkToLen(chunkListAndChainId.chunks[idx])) + " is written to PRED", ModuleLogLevel);
                }
            }
        }

        List<ChunkMetaData> DecodePredictionAckMessage(byte[] buffer, int offset, out uint chunksCount)
        {
            uint buffer_idx = (uint)offset;
            List<ChunkMetaData> chunkMetaDataAndId;

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out chunksCount);

            chunkMetaDataAndId = new List<ChunkMetaData>((int)chunksCount);

            for (int idx = 0; idx < chunksCount;idx++ )
            {
                ChunkMetaData chunkMetaData = new ChunkMetaData();

                chunkMetaData.hint = buffer[buffer_idx++];
                buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, buffer_idx, out chunkMetaData.chunk);
                chunkMetaDataAndId.Add(chunkMetaData);
            }
            return chunkMetaDataAndId;
        }

        uint ReceiverOnDataMsg(byte[] packet, int packet_offset, byte flag, out List<ChunkListAndChainId> chainChunkList, out uint chainOffset)
        {
            onDataReceived(packet, packet_offset, packet.Length - packet_offset);

            List<long> chunkList = new List<long>();
            Monitor.Enter(libMutex);
            /* process the stream (+reminder) to get chunks */
            int processed_bytes = packChunking.getChunks(chunkList, packet, packet_offset, packet.Length, /*is_last*/true, true);
            uint offset = (uint)packet_offset;
            List<ChunkMetaData[]> chunkMetaDataList = new List<ChunkMetaData[]>(100);
            int idx = 0;
            int lastNonMatchingChunk = chunkList.Count;
            int firstNonMatchingChunk = chunkList.Count;
            int firstNonMatchingChunkOffset = 0;
            chainChunkList = new List<ChunkListAndChainId>(100);

            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " processing " + Convert.ToString(chunkList.Count) + " chunks, CurrentOffset " + Convert.ToString(CurrentOffset), ModuleLogLevel);
            
            foreach (long chunk in chunkList)
            {
                int rc = chunkAndChainFileManager.ChainMatch(chunkList, idx, chainChunkList, m_SentChainList);
               
                if (rc < 0)
                {
                    lastNonMatchingChunk = idx;
                    if (firstNonMatchingChunk == chunkList.Count)
                    {
//                        LogUtility.LogUtility.LogFile("starting non-mactching range " + Convert.ToString(idx), ModuleLogLevel);
                        firstNonMatchingChunk = idx;
                        firstNonMatchingChunkOffset = (int)offset;
                    }
                    else
                    {
//                        LogUtility.LogUtility.LogFile("updating non-matching range " + Convert.ToString(idx), ModuleLogLevel);
                    }
                }
                else if(lastNonMatchingChunk != chunkList.Count)
                {
//                    LogUtility.LogUtility.LogFile("end of non-matching range " + Convert.ToString(lastNonMatchingChunk), ModuleLogLevel);
                    Chains2Save chain2Save = new Chains2Save(chunkList, firstNonMatchingChunk, lastNonMatchingChunk, packet, firstNonMatchingChunkOffset, chunkAndChainFileManager);
                    AddChain2Save(chain2Save);
                    firstNonMatchingChunk = chunkList.Count;
                    lastNonMatchingChunk = chunkList.Count;
                }
                ChunksProcessed++;
                offset += (uint)PackChunking.chunkToLen(chunkList[idx]);
                idx++;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " AddUpdateChunk: offset=" + Convert.ToString(CurrentOffset+offset) + " chainChunkList " + Convert.ToString(chainChunkList.Count), ModuleLogLevel);
                if (rc > 0)
                {
                    m_SentChainList.Add(chainChunkList[0].chainId);
                    break;
                }
            }
            if (lastNonMatchingChunk != chunkList.Count)
            {
//                LogUtility.LogUtility.LogFile("end of non-matching range (last) " + Convert.ToString(lastNonMatchingChunk), ModuleLogLevel);
                Chains2Save chain2Save = new Chains2Save(chunkList, firstNonMatchingChunk, lastNonMatchingChunk, packet, firstNonMatchingChunkOffset,chunkAndChainFileManager);
                AddChain2Save(chain2Save);
            }
            //Vadim 10/01/13 onDataReceived(packet, packet_offset, packet.Length - packet_offset);
            chainOffset = (uint)(CurrentOffset + processed_bytes);
            CurrentOffset += (uint)(packet.Length - packet_offset);
            if (chainChunkList.Count == 0)
            {
                Monitor.Exit(libMutex);
                return 0;
            }
            Monitor.Exit(libMutex);
            return GetChainsListSize(chainChunkList);
        }

        uint CopyBytes(byte[] src, byte[] dst,uint src_offset,uint dst_offset,uint number_of_bytes_2_copy)
        {
            uint idx = 0;

            for (idx = 0;((dst_offset + idx) < dst.Length) && ((src_offset + idx) < src.Length) && (idx < number_of_bytes_2_copy); idx++)
            {
                dst[dst_offset + idx] = src[src_offset + idx];
            }
            return idx;
        }

        byte []ProcessDataMsg(byte []packet,int offset,byte Flags,int room_space)
        {
            uint predMsgSize;
            byte[] predMsg;
            List<ChunkListAndChainId> chainChunkList;
            uint chainOffset;
//            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " ProcessDataMsg: Total " + Convert.ToString(CurrentOffset) + " saved " + Convert.ToString(TotalSaved) + " flags " + Convert.ToString(Flags), ModuleLogLevel);
            predMsgSize = ReceiverOnDataMsg(packet, offset, Flags,out chainChunkList,out chainOffset);
            if (predMsgSize == 0)
            {
                return null;
            }
            LogUtility.LogUtility.LogFile("PredMsgSize=" + Convert.ToString(predMsgSize), ModuleLogLevel);
            predMsg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)(predMsgSize + room_space),0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_MSG_KIND, out offset);
            EncodePredictionMessage(predMsg, offset + room_space, chainChunkList,chainOffset);
            PredMsgSent++;
            return predMsg;
        }

        byte []ProcessPredAckMsg(byte[] packet, int offset,byte Flags,int dummy_room_space)
        {
            List<ChunkMetaData> chunkMetaDataAndId;
            uint chunksCount;
            LogUtility.LogUtility.LogFile("PRED ACK message", LogLevels.LEVEL_LOG_HIGH);
            chunkMetaDataAndId = DecodePredictionAckMessage(packet, offset, out chunksCount);
            ReceiverOnPredictionConfirm(chunkMetaDataAndId, chunksCount);
            if ((Flags & PackMsg.PackMsg.LastChunkFlag) == PackMsg.PackMsg.LastChunkFlag)
            {
                onTransactionEnd(onTransactionEndParam);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " ProcessPredAckMsg: Total  " + Convert.ToString(CurrentOffset) , ModuleLogLevel);
            PredAckMsgReceived++;
            return null;
        }

        public void OnDispose()
        {
            chunkAndChainFileManager.OnDispose();
        }

        public byte[] ReceiverOnData(byte[] packet, int room_space)
        {
            uint DataSize;
            byte MsgKind;
            byte Flags;
            int offset;
            int rc;
            

            if ((rc = PackMsg.PackMsg.DecodeMsg(packet, out DataSize, out Flags,out MsgKind, out offset)) != 0)
            {
                return null;
            }
            switch (MsgKind)
            {
                case (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND:
                    return ProcessDataMsg(packet,offset,Flags,room_space);
                case (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND:
                    ProcessPredAckMsg(packet, offset,Flags,0);
                    break;
                default:
                    return null;
            }
            return null;
        }

        public new string GetDebugInfo()
        {
            string debugInfo = "ChunksProcessed " + Convert.ToString(ChunksProcessed) + " PredMsgSent " + Convert.ToString(PredMsgSent) + " PredAckMsgReceived " + Convert.ToString(PredAckMsgReceived) + chunkAndChainFileManager.GetDebugInfo();
            debugInfo += " Total " + Convert.ToString(CurrentOffset) + " TotalSaved " + Convert.ToString(TotalSaved) + " " + base.GetDebugInfo();
            return debugInfo;
        }
    }
}

