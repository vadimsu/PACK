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
        protected bool ErrorSent;
        protected bool m_OnceConnected;

        public ServerSideProxy(Socket sock)
            : base()
        {
            clientSideSocket = sock;
            destinationSideSocket = null;
            ErrorSent = false;
            m_OnceConnected = false;
        }

        protected override void OnProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                int Ret = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                Ret = e.BytesTransferred;
                
                if (Ret < 0)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " error on EndSend (proprietary segment) " + Convert.ToString(Ret), ModuleLogLevel);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                TransmittedClient += (uint)Ret;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sent (proprietary segment) " + Convert.ToString(Ret), ModuleLogLevel);
                if (ErrorSent)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " ErrorSent flag is up, checking if queue is empty", ModuleLogLevel);
                    if (IsClientTxQueueEmpty())
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + "OnProprietarySegmentTransmitted: queue is empty and error is sent", ModuleLogLevel);
                        Dispose();
                        LeaveProprietarySegmentTxCriticalArea();
                        return;
                    }
                }
                ProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentTxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void OnNonProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentTxCriticalArea();
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                sent = e.BytesTransferred;
                
                TransmittedServer += (uint)sent;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " " + Convert.ToString(sent) + " sent to destination " + Convert.ToString(sent) + " overall " + Convert.ToString(TransmittedServer), ModuleLogLevel);
                OnDestinationTransmitted(sent);
                NonProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentTransmitted", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        void _ProprietarySegmentTransmit(byte[] buff2transmit)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering _ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                if (clientSideSocket.SendBufferSize < buff2transmit.Length)
                {
                    LogUtility.LogUtility.LogFile("increasing tx buffer size", ModuleLogLevel);
                    clientSideSocket.SendBufferSize = buff2transmit.Length;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " send (proprietary segment) " + Convert.ToString(buff2transmit.Length), ModuleLogLevel);
                
                m_OnProprietaryTransmittedCbk.SetBuffer(buff2transmit, 0, buff2transmit.Length);
                if (!clientSideSocket.SendAsync(m_OnProprietaryTransmittedCbk))
                {
                    if (m_OnProprietaryTransmittedCbk.BytesTransferred != buff2transmit.Length)
                    {
                    }
                    TransmittedClient += (uint)buff2transmit.Length;
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _ProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                    return;
                }
                ProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                //Dispose2();
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }

        protected void ProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (ErrorSent)
            {
                LogUtility.LogUtility.LogFile("discard, ErrorSent flag is up, leaving ProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                return;
            }
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx stream client queue", ModuleLogLevel);
                SubmitStream4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected void ProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (ErrorSent)
            {
                LogUtility.LogUtility.LogFile("discard, ErrorSent flag is up, leaving ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                return;
            }
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx msg to client queue", ModuleLogLevel);
                SubmitMsg4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void ProprietarySegmentTransmit()
        {
            byte[] buff2transmit;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if (ProprietarySegmentTxInProgress)
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
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " queue is empty, exiting the loop", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                        break;
                    }
                    if (!txStateMachine.IsBusy())
                    {
                        uint length;
                        bool isMsg;
                        byte[] buf2tx = GetClient2Transmit(out length, out isMsg);
                        if (buf2tx != null)
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " tx to client  msg len " + Convert.ToString(length) + " isMsg " + Convert.ToString(isMsg), ModuleLogLevel);
                            if (isMsg)
                            {
                                txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_MSG_KIND);
                            }
                            else
                            {
                                txStateMachine.SetKind((byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_DATA_KIND);
                            }
                            txStateMachine.SetLength(length);
                            txStateMachine.SetMsgBody(buf2tx);
                        }
                        else
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " queue is empty, exiting the loop", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                            break;
                        }
                    }
                    buff2transmit = txStateMachine.GetBytes();
                    if (buff2transmit != null)
                    {
                        //_ProprietarySegmentTransmit(buff2transmit);
                        stream.Write(buff2transmit, 0, buff2transmit.Length);
                        if (txStateMachine.IsInBody())
                        {
                            if (txStateMachine.IsWholeMessage())
                            {
                                OnClientTransmitted(buff2transmit.Length - txStateMachine.GetHeaderLength());
                            }
                            else
                            {
                                OnClientTransmitted(buff2transmit.Length);
                            }
                        }
                        txStateMachine.OnTxComplete((uint)buff2transmit.Length);
                        if (txStateMachine.IsTransactionCompleted())
                        {
                            txStateMachine.ClearMsgBody();
                            TransmittedMsgsClient++;
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
                    _ProprietarySegmentTransmit(buff2transmit);
                } 
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentTxCriticalArea();
        }
        public override bool IsClientTxQueueEmpty()
        {
            return ((!txStateMachine.IsBusy()) && base.IsClientTxQueueEmpty());
        }
        public abstract void ProcessDownStreamData(byte []data);
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
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: rx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    CheckConnectionAndShutDownIfGone();
                    goto end_server_rx;
                }
                ReceivedServer += (uint)Received;
                ReceivedMsgs++;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (non-proprietary segment) " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedServer), ModuleLogLevel);

                byte[] buff = new byte[Received];
                CopyBytes(NonProprietarySegmentRxBuf, buff, Received);
                ProcessDownStreamData(buff);
                NonProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
