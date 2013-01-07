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
#if false
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " processing " + Convert.ToString(receiverChunkList[m_chainIdx].chunkMetaData.Count) + " chunks ", ModuleLogLevel);
                {
                    for (int i = 0; i < senderChunkList.Count; i++)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ###calc chunk " + Convert.ToString(senderChunkList[i]) + " len " + Convert.ToString(PackChunking.chunkToLen(senderChunkList[i])), ModuleLogLevel);
                    }
                    for (int i = 0; i < receiverChunkList[m_chainIdx].chunkMetaData.Count; i++)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ###recvd chunk " + Convert.ToString(PackChunking.chunkToLen(receiverChunkList[m_chainIdx].chunkMetaData[i].chunk)), ModuleLogLevel);
                    }
                }
#endif
                while ((senderChunkIdx < m_SenderChunkList.Count) && (receiverChunkIdx < predMsg.Count))
                {
                    byte senderHint = PackChunking.GetChunkHint(m_data, (uint)offset, (uint)PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]));
                    long senderChunk = PackChunking.chunkCode(0, PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx]));
                    switch (match)
                    {
                        case false:
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " in non-match " + Convert.ToString(PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx])), ModuleLogLevel);
                            receiverChunkIdx = (int)FindFirstMatchingChunk(predMsg, senderChunk, senderHint);
                            if (receiverChunkIdx != predMsg.Count)
                            {
                                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " match " + Convert.ToString(PackChunking.chunkToLen(predMsg[(int)receiverChunkIdx].chunk)), ModuleLogLevel);
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
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "in match " + Convert.ToString(PackChunking.chunkToLen(m_SenderChunkList[senderChunkIdx])), ModuleLogLevel);
                            if ((senderChunk != PackChunking.chunkCode(0, PackChunking.chunkToLen(predMsg[receiverChunkIdx].chunk))) ||
                                (senderHint != predMsg[receiverChunkIdx].hint))
                            {
                                match = false;
                                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "stopped. matching sha1 ", ModuleLogLevel);
                                OnEndOfMatch(predMsg, matchLen, matchChunkCount, firstSenderIdx, firstReceiverIdx, savedOffset);
                                if (matchLen >= (m_data.Length / 3))
                                {
                                    return;
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
                                        return;
                                    }
                                }
                            }
                            break;
                    }
                    LogUtility.LogUtility.LogFile("Sender's offset " + Convert.ToString(offset), ModuleLogLevel);
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
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "match " + Convert.ToString(idx), ModuleLogLevel);
                    break;
                }
            }
            return idx;
        }

        uint IsSha1Match(uint senderFirstIdx, List<ChunkMetaData> receiverChunksList, uint receiverFirstIdx, uint matchLength)
        {
            long sha1;
            uint idx;
#if false
            for (idx = 0; idx < senderFirstIdx; idx++)
            {
                offset += PackChunking.chunkToLen(senderChunkList[(int)idx]);
            }
            for (idx = 0; idx < matchLength; idx++)
            {
                chunkLen = PackChunking.chunkToLen(senderChunkList[(int)senderFirstIdx]);
                sha1 = PackChunking.calcSha1(data, offset, (int)chunkLen);
                if (sha1 != PackChunking.chunkToSha1(receiverChunksList[(int)receiverFirstIdx].chunk))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sha mismatch " + Convert.ToString(idx) + " " + Convert.ToString(senderFirstIdx), ModuleLogLevel);
                    return idx;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sha match " + Convert.ToString(idx) + " " + Convert.ToString(senderFirstIdx), ModuleLogLevel);
                offset += (int)chunkLen;
                senderFirstIdx++;
                receiverFirstIdx++;
            }
#else
            for (idx = 0; idx < matchLength; idx++)
            {
                sha1 = PackChunking.chunkToSha1(m_SenderChunkListWithSha1[(int)senderFirstIdx]);
                if (sha1 != PackChunking.chunkToSha1(receiverChunksList[(int)receiverFirstIdx].chunk))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sha mismatch " + Convert.ToString(idx) + " " + Convert.ToString(senderFirstIdx), ModuleLogLevel);
                    return idx;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sha match " + Convert.ToString(idx) + " " + Convert.ToString(senderFirstIdx), ModuleLogLevel);
                senderFirstIdx++;
                receiverFirstIdx++;
            }
#endif
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
        uint TotalDataReceived;
        uint TotalDataSent;
        uint TotalSavedData;
        uint TotalPreSaved;
        uint TotalPostSaved;
        uint TotalRawSent;
        uint PredMsgReceived;
        uint PredAckMsgSent;
        uint DataMsgSent;
        EndPoint Id;

        object libMutex;
#if false
        static object predMsgListMutex = new object();
        static TimeSpan timeDelta = new TimeSpan(0, 0, 10, 0, 0);
        static Dictionary<IPAddress, LinkedList<PredMsgAndTimeStamp>> predMsgList = new Dictionary<IPAddress, LinkedList<PredMsgAndTimeStamp>>();
        const int SecondsInMinute = 10;
        const int Minutes2Sleep = 1;
        const int MillisInSecond = 1000;
        static void PredMsgCacheTimeOutingThreadProc()
        {
            if (predMsgListMutex == null)
            {
                predMsgListMutex = new object();
            }
            while(true)
            {
                Monitor.Enter(predMsgListMutex);
                try
                {
                    foreach (KeyValuePair<IPAddress,LinkedList<PredMsgAndTimeStamp>> predMsgAndTimeStampKeyValuePair in predMsgList)
                    {
                        LinkedList<PredMsgAndTimeStamp> linkedList = predMsgAndTimeStampKeyValuePair.Value;
                        List<PredMsgAndTimeStamp> removeList = new List<PredMsgAndTimeStamp>();
                        foreach (PredMsgAndTimeStamp predMsgAndTimeStamp in linkedList)
                        {
                            TimeSpan ts = DateTime.Now - predMsgAndTimeStamp.timeStamp;
                            LogUtility.LogUtility.LogFile(" time stamp diff " + Convert.ToString(ts.Minutes) + " " + Convert.ToString(ts.Seconds), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                            if (ts.Seconds > 10)
                            {
                                LogUtility.LogUtility.LogFile(" add to remove list", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                                removeList.Add(predMsgAndTimeStamp);
                            }
                        }
                        foreach (PredMsgAndTimeStamp pmts in removeList)
                        {
                            LogUtility.LogUtility.LogFile("removed " + Convert.ToString(pmts.predMsg.Count) + " " + Convert.ToString(pmts.timeStamp), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                            linkedList.Remove(pmts);
                        }
                        removeList = null;
                    }
                }
                catch(Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace,LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                Monitor.Exit(predMsgListMutex);
                Thread.Sleep(MillisInSecond*SecondsInMinute*Minutes2Sleep);
            }
        }
        static System.Threading.Thread PredMsgCacheTimeOutingThread = new Thread(new ThreadStart(PredMsgCacheTimeOutingThreadProc));
        static bool isThreadStarted = false;
        public static void AddPredMsg(IPAddress ipAddress, List<ChunkMetaDataAndOffset> predMsg)
        {
            bool found = false;
            LinkedList <PredMsgAndTimeStamp> list = null;
            PredMsgAndTimeStamp predMsgAndTimeStamp = new PredMsgAndTimeStamp();
            predMsgAndTimeStamp.predMsg = predMsg;
            predMsgAndTimeStamp.timeStamp = DateTime.Now;
            Monitor.Enter(predMsgListMutex);
            if (!predMsgList.TryGetValue(ipAddress, out list))
            {
                list = new LinkedList<PredMsgAndTimeStamp>();
                predMsgList.Add(ipAddress, list);
                LogUtility.LogUtility.LogFile("Added new list for " + Convert.ToString(ipAddress), LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
#if false
            else
            {
                /* for each message from that host */
                foreach (PredMsgAndTimeStamp msg in list)
                {
                    /* for each chain in the message */
                    foreach (ChunkMetaDataAndOffset chMetaDataAndOffset in msg.predMsg)
                    {
                        int existingChunksListLen = msg.predMsg.Count;
                        int newChunksListLen = predMsg.Count;
                        int existingChunksListIdx = 0;
                        int newChunksListIdx = 0;
                        while((existingChunksListIdx < existingChunksListLen)&&(newChunksListIdx < newChunksListLen))
                        {
                            int existingChunkCount = msg.predMsg[existingChunksListIdx].chunkMetaData.Count;
                            int newChunkCount = predMsg[newChunksListIdx].chunkMetaData.Count;
                            bool notEqual = false;
                            int existingChunkIdx = 0;
                            int newChunkIdx = 0;
                            while((existingChunkIdx < existingChunkCount)&&(newChunkIdx < newChunksListLen))
                            {
                                if(msg.predMsg[existingChunksListIdx].chunkMetaData[existingChunkIdx].chunk !=
                                    predMsg[newChunksListIdx].chunkMetaData[newChunkIdx].chunk)
                                {
                                    notEqual = true;
                                    break;
                                }
                                existingChunkIdx++;
                                newChunkIdx++;
                            }
                            if(!notEqual)
                            {
                                return;
                            }
                        }
                    }
                }
            }
#endif
            if (list.Count >= 10)
            {
                list.RemoveLast();
            }
            LogUtility.LogUtility.LogFile("PRED Message added to list for " + Convert.ToString(ipAddress), LogUtility.LogLevels.LEVEL_LOG_HIGH);
            list.AddFirst(predMsgAndTimeStamp);
            Monitor.Exit(predMsgListMutex);
        }
        static LinkedList<PredMsgAndTimeStamp> GetPredMsg4IpAddress(IPAddress ipAddress)
        {
            LinkedList<PredMsgAndTimeStamp> list = null;
            try
            {
                Monitor.Enter(predMsgListMutex);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace + " mutex " + Convert.ToString(predMsgListMutex == null), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return null;
            }

            if (!predMsgList.TryGetValue(ipAddress, out list))
            {
                Monitor.Exit(predMsgListMutex);
                LogUtility.LogUtility.LogFile("no PRED Message for " + Convert.ToString(ipAddress), ModuleLogLevel);
                return null;
            }
            Monitor.Exit(predMsgListMutex);
            LogUtility.LogUtility.LogFile("PRED Message retrieved for " + Convert.ToString(ipAddress), ModuleLogLevel);
            return list;
        }
        static void UpdateTimeStamp(PredMsgAndTimeStamp predMsgAndTimeStamp)
        {
            try
            {
                predMsgAndTimeStamp.timeStamp = DateTime.Now;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
#else
        List<List<ChunkMetaData>> m_PredMsg;
        public void AddPredMsg(IPAddress ipAddress, List<List<ChunkMetaData>> predMsg)
        {
            m_PredMsg = predMsg;
        }
        List<List<ChunkMetaData>> GetPredMsg4IpAddress(IPAddress ipAddress)
        {
            return m_PredMsg;
        }
#endif
        void InitInstance(byte[]data)
        {
#if false
            if (!isThreadStarted)
            {
                isThreadStarted = true;
                PredMsgCacheTimeOutingThread.Start();
            }
#else
            m_PredMsg = null;
#endif
            packChunking = new PackChunking(8);
            TotalDataReceived = 0;
            TotalDataSent = 0;
            TotalSavedData = 0;
            TotalRawSent = 0;
            PredMsgReceived = 0;
            PredAckMsgSent = 0;
            DataMsgSent = 0;
            libMutex = new object();
            LogUtility.LogUtility.LogFile("SenderLib:InitInstance", LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }

        public uint GetTotalAdded()
        {
            return TotalDataReceived;
        }
        public uint GetTotalSent()
        {
            return TotalDataSent;
        }
        public uint GetTotalSavedData()
        {
            return TotalSavedData;
        }

        public uint GetTotalPreSavedData()
        {
            return TotalPreSaved;
        }

        public uint GetTotalPostSavedData()
        {
            return TotalPostSaved;
        }

        public SenderPackLib(byte[] data) : base(null)
        {
            Id = new IPEndPoint(0,0);
            InitInstance(data);
        }

        void ForwardData(byte[] data)
        {
            int offset;
            byte []msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)data.Length, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset);
            data.CopyTo(msg, offset);
            onMessageReadyToTx(onTxMessageParam, msg);
            TotalDataSent += (uint)data.Length;
            TotalRawSent += (uint)data.Length;
            DataMsgSent++;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " PreSaved " + Convert.ToString(TotalPreSaved) + " Saved " + Convert.ToString(TotalSavedData) + " PostSaved " + Convert.ToString(TotalPostSaved) + " Received from server " + Convert.ToString(TotalDataReceived) + " Total sent to client " + Convert.ToString(TotalDataSent) + " Sent raw " + Convert.ToString(TotalRawSent), ModuleLogLevel);
        }

        void SendChunksData(byte []data,int count)
        {
            int idx,offset_in_msg;
            byte[] msg;

            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Sending pre-PredAck DATA " + Convert.ToString(count), ModuleLogLevel);
            msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)count, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset_in_msg);
            for (idx = 0; idx < count; idx++)
            {
                msg[idx + offset_in_msg] = data[idx];
            }
            onMessageReadyToTx(onTxMessageParam, msg);
            TotalDataSent += (uint)count;
            TotalPreSaved += (uint)count;
            DataMsgSent++;
        }

        public void AddData(byte[] data)
        {
            int offset;
            //int receiverChunkIdx;
            byte[] msg;
            Monitor.Enter(libMutex);
            uint offset_in_stream = TotalDataSent + TotalSavedData;
            
            TotalDataReceived += (uint)data.Length;
            LogUtility.LogUtility.LogFile("AddData: " + Convert.ToString(data.Length) + " bytes received", ModuleLogLevel);
            
            LongestMatch longestMatch = new LongestMatch();
            List<List<ChunkMetaData>> predMsgs = GetPredMsg4IpAddress(((IPEndPoint)Id).Address);

            if (predMsgs != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " AddData: process " + Convert.ToString(predMsgs.Count) + " chains total received " + Convert.ToString(TotalDataReceived) + " total sent " + Convert.ToString(TotalDataSent) + " total saved " + Convert.ToString(TotalSavedData) + " data len " + Convert.ToString(data.Length), ModuleLogLevel);
                MatchStateMachine matchStateMachine = new MatchStateMachine(Id, longestMatch, packChunking, data, predMsgs);
                matchStateMachine.Run();
            }
            
            if (!longestMatch.IsMatchFound())
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " no match at all", ModuleLogLevel);
                ForwardData(data);
                Monitor.Exit(libMutex);
                return;
            }
            if (longestMatch.GetPrecedingBytesCount() > 0)
            {
                SendChunksData(data, (int)longestMatch.GetPrecedingBytesCount());
            }
            
            byte []buff = PackMsg.PackMsg.AllocateMsgAndBuildHeader(GetPredictionAckMessageSize((uint)longestMatch.GetLongestChunkCount()), 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, out offset);
            EncodePredictionAckMessage(buff, offset, longestMatch.GetPredMsg(), (uint)longestMatch.GetLongestChunkCount(), longestMatch.GetFirstReceiverChunkIdx());
            onMessageReadyToTx(onTxMessageParam, buff);
            PredAckMsgSent++;
            int remainder = longestMatch.GetRemainder(data.Length);
            int alreadySent = (int)longestMatch.GetPrecedingBytesCount() + (int)longestMatch.GetLongestChainLen();
            if ((alreadySent + remainder) != data.Length)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " something wrong: prec+chainLen+remainder " + Convert.ToString(longestMatch.GetPrecedingBytesCount()) + " " + Convert.ToString(longestMatch.GetLongestChainLen()) + " " + Convert.ToString(remainder) + " does not equal data.length " + Convert.ToString(data.Length), ModuleLogLevel);
            }
            
            if (remainder > 0)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sending pos-PredAck data " + Convert.ToString(remainder), ModuleLogLevel);
                msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)remainder, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset);
                for (int i = alreadySent; i < data.Length; i++)
                {
                    msg[(i - alreadySent) + offset] = data[i];
                }
                onMessageReadyToTx(onTxMessageParam, msg);
                TotalDataSent += (uint)remainder;
                TotalPostSaved += (uint)remainder;
                DataMsgSent++;
            }
            longestMatch.GetPredMsg().RemoveRange((int)longestMatch.GetFirstReceiverChunkIdx(), (int)longestMatch.GetLongestChunkCount());
