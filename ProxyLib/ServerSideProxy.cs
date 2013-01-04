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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentTransmitted", ModuleLogLevel);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                int Ret = 0;
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentTransmitted: tx is not in progress,return", ModuleLogLevel);
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
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " sent (proprietary segment) " + Convert.ToString(Ret) + " " + Convert.ToString(Id), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentTransmitted", ModuleLogLevel);
            LeaveProprietarySegmentTxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void OnNonProprietarySegmentTransmitted(object sender, SocketAsyncEventArgs e)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnNonProprietarySegmentTransmitted", ModuleLogLevel);
            EnterNonProprietarySegmentTxCriticalArea();
            try
            {
                int sent = 0;
                LogUtility.LogUtility.LogFile("entered",ModuleLogLevel);
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentTransmitted: tx is not in progress,return", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                sent = e.BytesTransferred;
                
                TransmittedServer += (uint)sent;
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " " + Convert.ToString(sent) + " sent to destination overall " + Convert.ToString(TransmittedServer), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                OnDestinationTransmitted(sent);
                NonProprietarySegmentTxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentTransmitted", ModuleLogLevel);
            LeaveNonProprietarySegmentTxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        void _ProprietarySegmentTransmit(byte[] buff2transmit)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering _ProprietarySegmentTransmit", ModuleLogLevel);
            try
            {
                if (clientSideSocket.SendBufferSize < buff2transmit.Length)
                {
                    LogUtility.LogUtility.LogFile("increasing tx buffer size", ModuleLogLevel);
                    clientSideSocket.SendBufferSize = buff2transmit.Length;
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " send (proprietary segment) " + Convert.ToString(buff2transmit.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                
                m_OnProprietaryTransmittedCbk.SetBuffer(buff2transmit, 0, buff2transmit.Length);
                if (!clientSideSocket.SendAsync(m_OnProprietaryTransmittedCbk))
                {
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _ProprietarySegmentTransmit", ModuleLogLevel);
        }

        protected void ProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmit4Tx", ModuleLogLevel);
            if (ErrorSent)
            {
                LogUtility.LogUtility.LogFile("discard, ErrorSent flag is up, leaving ProprietarySegmentSubmit4Tx", ModuleLogLevel);
                return;
            }
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx stream client queue", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                SubmitStream4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmit4Tx", ModuleLogLevel);
        }
        protected void ProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
            if (ErrorSent)
            {
                LogUtility.LogUtility.LogFile("discard, ErrorSent flag is up, leaving ProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
                return;
            }
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx msg to client queue", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                SubmitMsg4ClientTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
        }
        protected override void ProprietarySegmentTransmit()
        {
            byte[] buff2transmit;
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProprietarySegmentTransmit", ModuleLogLevel);
            EnterProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (ProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile("ProprietarySegmentTransmit: tx is in progress,return", ModuleLogLevel);
                    LeaveProprietarySegmentTxCriticalArea();
                    return;
                }
                MemoryStream stream = new MemoryStream();
                while (true)
                {
                    if (IsClientTxQueueEmpty())
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " queue is empty, exiting the loop", ModuleLogLevel);
                        break;
                    }
                    if (!txStateMachine.IsBusy())
                    {
                        uint length;
                        bool isMsg;
                        byte[] buf2tx = GetClient2Transmit(out length, out isMsg);
                        if (buf2tx != null)
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " tx to client  msg len " + Convert.ToString(length) + " isMsg " + Convert.ToString(isMsg), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
                            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " queue is empty, exiting the loop", ModuleLogLevel);
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
                        LogUtility.LogUtility.LogFile("cannot get more bytes to tx, exiting the loop", ModuleLogLevel);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentTransmit", ModuleLogLevel);
            LeaveProprietarySegmentTxCriticalArea();
        }
        public override bool IsClientTxQueueEmpty()
        {
            return ((!txStateMachine.IsBusy()) && base.IsClientTxQueueEmpty());
        }
        public abstract void ProcessDownStreamData(byte []data);
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
                    LogUtility.LogUtility.LogFile("OnNonProprietarySegmentReceived: rx is not in progress,return", ModuleLogLevel);
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
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Received (non-proprietary segment) " + Convert.ToString(Received) + " overall " + Convert.ToString(ReceivedServer), LogUtility.LogLevels.LEVEL_LOG_HIGH);

                byte[] buff = new byte[Received];
                CopyBytes(NonProprietarySegmentRxBuf, buff, Received);
                ProcessDownStreamData(buff);
                NonProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnNonProprietarySegmentReceived", ModuleLogLevel);
end_server_rx:
            LeaveNonProprietarySegmentRxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
        }
        protected override void NonProprietarySegmentReceive()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentReceive", ModuleLogLevel);
            EnterNonProprietarySegmentRxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if ((destinationSideSocket == null) || (!destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentReceive (socket is not connected)", ModuleLogLevel);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                
                if (NonProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("NonProprietarySegmentReceive: rx is in progess,return", ModuleLogLevel);
                    LeaveNonProprietarySegmentRxCriticalArea();
                    return;
                }
                NonProprietarySegmentRxInProgress = true;
                m_OnNonProprietaryReceivedCbk.SetBuffer(NonProprietarySegmentRxBuf, 0, NonProprietarySegmentRxBuf.Length);
                destinationSideSocket.ReceiveAsync(m_OnNonProprietaryReceivedCbk);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentReceive", ModuleLogLevel);
            LeaveNonProprietarySegmentRxCriticalArea();
            CheckConnectionAndShutDownIfGone();
        }
        void _NonProprietarySegmentTransmit(byte []data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering _NonProprietarySegmentTransmit", ModuleLogLevel);
            try
            {
                m_OnNonProprietaryTransmittedCbk.SetBuffer(data, 0, data.Length);
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Sending to destination " + Convert.ToString(data.Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                if (!destinationSideSocket.SendAsync(m_OnNonProprietaryTransmittedCbk))
                {
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving _NonProprietarySegmentTransmit", ModuleLogLevel);
        }
        protected void NonProprietarySegmentSubmitStream4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentSubmit4Tx", ModuleLogLevel);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx client queue", ModuleLogLevel);
                SubmitStream4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentSubmit4Tx", ModuleLogLevel);
        }
        protected void NonProprietarySegmentSubmitMsg4Tx(byte[] data)
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
            if (data != null)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Subm2Tx client queue", ModuleLogLevel);
                SubmitMsg4DestinationTx(data);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentSubmitMsg4Tx", ModuleLogLevel);
        }
        protected override void NonProprietarySegmentTransmit()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering NonProprietarySegmentTransmit", ModuleLogLevel);
            EnterNonProprietarySegmentTxCriticalArea();
            try
            {
                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if ((destinationSideSocket == null) || (!destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit (socket is not connected)", ModuleLogLevel);
                    LeaveNonProprietarySegmentTxCriticalArea();
                    return;
                }
                if (!NonProprietarySegmentTxInProgress)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " is not in tx", ModuleLogLevel);
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
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " DestinationTx is busy and queueElement is null", ModuleLogLevel);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving NonProprietarySegmentTransmit", ModuleLogLevel);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietaryMsgReceived", ModuleLogLevel);
            try
            {
                LogUtility.LogUtility.LogFile("Received msg type " + Convert.ToString(rxStateMachine.GetKind()), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering OnProprietarySegmentReceived", ModuleLogLevel);
            EnterProprietarySegmentRxCriticalArea();
            try
            {
                int Received = 0;

                LogUtility.LogUtility.LogFile("entered", ModuleLogLevel);
                if (!ProprietarySegmentRxInProgress)
                {
                    LogUtility.LogUtility.LogFile("OnProprietarySegmentReceived: rx is not in progress,return", ModuleLogLevel);
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                Received = e.BytesTransferred;
                if (Received <= 0)
                {
                    LeaveProprietarySegmentRxCriticalArea();
                    return;
                }
                LogUtility.LogUtility.LogFile("Received (proprietary segment) " + Convert.ToString(Received), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                ReceivedClient += (uint)Received;
                rxStateMachine.OnRxComplete(e.Buffer, Received);
                ProprietarySegmentRxInProgress = false;
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return;
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentReceived", ModuleLogLevel);
            LeaveProprietarySegmentRxCriticalArea();
            ReStartAllOperations(!m_OnceConnected);
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
                ProprietarySegmentRxInProgress = true;
                m_OnProprietaryReceivedCbk.SetBuffer(ProprietarySementRxBuf, 0, ProprietarySementRxBuf.Length);
                clientSideSocket.ReceiveAsync(m_OnProprietaryReceivedCbk);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + "EXCEPTION " + exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProprietarySegmentReceive", ModuleLogLevel);
            LeaveProprietarySegmentRxCriticalArea();
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
