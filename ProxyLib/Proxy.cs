using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RxTxStateMachine;
using ProxyLibTypes;
using PackMsg;
//using System.Runtime.Remoting.Contexts;

namespace ProxyLib
{
    //[Synchronization()]
    public abstract class Proxy
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_HIGH;

#if false
        protected Queue m_clientTxQueue;
        protected Queue m_destinationTxQueue;
#else
        protected LinkedList<byte []> m_clientTxQueue;
        protected LinkedList<byte []> m_destinationTxQueue;
        protected object m_clientTxQueueMutex;
        protected object m_destinationTxQueueMutex;
#endif
        protected Socket m_clientSideSocket;
        protected Socket m_destinationSideSocket;
        protected RxStateMachine m_rxStateMachine;
        protected TxStateMachine m_txStateMachine;
        protected object m_ProprietarySegmentTxMutex;
        protected object m_NonProprietarySegmentTxMutex;
        protected object m_ProprietarySegmentRxMutex;
        protected object m_NonProprietarySegmentRxMutex;
        protected object m_disposeMutex;
        protected SocketAsyncEventArgs m_OnNonProprietaryReceivedCbk;
        protected SocketAsyncEventArgs m_OnNonProprietaryTransmittedCbk;
        protected SocketAsyncEventArgs m_OnProprietaryReceivedCbk;
        protected SocketAsyncEventArgs m_OnProprietaryTransmittedCbk;
        SocketAsyncEventArgs m_clientSideDiscAsyncArgs;
        SocketAsyncEventArgs m_ServerSideDiscAsyncArgs;
        static protected object m_proprietaryLibMutex = new object();

        protected object m_clientStreamMutex;
        protected object m_destinationStreamMutex;

        protected bool  m_ProprietarySegmentTxInProgress;
        protected bool  m_ProprietarySegmentRxInProgress;
        protected bool  m_NonProprietarySegmentTxInProgress;
        protected bool  m_NonProprietarySegmentRxInProgress;
        protected bool   m_IsMsgBeingTransmitted2Client;
        protected bool m_IsMsgBeingTransmitted2Destination;
        protected EndPoint m_Id;
        protected byte []m_NonProprietarySegmentRxBuf;
        protected byte[] m_ProprietarySementRxBuf;
        protected bool m_ShutDownFlag;
        protected uint m_TransmittedClient;
        protected uint m_ReceivedClient;
        protected uint m_TransmittedServer;
        protected uint m_ReceivedServer;
        protected uint m_TransmittedMsgsClient;
        protected uint m_ReceivedMsgs;
        protected uint m_SubmittedMsgsClient;
        protected uint m_SubmittedMsgsServer;
        protected uint m_SubmittedClient;
        protected uint m_SubmittedServer;
        protected uint m_Saved;
        protected MyMemoryStream.MyMemoryStream m_clientStream;
        protected MyMemoryStream.MyMemoryStream m_destinationStream;
        public delegate void OnGotResults(object res);
        protected OnGotResults m_onGotResults;
        public delegate void OnDisposed(Proxy p);
        OnDisposed m_onDisposed;
        TimeSpan m_InfiniteWaitTs;
        TimeSpan m_ImmediateReturnTs;
        protected DateTime m_ProprietaryRxInitiatedTs;
        protected DateTime m_ProprietaryRxCompletedTs;
        protected DateTime m_ProprietaryTxInitiatedTs;
        protected DateTime m_ProprietaryTxCompletedTs;
        protected DateTime m_NonProprietaryRxInitiatedTs;
        protected DateTime m_NonProprietaryRxCompletedTs;
        protected DateTime m_NonProprietaryTxInitiatedTs;
        protected DateTime m_NonProprietaryTxCompletedTs;

        public static void InitGlobalObjects()
        {
            PackClientSide.InitGlobalObjects();
        }

