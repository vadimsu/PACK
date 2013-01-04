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
        SenderPackLib.SenderPackLib senderPackLib;
        OnMessageReadyToTx onMessageReadyToTx;
        public PackHttpServerSide(Socket sock)
            : base(sock)
        {
            onMessageReadyToTx = new OnMessageReadyToTx(OnMsgRead4Tx);
            senderPackLib = new SenderPackLib.SenderPackLib(Id,onMessageReadyToTx);
        }
        protected override uint GetSaved()
        {
            if (senderPackLib != null)
            {
                return senderPackLib.GetTotalSavedData();
            }
            return base.GetSaved();
        }
        void OnMsgRead4Tx(object param, byte[] msg)
        {
            LogUtility.LogUtility.LogFile("Entering OnMsgReady4Tx", ModuleLogLevel);
            ProprietarySegmentSubmitMsg4Tx(msg);
            //ProprietarySegmentTransmit();
            LogUtility.LogUtility.LogFile("Leaving OnMsgReady4Tx", ModuleLogLevel);
        }
        public override void OnDownStreamTransmissionOpportunity()
        {
        }
        void Flush()
        {
        }
        public override void ProcessUpStreamMsgKind()
        {
            LogUtility.LogUtility.LogFile("Entering ProcessUpStreamMsgKind", ModuleLogLevel);
            try
            {
                EnterProprietaryLibCriticalArea();
                try
                {
                    senderPackLib.OnDataByteMode(rxStateMachine.GetMsgBody(), 0);
                    LeaveProprietaryLibCriticalArea();
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    LeaveProprietaryLibCriticalArea();
                }
                //ProprietarySegmentSubmitStream4Tx(data);
                //ProprietarySegmentTransmit();
                LogUtility.LogUtility.LogFile("Leaving ProcessUpStreamMsgKind", ModuleLogLevel);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        protected override void OnBeginShutdown()
        {
            LogUtility.LogUtility.LogFile("Entering OnBeginShutdown", ModuleLogLevel);
            Flush();
            LogUtility.LogUtility.LogFile("Leaving OnBeginShutdown", ModuleLogLevel);
        }
        public override void ProcessDownStreamData(byte[] data)
        {
            LogUtility.LogUtility.LogFile("Entering ProcessDownStreamData", ModuleLogLevel);
            try
            {
                //LogUtility.LogUtility.LogFile(ASCIIEncoding.ASCII.GetString(data),LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            catch (Exception exc)
            {
                //LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            try
            {
                EnterProprietaryLibCriticalArea();
                try
                {
                    senderPackLib.AddData(data);
                    LeaveProprietaryLibCriticalArea();
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    LeaveProprietaryLibCriticalArea();
                }
                LogUtility.LogUtility.LogFile("Leaving ProcessDownStreamData", /*ModuleLogLevel*/LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        string GenerateDebugInfo()
        {
            bool NonProrietaryTxMutexAvailable = NonProprietaryTxMutexAvailable();
            bool ProrietaryTxMutexAvailable = ProprietaryTxMutexAvailable();
            bool NonProrietaryRxMutexAvailable = NonProprietaryRxMutexAvailable();
            bool ProrietaryRxMutexAvailable = NonProprietaryRxMutexAvailable();
            bool clientMutexAvailable = ClientMutexAvailable();
            bool destinationMutexAvailable = DestinationMutexAvailable();
            string debufInfo = Convert.ToString(Id) + " Received client " + Convert.ToString(ReceivedClient) + " Transmitted client " + Convert.ToString(TransmittedClient) + " NonProprietarySegmentRxInProgress " + Convert.ToString(NonProprietarySegmentRxInProgress) +
                " NonProprietarySegmentTxInProgress " + Convert.ToString(NonProprietarySegmentTxInProgress) +
                " ProprietarySegmentRxInProgress " + Convert.ToString(ProprietarySegmentRxInProgress) +
                " ProprietarySegmentTxInProgress " + Convert.ToString(ProprietarySegmentTxInProgress) +
                " NonProrietaryTxMutexAvailable " + Convert.ToString(NonProrietaryTxMutexAvailable) +
                " ProrietaryTxMutexAvailable " + Convert.ToString(ProrietaryTxMutexAvailable) +
                " NonProrietaryRxMutexAvailable " + Convert.ToString(NonProrietaryRxMutexAvailable) +
                " ProrietaryRxMutexAvailable " + Convert.ToString(ProrietaryRxMutexAvailable) +
                " clientMutexAvailable " + Convert.ToString(clientMutexAvailable) +
                " destinationMutexAvailable " + Convert.ToString(destinationMutexAvailable) +
                rxStateMachine.GetDebugInfo() + " " + txStateMachine.GetDebugInfo() + senderPackLib.GetDebugInfo();
            if (destinationSideSocket == null)
            {
                debufInfo += " destination socket is null ";
            }
            else
            {
                debufInfo += " destination socket Connected " + Convert.ToString(destinationSideSocket.Connected);
            }
            return debufInfo;
        }
        public override object GetResults()
        {
            object[] res = new object[5];
            res[0] = GetRequestPath();
            res[1] = senderPackLib.GetTotalAdded();
            res[2] = senderPackLib.GetTotalSent();
            res[3] = senderPackLib.GetTotalSavedData();    
            res[4] = GenerateDebugInfo();
            return res;
        }
        protected override uint GetPreSaved()
        {
            return senderPackLib.GetTotalPreSavedData();
        }
        protected override uint GetPostSaved()
        {
            return senderPackLib.GetTotalPostSavedData();
        }
        protected override void Disposing()
        {
            if (onGotResults != null)
            {
                object[] res = new object[5];

                res[0] = GetRequestPath();
                res[1] = senderPackLib.GetTotalAdded();
                res[2] = senderPackLib.GetTotalSent();
                res[3] = senderPackLib.GetTotalSavedData();
                res[4] = GenerateDebugInfo();
                onGotResults(res);
            }
            LogUtility.LogUtility.LogFile(GetRequestPath(),LogUtility.LogLevels.LEVEL_LOG_HIGH);
            //LogUtility.LogUtility.LogFile(GetRequestPath() +  " Total added  " + Convert.ToString(senderPackLib.GetTotalAdded()) +
            //"Total sent " + Convert.ToString(senderPackLib.GetTotalSent()) + 
            //"Total saved " + Convert.ToString(senderPackLib.GetTotalSavedData()),LogUtility.LogLevels.LEVEL_LOG_HIGH3);
        }
    }
}
