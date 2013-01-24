using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using RxTxStateMachine;
using ProxyLibTypes;
using System.IO;

namespace ProxyLib
{
    //[Synchronization()]
    public abstract class ServerSideProxy : Proxy
    {
        protected bool m_ErrorSent;
        protected bool m_OnceConnected;

        public ServerSideProxy(Socket sock)
            : base()
        {
            m_clientSideSocket = sock;
            m_destinationSideSocket = null;
            m_ErrorSent = false;
            m_OnceConnected = false;
        }

        protected override void OnProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentTxCriticalArea(true);
            try
            {
                int Ret = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!m_ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                Ret = e.BytesTransferred;
                
                if (Ret <= 0)
                {
                    LogUtility.LogUtility.LogFile("!!!Proprietary: transferred < 0" + " " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                m_ProprietaryTxCompletedTs = DateTime.Now;
                m_TransmittedClient += (uint)Ret;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sent (proprietary segment) " + Convert.ToString(Ret), ModuleLogLevel);
                if (m_ErrorSent)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ErrorSent flag is up, checking if queue is empty", ModuleLogLevel);
                    if (IsClientTxQueueEmpty())
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "OnProprietarySegmentTransmitted: queue is empty and error is sent", LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                        Dispose();
                        LeaveProprietarySegmentTxCriticalArea();
                        return;
                    }
                }
                m_ProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentTxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void OnNonProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentTxCriticalArea(true);
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (!m_NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                sent = e.BytesTransferred;

                if (sent <= 0)
                {
                    LogUtility.LogUtility.LogFile("!!!NonProprietary: transferred < 0" + " " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                }
                m_NonProprietaryTxCompletedTs = DateTime.Now;
                m_TransmittedServer += (uint)sent;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " " + Convert.ToString(sent) + " sent to destination " + Convert.ToString(sent) + " overall " + Convert.ToString(m_TransmittedServer), ModuleLogLevel);
                OnDestinationTransmitted(sent);
                m_NonProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        bool _ProprietarySegmentTransmit(byte[] buff2transmit)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering _ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                if (m_clientSideSocket.SendBufferSize < buff2transmit.Length)
                {
                    LogUtility.LogUtility.LogFile("increasing tx buffer size", ModuleLogLevel);
                    m_clientSideSocket.SendBufferSize = buff2transmit.Length;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " send (proprietary segment) " + Convert.ToString(buff2transmit.Length), ModuleLogLevel);
                
                m_OnProprietaryTransmittedCbk.SetBuffer(buff2transmit, 0, buff2transmit.Length);
                m_ProprietaryTxInitiatedTs = DateTime.Now;
                if (!m_clientSideSocket.SendAsync(m_OnProprietaryTransmittedCbk))
                {
                    m_ProprietaryTxCompletedTs = DateTime.Now;
                    if (m_OnProprietaryTransmittedCbk.BytesTransferred != buff2transmit.Length)
                    {
                        LogUtility.LogUtility.LogFile("!!!NonProprietary: Attempted to tx: " + Convert.ToString(buff2transmit.Length) + " Transmitted " + Convert.ToString(m_OnProprietaryTransmittedCbk.BytesTransferred) + " " + System.Enum.GetName(typeof(SocketError), m_OnNonProprietaryTransmittedCbk.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    }
                    m_TransmittedClient += (uint)buff2transmit.Length;
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving _ProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                    return true;
                }
                m_ProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                //Dispose2();
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving _ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            return false;
        }

        protected void ProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (m_ErrorSent)
            {
                LogUtility.LogUtility.LogFile("discard, ErrorSent flag is up, leaving ProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                return;
            }
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Subm2Tx stream client queue", ModuleLogLevel);
                SubmitStream4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected void ProprietarySegmentSubmitMsg4Tx(byte[] data,bool submit2Head)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (m_ErrorSent)
            {
                LogUtility.LogUtility.LogFile("discard, ErrorSent flag is up, leaving ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                return;
            }
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Subm2Tx msg to client queue", ModuleLogLevel);
                SubmitMsg4ClientTx(data,submit2Head);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void ProprietarySegmentTransmit()
        {
            bool IsRestartRequired = false;
            byte[] buff2transmit;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (!EnterProprietarySegmentTxCriticalArea(false))
            {
                return;
            }
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (m_ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ProprietarySegmentTransmit: tx is in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                MemoryStream stream = new MemoryStream();
                while (true)
                {
                    if (IsClientTxQueueEmpty())
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " queue is empty, exiting the loop", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                        break;
                    }
                    if (!m_txStateMachine.IsBusy())
                    {
                        uint length;
                        bool isMsg;
                        byte[] buf2tx = GetClient2Transmit(out length, out isMsg);
                        if (buf2tx != null)
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " tx to client  msg len " + Convert.ToString(length) + " isMsg " + Convert.ToString(isMsg), ModuleLogLevel);
                            if (isMsg)
                            {
                                m_txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_MSG_KIND);
                            }
                            else
                            {
                                m_txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_DATA_KIND);
                            }
                            m_txStateMachine.SetLength(length);
                            m_txStateMachine.SetMsgBody(buf2tx);
                        }
                        else
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " queue is empty, exiting the loop", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                            break;
                        }
                    }
                    buff2transmit = m_txStateMachine.GetBytes();
                    if (buff2transmit != null)
                    {
                        //_ProprietarySegmentTransmit(buff2transmit);
                        stream.Write(buff2transmit, 0, buff2transmit.Length);
                        if (m_txStateMachine.IsInBody())
                        {
                            if (m_txStateMachine.IsWholeMessage())
                            {
                                OnClientTransmitted(buff2transmit.Length - m_txStateMachine.GetHeaderLength());
                            }
                            else
                            {
                                OnClientTransmitted(buff2transmit.Length);
                            }
                        }
                        m_txStateMachine.OnTxComplete((uint)buff2transmit.Length);
                        if (m_txStateMachine.IsTransactionCompleted())
                        {
                            m_txStateMachine.ClearMsgBody();
                            m_TransmittedMsgsClient++;
                        }
                    }
                    else
                    {
                        LogUtility.LogUtility.LogFile("cannot get more bytes to tx, exiting the loop", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                        break;
                    }
                }
                
                if (stream.Length > 0)
                {
                    //stream.Capacity = (int)stream.Length;
                    //_ProprietarySegmentTransmit(stream.GetBuffer());
                    buff2transmit = new byte[stream.Length];
                    stream.Position = 0;
                    if (stream.Read(buff2transmit, 0, buff2transmit.Length) != buff2transmit.Length)
                    {
                        LogUtility.LogUtility.LogFile("Cannot read from stream", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                    IsRestartRequired = _ProprietarySegmentTransmit(buff2transmit);
                } 
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentTxCriticalArea();
            if (IsRestartRequired)
            {
                ReStartAllOperations(!m_OnceConnected);
            }
        }
        public override bool IsClientTxQueueEmpty()
        {
            return ((!m_txStateMachine.IsBusy()) && base.IsClientTxQueueEmpty());
        }
        
        protected override void OnNonProprietarySegmentReceived(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnNonProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentRxCriticalArea(true);
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!m_NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: rx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Rx ERROR " + System.Enum.GetName(typeof(SocketError),e.SocketError), ModuleLogLevel);
                    CheckConnectionAndShutDownIfGone();
                    Dispose();
                    goto end_server_rx;
                }
                m_NonProprietaryRxCompletedTs = DateTime.Now;
                m_ReceivedServer += (uint)Received;
                m_ReceivedMsgs++;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received (non-proprietary segment) " + Convert.ToString(Received) + " overall " + Convert.ToString(m_ReceivedServer), ModuleLogLevel);

                byte[] buff = new byte[Received];
                CopyBytes(m_NonProprietarySegmentRxBuf, buff, Received);
                ProcessDownStreamData(buff,false);
                m_NonProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnNonProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
end_server_rx:
            LeaveNonProprietarySegmentRxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void NonProprietarySegmentReceive()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering NonProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (!EnterNonProprietarySegmentRxCriticalArea(false))
            {
                return;
            }
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if ((m_destinationSideSocket == null) || (!m_destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentReceive (socket is not connected)", ModuleLogLevel);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                
                if (m_NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentReceive: rx is in progess,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                m_OnNonProprietaryReceivedCbk.SetBuffer(m_NonProprietarySegmentRxBuf, 0, m_NonProprietarySegmentRxBuf.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " NonProprietary ReceivAsync ", ModuleLogLevel);
                m_NonProprietaryRxInitiatedTs = DateTime.Now;
                if (!m_destinationSideSocket.ReceiveAsync(m_OnNonProprietaryReceivedCbk))
                {
                    m_NonProprietaryRxCompletedTs = DateTime.Now;
                    int Received = m_OnNonProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received(sync) (non-proprietary segment) ERROR ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        Dispose();
                        goto end_server_rx;
                    }
                    m_ReceivedServer += (uint)Received;
                    m_ReceivedMsgs++;
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received(sync) (non-proprietary segment) " + Convert.ToString(Received) + " overall " + Convert.ToString(m_ReceivedServer), LogUtility.LogLevels.LEVEL_LOG_HIGH);

                    byte[] buff = new byte[Received];
                    CopyBytes(m_NonProprietarySegmentRxBuf, buff, Received);
                    ProcessDownStreamData(buff,false);
                    IsRestartRequired = true;
                }
                else
                {
                    m_NonProprietarySegmentRxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
end_server_rx:
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        bool _NonProprietarySegmentTransmit(byte []data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering _NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                m_OnNonProprietaryTransmittedCbk.SetBuffer(data, 0, data.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Sending to destination " + Convert.ToString(data.Length), ModuleLogLevel);
                m_NonProprietaryTxInitiatedTs = DateTime.Now;
                if (!m_destinationSideSocket.SendAsync(m_OnNonProprietaryTransmittedCbk))
                {
                    m_NonProprietaryTxCompletedTs = DateTime.Now;
                    if (m_OnNonProprietaryTransmittedCbk.BytesTransferred != data.Length)
                    {
                        LogUtility.LogUtility.LogFile("!!!NonProprietary: Attempted to tx: " + Convert.ToString(data.Length) + " Transmitted " + Convert.ToString(m_OnNonProprietaryTransmittedCbk.BytesTransferred) + " " + System.Enum.GetName(typeof(SocketError), m_OnNonProprietaryTransmittedCbk.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    }
                    m_TransmittedServer += (uint)data.Length;
                    OnDestinationTransmitted(data.Length);
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving _NonProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                    return true;
                }
                m_NonProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving _NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            return false;
        }
        protected void NonProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering NonProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Subm2Tx client queue", ModuleLogLevel);
                SubmitStream4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected void NonProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering NonProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Subm2Tx client queue", ModuleLogLevel);
                SubmitMsg4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void NonProprietarySegmentTransmit()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (!EnterNonProprietarySegmentTxCriticalArea(false))
            {
                return;
            }
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if ((m_destinationSideSocket == null) || (!m_destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentTransmit (socket is not connected)", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                if (!m_NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " is not in tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    uint length;
                    bool isMsg;
                    byte []data  = GetDestination2Transmit(out length,out isMsg);
                    if (data != null)
                    {
                        IsRestartRequired = _NonProprietarySegmentTransmit(data);
                    }
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " DestinationTx is busy and queueElement is null", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(!m_OnceConnected);
            }
        }
        protected static bool IsLocalIP(IPAddress IP)
        {
            byte First = (byte)Math.Floor((decimal)(IP.Address % 256));
            byte Second = (byte)Math.Floor((decimal)((IP.Address % 65536)) / 256);
            //10.x.x.x Or 172.16.x.x <-> 172.31.x.x Or 192.168.x.x
            return (First == 10) ||
                (First == 172 && (Second >= 16 && Second <= 31)) ||
                (First == 192 && Second == 168);
        }
        public static IPAddress GetLocalInternalIP()
        {
            try
            {
                IPHostEntry he = Dns.Resolve(Dns.GetHostName());
                for (int Cnt = 0; Cnt < he.AddressList.Length; Cnt++)
                {
                    if (IsLocalIP(he.AddressList[Cnt]))
                        return he.AddressList[Cnt];
                }
                return he.AddressList[0];
            }
            catch
            {
                return IPAddress.Any;
            }
        }
        public abstract byte []GetFirstBuffToTransmitDestination();
        public abstract void ProcessUpStreamDataKind();
        public abstract void ProcessUpStreamMsgKind();

        public virtual void OnDownStreamTransmissionOpportunity()
        {
        }

        void OnProprietarySegmentMsgReceived()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnProprietaryMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                LogUtility.LogUtility.LogFile("Received msg type " + Convert.ToString(m_rxStateMachine.GetKind()), ModuleLogLevel);
                if (m_rxStateMachine.GetMsgBody() == null)
                {
                    LogUtility.LogUtility.LogFile("msg body is null!!!", ModuleLogLevel);
                    return;
                }
                switch (m_rxStateMachine.GetKind())
                {
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_DATA_KIND:
                        ProcessUpStreamDataKind();
                        break;
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_MSG_KIND:
                        ProcessUpStreamMsgKind();
                        OnDownStreamTransmissionOpportunity();
                        break;
                }
                m_rxStateMachine.ClearMsgBody();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietaryMsgReceived", ModuleLogLevel);
        }

        protected override void OnProprietarySegmentReceived(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentRxCriticalArea(true);
            try
            {
                int Received = 0;

                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!m_ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    LogUtility.LogUtility.LogFile(" Rx ERROR, " + System.Enum.GetName(typeof(SocketError), e.SocketError), ModuleLogLevel);
                    LeaveProprietarySegmentRxCriticalArea();
                    if (e.SocketError == SocketError.Success)
                    {
                    //    ReStartAllOperations(!m_OnceConnected);
                    }
                    else
                    {
                      //  LogUtility.LogUtility.LogFile(" Rx ERROR, " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    }
                    return;
                }
                m_ProprietaryRxCompletedTs = DateTime.Now;
                LogUtility.LogUtility.LogFile("Received (proprietary segment) " + Convert.ToString(Received), ModuleLogLevel);
                m_ReceivedClient += (uint)Received;
                m_rxStateMachine.OnRxComplete(e.Buffer, Received);
                m_ProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentRxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void ProprietarySegmentReceive()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (!EnterProprietarySegmentRxCriticalArea(false))
            {
                return;
            }
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (m_ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ProprietarySegmentReceive: rx is in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                m_OnProprietaryReceivedCbk.SetBuffer(m_ProprietarySementRxBuf, 0, m_ProprietarySementRxBuf.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Proprietary ReceivAsync ", ModuleLogLevel);
                m_ProprietaryRxInitiatedTs = DateTime.Now;
                if (!m_clientSideSocket.ReceiveAsync(m_OnProprietaryReceivedCbk))
                {
                    m_ProprietaryRxCompletedTs = DateTime.Now;
                    int Received = m_OnProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ProprietarySegmentReceived (sync) ERROR", LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                        LeaveProprietarySegmentRxCriticalArea();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " ProprietarySegmentReceived (sync) " + Convert.ToString(Received), ModuleLogLevel);
                    m_ReceivedClient += (uint)Received;
                    m_rxStateMachine.OnRxComplete(m_OnProprietaryReceivedCbk.Buffer, Received);
                    IsRestartRequired = true;
                }
                else
                {
                    m_ProprietarySegmentRxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "EXCEPTION " + exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentRxCriticalArea();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        protected override bool ClientTxInProgress()
        {
            return m_ProprietarySegmentTxInProgress;
        }
        public override void Start()
        {
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Starting new server", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                m_rxStateMachine.SetCallback(new OnMsgReceived(OnProprietarySegmentMsgReceived));
                ProprietarySegmentReceive();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + "EXCEPTION " + exc.Message,LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
    }
}
