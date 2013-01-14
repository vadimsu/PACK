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
        IPEndPoint destinationSideEndPoint;
        
        public ClientSideProxy(Socket clientSocket, IPEndPoint remoteEndPoint)
            : base()
        {
            try
            {
                clientSideSocket = clientSocket;
                destinationSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                destinationSideEndPoint = remoteEndPoint;
                destinationSideSocket.Connect(destinationSideEndPoint);
                Id = destinationSideSocket.LocalEndPoint;
                rxStateMachine.SetCallback(new OnMsgReceived(OnProprietarySegmentMsgReceived));
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }      
        
        protected virtual void NonProprietarySegmentSubmitStream4Tx(byte []data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);

            if (data != null)
            {
                LogUtility.LogUtility.LogFile("Submit stream to client queue " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitStream4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void NonProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentTransmit tx client " + Convert.ToString(TransmittedClient) + " rx server " + Convert.ToString(ReceivedServer), LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentTxCriticalArea();

            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentTransmit: tx is in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }

                if (IsClientTxQueueEmpty())
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " cannot get from the queue", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Sending (non-proprietary segment) " + Convert.ToString(clientStream.Length), ModuleLogLevel);
                
                uint length;
                bool isMsg;
                byte[] buff = (byte [])GetClient2Transmit(out length,out isMsg);
                if (buff != null)
                {
                    m_OnNonProprietaryTransmittedCbk.SetBuffer(buff, 0, buff.Length);
                    if (!clientSideSocket.SendAsync(m_OnNonProprietaryTransmittedCbk))
                    {
                        if (m_OnNonProprietaryTransmittedCbk.BytesTransferred != buff.Length)
                        {
                        }
                        TransmittedClient += (uint)buff.Length;
                        OnClientTransmitted(buff.Length);
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                        LeaveNonProprietarySegmentTxCriticalArea();
                        return;
                    }
                    NonProprietarySegmentTxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentTxInProgress = false;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
        }
        protected virtual void OnProprietarySegmentMsgReceived()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received message type " + Convert.ToString(rxStateMachine.GetKind()), ModuleLogLevel);
                if (rxStateMachine.GetMsgBody() == null)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " msg body is null", ModuleLogLevel);
                    return;
                }
                ReceivedMsgs++;
                switch (rxStateMachine.GetKind())
                {
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_DATA_KIND:
                        NonProprietarySegmentSubmitStream4Tx(rxStateMachine.GetMsgBody());
                        //NonProprietarySegmentTransmit();
                        rxStateMachine.ClearMsgBody();
                        break;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void OnProprietarySegmentReceived(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentRxCriticalArea();
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    CheckConnectionAndShutDownIfGone();
                    goto prop_rx;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (proprietary segment) " + Convert.ToString(Received), ModuleLogLevel);
                ReceivedServer += (uint)Received;
                rxStateMachine.OnRxComplete(ProprietarySementRxBuf, Received);
                ProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        prop_rx:
            LeaveProprietarySegmentRxCriticalArea();
            ReStartAllOperations(false);
        }

        protected override void ProprietarySegmentReceive()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentRxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ProprietarySegmentReceive: rx is in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " BeginReceive (proprietary segment)", ModuleLogLevel);
                m_OnProprietaryReceivedCbk.SetBuffer(ProprietarySementRxBuf, 0, ProprietarySementRxBuf.Length);
                if (!destinationSideSocket.ReceiveAsync(m_OnProprietaryReceivedCbk))
                {
                    int Received = m_OnProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (sync) (proprietary segment) ERROR", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        LeaveProprietarySegmentRxCriticalArea();
                        CheckConnectionAndShutDownIfGone();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (sync) (proprietary segment) " + Convert.ToString(Received), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    ReceivedServer += (uint)Received;
                    rxStateMachine.OnRxComplete(ProprietarySementRxBuf, Received);
                    IsRestartRequired = true;
                }
                else
                {
                    ProprietarySegmentRxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        protected void OnProprietarySegmentTransmitted(int transmitted)
        {
            if (txStateMachine.IsInBody())
            {
                if (txStateMachine.IsWholeMessage())
                {
                    OnDestinationTransmitted(transmitted - txStateMachine.GetHeaderLength());
                }
                else
                {
                    OnDestinationTransmitted(transmitted);
                }
            }
            TransmittedServer += (uint)transmitted;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sent (proprietary segment) " + Convert.ToString(transmitted), ModuleLogLevel);
            txStateMachine.OnTxComplete((uint)transmitted);
            if (txStateMachine.IsTransactionCompleted())
            {
                txStateMachine.ClearMsgBody();
            }
        }
        protected override void OnProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress, return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                int Ret = 0;
                Ret = e.BytesTransferred;
                
                if (Ret <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " error on EndSend " + Convert.ToString(Ret), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    goto end_server_tx;
                }
                OnProprietarySegmentTransmitted(Ret);
                ProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
end_server_tx:
            LeaveProprietarySegmentTxCriticalArea();
            ReStartAllOperations(false);
        }
        protected virtual void _ProprietarySegmentTransmit(byte []buff2transmit)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering _ProprietarySegmentTransmit", ModuleLogLevel);
            try
            {
                m_OnProprietaryTransmittedCbk.SetBuffer(buff2transmit, 0, buff2transmit.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Trying to send to destination " + Convert.ToString(buff2transmit.Length), ModuleLogLevel);
                if (!destinationSideSocket.SendAsync(m_OnProprietaryTransmittedCbk))
                {
                    if (m_OnProprietaryTransmittedCbk.BytesTransferred != buff2transmit.Length)
                    {
                    }
                    OnProprietarySegmentTransmitted(buff2transmit.Length);
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _ProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                    return;
                }
                ProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _ProprietarySegmentTransmit", ModuleLogLevel);
        }

        protected override void OnNonProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentTxCriticalArea();
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress, return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                sent = e.BytesTransferred;
                
                if (sent < 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnNonProprietarySegmentTransmitted " + "sent error", ModuleLogLevel);
                    return;
                }
                TransmittedClient += (uint)sent;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " " + Convert.ToString(sent) + " sent to client overall " + Convert.ToString(TransmittedClient), ModuleLogLevel);
                NonProprietarySegmentTxInProgress = false;
                OnClientTransmitted(sent);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                LeaveNonProprietarySegmentTxCriticalArea();
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
            ReStartAllOperations(false);
        }
        protected virtual void ProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            
            if (data != null)
            {
                LogUtility.LogUtility.LogFile("submit msg to dest " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitMsg4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected virtual void ProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile("submit stream for tx to dest " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitStream4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmitStream4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void ProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " tx is in progress, return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                if(!txStateMachine.IsBusy())
                {
                    uint length;
                    bool isMsg;
                    byte []data = GetDestination2Transmit(out length,out isMsg);
                    if (data == null)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " queue is empty", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                        LeaveProprietarySegmentTxCriticalArea();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " initiating new msg len " + Convert.ToString(data.Length) + " " + Convert.ToString(isMsg), ModuleLogLevel);
                    if (isMsg)
                    {
                        txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_MSG_KIND);
                    }
                    else
                    {
                        txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_DATA_KIND);
                    }
                    txStateMachine.SetLength((uint)length);
                    txStateMachine.SetMsgBody(data);
                }
                
                byte[] buff2transmit = txStateMachine.GetBytes(8192);
                if (buff2transmit != null)
                {
                    _ProprietarySegmentTransmit(buff2transmit);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentTxCriticalArea();
            CheckConnectionAndShutDownIfGone();
        }
        protected override bool ClientTxInProgress()
        {
            return NonProprietarySegmentTxInProgress;
        }
        protected override void OnNonProprietarySegmentReceived(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentRxCriticalArea();
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (!NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: exiting, rx is not in progress", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;

                if (Received <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnClientReceive error " + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    NonProprietarySegmentRxInProgress = false;
                    LeaveNonProprietarySegmentRxCriticalArea();
                    //Dispose();
                    return;
                }
                ReceivedClient += (uint)Received;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received on non-proprietary segment " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedClient), ModuleLogLevel);
                byte[] data = new byte[Received];
                CopyBytes(NonProprietarySegmentRxBuf, data, Received);
                ProprietarySegmentSubmitStream4Tx(data);
                //ProprietarySegmentTransmit();
                NonProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentRxCriticalArea();
            ReStartAllOperations(false);
        }
        protected override void NonProprietarySegmentReceive()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ReceiveNonProprietarySegment", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentRxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ReceiveNonProprietarySegment: exiting, rx is in progress", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                m_OnNonProprietaryReceivedCbk.SetBuffer(NonProprietarySegmentRxBuf, 0, NonProprietarySegmentRxBuf.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Calling BeginReceive (non-proprietary side)", ModuleLogLevel);
                if (!clientSideSocket.ReceiveAsync(m_OnNonProprietaryReceivedCbk))
                {
                    int Received = m_OnNonProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnClientReceive (sync) error " + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), m_OnNonProprietaryReceivedCbk.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        LeaveNonProprietarySegmentRxCriticalArea();
                        CheckConnectionAndShutDownIfGone();
                        return;
                    }
                    ReceivedClient += (uint)Received;
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received on non-proprietary segment (sync) " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedClient), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    byte[] data = new byte[Received];
                    CopyBytes(NonProprietarySegmentRxBuf, data, Received);
                    ProprietarySegmentSubmitStream4Tx(data);
                    IsRestartRequired = true;
                }
                else
                {
                    NonProprietarySegmentRxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ReceiveNonProprietarySegment", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
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
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Starting new client", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentReceive();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
    }
}
