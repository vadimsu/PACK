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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentSubmitStream4Tx", ModuleLogLevel);

            if (data != null)
            {
                LogUtility.LogUtility.LogFile("Submit stream to client queue " + Convert.ToString(data.Length), ModuleLogLevel);
                SubmitStream4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentSubmitStream4Tx", ModuleLogLevel);
        }
        protected override void NonProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentTransmit", ModuleLogLevel);
            EnterNonProprietarySegmentTxCriticalArea();

            try
            {
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentTransmit: tx is in progress,return", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }

                if (IsClientTxQueueEmpty())
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " cannot get from the queue", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Sending (non-proprietary segment) " + Convert.ToString(clientStream.Length), ModuleLogLevel);
                
                uint length;
                bool isMsg;
                byte[] buff = (byte [])GetClient2Transmit(out length,out isMsg);
                m_OnNonProprietaryTransmittedCbk.SetBuffer(buff, 0, buff.Length);
                clientSideSocket.SendAsync(m_OnNonProprietaryTransmittedCbk);
                NonProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentTxInProgress = false;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit", ModuleLogLevel);
            LeaveNonProprietarySegmentTxCriticalArea();
        }
        protected virtual void OnProprietarySegmentMsgReceived()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentMsgReceived", ModuleLogLevel);
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received message type " + Convert.ToString(rxStateMachine.GetKind()), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentMsgReceived", ModuleLogLevel);
        }
        protected override void OnProprietarySegmentReceived(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentReceived", ModuleLogLevel);
            EnterProprietarySegmentRxCriticalArea();
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress", ModuleLogLevel);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    CheckConnectionAndShutDownIfGone();
                    goto prop_rx;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (proprietary segment) " + Convert.ToString(Received), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                ReceivedServer += (uint)Received;
                rxStateMachine.OnRxComplete(ProprietarySementRxBuf, Received);
                ProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentReceived", ModuleLogLevel);
        prop_rx:
            LeaveProprietarySegmentRxCriticalArea();
            ReStartAllOperations(false);
        }

        protected override void ProprietarySegmentReceive()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentReceive", ModuleLogLevel);
            EnterProprietarySegmentRxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ProprietarySegmentReceive: rx is in progress,return", ModuleLogLevel);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " BeginReceive (proprietary segment)", ModuleLogLevel);
                m_OnProprietaryReceivedCbk.SetBuffer(ProprietarySementRxBuf, 0, ProprietarySementRxBuf.Length);
                destinationSideSocket.ReceiveAsync(m_OnProprietaryReceivedCbk);
                ProprietarySegmentRxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentReceive", ModuleLogLevel);
            LeaveProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            //NonProprietarySegmentTransmit();
        }
        protected override void OnProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentTransmitted", ModuleLogLevel);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (!ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress, return", ModuleLogLevel);
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
                if (txStateMachine.IsInBody())
                {
                    OnDestinationTransmitted(Ret);
                }
                TransmittedServer += (uint)Ret;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sent (proprietary segment) " + Convert.ToString(Ret), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                txStateMachine.OnTxComplete((uint)Ret);
                if (txStateMachine.IsTransactionCompleted())
                {
                    txStateMachine.ClearMsgBody();
                }
                ProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentTransmitted", ModuleLogLevel);
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
                destinationSideSocket.SendAsync(m_OnProprietaryTransmittedCbk);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentTransmitted", ModuleLogLevel);
            EnterNonProprietarySegmentTxCriticalArea();
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress, return", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                sent = e.BytesTransferred;
                
                if (sent < 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnNonProprietarySegmentTransmitted " + "sent error", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    return;
                }
                TransmittedClient += (uint)sent;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " " + Convert.ToString(sent) + " sent to client overall " + Convert.ToString(TransmittedClient), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentTxInProgress = false;
                OnClientTransmitted(sent);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                LeaveNonProprietarySegmentTxCriticalArea();
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentTransmitted", ModuleLogLevel);
            LeaveNonProprietarySegmentTxCriticalArea();
            ReStartAllOperations(false);
        }
        protected virtual void ProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
            
            if (data != null)
            {
                LogUtility.LogUtility.LogFile("submit msg to dest " + Convert.ToString(data.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                SubmitMsg4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
        }
        protected virtual void ProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmitStream4Tx", ModuleLogLevel);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile("submit stream for tx to dest " + Convert.ToString(data.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                SubmitStream4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmitStream4Tx", ModuleLogLevel);
        }
        protected override void ProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentTransmit", ModuleLogLevel);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " tx is in progress, return", ModuleLogLevel);
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
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " queue is empty", ModuleLogLevel);
                        LeaveProprietarySegmentTxCriticalArea();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " initiating new msg len" + Convert.ToString(data.Length) + " " + Convert.ToString(isMsg), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
                
                byte[] buff2transmit = txStateMachine.GetBytes();
                if (buff2transmit != null)
                {
                    _ProprietarySegmentTransmit(buff2transmit);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentTransmit", ModuleLogLevel);
            LeaveProprietarySegmentTxCriticalArea();
            CheckConnectionAndShutDownIfGone();
        }
        protected override bool ClientTxInProgress()
        {
            return NonProprietarySegmentTxInProgress;
        }
        protected override void OnNonProprietarySegmentReceived(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentReceived", ModuleLogLevel);
            EnterNonProprietarySegmentRxCriticalArea();
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: exiting, rx is not in progress", ModuleLogLevel);
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
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received on non-proprietary segment " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedClient), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentReceived", ModuleLogLevel);
            LeaveNonProprietarySegmentRxCriticalArea();
            ReStartAllOperations(false);
        }
        protected override void NonProprietarySegmentReceive()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ReceiveNonProprietarySegment", ModuleLogLevel);
            EnterNonProprietarySegmentRxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ReceiveNonProprietarySegment: exiting, rx is in progress", ModuleLogLevel);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                m_OnNonProprietaryReceivedCbk.SetBuffer(NonProprietarySegmentRxBuf, 0, NonProprietarySegmentRxBuf.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Calling BeginReceive (non-proprietary side)", ModuleLogLevel);
                clientSideSocket.ReceiveAsync(m_OnNonProprietaryReceivedCbk);
                NonProprietarySegmentRxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ReceiveNonProprietarySegment", ModuleLogLevel);
            LeaveNonProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
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