end_server_rx:
            LeaveNonProprietarySegmentRxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void NonProprietarySegmentReceive()
        {
            bool IsRestartRequired = false;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentRxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if ((destinationSideSocket == null) || (!destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentReceive (socket is not connected)", ModuleLogLevel);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                
                if (NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentReceive: rx is in progess,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                m_OnNonProprietaryReceivedCbk.SetBuffer(NonProprietarySegmentRxBuf, 0, NonProprietarySegmentRxBuf.Length);
                if (!destinationSideSocket.ReceiveAsync(m_OnNonProprietaryReceivedCbk))
                {
                    int Received = m_OnNonProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received(sync) (non-proprietary segment) ERROR ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        goto end_server_rx;
                    }
                    ReceivedServer += (uint)Received;
                    ReceivedMsgs++;
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received(sync) (non-proprietary segment) " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedServer), LogUtility.LogLevels.LEVEL_LOG_HIGH);

                    byte[] buff = new byte[Received];
                    CopyBytes(NonProprietarySegmentRxBuf, buff, Received);
                    ProcessDownStreamData(buff);
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
end_server_rx:
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        void _NonProprietarySegmentTransmit(byte []data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering _NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                m_OnNonProprietaryTransmittedCbk.SetBuffer(data, 0, data.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Sending to destination " + Convert.ToString(data.Length), ModuleLogLevel);
                if (!destinationSideSocket.SendAsync(m_OnNonProprietaryTransmittedCbk))
                {
                    if (m_OnNonProprietaryTransmittedCbk.BytesTransferred != data.Length)
                    {
                    }
                    TransmittedServer += (uint)data.Length;
                    OnDestinationTransmitted(data.Length);
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _NonProprietarySegmentTransmit (completed synchronously)", ModuleLogLevel);
                    return;
                }
                NonProprietarySegmentTxInProgress = true;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected void NonProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx client queue", ModuleLogLevel);
                SubmitStream4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentSubmit4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected void NonProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx client queue", ModuleLogLevel);
                SubmitMsg4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentSubmitMsg4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override void NonProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            EnterNonProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                if ((destinationSideSocket == null) || (!destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit (socket is not connected)", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " is not in tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    uint length;
                    bool isMsg;
                    byte []data  = GetDestination2Transmit(out length,out isMsg);
                    if (data != null)
                    {
                        _NonProprietarySegmentTransmit(data);
                    }
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " DestinationTx is busy and queueElement is null", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveNonProprietarySegmentTxCriticalArea();
            CheckConnectionAndShutDownIfGone();
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietaryMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                LogUtility.LogUtility.LogFile("Received msg type " + Convert.ToString(rxStateMachine.GetKind()), ModuleLogLevel);
                if (rxStateMachine.GetMsgBody() == null)
                {
                    LogUtility.LogUtility.LogFile("msg body is null!!!", ModuleLogLevel);
                    return;
                }
                switch (rxStateMachine.GetKind())
                {
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_DATA_KIND:
                        ProcessUpStreamDataKind();
                        break;
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_UPSTREAM_MSG_KIND:
                        ProcessUpStreamMsgKind();
                        OnDownStreamTransmissionOpportunity();
                        break;
                }
                rxStateMachine.ClearMsgBody();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietaryMsgReceived", ModuleLogLevel);
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
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress,return", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile("Received (proprietary segment) " + Convert.ToString(Received), ModuleLogLevel);
                ReceivedClient += (uint)Received;
                rxStateMachine.OnRxComplete(e.Buffer, Received);
                ProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentRxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
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
                m_OnProprietaryReceivedCbk.SetBuffer(ProprietarySementRxBuf, 0, ProprietarySementRxBuf.Length);
                if (!clientSideSocket.ReceiveAsync(m_OnProprietaryReceivedCbk))
                {
                    int Received = m_OnProprietaryReceivedCbk.BytesTransferred;
                    if (Received <= 0)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " ProprietarySegmentReceived (sync) ERROR", ModuleLogLevel);
                        LeaveProprietarySegmentRxCriticalArea();
                        return;
                    }
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " ProprietarySegmentReceived (sync) " + Convert.ToString(Received), ModuleLogLevel);
                    ReceivedClient += (uint)Received;
                    rxStateMachine.OnRxComplete(m_OnProprietaryReceivedCbk.Buffer, Received);
                    IsRestartRequired = true;
                }
                else
                {
                    ProprietarySegmentRxInProgress = true;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + "EXCEPTION " + exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentReceive", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietarySegmentRxCriticalArea();
            if (IsRestartRequired)
            {
                ReStartAllOperations(false);
            }
        }
        protected override bool ClientTxInProgress()
        {
            return ProprietarySegmentTxInProgress;
        }
        public override void Start()
        {
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Starting new server", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                rxStateMachine.SetCallback(new OnMsgReceived(OnProprietarySegmentMsgReceived));
                ProprietarySegmentReceive();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + "EXCEPTION " + exc.Message,LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
    }
}
