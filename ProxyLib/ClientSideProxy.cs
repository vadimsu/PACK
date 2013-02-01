using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using RxTxStateMachine;
using ProxyLibTypes;
//using System.Runtime.Remoting.Contexts;
using PerformanceMonitoring;

namespace ProxyLib
{
    //[Synchronization()]
    public class ClientSideProxy : Proxy
    {
        IPEndPoint m_destinationSideEndPoint;
        
        public ClientSideProxy(Socket clientSocket, IPEndPoint remoteEndPoint)
            : base()
        {
            try
            {
                m_clientSideSocket = clientSocket;
                m_destinationSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_destinationSideEndPoint = remoteEndPoint;
                m_destinationSideSocket.Connect(m_destinationSideEndPoint);
                m_Id = m_destinationSideSocket.LocalEndPoint;
                m_rxStateMachine.SetCallback(new OnMsgReceived(OnProprietarySegmentMsgReceived));
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }      
        
        protected virtual void NonProprietarySegmentSubmitStream4Tx(byte []data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering NonProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);

            if (data != null)
            {
                LogUtility.LogUtility.LogFile("Submit stream to client queue " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitStream4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void NonProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering NonProprietarySegmentTransmit tx client " + Convert.ToString(m_TransmittedClient) + " rx server " + Convert.ToString(m_ReceivedServer), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (!EnterNonProprietarySegmentTxCriticalArea(false))
            {
                return;
            }

            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (m_NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentTransmit: tx is in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }

                if (IsClientTxQueueEmpty())
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " cannot get from the queue", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Sending (non-proprietary segment) " + Convert.ToString(m_clientStream.Length), ModuleLogLevel);
                
                uint length;
                bool isMsg;
                byte[] buff = (byte [])GetClient2Transmit(out length,out isMsg);
                if (buff != null)
                {
                    m_OnNonProprietaryTransmittedCbk.SetBuffer(buff, 0, buff.Length);
                    m_NonProprietaryTxInitiatedTs = DateTime.Now;
                    if (!m_clientSideSocket.SendAsync(m_OnNonProprietaryTransmittedCbk))
                    {
                        if (m_OnNonProprietaryTransmittedCbk.BytesTransferred != buff.Length)
                        {
                            LogUtility.LogUtility.LogFile("!!!!NonProprietary: tx " + Convert.ToString(m_OnNonProprietaryTransmittedCbk.BytesTransferred) + " attempted " + Convert.ToString(buff.Length) + " " + System.Enum.GetName(typeof(SocketError), m_OnNonProprietaryTransmittedCbk.SocketError), ModuleLogLevel);
                        }
                        m_NonProprietaryTxCompletedTs = DateTime.Now;
                        m_TransmittedClient += (uint)buff.Length;
                        OnClientTransmitted(buff.Length);
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                        LeaveNonProprietarySegmentTxCriticalArea();
                        ReStartAllOperations(false);
                        return;
                    }
                    m_NonProprietarySegmentTxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                m_NonProprietarySegmentTxInProgress = false;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
        }
        protected virtual void OnProprietarySegmentMsgReceived()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnProprietarySegmentMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received message type " + Convert.ToString(m_rxStateMachine.GetKind()), ModuleLogLevel);
                if (m_rxStateMachine.GetMsgBody() == null)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " msg body is null", ModuleLogLevel);
                    return;
                }
                m_ReceivedMsgs++;
                switch (m_rxStateMachine.GetKind())
                {
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_DATA_KIND:
                        NonProprietarySegmentSubmitStream4Tx(m_rxStateMachine.GetMsgBody());
                        //NonProprietarySegmentTransmit();
                        m_rxStateMachine.ClearMsgBody();
                        break;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietarySegmentMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
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
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received (proprietary segment) ERROR " + " " + System.Enum.GetName(typeof(SocketError), e.SocketError), ModuleLogLevel);
                    CheckConnectionAndShutDownIfGone();
                    Dispose();
                    goto prop_rx;
                }
                m_ProprietaryRxCompletedTs = DateTime.Now;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received (proprietary segment) " + Convert.ToString(Received), ModuleLogLevel);
                m_ReceivedServer += (uint)Received;
                m_rxStateMachine.OnRxComplete(m_ProprietarySementRxBuf, Received);
                m_ProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        prop_rx:
            LeaveProprietarySegmentRxCriticalArea();
            ReStartAllOperations(false);
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
                if (!m_destinationSideSocket.ReceiveAsync(m_OnProprietaryReceivedCbk))
                {
                    m_ProprietaryRxCompletedTs = DateTime.Now;
                    int Received = m_OnProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received (sync) (proprietary segment) ERROR" + " " + System.Enum.GetName(typeof(SocketError), m_OnProprietaryReceivedCbk.SocketError), ModuleLogLevel);
                        LeaveProprietarySegmentRxCriticalArea();
                        CheckConnectionAndShutDownIfGone();
                        Dispose();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received (sync) (proprietary segment) " + Convert.ToString(Received), ModuleLogLevel);
                    m_ReceivedServer += (uint)Received;
                    m_rxStateMachine.OnRxComplete(m_ProprietarySementRxBuf, Received);
                    IsRestartRequired = true;
                }
                else
                {
                    m_ProprietarySegmentRxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        protected void OnProprietarySegmentTransmitted(int transmitted)
        {
            if (m_txStateMachine.IsInBody())
            {
                if (m_txStateMachine.IsWholeMessage())
                {
                    OnDestinationTransmitted(transmitted - m_txStateMachine.GetHeaderLength());
                }
                else
                {
                    OnDestinationTransmitted(transmitted);
                }
            }
            m_TransmittedServer += (uint)transmitted;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " sent (proprietary segment) " + Convert.ToString(transmitted), ModuleLogLevel);
            m_txStateMachine.OnTxComplete((uint)transmitted);
            if (m_txStateMachine.IsTransactionCompleted())
            {
                m_txStateMachine.ClearMsgBody();
            }
        }
        protected override void OnProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentTxCriticalArea(true);
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!m_ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress, return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                int Ret = 0;
                Ret = e.BytesTransferred;
                
                if (Ret <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " error on EndSend " + Convert.ToString(Ret) + " " + System.Enum.GetName(typeof(SocketError), e.SocketError), ModuleLogLevel);
                    goto end_server_tx;
                }
                m_ProprietaryTxCompletedTs = DateTime.Now;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Transmitted to server " + Convert.ToString(Ret), ModuleLogLevel);
                OnProprietarySegmentTransmitted(Ret);
                m_ProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietarySegmentTransmitted", ModuleLogLevel);
end_server_tx:
            LeaveProprietarySegmentTxCriticalArea();
            ReStartAllOperations(false);
        }
        protected virtual bool _ProprietarySegmentTransmit(byte []buff2transmit)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering _ProprietarySegmentTransmit", ModuleLogLevel);
            try
            {
                m_OnProprietaryTransmittedCbk.SetBuffer(buff2transmit, 0, buff2transmit.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Trying to send to destination " + Convert.ToString(buff2transmit.Length), ModuleLogLevel);
                m_ProprietaryTxInitiatedTs = DateTime.Now;
                if (!m_destinationSideSocket.SendAsync(m_OnProprietaryTransmittedCbk))
                {
                    m_ProprietaryTxCompletedTs = DateTime.Now;
                    if (m_OnProprietaryTransmittedCbk.BytesTransferred != buff2transmit.Length)
                    {
                        LogUtility.LogUtility.LogFile("!!!Proprietary tx " + Convert.ToString(m_OnProprietaryTransmittedCbk.BytesTransferred) + " attempted " + Convert.ToString(buff2transmit.Length) + " " + System.Enum.GetName(typeof(SocketError), m_OnProprietaryTransmittedCbk.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    }
                    OnProprietarySegmentTransmitted(buff2transmit.Length);
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving _ProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                    return true;
                }
                m_ProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving _ProprietarySegmentTransmit", ModuleLogLevel);
            return false;
        }

        protected override void OnNonProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentTxCriticalArea(true);
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!m_NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress, return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                sent = e.BytesTransferred;
                
                if (sent < 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " OnNonProprietarySegmentTransmitted " + "sent error " + " " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                    return;
                }
                m_NonProprietaryTxCompletedTs = DateTime.Now;
                m_TransmittedClient += (uint)sent;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " " + Convert.ToString(sent) + " sent to client overall " + Convert.ToString(m_TransmittedClient), ModuleLogLevel);
                m_NonProprietarySegmentTxInProgress = false;
                OnClientTransmitted(sent);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                LeaveNonProprietarySegmentTxCriticalArea();
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
            ReStartAllOperations(false);
        }
        protected virtual void ProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            
            if (data != null)
            {
                LogUtility.LogUtility.LogFile("submit msg to dest " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitMsg4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected virtual void ProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile("submit stream for tx to dest " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitStream4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void ProprietarySegmentTransmit()
        {
            bool IsRestartRequired = false;
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
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " tx is in progress, return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                if(!m_txStateMachine.IsBusy())
                {
                    uint length;
                    bool isMsg;
                    byte []data = GetDestination2Transmit(out length,out isMsg);
                    if (data == null)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " queue is empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                        LeaveProprietarySegmentTxCriticalArea();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " initiating new msg len " + Convert.ToString(data.Length) + " " + Convert.ToString(isMsg), ModuleLogLevel);
                    if (isMsg)
                    {
                        m_txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_MSG_KIND);
                    }
                    else
                    {
                        m_txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_DATA_KIND);
                    }
                    m_txStateMachine.SetLength((uint)length);
                    m_txStateMachine.SetMsgBody(data);
                }
                
                byte[] buff2transmit = m_txStateMachine.GetBytes(8192);
                if (buff2transmit != null)
                {
                    IsRestartRequired = _ProprietarySegmentTransmit(buff2transmit);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentTxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        protected override bool ClientTxInProgress()
        {
            return m_NonProprietarySegmentTxInProgress;
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
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: exiting, rx is not in progress", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;

                if (Received <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " OnClientReceive error " + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    m_NonProprietarySegmentRxInProgress = false;
                    LeaveNonProprietarySegmentRxCriticalArea();
                    if (e.SocketError == SocketError.Success)
                    {
                     //   ReStartAllOperations(false);
                    }
                    else
                    {
                       // LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " OnClientReceive error " + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                    return;
                }
                m_NonProprietaryRxCompletedTs = DateTime.Now;
                m_ReceivedClient += (uint)Received;
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received on non-proprietary segment " + Convert.ToString(Received) + " overall " + Convert.ToString(m_ReceivedClient), ModuleLogLevel);
                byte[] data = new byte[Received];
                CopyBytes(m_NonProprietarySegmentRxBuf, data, Received);
                ProprietarySegmentSubmitStream4Tx(data);
                //ProprietarySegmentTransmit();
                m_NonProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnNonProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentRxCriticalArea();
            ReStartAllOperations(false);
        }
        protected override void NonProprietarySegmentReceive()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ReceiveNonProprietarySegment", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (!EnterNonProprietarySegmentRxCriticalArea(false))
            {
                return;
            }
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (m_NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ReceiveNonProprietarySegment: exiting, rx is in progress", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                m_OnNonProprietaryReceivedCbk.SetBuffer(m_NonProprietarySegmentRxBuf, 0, m_NonProprietarySegmentRxBuf.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " NonProprietary ReceivAsync ", ModuleLogLevel);
                m_NonProprietaryRxInitiatedTs = DateTime.Now;
                if (!m_clientSideSocket.ReceiveAsync(m_OnNonProprietaryReceivedCbk))
                {
                    m_NonProprietaryRxCompletedTs = DateTime.Now;
                    int Received = m_OnNonProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " OnClientReceive (sync) error " + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), m_OnNonProprietaryReceivedCbk.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
                        LeaveNonProprietarySegmentRxCriticalArea();
                        CheckConnectionAndShutDownIfGone();
                        return;
                    }
                    m_ReceivedClient += (uint)Received;
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Received on non-proprietary segment (sync) " + Convert.ToString(Received) + " overall " + Convert.ToString(m_ReceivedClient), ModuleLogLevel);
                    byte[] data = new byte[Received];
                    CopyBytes(m_NonProprietarySegmentRxBuf, data, Received);
                    ProprietarySegmentSubmitStream4Tx(data);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ReceiveNonProprietarySegment", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        protected override void Disposing()
        {
        }
        public override void Start()
        {
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Starting new client", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentReceive();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
    }
}
