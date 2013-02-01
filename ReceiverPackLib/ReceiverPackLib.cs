using System;
using System.Collections.Generic;
using System.Collections;
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
        const uint mc_MinChainLengthInBytes = 1024 * 10;
        const uint mc_MinChainLengthInChunks = 10;
        static object m_Chains2SaveMutex = new object();
#if true
        static List<Chains2Save> m_Chains2Save = new List<Chains2Save>();
#else
        static Dictionary<EndPoint, List<Chains2Save>> m_ChainsPerIpEndpoint = new Dictionary<EndPoint, List<Chains2Save>>();
        static AutoResetEvent m_SaveEvent = new AutoResetEvent(false);
        static Queue m_SaveQueue = new Queue();
#endif
        public static uint m_ChunksProcessed = 0;
        public static uint m_PredMsgSent = 0;
        public static uint m_PredAckMsgReceived = 0;
        static StreamChunckingLib.PackChunking m_packChunking = new PackChunking(8);

        //private ChunkAndChainFileManager.ChunkAndChainFileManager chunkAndChainFileManager;
        //private packChunking;
        private long m_CurrentOffset;
        OnData m_onDataReceived;
        OnEnd m_onTransactionEnd;
        object m_onTransactionEndParam;
        uint m_TotalSaved;
        uint m_TotalReceived;
        uint m_TotalPredAckSize;
        uint m_TotalPredSize;
        EndPoint m_Id;
        List<long> m_SentChainList;
        object m_libMutex;
        ChunkAndChainFileManager.ChunkAndChainFileManager m_chunkAndChainFileManager;
        
        static void OnComplete(EndPoint ipEndPoint)
        {
#if false
            try
            {
                Queue.Synchronized(m_SaveQueue).Enqueue(ipEndPoint);
                m_SaveEvent.Set();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
#endif
        }
        static void AddChain2Save(Chains2Save chain2Save,EndPoint ipEndPoint)
        {
            Monitor.Enter(m_Chains2SaveMutex);
            try
            {
#if true
                m_Chains2Save.Add(chain2Save);
#else 
                List<Chains2Save> list;
                if (m_ChainsPerIpEndpoint.TryGetValue(ipEndPoint, out list))
                {
                    list.Add(chain2Save);
                }
                else
                {
                    list = new List<Chains2Save>();
                    list.Add(chain2Save);
                    m_ChainsPerIpEndpoint.Add(ipEndPoint, list);
                }
#endif
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            Monitor.Exit(m_Chains2SaveMutex);
        }
        static uint GetChainLength(List<long> chunkList)
        {
            uint length = 0;

            foreach (long chunk in chunkList)
            {
                length += (uint)PackChunking.chunkToLen(chunk);
            }
            return length;
        }
        static bool PassesSaveCriteria(Chains2Save chain2Save)
        {
            return ((GetChainLength(chain2Save.GetChunkList()) >= mc_MinChainLengthInBytes)&&((chain2Save.GetLastNonMatchingChunk() - chain2Save.GetFirstNonMatchingChunk() >= mc_MinChainLengthInChunks)));
        }
        static void WriteChunkChainsThreadProc()
        {
            int FlushSwTimer = 0;
            while (true)
            {
                try
                {
#if true
                    Monitor.Enter(m_Chains2SaveMutex);
                    Chains2Save[] chains2Save = new Chains2Save[m_Chains2Save.Count];
                    m_Chains2Save.CopyTo(chains2Save);
                    m_Chains2Save.Clear();
                    Monitor.Exit(m_Chains2SaveMutex);
#else
                    m_SaveEvent.WaitOne();
                    if (Queue.Synchronized(m_SaveQueue).Count == 0)
                    {
                        goto flush;
                    }
                    EndPoint ipEndPoint = (EndPoint)Queue.Synchronized(m_SaveQueue).Dequeue();
                    List<Chains2Save> list;
                    Chains2Save[] chains2Save;
                    Monitor.Enter(m_Chains2SaveMutex);
                    if (m_ChainsPerIpEndpoint.TryGetValue(ipEndPoint, out list))
                    {
                        chains2Save = new Chains2Save[list.Count];
                        list.CopyTo(chains2Save);
                        list.Clear();
                    }
                    else
                    {
                        chains2Save = null;
                    }
                    Monitor.Exit(m_Chains2SaveMutex);
#endif
                    if (chains2Save != null)
                    {
                        foreach (Chains2Save chain2Save in chains2Save)
                        {
                            if (PassesSaveCriteria(chain2Save))
                            {
                                ChunkAndChainFileManager.ChunkAndChainFileManager.SaveChain(chain2Save.GetChunkList(), chain2Save.GetFirstNonMatchingChunk(), chain2Save.GetLastNonMatchingChunk(), chain2Save.GetPacket(), chain2Save.GetFirstNonMatchingChunkOffset(), chain2Save.GetChunkAndChainFileManager());
                                LogUtility.LogUtility.LogFile("Saved chain " + Convert.ToString(chain2Save.GetChunkList().Count) + " " + Convert.ToString(chain2Save.GetFirstNonMatchingChunk()) + " " + Convert.ToString(chain2Save.GetLastNonMatchingChunk()) + " " + Convert.ToString(chain2Save.GetFirstNonMatchingChunkOffset()), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                            }
                        }
                    }
                }
                catch(Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
            flush:
                try
                {
                    if (++FlushSwTimer >= 300)
                    {
                        LogUtility.LogUtility.LogFile("FLUSHING ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        Flush();
                        FlushSwTimer = 0;
                        //ChunkAndChainFileManager.ChunkAndChainFileManager.Restart();
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                Thread.Sleep(100);
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
            m_onDataReceived = onData;
            m_onTransactionEnd = onEnd;
            m_onTransactionEndParam = onEndParam;
            m_TotalSaved = 0;
            m_TotalReceived = 0;
            m_TotalPredAckSize = 0;
            m_TotalPredSize = 0;
            m_CurrentOffset = 0;
            m_chunkAndChainFileManager = new ChunkAndChainFileManager.ChunkAndChainFileManager();
            m_libMutex = new object();
            m_SentChainList = new List<long>();
        }
        public long GetTotalData()
        {
            return m_CurrentOffset;
        }
        public uint GetTotalDataSaved()
        {
            return m_TotalSaved;
        }
        public ReceiverPackLib(OnData onData,OnEnd onEnd,object onEndParam) : base(null)
        {
            m_Id = new IPEndPoint(0,0);
            InitInstance(onData,onEnd,onEndParam);
        }

        public void Reset()
        {
            m_CurrentOffset = 0;
        }
        
        public ReceiverPackLib(ChunkChainDataTypes.OnData onData, OnEnd onEnd,object onEndParam,OnMessageReadyToTx onMsgReadyToTx) : base(onMsgReadyToTx)
        {
            m_Id = new IPEndPoint(0,0);
            OnMessageReceived onDataMsg = new OnMessageReceived(ProcessDataMsg);
            OnMessageReceived onPredAckMsg = new OnMessageReceived(ProcessPredAckMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, onDataMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_FINALLY_PROCESSED_DATA_MSG_KIND, onDataMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, onPredAckMsg);
            InitInstance(onData,onEnd,onEndParam);
        }

        public ReceiverPackLib(EndPoint id,ChunkChainDataTypes.OnData onData, OnEnd onEnd, object onEndParam, OnMessageReadyToTx onMsgReadyToTx)
            : base(onMsgReadyToTx)
        {
            m_Id = id;
            OnMessageReceived onDataMsg = new OnMessageReceived(ProcessDataMsg);
            OnMessageReceived onPredAckMsg = new OnMessageReceived(ProcessPredAckMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, onDataMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_FINALLY_PROCESSED_DATA_MSG_KIND, onDataMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, onPredAckMsg);
            InitInstance(onData, onEnd, onEndParam);
        }

        void ReceiverOnPredictionConfirm(List<ChunkMetaData> chunkMetaDataAndId,uint chunksCount)
        {
            Monitor.Enter(m_libMutex);
            uint idx;
            uint savedInThisCall = 0;

            idx = 0;

            foreach (ChunkMetaData chMetaData in chunkMetaDataAndId)
            {
                int ChunkLength;
                object[] o;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " writing acked " + Convert.ToString(chMetaData.chunk), ModuleLogLevel);
                ChunkLength = PackChunking.chunkToLen(chMetaData.chunk);
                byte[] buff = ChunkAndChainFileManager.ChunkAndChainFileManager.GetChunkData(chMetaData.chunk);
                m_onDataReceived(buff, 0, buff.Length);
                m_CurrentOffset += ChunkLength;
                m_TotalSaved += (uint)ChunkLength;
                savedInThisCall += (uint)ChunkLength;
                idx++;
                if (idx == chunksCount)
                {
                    break;
                }
            }
            Monitor.Exit(m_libMutex);
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Saved in this call " + Convert.ToString(savedInThisCall), LogLevels.LEVEL_LOG_HIGH3);
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
                LogUtility.LogUtility.LogFile("chunkListAndChainId.chunks.Length=" + Convert.ToString(chunkListAndChainId.chunks.Length) + " chunkListAndChainId.firstChunkIdx=" + Convert.ToString(chunkListAndChainId.firstChunkIdx), LogLevels.LEVEL_LOG_HIGH3);
                size += (uint)(sizeof(long) + sizeof(uint) + (chunkCount*(sizeof(byte)+sizeof(long))));
            }

            return size;
        }

        void EncodePredictionMessage(byte[] buffer, int offset, List<ChunkListAndChainId> chainChunkList,uint chainOffset)
        {
            uint buffer_idx = (uint)offset;

            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Composing Pred msg: chainOffset " + Convert.ToString(chainOffset), ModuleLogLevel);

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
            m_onDataReceived(packet, packet_offset, packet.Length - packet_offset);

            List<long> chunkList = new List<long>();
            Monitor.Enter(m_libMutex);
            /* process the stream (+reminder) to get chunks */
            int processed_bytes = m_packChunking.getChunks(chunkList, packet, packet_offset, packet.Length, /*is_last*/true, true);
            uint offset = (uint)packet_offset;
            List<ChunkMetaData[]> chunkMetaDataList = new List<ChunkMetaData[]>(100);
            int idx = 0;
            int lastNonMatchingChunk = chunkList.Count;
            int firstNonMatchingChunk = chunkList.Count;
            int firstNonMatchingChunkOffset = 0;
            chainChunkList = new List<ChunkListAndChainId>(100);

            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " processing " + Convert.ToString(chunkList.Count) + " chunks, CurrentOffset " + Convert.ToString(m_CurrentOffset), ModuleLogLevel);
            
            foreach (long chunk in chunkList)
            {
                int rc = m_chunkAndChainFileManager.ChainMatch(chunkList, idx, chainChunkList, m_SentChainList);
               
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
                    Chains2Save chain2Save = new Chains2Save(chunkList, firstNonMatchingChunk, lastNonMatchingChunk, packet, firstNonMatchingChunkOffset, m_chunkAndChainFileManager);
                    AddChain2Save(chain2Save, m_Id);
                    firstNonMatchingChunk = chunkList.Count;
                    lastNonMatchingChunk = chunkList.Count;
                }
                m_ChunksProcessed++;
                offset += (uint)PackChunking.chunkToLen(chunkList[idx]);
                idx++;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " AddUpdateChunk: offset=" + Convert.ToString(m_CurrentOffset+offset) + " chainChunkList " + Convert.ToString(chainChunkList.Count), ModuleLogLevel);
                if (rc > 0)
                {
                    m_SentChainList.Add(chainChunkList[0].chainId);
                    break;
                }
                else if (rc == 0)
                {
                    //break;
                }
            }
            if (lastNonMatchingChunk != chunkList.Count)
            {
//                LogUtility.LogUtility.LogFile("end of non-matching range (last) " + Convert.ToString(lastNonMatchingChunk), ModuleLogLevel);
                Chains2Save chain2Save = new Chains2Save(chunkList, firstNonMatchingChunk, lastNonMatchingChunk, packet, firstNonMatchingChunkOffset,m_chunkAndChainFileManager);
                AddChain2Save(chain2Save, m_Id);
            }
            //Vadim 10/01/13 onDataReceived(packet, packet_offset, packet.Length - packet_offset);
            chainOffset = (uint)(m_CurrentOffset + processed_bytes);
            m_CurrentOffset += (uint)(packet.Length - packet_offset);
            if (chainChunkList.Count == 0)
            {
                Monitor.Exit(m_libMutex);
                return 0;
            }
            Monitor.Exit(m_libMutex);
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
            uint received = (uint)(packet.Length - offset);
            m_TotalReceived += received;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ProcessDataMsg: Received: " + Convert.ToString(received) + " Total received " + Convert.ToString(m_TotalReceived) + " Total saved "+ Convert.ToString(m_TotalSaved), LogLevels.LEVEL_LOG_HIGH3);
            predMsgSize = ReceiverOnDataMsg(packet, offset, Flags,out chainChunkList,out chainOffset);
            if (predMsgSize == 0)
            {
                return null;
            }
            LogUtility.LogUtility.LogFile("PredMsgSize=" + Convert.ToString(predMsgSize) + " Total PredMsgSize " + Convert.ToString(m_TotalPredSize), LogLevels.LEVEL_LOG_HIGH3);
            predMsg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)(predMsgSize + room_space),0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_MSG_KIND, out offset);
            EncodePredictionMessage(predMsg, offset + room_space, chainChunkList,chainOffset);
            m_PredMsgSent++;
            return predMsg;
        }
        byte[] TryGeneratePredMsgOnPredAck(long chunk, int dummy_room_space)
        {
            List<ChunkListAndChainId> chainsChunksList = new List<ChunkListAndChainId>();
            if (m_chunkAndChainFileManager.GetChainAfterChunk(chunk, chainsChunksList) != 0)
            {
                chainsChunksList = null;
                return null;
            }
            int offset;
            uint predMsgSize = GetChainsListSize(chainsChunksList);
            byte[] predMsg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)(predMsgSize + dummy_room_space), 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_MSG_KIND, out offset);
            EncodePredictionMessage(predMsg, offset + dummy_room_space, chainsChunksList, (uint)m_CurrentOffset);
            m_PredMsgSent++;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Generated PredMsg, size " + Convert.ToString(predMsgSize), LogLevels.LEVEL_LOG_HIGH);
            return predMsg;
        }
        byte []ProcessPredAckMsg(byte[] packet, int offset,byte Flags,int dummy_room_space)
        {
            List<ChunkMetaData> chunkMetaDataAndId;
            uint chunksCount;
            LogUtility.LogUtility.LogFile("PRED ACK message", LogLevels.LEVEL_LOG_HIGH);
            m_TotalPredAckSize += (uint)(packet.Length - offset);
            chunkMetaDataAndId = DecodePredictionAckMessage(packet, offset, out chunksCount);
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ProcessPredAckMsg: Received msg size " + Convert.ToString(packet.Length - offset) + " Total PredAck size " + Convert.ToString(m_TotalPredAckSize) + " Chunks in msg " + Convert.ToString(chunksCount), LogLevels.LEVEL_LOG_HIGH3);
            ReceiverOnPredictionConfirm(chunkMetaDataAndId, chunksCount);
            byte[] predMsg = null;
            if ((chunkMetaDataAndId.Count >= chunksCount)&&(chunksCount > 0))
            {
                predMsg = TryGeneratePredMsgOnPredAck(chunkMetaDataAndId[(int)chunksCount - 1].chunk, dummy_room_space);
            }
            if ((Flags & PackMsg.PackMsg.LastChunkFlag) == PackMsg.PackMsg.LastChunkFlag)
            {
                m_onTransactionEnd(m_onTransactionEndParam);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ProcessPredAckMsg: Total  " + Convert.ToString(m_CurrentOffset) , ModuleLogLevel);
            
            m_PredAckMsgReceived++;
            return predMsg;
        }

        public void OnDispose()
        {
            OnComplete(m_Id);
            m_chunkAndChainFileManager.OnDispose();
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
                case (byte)PackMsg.PackMsg.MsgKind_e.PACK_FINALLY_PROCESSED_DATA_MSG_KIND:
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
            string debugInfo = "ChunksProcessed " + Convert.ToString(m_ChunksProcessed) + " PredMsgSent " + Convert.ToString(m_PredMsgSent) + " PredAckMsgReceived " + Convert.ToString(m_PredAckMsgReceived) + m_chunkAndChainFileManager.GetDebugInfo();
            debugInfo += " Total " + Convert.ToString(m_CurrentOffset) + " TotalSaved " + Convert.ToString(m_TotalSaved) + " " + base.GetDebugInfo();
            return debugInfo;
        }
    }
}

