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
    class PredMsgAndTimeStamp
    {
        public List<ChunkMetaDataAndOffset> predMsg;
        public DateTime timeStamp;
    };
    class LongestMatch
    {
        bool m_ChainNotFound;
        uint m_longestChain;
        uint m_longestChainLen;
        uint m_longestProcessedBytes;
        List<long> m_longestChunkList;
        uint m_longestChainSenderFirstChunkIdx;
        uint m_longestChainReceiverFirstChunkIdx;
        PredMsgAndTimeStamp m_predMsg;
        private void SetFields(PredMsgAndTimeStamp predMsg,uint longestChain,uint longestChainLen,uint longestProcessedBytes,List<long> longestChunkList,uint longestChainSenderFirstChunkIdx,uint longestChainReceiverFirstChunkIdx)
        {
            m_predMsg = predMsg;
            m_longestChain = longestChain;
            m_longestChainLen = longestChainLen;
            m_longestProcessedBytes = longestProcessedBytes;
            m_longestChunkList = longestChunkList;
            m_longestChainSenderFirstChunkIdx = longestChainSenderFirstChunkIdx;
            m_longestChainReceiverFirstChunkIdx = longestChainReceiverFirstChunkIdx;
        }
        public void UpdateFields(PredMsgAndTimeStamp predMsg,uint longestChain, uint longestChainLen, uint longestProcessedBytes,List<long> longestChunkList, uint longestChainSenderFirstChunkIdx, uint longestChainReceiverFirstChunkIdx)
        {
            SetFields(predMsg,longestChain, longestChainLen, longestProcessedBytes, longestChunkList, longestChainSenderFirstChunkIdx, longestChainReceiverFirstChunkIdx);
            m_ChainNotFound = false;
        }
        public bool IsLonger(uint matchLen)
        {
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
        public uint GetLongestChainIdx()
        {
            return m_longestChain;
        }
        public uint GetLongestChainLen()
        {
            return m_longestChainLen;
        }
        public uint GetLongestProcessedBytes()
        {
            return m_longestProcessedBytes;
        }
        public int GetReminder(int length)
        {
            return (length - (int)m_longestProcessedBytes);
        }
        public PredMsgAndTimeStamp GetPredMsg()
        {
            return m_predMsg;
        }
        public LongestMatch()
        {
            m_ChainNotFound = true;
            SetFields(null,0, 0, 0, null, 0, 0);
        }
    }
    class MatchStateMachine
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        LongestMatch m_LongestMatch;
        StreamChunckingLib.PackChunking m_packChunking;
        EndPoint m_Id;
        byte[] m_data;
        int m_offset;
        int m_chainIdx;
        
        public MatchStateMachine(EndPoint Id, int chainIdx,LongestMatch longestMatch, StreamChunckingLib.PackChunking packChunking, byte[] data, int offset)
        {
            m_LongestMatch = longestMatch;
            m_packChunking = packChunking;
            m_Id = Id;
            m_data = data;
            m_offset = offset;
            m_chainIdx = chainIdx;
        }
        public void Run(PredMsgAndTimeStamp predMsg)
        {
            try
            {
                uint matchLen = 0;
                uint firstSenderIdx = 0;
                uint firstReceiverIdx = 0;
                int senderChunkIdx = 0;
                int receiverChunkIdx = 0;
                bool match = false;
                List<long> senderChunkList = new List<long>();
                int processedBytes = m_packChunking.getChunks(senderChunkList, m_data, (int)m_offset, m_data.Length, true, false);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " offset " + Convert.ToString(m_offset) + " processedBytes " + Convert.ToString(processedBytes) + " chunk count " + Convert.ToString(senderChunkList.Count), ModuleLogLevel);
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
                while ((senderChunkIdx < senderChunkList.Count) && (receiverChunkIdx < predMsg.predMsg[m_chainIdx].chunkMetaData.Count))
                {
                    byte senderHint = PackChunking.GetChunkHint(m_data, (uint)m_offset, (uint)PackChunking.chunkToLen(senderChunkList[senderChunkIdx]));
                    long senderChunk = PackChunking.chunkCode(0, PackChunking.chunkToLen(senderChunkList[senderChunkIdx]));
                    switch (match)
                    {
                        case false:
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " in non-match " + Convert.ToString(PackChunking.chunkToLen(senderChunkList[senderChunkIdx])), ModuleLogLevel);
                            receiverChunkIdx = (int)FindFirstMatchingChunk(predMsg.predMsg[m_chainIdx].chunkMetaData, senderChunk, senderHint);
                            if (receiverChunkIdx != predMsg.predMsg[m_chainIdx].chunkMetaData.Count)
                            {
                                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " match " + Convert.ToString(PackChunking.chunkToLen(predMsg.predMsg[m_chainIdx].chunkMetaData[(int)receiverChunkIdx].chunk)), ModuleLogLevel);
                                match = true;
                                firstReceiverIdx = (uint)receiverChunkIdx;
                                firstSenderIdx = (uint)senderChunkIdx;
                                matchLen = 1;
                                receiverChunkIdx++;
                            }
                            else
                            {
                                receiverChunkIdx = 0;
                            }
                            break;
                        case true:
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "in match " + Convert.ToString(PackChunking.chunkToLen(senderChunkList[senderChunkIdx])), ModuleLogLevel);
                            if ((senderChunk != PackChunking.chunkCode(0, PackChunking.chunkToLen(predMsg.predMsg[m_chainIdx].chunkMetaData[receiverChunkIdx].chunk))) ||
                                (senderHint != predMsg.predMsg[m_chainIdx].chunkMetaData[receiverChunkIdx].hint))
                            {
                                match = false;
                                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "stopped. matching sha1 ", ModuleLogLevel);
                                if (m_LongestMatch.IsLonger(matchLen))
                                {
                                    matchLen = IsSha1Match(senderChunkList, firstSenderIdx, predMsg.predMsg[m_chainIdx].chunkMetaData, firstReceiverIdx, matchLen, m_data, (int)predMsg.predMsg[m_chainIdx].offset);
                                    if (m_LongestMatch.IsLonger(matchLen))
                                    {
                                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " chain longer " + Convert.ToString(senderChunkIdx) + " " + Convert.ToString(m_LongestMatch.GetLongestChainLen()), ModuleLogLevel);
                                        m_LongestMatch.UpdateFields(predMsg, (uint)m_chainIdx, matchLen, (uint)processedBytes, senderChunkList, firstSenderIdx, firstReceiverIdx);
                                    }
                                }
                            }
                            else
                            {
                                matchLen++;
                                receiverChunkIdx++;
                                if (senderChunkIdx == (senderChunkList.Count - 1))
                                {
                                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "stopped (end). matching sha1 ", ModuleLogLevel);
                                    if (m_LongestMatch.IsLonger(matchLen))
                                    {
                                        matchLen = IsSha1Match(senderChunkList, firstSenderIdx, predMsg.predMsg[m_chainIdx].chunkMetaData, firstReceiverIdx, matchLen, m_data, (int)predMsg.predMsg[m_chainIdx].offset);
                                        if (m_LongestMatch.IsLonger(matchLen))
                                        {
                                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " chain longer " + Convert.ToString(senderChunkIdx) + " " + Convert.ToString(m_LongestMatch.GetLongestChainLen()), ModuleLogLevel);
                                            m_LongestMatch.UpdateFields(predMsg, (uint)m_chainIdx, matchLen, (uint)processedBytes, senderChunkList, firstSenderIdx, firstReceiverIdx);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    m_offset += (int)PackChunking.chunkToLen(senderChunkList[senderChunkIdx]);
                    senderChunkIdx++;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace + " " + exc.InnerException, LogUtility.LogLevels.LEVEL_LOG_HIGH);
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

        uint IsSha1Match(List<long> senderChunkList, uint senderFirstIdx, List<ChunkMetaData> receiverChunksList, uint receiverFirstIdx, uint matchLength, byte[] data, int offset)
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
            senderChunkList = new List<long>();
            m_packChunking.getChunks(senderChunkList, data, (int)offset, data.Length, true, true);
            for (idx = 0; idx < matchLength; idx++)
            {
                sha1 = PackChunking.chunkToSha1(senderChunkList[(int)senderFirstIdx]);
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
        uint PredMsgReceived;
        uint PredAckMsgSent;
        uint DataMsgSent;
        EndPoint Id;

        object libMutex;
#if true
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
        LinkedList<PredMsgAndTimeStamp> m_PredMsgList;
        public void AddPredMsg(IPAddress ipAddress, List<ChunkMetaDataAndOffset> predMsg)
        {
            PredMsgAndTimeStamp pmts = new PredMsgAndTimeStamp();
            pmts.predMsg = predMsg;
            pmts.timeStamp = DateTime.Now;
            m_PredMsgList = new LinkedList<PredMsgAndTimeStamp>();
            m_PredMsgList.AddFirst(pmts);
        }
        LinkedList<PredMsgAndTimeStamp> GetPredMsg4IpAddress(IPAddress ipAddress)
        {
            return m_PredMsgList;
        }
        void UpdateTimeStamp(PredMsgAndTimeStamp predMsgAndTimeStamp)
        {
        }
#endif
        void InitInstance(byte[]data)
        {
#if true
            if (!isThreadStarted)
            {
                isThreadStarted = true;
                PredMsgCacheTimeOutingThread.Start();
            }
#else
            m_PredMsgList = null;
#endif
            packChunking = new PackChunking(8);
            TotalDataReceived = 0;
            TotalDataSent = 0;
            TotalSavedData = 0;
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
            DataMsgSent++;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " forwarded " + Convert.ToString(msg.Length) + " TotalSent " + Convert.ToString(TotalDataSent), ModuleLogLevel);
        }

        void SendChunksData(List<long> senderChunkList, uint chunksData2Send,byte []data,int offset)
        {
            uint overall_msg_len = (uint)offset;
            int idx,offset_in_msg;
            byte[] msg;

            for (idx = 0; idx < chunksData2Send; idx++)
            {
                overall_msg_len += (uint)PackChunking.chunkToLen(senderChunkList[idx]);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " calculated " + Convert.ToString(overall_msg_len) + " for " + Convert.ToString(chunksData2Send) + " chunks, offset " + Convert.ToString(offset), ModuleLogLevel);
            msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader(overall_msg_len, 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset_in_msg);
            for (idx = 0; idx < overall_msg_len; idx++)
            {
                msg[idx + offset_in_msg] = data[idx];
            }
            onMessageReadyToTx(onTxMessageParam, msg);
            TotalDataSent += overall_msg_len;
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
            
            LongestMatch longestMatch = new LongestMatch();
            LinkedList<PredMsgAndTimeStamp> predMsgsAndTimeStamp = GetPredMsg4IpAddress(((IPEndPoint)Id).Address);

            if (predMsgsAndTimeStamp != null)
            {
                foreach(PredMsgAndTimeStamp predMsgAndTimeStamp in predMsgsAndTimeStamp)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " AddData: process " + Convert.ToString(predMsgAndTimeStamp.predMsg.Count) + " chains total received " + Convert.ToString(TotalDataReceived) + " total sent " + Convert.ToString(TotalDataSent) + " total saved " + Convert.ToString(TotalSavedData) + " data len " + Convert.ToString(data.Length), ModuleLogLevel);
                    for (int chainIdx = 0; chainIdx < predMsgAndTimeStamp.predMsg.Count; chainIdx++)
                    {
                        MatchStateMachine matchStateMachine = new MatchStateMachine(Id, chainIdx, longestMatch, packChunking, data, (int)predMsgAndTimeStamp.predMsg[chainIdx].offset);
                        matchStateMachine.Run(predMsgAndTimeStamp);
                    }
                }
            }
            
            if (!longestMatch.IsMatchFound())
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " no match at all", ModuleLogLevel);
                ForwardData(data);
                Monitor.Exit(libMutex);
                return;
            }
            if (longestMatch.GetFirstSenderChunkIdx() > 0)
            {
                SendChunksData(longestMatch.GetChunkList(), longestMatch.GetFirstSenderChunkIdx(), data, (int)longestMatch.GetPredMsg().predMsg[(int)longestMatch.GetLongestChainIdx()].offset);
            }
            //offset = (int)predMsg[(int)longestMatch.GetLongestChainIdx()].offset;
            //receiverChunkIdx = (int)longestMatch.GetFirstReceiverChunkIdx();
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " preparing and sending PRED ACK total saved data total saved now " + Convert.ToString(TotalSavedData) + " chunks " + Convert.ToString(longestMatch.GetLongestChainLen()), ModuleLogLevel);
            byte []buff = PackMsg.PackMsg.AllocateMsgAndBuildHeader(GetPredictionAckMessageSize((uint)longestMatch.GetLongestChainLen()), 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_PRED_ACK_MSG_KIND, out offset);
            EncodePredictionAckMessage(buff, offset, longestMatch.GetPredMsg().predMsg[(int)longestMatch.GetLongestChainIdx()], (uint)longestMatch.GetLongestChainLen(), longestMatch.GetFirstReceiverChunkIdx());
            onMessageReadyToTx(onTxMessageParam, buff);
            PredAckMsgSent++;
            UpdateTimeStamp(longestMatch.GetPredMsg());
            if (longestMatch.GetReminder(data.Length) > 0)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sending data after chain " + Convert.ToString(longestMatch.GetReminder(data.Length)), ModuleLogLevel);
                msg = PackMsg.PackMsg.AllocateMsgAndBuildHeader((uint)longestMatch.GetReminder(data.Length), 0, (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND, out offset);
                for (int i = (int)longestMatch.GetLongestProcessedBytes(); i < data.Length; i++)
                {
                    msg[(i - longestMatch.GetLongestProcessedBytes()) + offset] = data[i];
                }
                onMessageReadyToTx(onTxMessageParam, msg);
                TotalDataSent += (uint)longestMatch.GetReminder(data.Length);
                DataMsgSent++;
            }
