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
//using System.Runtime.Remoting.Contexts;

namespace ProxyLib
{
    //[Synchronization()]
    public abstract class Proxy
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;

        protected Queue clientTxQueue;
        protected Queue destinationTxQueue;
        protected Socket clientSideSocket;
        protected Socket destinationSideSocket;
        protected RxStateMachine rxStateMachine;
        protected TxStateMachine txStateMachine;
        protected object ProprietarySegmentTxMutex;
        protected object NonProprietarySegmentTxMutex;
        protected object ProprietarySegmentRxMutex;
        protected object NonProprietarySegmentRxMutex;
        protected object disposeMutex;
        static protected object proprietaryLibMutex = new object();

        protected object clientStreamMutex;
        protected object destinationStreamMutex;

        protected bool  ProprietarySegmentTxInProgress;
        protected bool  ProprietarySegmentRxInProgress;
        protected bool  NonProprietarySegmentTxInProgress;
        protected bool  NonProprietarySegmentRxInProgress;
        protected bool   IsMsgBeingTransmitted2Client;
        protected bool IsMsgBeingTransmitted2Destination;
        protected EndPoint Id;
        protected byte []NonProprietarySegmentRxBuf;
        protected byte[] ProprietarySementRxBuf;
        protected bool ShutDownFlag;
        protected uint TransmittedClient;
        protected uint ReceivedClient;
        protected uint TransmittedServer;
        protected uint ReceivedServer;
        protected uint TransmittedMsgsClient;
        protected uint ReceivedMsgs;
        protected uint SubmittedMsgsClient;
        protected uint SubmittedMsgsServer;
        protected uint SubmittedClient;
        protected uint SubmittedServer;
        protected MyMemoryStream.MyMemoryStream clientStream;
        protected MyMemoryStream.MyMemoryStream destinationStream;
        public delegate void OnGotResults(object res);
        protected OnGotResults onGotResults;
        public delegate void OnDisposed(Proxy p);
        OnDisposed onDisposed;
        protected AsyncCallback m_OnNonProprietaryReceivedCbk;
        protected AsyncCallback m_OnProprietaryReceivedCbk;
        protected AsyncCallback m_OnNonProprietaryTransmittedCbk;
        protected AsyncCallback m_OnProprietaryTransmittedCbk;

        public static void InitGlobalObjects()
        {
            PackClientSide.InitGlobalObjects();
        }

