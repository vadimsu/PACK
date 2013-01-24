using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ProxyLib;
using ProxyLibTypes;
using RxTxStateMachine;
using SenderPackLib;
using ChunkChainDataTypes;

namespace ProxyLib
{
    public class PackHttpServerSide : HttpServerSideProxy
    {
        SenderPackLib.SenderPackLib m_senderPackLib;
        OnMessageReadyToTx m_onMessageReadyToTx;
        public PackHttpServerSide(Socket sock)
            : base(sock)
        {
            m_onMessageReadyToTx = new OnMessageReadyToTx(OnMsgRead4Tx);
            m_senderPackLib = new SenderPackLib.SenderPackLib(m_Id,m_onMessageReadyToTx);
        }
        protected override uint GetSaved()
        {
            if (m_senderPackLib != null)
            {
                return m_senderPackLib.GetTotalSavedData();
            }
            return base.GetSaved();
        }
        void OnMsgRead4Tx(object param, byte[] msg,bool submit2Head)
        {
            LogUtility.LogUtility.LogFile("Entering OnMsgReady4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            ProprietarySegmentSubmitMsg4Tx(msg,submit2Head);
            //ProprietarySegmentTransmit();
            LogUtility.LogUtility.LogFile("Leaving OnMsgReady4Tx", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
        }
        public override void OnDownStreamTransmissionOpportunity()
        {
        }
        void Flush()
        {
        }
        public override void ProcessUpStreamMsgKind()
        {
            LogUtility.LogUtility.LogFile("Entering ProcessUpStreamMsgKind", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                EnterProprietaryLibCriticalArea();
                try
                {
                    m_senderPackLib.OnDataByteMode(m_rxStateMachine.GetMsgBody(), 0);
                    LeaveProprietaryLibCriticalArea();
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    LeaveProprietaryLibCriticalArea();
                }
                //ProprietarySegmentSubmitStream4Tx(data);
                //ProprietarySegmentTransmit();
                LogUtility.LogUtility.LogFile("Leaving ProcessUpStreamMsgKind", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        protected override void OnBeginShutdown()
        {
            LogUtility.LogUtility.LogFile("Entering OnBeginShutdown", ModuleLogLevel);
            Flush();
            LogUtility.LogUtility.LogFile("Leaving OnBeginShutdown", ModuleLogLevel);
        }
        public override bool ProcessDownStreamData(byte[] data, bool isInvokedOnTransmit)
        {
            bool ret = false;
            LogUtility.LogUtility.LogFile("Entering ProcessDownStreamData", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            try
            {
                EnterProprietaryLibCriticalArea();
                ret = m_senderPackLib.AddData(data, isInvokedOnTransmit);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile("Leaving ProcessDownStreamData", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
            LeaveProprietaryLibCriticalArea();
            return ret;
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
                m_rxStateMachine.GetDebugInfo() + " " + m_txStateMachine.GetDebugInfo() + m_senderPackLib.GetDebugInfo();
            if (m_destinationSideSocket == null)
            {
                debufInfo += " destination socket is null ";
            }
            else
            {
                debufInfo += " destination socket Connected " + Convert.ToString(m_destinationSideSocket.Connected);
            }
            return debufInfo;
        }
        public override object GetResults()
        {
            object[] res = new object[5];
            res[0] = GetRequestPath();
            res[1] = m_senderPackLib.GetTotalAdded();
            res[2] = m_senderPackLib.GetTotalSent();
            res[3] = m_senderPackLib.GetTotalSavedData();    
            res[4] = GenerateDebugInfo();
            return res;
        }
#if false
        protected override uint GetPreSaved()
        {
            return m_senderPackLib.GetTotalPreSavedData();
        }
        protected override uint GetPostSaved()
        {
            return m_senderPackLib.GetTotalPostSavedData();
        }
#endif
        protected override void Disposing()
        {
            if (m_onGotResults != null)
            {
                object[] res = new object[5];

                res[0] = GetRequestPath();
                res[1] = m_senderPackLib.GetTotalAdded();
                res[2] = m_senderPackLib.GetTotalSent();
                res[3] = m_senderPackLib.GetTotalSavedData();
                res[4] = GenerateDebugInfo();
                m_onGotResults(res);
            }
            LogUtility.LogUtility.LogFile(GetRequestPath(),LogUtility.LogLevels.LEVEL_LOG_HIGH);
            //LogUtility.LogUtility.LogFile(GetRequestPath() +  " Total added  " + Convert.ToString(senderPackLib.GetTotalAdded()) +
            //"Total sent " + Convert.ToString(senderPackLib.GetTotalSent()) + 
            //"Total saved " + Convert.ToString(senderPackLib.GetTotalSavedData()),LogUtility.LogLevels.LEVEL_LOG_HIGH3);
        }
    }
}