#if false
            longestMatch.GetPredMsg()[(int)longestMatch.GetLongestChainIdx()].chunkMetaData.RemoveRange((int)longestMatch.GetFirstReceiverChunkIdx(), (int)longestMatch.GetLongestChainLen());
            longestMatch.GetPredMsg()[(int)longestMatch.GetLongestChainIdx()].chunkMetaData.RemoveRange(0, (int)longestMatch.GetFirstReceiverChunkIdx());
#endif
            Monitor.Exit(libMutex);
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
        List<ChunkMetaDataAndOffset> DecodePredictionMessage(byte[] buffer, int offset, out uint decodedOffsetInStream)
        {
            uint buffer_idx = (uint)offset;
            uint chainsListSize;
            List<ChunkMetaDataAndOffset> chainsList;

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out decodedOffsetInStream);

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out chainsListSize);

            chainsList = new List<ChunkMetaDataAndOffset>((int)chainsListSize);

            for (int chain_idx = 0; chain_idx < chainsListSize;chain_idx++ )
            {
                ChunkMetaDataAndOffset chunkMetaDataAndId = new ChunkMetaDataAndOffset();
                uint chunkListSize;
                
                buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(buffer, buffer_idx, out chunkListSize);
                chunkMetaDataAndId.chunkMetaData = new List<ChunkMetaData>((int)chunkListSize);
                for (uint idx = 0; idx < chunkListSize; idx++)
                {
                    ChunkMetaData chunkMetaData = new ChunkMetaData();
                    chunkMetaData.hint = buffer[buffer_idx++];
                    buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Long(buffer, buffer_idx, out chunkMetaData.chunk);
                    chunkMetaDataAndId.chunkMetaData.Add(chunkMetaData);
                }
                chainsList.Add(chunkMetaDataAndId);
            }
            return chainsList;
        }
        uint GetPredictionAckMessageSize(uint chunksCount)
        {
            return (sizeof(uint) + sizeof(uint) /*+ sizeof(uint)*/ + (uint)chunksCount * ChunkMetaData.GetSize());
        }
        void EncodePredictionAckMessage(byte[] buffer, int offset, ChunkMetaDataAndOffset chunkMetaDataAndId,uint chunksCount, uint firstChunk)
        {
            uint buffer_idx = (uint)offset;
            uint chunkCounter = 0;

            buffer_idx +=
                    ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(buffer, buffer_idx, chunksCount);
            
            foreach (ChunkMetaData chunkMetaData in chunkMetaDataAndId.chunkMetaData)
            {
                if (chunkCounter >= firstChunk)
                {
                    buffer[buffer_idx++] = chunkMetaData.hint;
                    buffer_idx +=
                        ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Long2ByteArray(buffer, buffer_idx, chunkMetaData.chunk);
                    TotalSavedData += (uint)PackChunking.chunkToLen(chunkMetaData.chunk);
                }
                chunkCounter++;
                if ((chunkCounter-firstChunk) == chunksCount)
                {
                    break;
                }
            }
        }
        
        byte []ProcessPredMsg(byte []packet,int offset,byte Flags,int room_space)
        {
            List<ChunkMetaDataAndOffset> chunkMetaDataAndIdList;
            uint decodedOffsetInStream;
            Monitor.Enter(libMutex);
            uint offset_in_stream = TotalDataSent + TotalSavedData;
            PredMsgReceived++;
            LogUtility.LogUtility.LogFile("PRED Message", ModuleLogLevel);
            chunkMetaDataAndIdList = DecodePredictionMessage(packet, offset, out decodedOffsetInStream);
            LogUtility.LogUtility.LogFile("chainOffset " + Convert.ToString(decodedOffsetInStream) + " TotalSent " + Convert.ToString(TotalDataSent) + " TotalSaved " + Convert.ToString(TotalSavedData), ModuleLogLevel);
            if (chunkMetaDataAndIdList == null)
            {
                Monitor.Exit(libMutex);
                return null;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " processing " + Convert.ToString(chunkMetaDataAndIdList.Count) + " chains, offset " + Convert.ToString(decodedOffsetInStream) + " offset in stream " + Convert.ToString(offset_in_stream), ModuleLogLevel);
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
            AddPredMsg(((IPEndPoint)Id).Address, chunkMetaDataAndIdList);
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

            debugInfo += " TotalDataReceived " + Convert.ToString(TotalDataReceived) + " TotalSent " + Convert.ToString(TotalDataSent) + " TotalSaved " + Convert.ToString(TotalSavedData) + " PredMsgReceived " + Convert.ToString(PredMsgReceived) + " PredAckSent" + Convert.ToString(PredAckMsgSent) + " DataMsgSent " + Convert.ToString(DataMsgSent) + " " + base.GetDebugInfo();
            return debugInfo;
        }
    }
}
