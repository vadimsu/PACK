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
        PerformanceMonitoring.PerformanceMonitoring NonProprietarySegmentReceivedTicks;
        PerformanceMonitoring.PerformanceMonitoring ProprietarySegmentReceivedTicks;
        PerformanceMonitoring.PerformanceMonitoring NonProprietarySegmentTransmittedTicks;
        PerformanceMonitoring.PerformanceMonitoring ProprietarySegmentTransmittedTicks;

        PerformanceMonitoring.PerformanceMonitoring ProprietarySegmentTransmitTicks;
        PerformanceMonitoring.PerformanceMonitoring NonProprietarySegmentTransmitTicks;
        PerformanceMonitoring.PerformanceMonitoring ProprietarySegmentReceiveTicks;
        PerformanceMonitoring.PerformanceMonitoring NonProprietarySegmentReceiveTicks;
        
        public ClientSideProxy(Socket clientSocket, IPEndPoint remoteEndPoint)
            : base()
        {
            clientSideSocket = clientSocket;
            destinationSideSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //destinationSideSocket.LingerState.Enabled = true;
            //destinationSideSocket.LingerState.LingerTime = 10000;
            //destinationSideSocket.NoDelay = false;
            clientSideSocket.LingerState.Enabled = true;
            clientSideSocket.LingerState.LingerTime = 10000;
            destinationSideEndPoint = remoteEndPoint;
            clientSideSocket.NoDelay = false;

            NonProprietarySegmentReceivedTicks = new PerformanceMonitoring.PerformanceMonitoring("NonProprietarySegmentReceived");
            ProprietarySegmentReceivedTicks = new PerformanceMonitoring.PerformanceMonitoring("ProprietarySegmentReceived");
            NonProprietarySegmentTransmittedTicks = new PerformanceMonitoring.PerformanceMonitoring("NonProprietarySegmentTransmitted");
            ProprietarySegmentTransmittedTicks = new PerformanceMonitoring.PerformanceMonitoring("ProprietarySegmentTransmitted");

            ProprietarySegmentTransmitTicks = new PerformanceMonitoring.PerformanceMonitoring("ProprietarySegmentTransmit");
            NonProprietarySegmentTransmitTicks = new PerformanceMonitoring.PerformanceMonitoring("NonProprietarySegmentTransmit");
            ProprietarySegmentReceiveTicks = new PerformanceMonitoring.PerformanceMonitoring("ProprietarySegmentReceive");
            NonProprietarySegmentReceiveTicks = new PerformanceMonitoring.PerformanceMonitoring("NonProprietarySegmentReceive");
            Id = clientSideSocket.RemoteEndPoint;
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
        protected virtual void NonProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentTransmit", ModuleLogLevel);
            EnterNonProprietarySegmentTxCriticalArea();
            NonProprietarySegmentTransmitTicks.EnterFunction();
            try
            {
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentTransmit: tx is in progress,return", ModuleLogLevel);
                    NonProprietarySegmentTransmitTicks.LeaveFunction();
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }

                if (IsClientTxQueueEmpty())
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " cannot get from the queue", ModuleLogLevel);
                    if (ShutDownFlag)
                    {
                        if (Dispose2(false))
                        {
                            LeaveNonProprietarySegmentTxCriticalArea();
                            return;
                        }
                    }
                    NonProprietarySegmentTransmitTicks.LeaveFunction();
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Sending (non-proprietary segment) " + Convert.ToString(clientStream.Length), ModuleLogLevel);
                SocketError se = SocketError.Success;
                //NonProprietarySegmentTxInProgress = true;
                try
                {
                    uint length;
                    bool isMsg;
                    byte[] buff = (byte [])GetClient2Transmit(out length,out isMsg);
                    if (clientSideSocket.SendBufferSize < buff.Length)
                    {
                        clientSideSocket.SendBufferSize = buff.Length;
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " increasing tx buffer (non-proprietary) " + Convert.ToString(buff.Length), ModuleLogLevel);
                    }
                    clientSideSocket.BeginSend(buff, 0, buff.Length, SocketFlags.None, out se, m_OnNonProprietaryTransmittedCbk, null);
                    if (SocketError.Success != se)
                    {
                        LogUtility.LogUtility.LogFile(" Non-prop send ERROR CODE " + System.Enum.GetName(typeof(SocketError), se), ModuleLogLevel);
                    }
                    else
                    {
                        NonProprietarySegmentTxInProgress = true;
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    NonProprietarySegmentTxInProgress = false;
                    Dispose2(false);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                NonProprietarySegmentTxInProgress = false;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit", ModuleLogLevel);
            NonProprietarySegmentTransmitTicks.LeaveFunction();
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
        protected override void OnProprietarySegmentReceived(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentReceived", ModuleLogLevel);
            EnterProprietarySegmentRxCriticalArea();
            ProprietarySegmentReceivedTicks.EnterFunction();
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress", ModuleLogLevel);
                    ProprietarySegmentReceivedTicks.LeaveFunction();
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                try
                {
                    Received = destinationSideSocket.EndReceive(ar);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    if (!Dispose2(true))
                    {
                        LeaveProprietarySegmentRxCriticalArea();
                        NonProprietarySegmentTransmit();
                    }
                    ProprietarySegmentReceivedTicks.LeaveFunction();
                    return;
                }
                if (Received > 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (proprietary segment) " + Convert.ToString(Received), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    ReceivedServer += (uint)Received;
                    rxStateMachine.OnRxComplete((byte[])ar.AsyncState, Received);
                    uint expBytes = rxStateMachine.GetExpectedBytes();
                    if (expBytes > destinationSideSocket.ReceiveBufferSize)
                    {
                        destinationSideSocket.ReceiveBufferSize = (int)expBytes;
                    }
                    ProprietarySegmentRxInProgress = false;
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnProprietarySegmentReceived error", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    //return;
                    //if (!destinationSideSocket.Connected)
                    try
                    {
                        if (destinationSideSocket.Poll(1, SelectMode.SelectRead) && destinationSideSocket.Available == 0)
                        {
                            if (!Dispose2(true))
                            {
                                LeaveProprietarySegmentRxCriticalArea();
                                NonProprietarySegmentTransmit();
                            }
                            ProprietarySegmentReceivedTicks.LeaveFunction();
                            return;
                        }
                    }
                    catch (Exception exc)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        ProprietarySegmentReceivedTicks.LeaveFunction();
                        return;
                    }
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                ProprietarySegmentReceivedTicks.LeaveFunction();
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentReceived", ModuleLogLevel);
            ProprietarySegmentReceivedTicks.LeaveFunction();
            LeaveProprietarySegmentRxCriticalArea();
            NonProprietarySegmentTransmit();
            ProprietarySegmentTransmit();
            ProprietarySegmentReceive();
        }

        protected virtual void ProprietarySegmentReceive()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentReceive", ModuleLogLevel);
            EnterProprietarySegmentRxCriticalArea();
            ProprietarySegmentReceivedTicks.EnterFunction();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ProprietarySegmentReceive: rx is in progress,return", ModuleLogLevel);
                    ProprietarySegmentReceivedTicks.LeaveFunction();
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                //ProprietarySegmentRxInProgress = true;
                try
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " BeginReceive (proprietary segment)", ModuleLogLevel);
                    destinationSideSocket.BeginReceive(ProprietarySementRxBuf, 0, ProprietarySementRxBuf.Length, SocketFlags.None, m_OnProprietaryReceivedCbk, ProprietarySementRxBuf);