#if false
            longestMatch.GetPredMsg()[(int)longestMatch.GetLongestChainIdx()].chunkMetaData.RemoveRange((int)longestMatch.GetFirstReceiverChunkIdx(), (int)longestMatch.GetLongestChainLen());
            longestMatch.GetPredMsg()[(int)longestMatch.GetLongestChainIdx()].chunkMetaData.RemoveRange(0, (int)longestMatch.GetFirstReceiverChunkIdx());
#endif
            Monitor.Exit(libMutex);
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " PreSaved " + Convert.ToString(TotalPreSaved) + " Saved " + Convert.ToString(TotalSavedData) + " PostSaved " + Convert.ToString(TotalPostSaved) + " Received from server " + Convert.ToString(TotalDataReceived) + " Total sent to client " + Convert.ToString(TotalDataSent) + " Sent raw " + Convert.ToString(TotalRawSent), ModuleLogLevel);
        }

        public void ClearData()
        {
            LogUtility.LogUtility.LogFile("SenderLib:ClearData", LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }

        public SenderPackLib(byte[] data, OnMessageReadyToTx onMsgReadyToTx) : base(onMsgReadyToTx)
        {
            Id = new IPEndPoint(0,0);
            ChunkChainDataTypes.OnMessageReceived onPredMsg = new ChunkChainDataTypes.OnMessageReceived(ProcessPredMsg);
            base.SetCallback((int)PackMsg.PackMsg.MsgKind_e.PACK_PRED_MSG_KIND, onPredMsg);
            InitInstance(data);
        }

        public SenderPackLib(EndPoint id, OnMessageReadyToTx onMsgReadyToTx)
            : base(onMsgReadyToTx)
        {
            Id = id;
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sending PRED ACK saved now " + Convert.ToString(thisTimeSaved) + " chunks " + Convert.ToString((chunkCounter - firstChunk)), ModuleLogLevel);
            TotalSavedData += (uint)thisTimeSaved;
        }
        
        byte []ProcessPredMsg(byte []packet,int offset,byte Flags,int room_space)
        {
            List<List<ChunkMetaData>> chunkMetaDataList;
            uint decodedOffsetInStream;
            Monitor.Enter(libMutex);
            uint offset_in_stream = TotalDataSent + TotalSavedData;
            PredMsgReceived++;
            LogUtility.LogUtility.LogFile("PRED Message", ModuleLogLevel);
            chunkMetaDataList = DecodePredictionMessage(packet, offset, out decodedOffsetInStream);
            LogUtility.LogUtility.LogFile("chainOffset " + Convert.ToString(decodedOffsetInStream) + " TotalSent " + Convert.ToString(TotalDataSent) + " TotalSaved " + Convert.ToString(TotalSavedData), ModuleLogLevel);
            if (chunkMetaDataList == null)
            {
                Monitor.Exit(libMutex);
                return null;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " processing " + Convert.ToString(chunkMetaDataList.Count) + " chains, offset " + Convert.ToString(decodedOffsetInStream) + " offset in stream " + Convert.ToString(offset_in_stream), ModuleLogLevel);
#if false            
            for (int i = 0; i < chunkMetaDataAndIdList.Count;i++)
            {
                uint decodedOffsetInStreamCopy = decodedOffsetInStream;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " chunks in this chain " + Convert.ToString(chunkMetaDataAndIdList[i].chunkMetaData.Count), ModuleLogLevel);
                while ((chunkMetaDataAndIdList[i].chunkMetaData.Count > 0) && (decodedOffsetInStreamCopy < offset_in_stream))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " chunk  " + Convert.ToString(PackChunking.chunkToLen(chunkMetaDataAndIdList[i].chunkMetaData[0].chunk)), ModuleLogLevel);
                    if ((decodedOffsetInStreamCopy + PackChunking.chunkToLen(chunkMetaDataAndIdList[i].chunkMetaData[0].chunk)) <= offset_in_stream)
                    {
                        decodedOffsetInStreamCopy += (uint)PackChunking.chunkToLen(chunkMetaDataAndIdList[i].chunkMetaData[0].chunk);
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " remove entire chunk " + Convert.ToString(PackChunking.chunkToLen(chunkMetaDataAndIdList[i].chunkMetaData[0].chunk)) + " updated offset " + Convert.ToString(decodedOffsetInStreamCopy), ModuleLogLevel);
                        chunkMetaDataAndIdList[i].chunkMetaData.RemoveAt(0);
                    }
                    else
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " updating offset chunk len " + Convert.ToString(PackChunking.chunkToLen(chunkMetaDataAndIdList[i].chunkMetaData[0].chunk)) + " delta to current stream pos " + Convert.ToString(offset_in_stream - decodedOffsetInStreamCopy) + " and removing first chunk ", ModuleLogLevel);
                        //ChunkMetaDataAndOffset temp = chunkMetaDataAndIdList[i];
                        ChunkMetaDataAndOffset temp = chunkMetaDataAndIdList.ElementAt(i);
                        temp.offset = (uint)PackChunking.chunkToLen(chunkMetaDataAndIdList[i].chunkMetaData[0].chunk) - (offset_in_stream - decodedOffsetInStreamCopy);
                        chunkMetaDataAndIdList[i].chunkMetaData.RemoveAt(0);
                        //chunkMetaDataAndIdList.RemoveAt(i);
                        //chunkMetaDataAndIdList.Insert(i, temp);
                        if (newChunkMetaDataAndIdList == null)
                        {
                            newChunkMetaDataAndIdList = new List<ChunkMetaDataAndOffset>();
                        }
                        newChunkMetaDataAndIdList.Add(temp);
                        break;
                    }
                }
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " leavingProcessPredMsg", ModuleLogLevel);
            predMsg = newChunkMetaDataAndIdList;
#else
            AddPredMsg(((IPEndPoint)Id).Address, chunkMetaDataList);
#endif
            Monitor.Exit(libMutex);
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
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Message cannot be decoded", LogUtility.LogLevels.LEVEL_LOG_HIGH);
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

            debugInfo += " TotalDataReceived " + Convert.ToString(TotalDataReceived) + " TotalSent " + Convert.ToString(TotalDataSent) + " TotalSaved " + Convert.ToString(TotalSavedData) + " TotalPreSaved " + Convert.ToString(TotalPreSaved) + " TotalPostSaved " + Convert.ToString(TotalPostSaved) + " PredMsgReceived " + Convert.ToString(PredMsgReceived) + " PredAckSent" + Convert.ToString(PredAckMsgSent) + " DataMsgSent " + Convert.ToString(DataMsgSent) + " " + base.GetDebugInfo();
            return debugInfo;
        }
    }
}
