using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using StreamChunckingLib;
using ChunkChainDataTypes;
using SenderPackLib;
using PackMsg;
using Stream2Message;
using MyMemoryStream;
using System.Threading;

namespace SenderPackLib
{
    class LongestMatch
    {
        bool m_ChainNotFound;
        uint m_longestChunkCount;
        uint m_longestChainLen;
        uint m_longestProcessedBytes;
        List<long> m_longestChunkList;
        uint m_longestChainSenderFirstChunkIdx;
        uint m_longestChainReceiverFirstChunkIdx;
        int m_Offset;
        List<ChunkMetaData> m_predMsg;
        private void SetFields(List<ChunkMetaData> predMsg,int offset,uint ChunkCount,uint ChainLen,uint ProcessedBytes,List<long> ChunkList,uint SenderFirstChunkIdx,uint ReceiverFirstChunkIdx)
        {
            LogUtility.LogUtility.LogFile("Setting fields offset " + Convert.ToString(offset) + " ChunkCount " + Convert.ToString(ChunkCount) + " ChainLen " + Convert.ToString(ChainLen) + " ProcessedBytes " + Convert.ToString(ProcessedBytes) + " Sender first " + Convert.ToString(SenderFirstChunkIdx) + " Receiver first " + Convert.ToString(ReceiverFirstChunkIdx), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            m_predMsg = predMsg;
            m_Offset = offset;
            m_longestChunkCount = ChunkCount;
            m_longestChainLen = ChainLen;
            m_longestProcessedBytes = ProcessedBytes;
            m_longestChunkList = ChunkList;
            m_longestChainSenderFirstChunkIdx = SenderFirstChunkIdx;
            m_longestChainReceiverFirstChunkIdx = ReceiverFirstChunkIdx;
        }
        public void UpdateFields(List<ChunkMetaData> predMsg, int offset, uint ChunkCount,uint ChainLen, uint ProcessedBytes,List<long> ChunkList, uint SenderFirstChunkIdx, uint ReceiverFirstChunkIdx)
        {
            SetFields(predMsg, offset,ChunkCount,ChainLen, ProcessedBytes, ChunkList, SenderFirstChunkIdx, ReceiverFirstChunkIdx);
            m_ChainNotFound = false;
        }
        public bool IsLonger(uint matchLen)
        {
            LogUtility.LogUtility.LogFile("IsLonger " + Convert.ToString(m_longestChainLen) + " " + Convert.ToString(matchLen), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            return (m_longestChainLen < matchLen);
        }
        public bool IsMatchFound()
        {
            return (!m_ChainNotFound);
        }
        public uint GetFirstSenderChunkIdx()
        {
            return m_longestChainSenderFirstChunkIdx;
        }
        public uint GetFirstReceiverChunkIdx()
        {
            return m_longestChainReceiverFirstChunkIdx;
        }
        public List<long> GetChunkList()
        {
            return m_longestChunkList;
        }
        public uint GetLongestChainLen()
        {
            return m_longestChainLen;
        }
        public uint GetLongestChunkCount()
        {
            return m_longestChunkCount;
        }
        public uint GetLongestProcessedBytes()
        {
            return m_longestProcessedBytes;
        }
        public int GetRemainder(int length)
        {
            return (length - (int)(m_longestChainLen + m_Offset));
        }
        public int GetPrecedingBytesCount()
        {
            return m_Offset;
        }
        public List<ChunkMetaData> GetPredMsg()
        {
            return m_predMsg;
        }
        public LongestMatch()
        {
            m_ChainNotFound = true;
            SetFields(null,0,0, 0, 0, null, 0, 0);
        }
        public int GetOffset()
        {
            return m_Offset;
        }
    }
    class MatchStateMachine
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        LongestMatch m_LongestMatch;
        StreamChunckingLib.PackChunking m_packChunking;
        EndPoint m_Id;
        byte[] m_data;
        int m_ProcessedBytes;
        List<long> m_SenderChunkList;
        List<long> m_SenderChunkListWithSha1;        
        List<List<ChunkMetaData>> m_PredMsg;
        
        public MatchStateMachine(EndPoint Id, LongestMatch longestMatch, StreamChunckingLib.PackChunking packChunking, byte[] data, List<List<ChunkMetaData>> predMsg)
        {
            m_LongestMatch = longestMatch;
            m_packChunking = packChunking;
            m_Id = Id;
            m_data = data;
            m_PredMsg = predMsg;
            m_SenderChunkList = new List<long>();

            m_ProcessedBytes = m_packChunking.getChunks(m_SenderChunkList, m_data, 0, m_data.Length, true, false);
            m_SenderChunkListWithSha1 = new List<long>();
            m_packChunking.getChunks(m_SenderChunkListWithSha1, m_data, (int)0, m_data.Length, true, true);
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " processedBytes " + Convert.ToString(m_ProcessedBytes) + " chunk count " + Convert.ToString(m_SenderChunkList.Count), ModuleLogLevel);
        }
        uint GetMatchLen(uint firstChunkIdx, uint matchLen)
        {
            uint len = 0;
            for (int idx = (int)firstChunkIdx; idx < (firstChunkIdx + matchLen); idx++)
            {
                len += (uint)PackChunking.chunkToLen(m_SenderChunkList[idx]);
            }
            return len;
        }
        void OnEndOfMatch(List<ChunkMetaData> predMsg,uint matchLen,uint matchChunkCount,uint firstSenderIdx,uint firstReceiverIdx,int offset)
        {
            if (m_LongestMatch.IsLonger(matchLen))
            {
                /*matchLen*/
                matchChunkCount = IsSha1Match(firstSenderIdx, predMsg, firstReceiverIdx, /*matchLen*/matchChunkCount);
                if (matchChunkCount > 0)
                {
                    matchLen = GetMatchLen(firstSenderIdx, matchChunkCount);
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " chain longer " + Convert.ToString(m_LongestMatch.GetLongestChainLen()), ModuleLogLevel);
                    m_LongestMatch.UpdateFields(predMsg, offset, matchChunkCount, matchLen,(uint)m_ProcessedBytes, m_SenderChunkList, firstSenderIdx, firstReceiverIdx);
                }
            }
        }
        void MatchChain(List<ChunkMetaData> predMsg)
        {
            try
            {
                uint matchLen = 0;
                uint matchChunkCount = 0;
                uint firstSenderIdx = 0;
                uint firstReceiverIdx = 0;
                int senderChunkIdx = 0;
                int receiverChunkIdx = 0;
                bool match = false;
                int offset = 0;
                int savedOffset = 0;

                while ((senderChunkIdx < m_SenderChunkList.Count) && (receiverChunkIdx < predMsg.Count))
                {
                    byte senderHint = PackChunking.GetChunkHint(m_data, (uint)offset, (uint)PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]));
                    long senderChunk = PackChunking.chunkCode(0, PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]));
                    switch (match)
                    {
                        case false:
                       //     LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " in non-match " + Convert.ToString(PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx])), ModuleLogLevel);
                            receiverChunkIdx = (int)FindFirstMatchingChunk(predMsg, senderChunk, senderHint);
                            if (receiverChunkIdx != predMsg.Count)
                            {
                         //       LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " match " + Convert.ToString(PackChunking.chunkToLen(predMsg[(int)receiverChunkIdx].chunk)), ModuleLogLevel);
                                match = true;
                                firstReceiverIdx = (uint)receiverChunkIdx;
                                firstSenderIdx = (uint)senderChunkIdx;
                                matchLen = (uint)PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]);
                                matchChunkCount = 1;
                                savedOffset = offset;
                                receiverChunkIdx++;
                            }
                            else
                            {
                                receiverChunkIdx = 0;
                            }
                            break;
                        case true:
                      //      LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "in match " + Convert.ToString(PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx])), ModuleLogLevel);
                            if ((senderChunk != PackChunking.chunkCode(0, PackChunking.chunkToLen(predMsg[receiverChunkIdx].chunk))) ||
                                (senderHint != predMsg[receiverChunkIdx].hint))
                            {
                                match = false;
                                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "stopped. matching sha1 ", ModuleLogLevel);
                                OnEndOfMatch(predMsg, matchLen, matchChunkCount, firstSenderIdx, firstReceiverIdx, savedOffset);
                                if (matchLen >= (m_data.Length / 3))
                                {
                                    //return;
                                }
                            }
                            else
                            {
                                matchLen += (uint)PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]);
                                matchChunkCount++;
                                receiverChunkIdx++;
                                if (senderChunkIdx == (m_SenderChunkList.Count - 1))
                                {
                                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "stopped (end). matching sha1 ", ModuleLogLevel);
                                    OnEndOfMatch(predMsg, matchLen, matchChunkCount, firstSenderIdx, firstReceiverIdx, savedOffset);
                                    if (matchLen >= (m_data.Length / 3))
                                    {
                                        //return;
                                    }
                                }
                            }
                            break;
                    }
                    //LogUtility.LogUtility.LogFile("Sender's offset " + Convert.ToString(offset), ModuleLogLevel);
                    offset += (int)PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]);
                    senderChunkIdx++;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace + " " + exc.InnerException, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void Run()
        {
            for (uint chainIdx = 0; chainIdx < m_PredMsg.Count; chainIdx++)
            {
                MatchChain(m_PredMsg[(int)chainIdx]);
            }
        }
        uint FindFirstMatchingChunk(List<ChunkMetaData> chunksList, long chunk, byte hint)
        {
            uint idx;

            for (idx = 0; idx < chunksList.Count; idx++)
            {
                if ((PackChunking.chunkCode(0, PackChunking.chunkToLen(chunksList[(int)idx].chunk)) == chunk) &&
                    (chunksList[(int)idx].hint == hint))
                {
                    //LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "match " + Convert.ToString(idx), ModuleLogLevel);
                    break;
                }
            }
            return idx;
        }

        uint IsSha1Match(uint senderFirstIdx, List<ChunkMetaData> receiverChunksList, uint receiverFirstIdx, uint matchLength)
        {
            long sha1;
            uint idx;

            for (idx = 0; idx < matchLength; idx++)
            {
                sha1 = PackChunking.chunkToSha1(m_SenderChunkListWithSha1[(int)senderFirstIdx]);
                if (sha1 != PackChunking.chunkToSha1(receiverChunksList[(int)receiverFirstIdx].chunk))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sha mismatch " + Convert.ToString(idx) + " " + Convert.ToString(senderFirstIdx), ModuleLogLevel);
                    return idx;
                }
                //LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sha match " + Convert.ToString(idx) + " " + Convert.ToString(senderFirstIdx), ModuleLogLevel);
                senderFirstIdx++;
                receiverFirstIdx++;
            }
            return idx;
        }
    }
    public class SenderPackLib : Stream2Message.Stream2Message
    {
        enum Sender_State_e
        {
            SENDER_HEADER_STATE,
            SENDER_MSG_BODY_STATE
        };
        
        private StreamChunckingLib.PackChunking packChunking;

        // for statistics & logging only
        uint m_TotalDataReceived;
        uint m_TotalDataSent;
        uint m_TotalSavedData;
        //uint m_TotalPreSaved;
        //uint m_TotalPostSaved;
        //uint m_TotalRawSent;
        uint m_PredMsgReceived;
        uint m_PredAckMsgSent;
        uint m_DataMsgSent;
        EndPoint m_Id;

        object m_libMutex;

        List<List<ChunkMetaData>> m_PredMsg;
        public void AddPredMsg(List<List<ChunkMetaData>> predMsg)
        {
            try
            {
                if (m_PredMsg != null)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Concatinating to PRED " + Convert.ToString(m_PredMsg.Count) + " " + Convert.ToString(predMsg.Count), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    m_PredMsg.Concat(predMsg);
                }
                else
                {
                    m_PredMsg = predMsg;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        List<List<ChunkMetaData>> GetPredMsg()
        {
            return m_PredMsg;
        }
        void InitInstance(byte[]data)
        {
            m_PredMsg = null;
            packChunking = new PackChunking(8);
            m_TotalDataReceived = 0;
            m_TotalDataSent = 0;
            m_TotalSavedData = 0;
            //m_TotalRawSent = 0;
            m_PredMsgReceived = 0;
            m_PredAckMsgSent = 0;
            m_DataMsgSent = 0;
            m_libMutex = new object();
            LogUtility.LogUtility.LogFile("SenderLib:InitInstance", LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }

        public uint GetTotalAdded()
        {
            return m_TotalDataReceived;
        }
        public uint GetTotalSent()
        {
            return m_TotalDataSent;
        }
        public uint GetTotalSavedData()
        {
            return m_TotalSavedData;
        }
        public SenderPackLib(byte[] data) : base(null)
        {
            m_Id = new IPEndPoint(0,0);
            InitInstance(data);
        }

        void ForwardData(byte[] data)
        {
            int offset;
            byte []msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)data.Length, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset);
            data.CopyTo(msg, offset);
            onMessageReadyToTx(onTxMessageParam, msg,false);
            m_TotalDataSent += (uint)data.Length;
            //m_TotalRawSent += (uint)data.Length;
            m_DataMsgSent++;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + /*" PreSaved " + Convert.ToString(m_TotalPreSaved) +*/ " Saved " + Convert.ToString(m_TotalSavedData) /*+ " PostSaved " + Convert.ToString(m_TotalPostSaved)*/ + " Received from server " + Convert.ToString(m_TotalDataReceived) + " Total sent to client " + Convert.ToString(m_TotalDataSent) /*+ " Sent raw " + Convert.ToString(m_TotalRawSent)*/, ModuleLogLevel);
        }

        void SendChunksData(byte []data,int count,bool submit2Head)
        {
            int idx,offset_in_msg;
            byte[] msg;
            byte Msgkind;

            if (submit2Head)
            {
                Msgkind = (byte)PackMsg.PackMsg.MsgKind_e.PACK_FINALLY_PROCESSED_DATA_MSG_KIND;
            }
            else
            {
                Msgkind = (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND;
            }

            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Sending pre-PredAck DATA " + Convert.ToString(count), ModuleLogLevel);
            msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)count, 0, Msgkind, out offset_in_msg);
            for (idx = 0; idx < count; idx++)
            {
                msg[idx + offset_in_msg] = data[idx];
            }
            onMessageReadyToTx(onTxMessageParam, msg, submit2Head);
            m_TotalDataSent += (uint)count;
            //m_TotalPreSaved += (uint)count;
            m_DataMsgSent++;
        }

        public bool AddData(byte[] data,bool invokedOnTransmit)
        {
            int offset;
            bool ret = true;
            byte[] msg;
            Monitor.Enter(m_libMutex);
            uint offset_in_stream = m_TotalDataSent + m_TotalSavedData;
            
            m_TotalDataReceived += (uint)data.Length;
            LogUtility.LogUtility.LogFile("AddData: " + Convert.ToString(data.Length) + " bytes received, isInvokedOnTx " + Convert.ToString(invokedOnTransmit), ModuleLogLevel);
            
            LongestMatch longestMatch = new LongestMatch();
            List<List<ChunkMetaData>> predMsgs = GetPredMsg();

            if (predMsgs != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " AddData: process " + Convert.ToString(predMsgs.Count) + " chains total received " + Convert.ToString(m_TotalDataReceived) + " total sent " + Convert.ToString(m_TotalDataSent) + " total saved " + Convert.ToString(m_TotalSavedData) + " data len " + Convert.ToString(data.Length), ModuleLogLevel);
                MatchStateMachine matchStateMachine = new MatchStateMachine(m_Id, longestMatch, packChunking, data, predMsgs);
                matchStateMachine.Run();
            }
            
            if (!longestMatch.IsMatchFound())
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " no match at all", ModuleLogLevel);

                if (!invokedOnTransmit)
                {
                    ForwardData(data);
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " not submitting - tx case", ModuleLogLevel);
                    ret = false;
                }
                Monitor.Exit(m_libMutex);
                return ret;
            }
            if (invokedOnTransmit)
            {
                m_TotalDataSent -= (uint)data.Length;
                //m_TotalPreSaved -= (uint)count;
                m_DataMsgSent--;
                int remainder = longestMatch.GetRemainder(data.Length);
                int alreadySent = (int)longestMatch.GetPrecedingBytesCount() + (int)longestMatch.GetLongestChainLen();
                if ((alreadySent + remainder) != data.Length)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " something wrong: prec+chainLen+remainder " + Convert.ToString(longestMatch.GetPrecedingBytesCount()) + " " + Convert.ToString(longestMatch.GetLongestChainLen()) + " " + Convert.ToString(remainder) + " does not equal data.length " + Convert.ToString(data.Length), ModuleLogLevel);
                }

                if (remainder > 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sending pos-PredAck data " + Convert.ToString(remainder), ModuleLogLevel);
                    msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)remainder, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_FINALLY_PROCESSED_DATA_MSG_KIND, out offset);
                    for (int i = alreadySent; i < data.Length; i++)
                    {
                        msg[(i - alreadySent) + offset] = data[i];
                    }
                    onMessageReadyToTx(onTxMessageParam, msg, invokedOnTransmit);
                    m_TotalDataSent += (uint)remainder;
                  //  m_TotalPostSaved += (uint)remainder;
                    m_DataMsgSent++;
                }
                byte[] buff = PackMsg.PackMsg.AllocateMsgAndBuildHeader(GetPredictionAckMessageSize((uint)longestMatch.GetLongestChunkCount()), 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, out offset);
                EncodePredictionAckMessage(buff, offset, longestMatch.GetPredMsg(), (uint)longestMatch.GetLongestChunkCount(), longestMatch.GetFirstReceiverChunkIdx());
                onMessageReadyToTx(onTxMessageParam, buff, invokedOnTransmit);
                m_PredAckMsgSent++;
                if (longestMatch.GetPrecedingBytesCount() > 0)
                {
                    SendChunksData(data, (int)longestMatch.GetPrecedingBytesCount(), invokedOnTransmit);
                }
            }
            else
            {
                if (longestMatch.GetPrecedingBytesCount() > 0)
                {
                    SendChunksData(data, (int)longestMatch.GetPrecedingBytesCount(), invokedOnTransmit);
                }

                byte[] buff = PackMsg.PackMsg.AllocateMsgAndBuildHeader(GetPredictionAckMessageSize((uint)longestMatch.GetLongestChunkCount()), 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, out offset);
                EncodePredictionAckMessage(buff, offset, longestMatch.GetPredMsg(), (uint)longestMatch.GetLongestChunkCount(), longestMatch.GetFirstReceiverChunkIdx());
                onMessageReadyToTx(onTxMessageParam, buff, invokedOnTransmit);
                m_PredAckMsgSent++;
                int remainder = longestMatch.GetRemainder(data.Length);
                int alreadySent = (int)longestMatch.GetPrecedingBytesCount() + (int)longestMatch.GetLongestChainLen();
                if ((alreadySent + remainder) != data.Length)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " something wrong: prec+chainLen+remainder " + Convert.ToString(longestMatch.GetPrecedingBytesCount()) + " " + Convert.ToString(longestMatch.GetLongestChainLen()) + " " + Convert.ToString(remainder) + " does not equal data.length " + Convert.ToString(data.Length), ModuleLogLevel);
                }

                if (remainder > 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sending pos-PredAck data " + Convert.ToString(remainder), ModuleLogLevel);
                    msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)remainder, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset);
                    for (int i = alreadySent; i < data.Length; i++)
                    {
                        msg[(i - alreadySent) + offset] = data[i];
                    }
                    onMessageReadyToTx(onTxMessageParam, msg, invokedOnTransmit);
                    m_TotalDataSent += (uint)remainder;
                    //m_TotalPostSaved += (uint)remainder;
                    m_DataMsgSent++;
                }
            }
            longestMatch.GetPredMsg().RemoveRange((int)longestMatch.GetFirstReceiverChunkIdx(), (int)longestMatch.GetLongestChunkCount());
            Monitor.Exit(m_libMutex);
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) /*+ " PreSaved " + Convert.ToString(m_TotalPreSaved)*/ + " Saved " + Convert.ToString(m_TotalSavedData) /*+ " PostSaved " + Convert.ToString(m_TotalPostSaved)*/ + " Received from server " + Convert.ToString(m_TotalDataReceived) + " Total sent to client " + Convert.ToString(m_TotalDataSent) /*+ " Sent raw " + Convert.ToString(m_TotalRawSent)*/, ModuleLogLevel);
            return ret;
        }

        public void ClearData()
        {
            LogUtility.LogUtility.LogFile("SenderLib:ClearData", LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }

        public SenderPackLib(EndPoint id, OnMessageReadyToTx onMsgReadyToTx)
            : base(onMsgReadyToTx)
        {
            m_Id = id;
            ChunkChainDataTypes.OnMessageReceived onPredMsg = new ChunkChainDataTypes.OnMessageReceived(ProcessPredMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_PRED_MSG_KIND, onPredMsg);
            InitInstance(null);
        }    
        List<List<ChunkMetaData>> DecodePredictionMessage(byte[] buffer, int offset, out uint decodedOffsetInStream)
        {
            uint buffer_idx = (uint)offset;
            uint chainsListSize;
            List<List<ChunkMetaData>> chainsList;

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out decodedOffsetInStream);

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out chainsListSize);

            chainsList = new  List<List<ChunkMetaData>>((int)chainsListSize);

            for (int chain_idx = 0; chain_idx < chainsListSize;chain_idx++ )
            {
                uint chunkListSize;
                
                buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out chunkListSize);
                List<ChunkMetaData> chunkMetaDataList = new List<ChunkMetaData>((int)chunkListSize);
                for (uint idx = 0; idx < chunkListSize; idx++)
                {
                    ChunkMetaData chunkMetaData = new ChunkMetaData();
                    chunkMetaData.hint = buffer[buffer_idx++];
                    buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, buffer_idx, out chunkMetaData.chunk);
                    chunkMetaDataList.Add(chunkMetaData);
                }
                chainsList.Add(chunkMetaDataList);
            }
            return chainsList;
        }
        uint GetPredictionAckMessageSize(uint chunksCount)
        {
            return (sizeof(uint) + sizeof(uint) /*+ sizeof(uint)*/ + (uint)chunksCount * ChunkMetaData.GetSize());
        }
        void EncodePredictionAckMessage(byte[] buffer, int offset, List<ChunkMetaData> chunkMetaDataList,uint chunksCount, uint firstChunk)
        {
            uint buffer_idx = (uint)offset;
            uint chunkCounter = 0;
            uint thisTimeSaved = 0;

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, buffer_idx, chunksCount);

            foreach (ChunkMetaData chunkMetaData in chunkMetaDataList)
            {
                if (chunkCounter >= firstChunk)
                {
                    buffer[buffer_idx++] = chunkMetaData.hint;
                    buffer_idx +=
                        ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, buffer_idx, chunkMetaData.chunk);
                    thisTimeSaved += (uint)PackChunking.chunkToLen(chunkMetaData.chunk);
                }
                chunkCounter++;
                if ((chunkCounter-firstChunk) == chunksCount)
                {
                    break;
                }
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sending PRED ACK saved now " + Convert.ToString(thisTimeSaved) + " chunks " + Convert.ToString((chunkCounter - firstChunk)), ModuleLogLevel);
            m_TotalSavedData += (uint)thisTimeSaved;
        }
        
        byte []ProcessPredMsg(byte []packet,int offset,byte Flags,int room_space)
        {
            List<List<ChunkMetaData>> chunkMetaDataList;
            uint decodedOffsetInStream;
            Monitor.Enter(m_libMutex);
            uint offset_in_stream = m_TotalDataSent + m_TotalSavedData;
            m_PredMsgReceived++;
            LogUtility.LogUtility.LogFile("PRED Message", ModuleLogLevel);
            chunkMetaDataList = DecodePredictionMessage(packet, offset, out decodedOffsetInStream);
            LogUtility.LogUtility.LogFile("chainOffset " + Convert.ToString(decodedOffsetInStream) + " TotalSent " + Convert.ToString(m_TotalDataSent) + " TotalSaved " + Convert.ToString(m_TotalSavedData), ModuleLogLevel);
            if (chunkMetaDataList == null)
            {
                Monitor.Exit(m_libMutex);
                return null;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " processing " + Convert.ToString(chunkMetaDataList.Count) + " chains, offset " + Convert.ToString(decodedOffsetInStream) + " offset in stream " + Convert.ToString(offset_in_stream), ModuleLogLevel);
            AddPredMsg(chunkMetaDataList);
            Monitor.Exit(m_libMutex);
            return null;
        }
        public byte[] SenderOnData(byte[] packet,int room_space)
        {
            uint DataSize;
            byte MsgKind;
            byte Flags;
            int offset;
            int rc;

            if ((rc = PackMsg.PackMsg.DecodeMsg(packet, out DataSize, out Flags,out MsgKind, out offset)) != 0)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Message cannot be decoded", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return null;
            }
            switch (MsgKind)
            {
                case (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_MSG_KIND:
                    return ProcessPredMsg(packet, offset, Flags, room_space);
                default:
                    return null;
            }
        }

        public new string GetDebugInfo()
        {
            string debugInfo = "";

            debugInfo += " TotalDataReceived " + Convert.ToString(m_TotalDataReceived) + " TotalSent " + Convert.ToString(m_TotalDataSent) + " TotalSaved " + Convert.ToString(m_TotalSavedData) /*+ " TotalPreSaved " + Convert.ToString(m_TotalPreSaved) + " TotalPostSaved " + Convert.ToString(m_TotalPostSaved)*/ + " PredMsgReceived " + Convert.ToString(m_PredMsgReceived) + " PredAckSent" + Convert.ToString(m_PredAckMsgSent) + " DataMsgSent " + Convert.ToString(m_DataMsgSent) + " " + base.GetDebugInfo();
            return debugInfo;
        }
    }
}