ProprietarySegmentRxInProgress = true;
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    if (Dispose2(true))
                    {
LeaveProprietarySegmentRxCriticalArea();
//                        NonProprietarySegmentTransmit();
                        return;
                    }
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentReceive", ModuleLogLevel);
            ProprietarySegmentReceivedTicks.LeaveFunction();
            LeaveProprietarySegmentRxCriticalArea();
            NonProprietarySegmentTransmit();
        }
        protected override void OnProprietarySegmentTransmitted(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentTransmitted", ModuleLogLevel);
            EnterProprietarySegmentTxCriticalArea();
            ProprietarySegmentTransmittedTicks.EnterFunction();
            try
            {
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (!ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress, return", ModuleLogLevel);
                    ProprietarySegmentTransmittedTicks.LeaveFunction();
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                int Ret = 0;
                try
                {
                    Ret = destinationSideSocket.EndSend(ar);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    if (!Dispose2(true))
                    {
                        LeaveProprietarySegmentTxCriticalArea();
                        ProprietarySegmentReceive();
                        NonProprietarySegmentTransmit();
                    }
                    ProprietarySegmentTransmittedTicks.LeaveFunction();
                    return;
                }
                if (Ret <= 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " error on EndSend " + Convert.ToString(Ret), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    if (!Dispose2(true))
                    {
                        LeaveProprietarySegmentTxCriticalArea();
                        ProprietarySegmentReceive();
                        NonProprietarySegmentTransmit();
                    }
                    ProprietarySegmentTransmittedTicks.LeaveFunction();
                    return;
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
            ProprietarySegmentTransmittedTicks.LeaveFunction();
            LeaveProprietarySegmentTxCriticalArea();
            ProprietarySegmentReceive();
            ProprietarySegmentTransmit();
            NonProprietarySegmentTransmit();
        }
        protected virtual void _ProprietarySegmentTransmit(byte []buff2transmit)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering _ProprietarySegmentTransmit", ModuleLogLevel);
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                //ProprietarySegmentTxInProgress = true;
                try
                {
                    if (destinationSideSocket.SendBufferSize < buff2transmit.Length)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " increasing tx buffer size", ModuleLogLevel);
                        destinationSideSocket.SendBufferSize = buff2transmit.Length;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Trying to send to destination " + Convert.ToString(buff2transmit.Length), ModuleLogLevel);
                    destinationSideSocket.BeginSend(buff2transmit, 0, buff2transmit.Length, SocketFlags.None, m_OnProprietaryTransmittedCbk, null);
ProprietarySegmentTxInProgress = true;
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    if (Dispose2(true))
                    {
                        return;
                    }
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _ProprietarySegmentTransmit", ModuleLogLevel);
        }

        protected override void OnNonProprietarySegmentTransmitted(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentTransmitted", ModuleLogLevel);
            EnterNonProprietarySegmentTxCriticalArea();
            NonProprietarySegmentTransmittedTicks.EnterFunction();
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress, return", ModuleLogLevel);
                    NonProprietarySegmentTransmittedTicks.LeaveFunction();
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                try
                {
                    sent = clientSideSocket.EndSend(ar);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    Dispose2(false);
                    NonProprietarySegmentTransmittedTicks.LeaveFunction();
                    return;
                }
                if (sent < 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnNonProprietarySegmentTransmitted " + "sent error", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    NonProprietarySegmentTransmittedTicks.LeaveFunction();
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
                NonProprietarySegmentTransmittedTicks.LeaveFunction();
                LeaveNonProprietarySegmentTxCriticalArea();
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentTransmitted", ModuleLogLevel);
            NonProprietarySegmentTransmittedTicks.LeaveFunction();
            LeaveNonProprietarySegmentTxCriticalArea();
            NonProprietarySegmentTransmit();
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
        protected virtual void ProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentTransmit", ModuleLogLevel);
            EnterProprietarySegmentTxCriticalArea();
            ProprietarySegmentTransmitTicks.EnterFunction();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " tx is in progress, return", ModuleLogLevel);
                    ProprietarySegmentTransmitTicks.LeaveFunction();
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
                        ProprietarySegmentTransmitTicks.LeaveFunction();
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
                if (!destinationSideSocket.Connected)
                {
                    destinationSideSocket.Connect(destinationSideEndPoint);
                    Id = destinationSideSocket.LocalEndPoint;
                    rxStateMachine.SetCallback(new OnMsgReceived(OnProprietarySegmentMsgReceived));
                    //ProprietarySegmentReceive();
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
            ProprietarySegmentTransmitTicks.LeaveFunction();
            LeaveProprietarySegmentTxCriticalArea();
        }
        protected override void OnNonProprietarySegmentReceived(IAsyncResult ar)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentReceived", ModuleLogLevel);
            EnterNonProprietarySegmentRxCriticalArea();
            NonProprietarySegmentReceivedTicks.EnterFunction();
            try
            {
                int Received = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: exiting, rx is not in progress", ModuleLogLevel);
                    NonProprietarySegmentReceivedTicks.LeaveFunction();
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                SocketError se = SocketError.Success;
                try
                {
                    Received = clientSideSocket.EndReceive(ar,out se);
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), se), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    NonProprietarySegmentReceivedTicks.LeaveFunction();
                    Dispose2(false);
                    return;
                }
                if (Received > 0)
                {
                    ReceivedClient += (uint)Received;
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received on non-proprietary segment " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedClient), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    byte[] data = new byte[Received];
                    CopyBytes(NonProprietarySegmentRxBuf, data, Received);
                    ProprietarySegmentSubmitStream4Tx(data);
                    //ProprietarySegmentTransmit();
                    NonProprietarySegmentRxInProgress = false;
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnClientReceive error " + " ERROR CODE " + System.Enum.GetName(typeof(SocketError), se), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    Dispose2(false);//
                    NonProprietarySegmentRxInProgress = false;//
#if false
                    NonProprietarySegmentReceivedTicks.LeaveFunction();
                    LeaveNonProprietarySegmentRxCriticalArea();
                    NonProprietarySegmentTransmit();
                    ProprietarySegmentTransmit();
                    return;
                    //NonProprietarySegmentRxInProgress = false;
                    //ReceiveNonProprietarySegment();
#endif
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentReceived", ModuleLogLevel);
            NonProprietarySegmentReceivedTicks.LeaveFunction();
            LeaveNonProprietarySegmentRxCriticalArea();
            NonProprietarySegmentTransmit();
            ProprietarySegmentTransmit();
            NonProprietarySegmentReceive();
        }
        protected virtual void NonProprietarySegmentReceive()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ReceiveNonProprietarySegment", ModuleLogLevel);
            EnterNonProprietarySegmentRxCriticalArea();
            NonProprietarySegmentReceiveTicks.EnterFunction();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ReceiveNonProprietarySegment: exiting, rx is in progress", ModuleLogLevel);
                    NonProprietarySegmentReceiveTicks.LeaveFunction();
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                //NonProprietarySegmentRxInProgress = true;
                SocketError se;
                try
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Calling BeginReceive (non-proprietary side)", ModuleLogLevel);
                    clientSideSocket.BeginReceive(NonProprietarySegmentRxBuf, 0, NonProprietarySegmentRxBuf.Length, SocketFlags.None, out se,m_OnNonProprietaryReceivedCbk, null);
if (SocketError.Success != se)
                    {
                        LogUtility.LogUtility.LogFile(" Non-prop recv ERROR CODE " + System.Enum.GetName(typeof(SocketError), se), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                    else
                    {
                        NonProprietarySegmentRxInProgress = true;
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    Dispose2(false);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ReceiveNonProprietarySegment", ModuleLogLevel);
            NonProprietarySegmentReceiveTicks.LeaveFunction();
            LeaveNonProprietarySegmentRxCriticalArea();
        }
        protected override void Disposing()
        {
            LogUtility.LogUtility.LogFile("PERFORMANCE COUNTERS", LogUtility.LogLevels.LEVEL_LOG_HIGH2);
            NonProprietarySegmentReceivedTicks.Log();
            ProprietarySegmentReceivedTicks.Log();
            NonProprietarySegmentTransmittedTicks.Log();
            ProprietarySegmentTransmittedTicks.Log();

            NonProprietarySegmentReceiveTicks.Log();
            ProprietarySegmentReceiveTicks.Log();
            NonProprietarySegmentTransmitTicks.Log();
            ProprietarySegmentTransmitTicks.Log();
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
