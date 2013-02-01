using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using RxTxStateMachine;
using ProxyLibTypes;
//using System.Runtime.Remoting.Contexts;
using ReceiverPackLib;
using ChunkChainDataTypes;

namespace ProxyLib
{
    public class PackClientSide : ClientSideProxy
    {
        ReceiverPackLib.ReceiverPackLib m_receiverPackLib;
        OnData m_onData;
        OnMessageReadyToTx m_onMsgRead4Tx;
        OnEnd m_onEnd;

        public static new void InitGlobalObjects()
        {
            LogUtility.LogUtility.LogFile("PackClientSide:InitGlobalObjects", ModuleLogLevel);
            ReceiverPackLib.ReceiverPackLib.InitGlobalObjects();
        }

        public PackClientSide(Socket clientSocket, IPEndPoint remoteEndPoint)
            : base(clientSocket, remoteEndPoint)
        {
            m_onData = new OnData(OnDataReceived);
            m_onMsgRead4Tx = new OnMessageReadyToTx(OnMsgRead4Tx);
            m_onEnd = new OnEnd(OnTransationEnd);
            m_receiverPackLib = new ReceiverPackLib.ReceiverPackLib(m_Id,m_onData, m_onEnd, null, m_onMsgRead4Tx);
        }
        void OnTransationEnd(object param)
        {
        }
        void OnMsgRead4Tx(object param, byte[] msg,bool dummy)
        {
            LogUtility.LogUtility.LogFile("Entering OnMsgReady4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            ProprietarySegmentSubmitMsg4Tx(msg);
            //ProprietarySegmentTransmit();
            LogUtility.LogUtility.LogFile("Leaving OnMsgReady4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        void OnDataReceived(byte[] data,int offset,int length)
        {
            LogUtility.LogUtility.LogFile("Entering OnDataReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                //LogUtility.LogUtility.LogFile(ASCIIEncoding.ASCII.GetString(data),LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            byte[] buff = new byte[length];
            for (int i = 0; i < length; i++)
            {
                buff[i] = data[offset + i];
            }
            //LogUtility.LogUtility.LogBinary("_total", buff);
            NonProprietarySegmentSubmitStream4Tx(/*data*/buff);
            NonProprietarySegmentTransmit();
            LogUtility.LogUtility.LogFile("Leaving OnDataReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        protected override uint GetSaved()
        {
            if (m_receiverPackLib != null)
            {
                return m_receiverPackLib.GetTotalDataSaved();
            }
            return base.GetSaved();
        }
        protected override void OnProprietarySegmentMsgReceived()
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
                        //receiverPackLib.OnDataByteMode(rxStateMachine.GetMsgBody(), 0);
                        //rxStateMachine.ClearMsgBody();
                        LogUtility.LogUtility.LogFile("SHOULD NOT OCCUR!!!!!!!!!!!!", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        break;
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_MSG_KIND:
                        EnterProprietaryLibCriticalArea();
                        try
                        {
                            m_receiverPackLib.OnDataByteMode(m_rxStateMachine.GetMsgBody(), 0);
                            LeaveProprietaryLibCriticalArea();
                        }
                        catch (Exception exc)
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                            LeaveProprietaryLibCriticalArea();
                        }
                        m_rxStateMachine.ClearMsgBody();
                        break;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                throw new Exception("Exception in OnProprietarySegmentMsgReceived", exc);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving OnProprietarySegmentMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        string GenerateDebugInfo()
        {
            bool NonProrietaryTxMutexAvailable = NonProprietaryTxMutexAvailable();
            bool ProrietaryTxMutexAvailable = ProprietaryTxMutexAvailable();
            bool NonProrietaryRxMutexAvailable = NonProprietaryRxMutexAvailable();
            bool ProrietaryRxMutexAvailable = NonProprietaryRxMutexAvailable();
            bool clientMutexAvailable = ClientMutexAvailable();
            bool destinationMutexAvailable = DestinationMutexAvailable();
            string debufInfo = Convert.ToString(m_Id) + " Received client " + Convert.ToString(m_ReceivedClient) + " Transmitted client " + Convert.ToString(m_TransmittedClient) + " m_NonProprietarySegmentRxInProgress " + Convert.ToString(m_NonProprietarySegmentRxInProgress) +
                " m_NonProprietarySegmentTxInProgress " + Convert.ToString(m_NonProprietarySegmentTxInProgress) +
                " m_ProprietarySegmentRxInProgress " + Convert.ToString(m_ProprietarySegmentRxInProgress) +
                " m_ProprietarySegmentTxInProgress " + Convert.ToString(m_ProprietarySegmentTxInProgress) +
                " NonProrietaryTxMutexAvailable " + Convert.ToString(NonProrietaryTxMutexAvailable) +
                " ProrietaryTxMutexAvailable " + Convert.ToString(ProrietaryTxMutexAvailable) +
                " NonProrietaryRxMutexAvailable " + Convert.ToString(NonProrietaryRxMutexAvailable) +
                " ProrietaryRxMutexAvailable " + Convert.ToString(ProrietaryRxMutexAvailable) +
                " clientMutexAvailable " + Convert.ToString(clientMutexAvailable) +
                " destinationMutexAvailable " + Convert.ToString(destinationMutexAvailable) +
                m_rxStateMachine.GetDebugInfo() + " " + m_txStateMachine.GetDebugInfo() + m_receiverPackLib.GetDebugInfo();
            return debufInfo;
        }
        public override object GetResults()
        {
            object[] res = new object[3];
            res[0] = m_receiverPackLib.GetTotalData();
            res[1] = m_receiverPackLib.GetTotalDataSaved();          
            res[2] = GenerateDebugInfo();
            return res;
        }
        protected override void Disposing()
        {
            if (m_onGotResults != null)
            {
                object[] res = new object[3];
                res[0] = m_receiverPackLib.GetTotalData();
                res[1] = m_receiverPackLib.GetTotalDataSaved();
                res[2] = GenerateDebugInfo();
                m_onGotResults(res);
            }
            //LogUtility.LogUtility.LogFile(receiverPackLib.GetDebugInfo(), LogUtility.LogLevels.LEVEL_LOG_HIGH2);
            //LogUtility.LogUtility.LogFile("TotalData " + Convert.ToString(receiverPackLib.GetTotalData()) + " TotalDataSaved " + Convert.ToString(receiverPackLib.GetTotalDataSaved()), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
            m_receiverPackLib.OnDispose();
            base.Disposing();
        }

        public static new void Flush()
        {
            ReceiverPackLib.ReceiverPackLib.Flush();
        }
    }
}