        public Proxy()
        {
#if false
            m_clientTxQueue = new Queue();
            m_destinationTxQueue = new Queue();
#else
            m_clientTxQueue = new LinkedList<byte[]>();
            m_destinationTxQueue = new LinkedList<byte[]>();
            m_clientTxQueueMutex = new object();
            m_destinationTxQueueMutex = new object();
#endif
            //ProprietarySegmentTxMutex = new Mutex();
            //NonProprietarySegmentTxMutex = new Mutex();
            //ProprietarySegmentRxMutex = new Mutex();
            //NonProprietarySegmentRxMutex = new Mutex();
            m_ProprietarySegmentTxMutex = new object();
            m_NonProprietarySegmentTxMutex = new object();
            m_ProprietarySegmentRxMutex = new object();
            m_NonProprietarySegmentRxMutex = new object();
            m_clientStreamMutex = new object();
            m_destinationStreamMutex = new object();
            m_disposeMutex = new object();
            m_ProprietarySegmentTxInProgress = false;
            m_ProprietarySegmentRxInProgress = false;
            m_NonProprietarySegmentTxInProgress = false;
            m_NonProprietarySegmentRxInProgress = false;
            m_IsMsgBeingTransmitted2Client = false;
            m_IsMsgBeingTransmitted2Destination = false;
            m_NonProprietarySegmentRxBuf = new byte[8192 * 4];
            m_ProprietarySementRxBuf = new byte[8192*4];
            m_rxStateMachine = new RxStateMachine(m_Id);
            m_txStateMachine = new TxStateMachine(m_Id);
            m_OnNonProprietaryReceivedCbk = new SocketAsyncEventArgs();
            m_OnNonProprietaryTransmittedCbk = new SocketAsyncEventArgs();
            m_OnProprietaryReceivedCbk = new SocketAsyncEventArgs();
            m_OnProprietaryTransmittedCbk = new SocketAsyncEventArgs();

            m_OnNonProprietaryReceivedCbk.Completed += new EventHandler<SocketAsyncEventArgs>(OnNonProprietarySegmentReceived);
            m_OnNonProprietaryTransmittedCbk.Completed += new EventHandler<SocketAsyncEventArgs>(OnNonProprietarySegmentTransmitted);
            m_OnProprietaryReceivedCbk.Completed += new EventHandler<SocketAsyncEventArgs>(OnProprietarySegmentReceived);
            m_OnProprietaryTransmittedCbk.Completed += new EventHandler<SocketAsyncEventArgs>(OnProprietarySegmentTransmitted);
            m_ShutDownFlag = false;
            m_TransmittedClient = 0;
            m_ReceivedClient = 0;
            m_TransmittedServer = 0;
            m_ReceivedServer = 0;
            m_TransmittedMsgsClient = 0;
            m_ReceivedMsgs = 0;
            m_SubmittedMsgsClient = 0;
            m_SubmittedMsgsServer = 0;
            m_SubmittedClient = 0;
            m_SubmittedServer = 0;
            m_Saved = 0;
            m_clientStream = new MyMemoryStream.MyMemoryStream();
            m_destinationStream = new MyMemoryStream.MyMemoryStream();
            m_onGotResults = null;
            m_onDisposed = null;
            m_clientSideDiscAsyncArgs = new SocketAsyncEventArgs();
            m_clientSideDiscAsyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnClientDisconnected);
            m_ServerSideDiscAsyncArgs = new SocketAsyncEventArgs();
            m_ServerSideDiscAsyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnDestinationDisconnected);
            m_InfiniteWaitTs = new TimeSpan(0, 0, 0, 0, -1);
            m_ImmediateReturnTs = new TimeSpan(0, 0, 0, 0, 0);
            m_ProprietaryRxInitiatedTs = new DateTime();
            m_ProprietaryRxCompletedTs = new DateTime();
            m_ProprietaryTxInitiatedTs = new DateTime();
            m_ProprietaryTxCompletedTs = new DateTime();
            m_NonProprietaryRxInitiatedTs = new DateTime();
            m_NonProprietaryRxCompletedTs = new DateTime();
            m_NonProprietaryTxInitiatedTs = new DateTime();
            m_NonProprietaryTxCompletedTs = new DateTime();
            LogUtility.LogUtility.LogFile("Started at " + DateTime.Now.ToLongTimeString(), LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }

        protected abstract void OnProprietarySegmentReceived(object sender, SocketAsyncEventArgs e);
        protected abstract void OnProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e);
        protected abstract void OnNonProprietarySegmentReceived(object sender, SocketAsyncEventArgs e);
        protected abstract void OnNonProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e);
        virtual public void SetRemoteEndpoint(IPEndPoint ipEndpoint)
        {
        }
        virtual public void SetOnGotResults(OnGotResults cbk)
        {
            m_onGotResults = cbk;
        }
        virtual public void SetOnDisposed(OnDisposed cbk)
        {
            m_onDisposed = cbk;
        }
        virtual public object GetResults()
        {
            return null;
        }
        public bool ProprietaryTxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(m_ProprietarySegmentTxMutex,100);
            if (ret)
            {
                Monitor.Exit(m_ProprietarySegmentTxMutex);
            }
            return ret;
        }
        public bool ProprietaryRxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(m_ProprietarySegmentRxMutex, 100);
            if (ret)
            {
                Monitor.Exit(m_ProprietarySegmentRxMutex);
            }
            return ret;
        }
        public bool NonProprietaryTxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(m_NonProprietarySegmentTxMutex, 100);
            if (ret)
            {
                Monitor.Exit(m_NonProprietarySegmentTxMutex);
            }
            return ret;
        }
        public bool NonProprietaryRxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(m_NonProprietarySegmentRxMutex, 100);
            if (ret)
            {
                Monitor.Exit(m_NonProprietarySegmentRxMutex);
            }
            return ret;
        }
        public bool ClientMutexAvailable()
        {
            bool ret = Monitor.TryEnter(m_clientStreamMutex, 100);
            if (ret)
            {
                Monitor.Exit(m_clientStreamMutex);
            }
            return ret;
        }
        public bool DestinationMutexAvailable()
        {
            bool ret = Monitor.TryEnter(m_destinationStreamMutex, 100);
            if (ret)
            {
                Monitor.Exit(m_destinationStreamMutex);
            }
            return ret;
        }
        protected abstract bool ClientTxInProgress();
        protected virtual void CheckConnectionAndShutDownIfGone()
        {
            return;
            try
            {
                if (m_destinationSideSocket.Poll(1, SelectMode.SelectRead) && m_destinationSideSocket.Available == 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "  server side connection is broken ", LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    //                            m_ServerSocket.Shutdown(SocketShutdown.Both);
                    if ((ClientTxInProgress()) || (!IsClientTxQueueEmpty()))
                    {
                        return;
                    }
                    Dispose();
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "  " + exc.Message + " " + exc.StackTrace, ModuleLogLevel);
            }
        }
        protected virtual bool IsAlive()
        {
            try
            {
                if (!m_destinationSideSocket.Connected)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) +" Socket disconnected ", ModuleLogLevel);
                }
                return m_destinationSideSocket.Connected;
            }
            catch
            {
                return false;
            }
        }
        public bool EnterProprietarySegmentTxCriticalArea(bool wait)
        {
            try
            {
                TimeSpan ts;

                if (wait)
                {
                    ts = m_InfiniteWaitTs;
                }
                else
                {
                    ts = m_ImmediateReturnTs;
                }
                //Monitor.Enter(m_ProprietarySegmentTxMutex);
                return Monitor.TryEnter(m_ProprietarySegmentTxMutex,ts);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return false;
            }
        }
        public void LeaveProprietarySegmentTxCriticalArea()
        {
            try
            {
                //ProprietarySegmentTxMutex.ReleaseMutex();
                Monitor.Exit(m_ProprietarySegmentTxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public bool EnterNonProprietarySegmentTxCriticalArea(bool wait)
        {
            try
            {
                TimeSpan ts;

                if (wait)
                {
                    ts = m_InfiniteWaitTs;
                }
                else
                {
                    ts = m_ImmediateReturnTs;
                }
                //Monitor.Enter(m_NonProprietarySegmentTxMutex);
                return Monitor.TryEnter(m_NonProprietarySegmentTxMutex, ts);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return false;
            }
        }
        public void LeaveNonProprietarySegmentTxCriticalArea()
        {
            try
            {
                Monitor.Exit(m_NonProprietarySegmentTxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public bool EnterProprietarySegmentRxCriticalArea(bool wait)
        {
            try
            {
                TimeSpan ts;

                if (wait)
                {
                    ts = m_InfiniteWaitTs;
                }
                else
                {
                    ts = m_ImmediateReturnTs;
                }
                //Monitor.Enter(m_ProprietarySegmentRxMutex);
                return Monitor.TryEnter(m_ProprietarySegmentRxMutex, ts);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return false;
            }
        }
        public void LeaveProprietarySegmentRxCriticalArea()
        {
            try
            {
                //ProprietarySegmentRxMutex.ReleaseMutex();
                Monitor.Exit(m_ProprietarySegmentRxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public bool EnterNonProprietarySegmentRxCriticalArea(bool wait)
        {
            try
            {
                TimeSpan ts;

                if (wait)
                {
                    ts = m_InfiniteWaitTs;
                }
                else
                {
                    ts = m_ImmediateReturnTs;
                }
                //Monitor.Enter(m_NonProprietarySegmentRxMutex);
                return Monitor.TryEnter(m_NonProprietarySegmentRxMutex, ts);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return false;
            }
        }
        public void LeaveNonProprietarySegmentRxCriticalArea()
        {
            try
            {
                //NonProprietarySegmentRxMutex.ReleaseMutex();
                Monitor.Exit(m_NonProprietarySegmentRxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void EnterClientStreamCriticalArea()
        {
            try
            {
                //ProprietarySegmentTxMutex.WaitOne();
                Monitor.Enter(m_clientStreamMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveClientStreamCriticalArea()
        {
            try
            {
                //ProprietarySegmentTxMutex.ReleaseMutex();
                Monitor.Exit(m_clientStreamMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void EnterDestinationStreamCriticalArea()
        {
            try
            {
                Monitor.Enter(m_destinationStreamMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveDestinationStreamCriticalArea()
        {
            try
            {
                Monitor.Exit(m_destinationStreamMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void EnterProprietaryLibCriticalArea()
        {
            try
            {
                Monitor.Enter(m_proprietaryLibMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveProprietaryLibCriticalArea()
        {
            try
            {
                //ProprietarySegmentTxMutex.ReleaseMutex();
                Monitor.Exit(m_proprietaryLibMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }

        void EnterClientMsgTxQueue()
        {
            try
            {
                Monitor.Enter(m_clientTxQueueMutex);
            }
            catch (Exception exc)
            {
            }
        }

        void LeaveClientMsgTxQueue()
        {
            try
            {
                Monitor.Exit(m_clientTxQueueMutex);
            }
            catch (Exception exc)
            {
            }
        }

        void EnterDestinationMsgTxQueue()
        {
            try
            {
                Monitor.Enter(m_destinationTxQueueMutex);
            }
            catch (Exception exc)
            {
            }
        }

        void LeaveDestinationMsgTxQueue()
        {
            try
            {
                Monitor.Exit(m_destinationTxQueueMutex);
            }
            catch (Exception exc)
            {
            }
        }
        
        public void SubmitStream4ClientTx(byte []data)
        {
            EnterClientStreamCriticalArea();
            try
            {
                m_clientStream.AddBytes((byte[])data);
                m_SubmittedMsgsClient++;
                m_SubmittedClient += (uint)((byte[])data).Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LeaveClientStreamCriticalArea();
        }
        public void SubmitMsg4ClientTx(byte[] data,bool submit2Head)
        {
            try
            {
#if false
                Queue.Synchronized(m_clientTxQueue).Enqueue(data);
#else
                EnterClientMsgTxQueue();
                if (submit2Head)
                {
                    m_clientTxQueue.AddFirst(data);
                }
                else
                {
                    m_clientTxQueue.AddLast(data);
                }
                LeaveClientMsgTxQueue();
#endif
                m_SubmittedMsgsClient++;
                m_SubmittedClient += (uint)data.Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public virtual bool IsClientTxQueueEmpty()
        {
            try
            {
#if false
                return ((m_clientStream.Length == 0)&&(Queue.Synchronized(m_clientTxQueue).Count == 0));
#else
                int count;
                EnterClientMsgTxQueue();
                count = m_clientTxQueue.Count;
                LeaveClientMsgTxQueue();
                return ((m_clientStream.Length == 0) && (count == 0));
#endif
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            return true;
        }
        public void SubmitStream4DestinationTx(byte []data)
        {
            EnterDestinationStreamCriticalArea();
            try
            {
                m_destinationStream.AddBytes(data);
                m_SubmittedMsgsServer++;
                m_SubmittedServer += (uint)data.Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LeaveDestinationStreamCriticalArea();
        }
        public void SubmitMsg4DestinationTx(byte []data)
        {
            try
            {
#if false
                Queue.Synchronized(m_destinationTxQueue).Enqueue(data);
#else
                EnterDestinationMsgTxQueue();
                m_destinationTxQueue.AddLast(data);
                LeaveDestinationMsgTxQueue();
#endif
                m_SubmittedMsgsServer++;
                m_SubmittedServer += (uint)data.Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public virtual bool ProcessDownStreamData(byte[] data,bool isInvokedOnTransmit)
        {
            return false;/* message is not intercepted */
        }
        public byte []GetClient2Transmit(out uint streamLength, out bool isMsg)
        {
            LogUtility.LogUtility.LogFile("Entering GetClient2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                byte []data;
#if false
                if (Queue.Synchronized(m_clientTxQueue).Count > 0)
                {
                    data = (byte[])Queue.Synchronized(m_clientTxQueue).Dequeue();
                    LogUtility.LogUtility.LogFile("Queue is not empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    streamLength = (uint)data.Length;
                    isMsg = true;
                    m_IsMsgBeingTransmitted2Client = true;
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    return data;
                }
#else
                data = null;
                bool isSecondPass = false;
second_pass:
                EnterClientMsgTxQueue();
                LinkedListNode<byte[]> lln = m_clientTxQueue.First;
                if (lln != null)
                {
                    data  = lln.Value;
                    m_clientTxQueue.RemoveFirst();
                }
                LeaveClientMsgTxQueue();
                if (data != null)
                {
                    if (!isSecondPass)
                    {
                        uint DataSize;
                        byte DummyFlags;
                        byte MsgKind;
                        int Offset;
                        PackMsg.PackMsg.DecodeMsg(data,out DataSize,out DummyFlags,out MsgKind,out Offset);
                        if (MsgKind == (byte)PackMsg.PackMsg.MsgKind_e.PACK_DATA_MSG_KIND)
                        {
                            byte []msg = new byte[DataSize];
                            CopyBytesFromOffset(data,Offset,msg,(int)DataSize);
                            LogUtility.LogUtility.LogFile("Reprocess " + Convert.ToString(DataSize) + " of data", ModuleLogLevel);
                            if (ProcessDownStreamData(msg, true))
                            {
                                LogUtility.LogUtility.LogFile("Data message has been intercepted on tx!!!", ModuleLogLevel);
                                msg = null;
                                goto second_pass;
                            }
                            msg = null;
                            isSecondPass = true;
                        }
                    }
                    LogUtility.LogUtility.LogFile("Queue is not empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    streamLength = (uint)data.Length;
                    isMsg = true;
                    m_IsMsgBeingTransmitted2Client = true;
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    return data;
                }
#endif
                LogUtility.LogUtility.LogFile("Queue is empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                EnterClientStreamCriticalArea();
                if (m_clientStream.Length == 0)
                {
                    streamLength = 0;
                    isMsg = false;
                    LogUtility.LogUtility.LogFile("Stream is empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveClientStreamCriticalArea();
                    return null;
                }
                LogUtility.LogUtility.LogFile("Stream is not empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);

                isMsg = false;
                m_IsMsgBeingTransmitted2Client = false;
#if true
                streamLength = (uint)m_clientStream.Length;
                byte []bytes = m_clientStream.GetBytes();
#else
                byte[] bytes = clientStream.GetBytesLimited(4096);
                streamLength = (uint)bytes.Length;
#endif
                LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit " + Convert.ToString(m_clientStream.Length), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                LeaveClientStreamCriticalArea();
                return bytes;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                streamLength = 0;
                isMsg = false;
                LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                return null;
            }
        }
        public byte []GetDestination2Transmit(out uint streamLength, out bool isMsg)
        {
            try
            {
                byte[] data;
#if false
                if (Queue.Synchronized(m_destinationTxQueue).Count > 0)
                {
                    data = (byte[])Queue.Synchronized(m_destinationTxQueue).Dequeue();
                    LogUtility.LogUtility.LogFile("Queue is not empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    streamLength = (uint)data.Length;
                    isMsg = true;
                    m_IsMsgBeingTransmitted2Destination = true;
                    LogUtility.LogUtility.LogFile("Leaving GetDestination2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    return data;
                }
#else
                data = null;
                EnterDestinationMsgTxQueue();
                LinkedListNode<byte[]> lln = m_destinationTxQueue.First;
                if (lln != null)
                {
                    data = lln.Value;
                    m_destinationTxQueue.RemoveFirst();
                }
                LeaveDestinationMsgTxQueue();
                if (data != null)
                {
                    LogUtility.LogUtility.LogFile("Queue is not empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    streamLength = (uint)data.Length;
                    isMsg = true;
                    m_IsMsgBeingTransmitted2Destination = true;
                    LogUtility.LogUtility.LogFile("Leaving GetDestination2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    return data;
                }
#endif
                LogUtility.LogUtility.LogFile("Queue is empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                EnterDestinationStreamCriticalArea();
                if (m_destinationStream.Length == 0)
                {
                    streamLength = 0;
                    isMsg = false;
                    LogUtility.LogUtility.LogFile("Stream is empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveDestinationStreamCriticalArea();
                    return null;
                }
                LogUtility.LogUtility.LogFile("Stream is not empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                streamLength = (uint)m_destinationStream.Length;
                isMsg = false;
                m_IsMsgBeingTransmitted2Destination = false;
                byte[] bytes = m_destinationStream.GetBytes();
                LogUtility.LogUtility.LogFile("Leaving GetDestination2Transmit " + Convert.ToString(m_destinationStream.Length), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                LeaveDestinationStreamCriticalArea();
                return bytes;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                streamLength = 0;
                isMsg = false;
                return null;
            }
        }
        public void OnDestinationTransmitted(int sent)
        {
            if (!m_IsMsgBeingTransmitted2Destination)
            {
                LogUtility.LogUtility.LogFile("incrementing dest stream  " + Convert.ToString(sent), ModuleLogLevel);
                EnterDestinationStreamCriticalArea();
                m_destinationStream.IncrementOffset((uint)sent);
                LeaveDestinationStreamCriticalArea();
                LogUtility.LogUtility.LogFile("done  ", ModuleLogLevel);
            }
        }
        public void OnClientTransmitted(int sent)
        {
            if (!m_IsMsgBeingTransmitted2Client)
            {
                LogUtility.LogUtility.LogFile("incrementing client stream  " + Convert.ToString(sent), ModuleLogLevel);
                EnterClientStreamCriticalArea();
                m_clientStream.IncrementOffset((uint)sent);
                LeaveClientStreamCriticalArea();
                LogUtility.LogUtility.LogFile("done  ", ModuleLogLevel);
            }
        }
        public void CopyBytes(byte[] src, byte[] dst, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dst[i] = src[i];
            }
        }
        public void CopyBytesFromOffset(byte[] src, int src_offset,byte[] dst, int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                dst[i] = src[i+src_offset];
            }
        }
        
        public abstract void Start();
        protected virtual void NonProprietarySegmentTransmit()
        {
        }
        protected virtual void ProprietarySegmentTransmit()
        {
        }
        protected virtual void NonProprietarySegmentReceive()
        {
        }
        protected virtual void ProprietarySegmentReceive()
        {
        }

        protected virtual void ReStartAllOperations(bool skipCheckIsAlive)
        {
            if (!skipCheckIsAlive)
            {
                if (!IsAlive())
                {
                    return;
                }
            }
            NonProprietarySegmentTransmit();
            ProprietarySegmentTransmit();
            NonProprietarySegmentReceive();
            ProprietarySegmentReceive();
        }

        void OnDestinationDisconnected(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " OnDestinationDisconnected", LogUtility.LogLevels.LEVEL_LOG_HIGH);
            
            try
            {
                m_destinationSideSocket.Close();
                //m_clientSideDiscAsyncArgs.DisconnectReuseSocket = false;
                //clientSideSocket.DisconnectAsync(m_clientSideDiscAsyncArgs);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            try
            {
//                clientSideSocket.BeginDisconnect(false, new AsyncCallback(OnClientDisconnected), null);
                m_clientSideSocket.DisconnectAsync(m_clientSideDiscAsyncArgs);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }

        void OnClientDisconnected(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " OnClientDisconnected", LogUtility.LogLevels.LEVEL_LOG_HIGH);
            try
            {
                m_clientSideSocket.Close();
                CleanUp();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        protected virtual void OnBeginShutdown()
        {
        }
        protected virtual void Disposing()
        {
        }
        protected virtual void Disposed()
        {
            if (m_onDisposed != null)
            {
                m_onDisposed(this);
            }
        }
        protected virtual uint GetSaved()
        {
            return m_Saved;
        }
        protected virtual uint GetPreSaved()
        {
            return 0;
        }
        protected virtual uint GetPostSaved()
        {
            return 0;
        }
        string GetHMSM(DateTime dt)
        {
            return Convert.ToString(dt.Hour) + ":" + Convert.ToString(dt.Minute) + ":" + Convert.ToString(dt.Second) + ":" + Convert.ToString(dt.Millisecond);
        }
        void CleanUp()
        {
            try
            {
                Disposing();
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " *****************************destroying all******************************", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                m_NonProprietarySegmentRxBuf = null;
                m_ProprietarySementRxBuf = null;
                if (m_clientStream != null)
                {
                    m_clientStream.Clear();
                    m_clientStream = null;
                }
                m_ProprietarySegmentTxMutex = null;
                m_ProprietarySegmentRxMutex = null;
                m_NonProprietarySegmentRxMutex = null;
                m_NonProprietarySegmentTxMutex = null;
                if (m_clientTxQueue != null)
                {
                    m_clientTxQueue.Clear();
                    m_clientTxQueue = null;
                }
                if (m_destinationTxQueue != null)
                {
                    m_destinationTxQueue.Clear();
                    m_destinationTxQueue = null;
                }
                m_txStateMachine = null;
                m_rxStateMachine = null;
                m_destinationSideSocket = null;
                m_clientSideSocket = null;
                LogUtility.LogUtility.LogFile("ID " + Convert.ToString(m_Id) + " m_TransmittedClient " + Convert.ToString(m_TransmittedClient) + " ReceivedClient " + Convert.ToString(m_ReceivedClient) + " TransmittedServer " + Convert.ToString(m_TransmittedServer) + " ReceivedServer " + Convert.ToString(m_ReceivedServer) + " TransmittedMsgsClient " + Convert.ToString(m_TransmittedMsgsClient) + " ReceivedMsgs " + Convert.ToString(m_ReceivedMsgs) + " SubmittedMsgsClient " + Convert.ToString(m_SubmittedMsgsClient) + " SubmittedMsgsServer " + Convert.ToString(m_SubmittedMsgsServer) + " SubmittedClient " + Convert.ToString(m_SubmittedClient) + " SubmittedServer " + Convert.ToString(m_SubmittedServer) + " Saved " + GetSaved() + " PreSaved " + GetPreSaved() + " PostSaved " + GetPostSaved(), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                LogUtility.LogUtility.LogFile("ID " + Convert.ToString(m_Id) + " Timestampts: prop tx init " + GetHMSM(m_ProprietaryTxInitiatedTs) + " prop tx compl " + GetHMSM(m_ProprietaryTxCompletedTs) + " prop rx init " + GetHMSM(m_ProprietaryRxInitiatedTs) + " prop rx compl " + GetHMSM(m_ProprietaryRxCompletedTs) + " nonprop tx init " + GetHMSM(m_NonProprietaryTxInitiatedTs) + " nonprop tx compl " + GetHMSM(m_NonProprietaryTxCompletedTs) + " nonprop rx init " + GetHMSM(m_NonProprietaryRxInitiatedTs) + " nonprop rx compl " + GetHMSM(m_NonProprietaryRxCompletedTs), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                LogUtility.LogUtility.LogFile("ID " + Convert.ToString(m_Id) + " Flags: prop tx " + Convert.ToString(m_ProprietarySegmentTxInProgress) + " prop rx " + Convert.ToString(m_ProprietarySegmentRxInProgress) + " nonprop tx " + Convert.ToString(m_NonProprietarySegmentTxInProgress) + " nonprop rx " + Convert.ToString(m_NonProprietarySegmentRxInProgress), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                Disposed();
                m_disposeMutex = null;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void Dispose()
        {
            try
            {
                m_destinationSideSocket.DisconnectAsync(m_ServerSideDiscAsyncArgs);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "  connection is broken - DONE ", ModuleLogLevel);
            }
            catch (Exception exc)
            {
            }
        }
        public static void Flush()
        {
            ReceiverPackLib.ReceiverPackLib.Flush();
        }
    }
}