        public Proxy()
        {
            clientTxQueue = new Queue();
            destinationTxQueue = new Queue();
            //ProprietarySegmentTxMutex = new Mutex();
            //NonProprietarySegmentTxMutex = new Mutex();
            //ProprietarySegmentRxMutex = new Mutex();
            //NonProprietarySegmentRxMutex = new Mutex();
            ProprietarySegmentTxMutex = new object();
            NonProprietarySegmentTxMutex = new object();
            ProprietarySegmentRxMutex = new object();
            NonProprietarySegmentRxMutex = new object();
            clientStreamMutex = new object();
            destinationStreamMutex = new object();
            disposeMutex = new object();
            ProprietarySegmentTxInProgress = false;
            ProprietarySegmentRxInProgress = false;
            NonProprietarySegmentTxInProgress = false;
            NonProprietarySegmentRxInProgress = false;
            IsMsgBeingTransmitted2Client = false;
            IsMsgBeingTransmitted2Destination = false;
            NonProprietarySegmentRxBuf = new byte[8192 * 4];
            ProprietarySementRxBuf = new byte[8192*4];
            rxStateMachine = new RxStateMachine(Id);
            txStateMachine = new TxStateMachine(Id);
            m_OnNonProprietaryReceivedCbk = new AsyncCallback(OnNonProprietarySegmentReceived);
            m_OnNonProprietaryTransmittedCbk = new AsyncCallback(OnNonProprietarySegmentTransmitted);
            m_OnProprietaryReceivedCbk = new AsyncCallback(OnProprietarySegmentReceived);
            m_OnProprietaryTransmittedCbk = new AsyncCallback(OnProprietarySegmentTransmitted);
            ShutDownFlag = false;
            TransmittedClient = 0;
            ReceivedClient = 0;
            TransmittedServer = 0;
            ReceivedServer = 0;
            TransmittedMsgsClient = 0;
            ReceivedMsgs = 0;
            SubmittedMsgsClient = 0;
            SubmittedMsgsServer = 0;
            SubmittedClient = 0;
            SubmittedServer = 0;
            clientStream = new MyMemoryStream.MyMemoryStream();
            destinationStream = new MyMemoryStream.MyMemoryStream();
            onGotResults = null;
            onDisposed = null;
            LogUtility.LogUtility.LogFile("Started at " + DateTime.Now.ToLongTimeString(), LogUtility.LogLevels.LEVEL_LOG_HIGH);
        }
        protected abstract void OnProprietarySegmentReceived(IAsyncResult ar);
        protected abstract void OnProprietarySegmentTransmitted(IAsyncResult ar);
        protected abstract void OnNonProprietarySegmentReceived(IAsyncResult ar);
        protected abstract void OnNonProprietarySegmentTransmitted(IAsyncResult ar);
        virtual public void SetRemoteEndpoint(IPEndPoint ipEndpoint)
        {
        }
        virtual public void SetOnGotResults(OnGotResults cbk)
        {
            onGotResults = cbk;
        }
        virtual public void SetOnDisposed(OnDisposed cbk)
        {
            onDisposed = cbk;
        }
        virtual public object GetResults()
        {
            return null;
        }
        public bool ProprietaryTxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(ProprietarySegmentTxMutex,100);
            if (ret)
            {
                Monitor.Exit(ProprietarySegmentTxMutex);
            }
            return ret;
        }
        public bool ProprietaryRxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(ProprietarySegmentRxMutex, 100);
            if (ret)
            {
                Monitor.Exit(ProprietarySegmentRxMutex);
            }
            return ret;
        }
        public bool NonProprietaryTxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(NonProprietarySegmentTxMutex, 100);
            if (ret)
            {
                Monitor.Exit(NonProprietarySegmentTxMutex);
            }
            return ret;
        }
        public bool NonProprietaryRxMutexAvailable()
        {
            bool ret = Monitor.TryEnter(NonProprietarySegmentRxMutex, 100);
            if (ret)
            {
                Monitor.Exit(NonProprietarySegmentRxMutex);
            }
            return ret;
        }
        public bool ClientMutexAvailable()
        {
            bool ret = Monitor.TryEnter(clientStreamMutex, 100);
            if (ret)
            {
                Monitor.Exit(clientStreamMutex);
            }
            return ret;
        }
        public bool DestinationMutexAvailable()
        {
            bool ret = Monitor.TryEnter(destinationStreamMutex, 100);
            if (ret)
            {
                Monitor.Exit(destinationStreamMutex);
            }
            return ret;
        }
        public void EnterProprietarySegmentTxCriticalArea()
        {
            try
            {
                //ProprietarySegmentTxMutex.WaitOne();
                Monitor.Enter(ProprietarySegmentTxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveProprietarySegmentTxCriticalArea()
        {
            try
            {
                //ProprietarySegmentTxMutex.ReleaseMutex();
                Monitor.Exit(ProprietarySegmentTxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void EnterNonProprietarySegmentTxCriticalArea()
        {
            try
            {
                //NonProprietarySegmentTxMutex.WaitOne();
                Monitor.Enter(NonProprietarySegmentTxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveNonProprietarySegmentTxCriticalArea()
        {
            try
            {
                //NonProprietarySegmentTxMutex.ReleaseMutex();
                Monitor.Exit(NonProprietarySegmentTxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void EnterProprietarySegmentRxCriticalArea()
        {
            try
            {
                //ProprietarySegmentRxMutex.WaitOne();
                Monitor.Enter(ProprietarySegmentRxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveProprietarySegmentRxCriticalArea()
        {
            try
            {
                //ProprietarySegmentRxMutex.ReleaseMutex();
                Monitor.Exit(ProprietarySegmentRxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void EnterNonProprietarySegmentRxCriticalArea()
        {
            try
            {
                //NonProprietarySegmentRxMutex.WaitOne();
                Monitor.Enter(NonProprietarySegmentRxMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public void LeaveNonProprietarySegmentRxCriticalArea()
        {
            try
            {
                //NonProprietarySegmentRxMutex.ReleaseMutex();
                Monitor.Exit(NonProprietarySegmentRxMutex);
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
                Monitor.Enter(clientStreamMutex);
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
                Monitor.Exit(clientStreamMutex);
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
                //ProprietarySegmentTxMutex.WaitOne();
                Monitor.Enter(destinationStreamMutex);
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
                //ProprietarySegmentTxMutex.ReleaseMutex();
                Monitor.Exit(destinationStreamMutex);
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
                //ProprietarySegmentTxMutex.WaitOne();
                Monitor.Enter(proprietaryLibMutex);
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
                Monitor.Exit(proprietaryLibMutex);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        
        public void SubmitStream4ClientTx(byte []data)
        {
            EnterClientStreamCriticalArea();
            try
            {
                clientStream.AddBytes((byte[])data);
                SubmittedMsgsClient++;
                SubmittedClient += (uint)((byte[])data).Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LeaveClientStreamCriticalArea();
        }
        public void SubmitMsg4ClientTx(byte[] data)
        {
            try
            {
                Queue.Synchronized(clientTxQueue).Enqueue(data);
                SubmittedMsgsClient++;
                SubmittedClient += (uint)data.Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public virtual bool IsClientTxQueueEmpty()
        {
            try
            {
                return ((clientStream.Length == 0)&&(Queue.Synchronized(clientTxQueue).Count == 0));
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            return true;
        }
        public void SubmitStream4DestinationTx(byte []data)
        {
            EnterDestinationStreamCriticalArea();
            try
            {
                destinationStream.AddBytes(data);
                SubmittedMsgsServer++;
                SubmittedServer += (uint)data.Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LeaveDestinationStreamCriticalArea();
        }
        public void SubmitMsg4DestinationTx(byte []data)
        {
            try
            {
                Queue.Synchronized(destinationTxQueue).Enqueue(data);
                SubmittedMsgsServer++;
                SubmittedServer += (uint)data.Length;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public byte []GetClient2Transmit(out uint streamLength, out bool isMsg)
        {
            LogUtility.LogUtility.LogFile("Entering GetClient2Transmit", ModuleLogLevel);
            try
            {
                byte []data;
                if (Queue.Synchronized(clientTxQueue).Count > 0)
                {
                    data = (byte[])Queue.Synchronized(clientTxQueue).Dequeue();
                    LogUtility.LogUtility.LogFile("Queue is not empty", ModuleLogLevel);
                    streamLength = (uint)data.Length;
                    isMsg = true;
                    IsMsgBeingTransmitted2Client = true;
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", ModuleLogLevel);
                    return data;
                }
                LogUtility.LogUtility.LogFile("Queue is empty", ModuleLogLevel);
                EnterClientStreamCriticalArea();
                if (clientStream.Length == 0)
                {
                    streamLength = 0;
                    isMsg = false;
                    LogUtility.LogUtility.LogFile("Stream is empty", ModuleLogLevel);
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", ModuleLogLevel);
                    LeaveClientStreamCriticalArea();
                    return null;
                }
                LogUtility.LogUtility.LogFile("Stream is not empty", ModuleLogLevel);

                isMsg = false;
                IsMsgBeingTransmitted2Client = false;
#if false
                streamLength = (uint)clientStream.Length;
                byte []bytes = clientStream.GetBytes();
#else
                byte[] bytes = clientStream.GetBytesLimited(4096);
                streamLength = (uint)bytes.Length;
#endif
                LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit " + Convert.ToString(clientStream.Length), ModuleLogLevel);
                LeaveClientStreamCriticalArea();
                return bytes;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                streamLength = 0;
                isMsg = false;
                LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", ModuleLogLevel);
                return null;
            }
        }
        public byte []GetDestination2Transmit(out uint streamLength, out bool isMsg)
        {
            try
            {
                byte[] data;
                if (Queue.Synchronized(destinationTxQueue).Count > 0)
                {
                    data = (byte[])Queue.Synchronized(destinationTxQueue).Dequeue();
                    LogUtility.LogUtility.LogFile("Queue is not empty", ModuleLogLevel);
                    streamLength = (uint)data.Length;
                    isMsg = true;
                    IsMsgBeingTransmitted2Destination = true;
                    LogUtility.LogUtility.LogFile("Leaving GetDestination2Transmit", ModuleLogLevel);
                    return data;
                }
                LogUtility.LogUtility.LogFile("Queue is empty", ModuleLogLevel);
                EnterDestinationStreamCriticalArea();
                if (destinationStream.Length == 0)
                {
                    streamLength = 0;
                    isMsg = false;
                    LogUtility.LogUtility.LogFile("Stream is empty", ModuleLogLevel);
                    LogUtility.LogUtility.LogFile("Leaving GetClient2Transmit", ModuleLogLevel);
                    LeaveDestinationStreamCriticalArea();
                    return null;
                }
                LogUtility.LogUtility.LogFile("Stream is not empty", ModuleLogLevel);
                streamLength = (uint)destinationStream.Length;
                isMsg = false;
                IsMsgBeingTransmitted2Destination = false;
                byte[] bytes = destinationStream.GetBytes();
                LogUtility.LogUtility.LogFile("Leaving GetDestination2Transmit " + Convert.ToString(destinationStream.Length), ModuleLogLevel);
                LeaveDestinationStreamCriticalArea();
                return bytes;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                streamLength = 0;
                isMsg = false;
                return null;
            }
        }
        public void OnDestinationTransmitted(int sent)
        {
            if (!IsMsgBeingTransmitted2Destination)
            {
                LogUtility.LogUtility.LogFile("incrementing dest stream  " + Convert.ToString(sent), ModuleLogLevel);
                EnterDestinationStreamCriticalArea();
                destinationStream.IncrementOffset((uint)sent);
                LeaveDestinationStreamCriticalArea();
                LogUtility.LogUtility.LogFile("done  ", ModuleLogLevel);
            }
        }
        public void OnClientTransmitted(int sent)
        {
            if (!IsMsgBeingTransmitted2Client)
            {
                LogUtility.LogUtility.LogFile("incrementing client stream  " + Convert.ToString(sent), ModuleLogLevel);
                EnterClientStreamCriticalArea();
                clientStream.IncrementOffset((uint)sent);
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
        
        public abstract void Start();

        void OnDestinationDisconnected(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnDestinationDisconnected", LogUtility.LogLevels.LEVEL_LOG_HIGH);
            try
            {
                destinationSideSocket.EndDisconnect(ar);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            try
            {
                destinationSideSocket.Close();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            bool success = false;
            try
            {
                clientSideSocket.BeginDisconnect(false, new AsyncCallback(OnClientDisconnected), null);
                success = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            if (!success)
            {
                try
                {
                    clientSideSocket.Close();
                    CleanUp();
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
            }
        }
        
        void OnClientDisconnected(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnClientDisconnected", LogUtility.LogLevels.LEVEL_LOG_HIGH);
            try
            {
                clientSideSocket.EndDisconnect(ar);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            try
            {
                clientSideSocket.Close();
                CleanUp();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
            if (onDisposed != null)
            {
                onDisposed(this);
            }
        }
        public bool Dispose2(bool isServerSide)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " In Dispose2 " + DateTime.Now.ToLongTimeString(), LogUtility.LogLevels.LEVEL_LOG_HIGH);
            if (isServerSide)
            {
                if ((destinationSideSocket.Available > 0) || (NonProprietarySegmentTxInProgress) || (ProprietarySegmentTxInProgress))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " available" + Convert.ToString(destinationSideSocket.Available) + " non-proprietary tx is in progress " + Convert.ToString(NonProprietarySegmentTxInProgress) + " Proprietary tx is in progress " + Convert.ToString(ProprietarySegmentTxInProgress), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    return false;
                }
                EnterProprietaryLibCriticalArea();
                bool isClientEmpty = true;
                bool isClientConnected = false;
                try
                {
                    isClientEmpty = IsClientTxQueueEmpty();
                }
                catch
                {
                }
                if (!isClientEmpty && (!ShutDownFlag))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " client queue is not empty. setting shutdown flag", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    OnBeginShutdown();
                    ShutDownFlag = true;
                    LeaveProprietaryLibCriticalArea();
                    return false;
                }
                else
                {
                    try
                    {
                        isClientConnected = (clientSideSocket == null) ? false : ((clientSideSocket.Connected) ? true : false);
                    }
                    catch
                    {
                    }
                    if (!isClientEmpty &&  isClientConnected)
                    {
                        LeaveProprietaryLibCriticalArea();
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " client queue is not empty and client socket is connected. return", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        return false;
                    }
                }
                LeaveProprietaryLibCriticalArea();
            }
            if (disposeMutex == null)
            {
                return true;
            }
            try
            {
                if (!Monitor.TryEnter(disposeMutex))
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }
            
            SocketAsyncEventArgs se = new SocketAsyncEventArgs();
            bool success = false;
            try
            {
                //destinationSideSocket.DisconnectAsync(se);
                destinationSideSocket.Shutdown(SocketShutdown.Both);
                //destinationSideSocket.Disconnect(false);
                //destinationSideSocket.BeginDisconnect(false, new AsyncCallback(OnDestinationDisconnected), null);
                //success = true;
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            if (!success)
            {
                try
                {
                    destinationSideSocket.Close();
                }
                catch (Exception exc)
                {
                    //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                try
                {
                    //clientSideSocket.DisconnectAsync(se);
                    clientSideSocket.Shutdown(SocketShutdown.Both);
                    //clientSideSocket.Disconnect(false);
                    //clientSideSocket.BeginDisconnect(false, new AsyncCallback(OnClientDisconnected), null);
                    //success = true;
                }
                catch (Exception exc)
                {
                    //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                if (!success) 
                {
                    try
                    {
                        clientSideSocket.Close();
                    }
                    catch (Exception exc)
                    {
                        //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                }
            }
            CleanUp();
            return false;
        }
        void CleanUp()
        {
            try
            {
                Disposing();
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " *****************************destroying all******************************", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentRxBuf = null;
                ProprietarySementRxBuf = null;
                if (clientStream != null)
                {
                    clientStream.Clear();
                    clientStream = null;
                }
                ProprietarySegmentTxMutex = null;
                ProprietarySegmentRxMutex = null;
                NonProprietarySegmentRxMutex = null;
                NonProprietarySegmentTxMutex = null;
                if (clientTxQueue != null)
                {
                    clientTxQueue.Clear();
                    clientTxQueue = null;
                }
                if (destinationTxQueue != null)
                {
                    destinationTxQueue.Clear();
                    destinationTxQueue = null;
                }
                txStateMachine = null;
                rxStateMachine = null;
                LogUtility.LogUtility.LogFile("ID " + Convert.ToString(Id) + " TransmittedClient " + Convert.ToString(TransmittedClient) + " ReceivedClient " + Convert.ToString(ReceivedClient) + " TransmittedServer " + Convert.ToString(TransmittedServer) + " ReceivedServer " + Convert.ToString(ReceivedServer) + " TransmittedMsgsClient " + Convert.ToString(TransmittedMsgsClient) + " ReceivedMsgs " + Convert.ToString(ReceivedMsgs) + " SubmittedMsgsClient " + Convert.ToString(SubmittedMsgsClient) + " SubmittedMsgsServer " + Convert.ToString(SubmittedMsgsServer) + " SubmittedClient " + Convert.ToString(SubmittedClient) + " SubmittedServer " + Convert.ToString(SubmittedServer), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                Disposed();
                disposeMutex = null;
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }

        public static void Flush()
        {
            ReceiverPackLib.ReceiverPackLib.Flush();
        }
    }
}
