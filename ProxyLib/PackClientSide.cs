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
        ReceiverPackLib.ReceiverPackLib receiverPackLib;
        OnData onData;
        OnMessageReadyToTx onMsgRead4Tx;
        OnEnd onEnd;

        public static new void InitGlobalObjects()
        {
            LogUtility.LogUtility.LogFile("PackClientSide:InitGlobalObjects", LogUtility.LogLevels.LEVEL_LOG_HIGH);
            ReceiverPackLib.ReceiverPackLib.InitGlobalObjects();
        }

        public PackClientSide(Socket clientSocket, IPEndPoint remoteEndPoint)
            : base(clientSocket, remoteEndPoint)
        {
            onData = new OnData(OnDataReceived);
            onMsgRead4Tx = new OnMessageReadyToTx(OnMsgRead4Tx);
            onEnd = new OnEnd(OnTransationEnd);
            receiverPackLib = new ReceiverPackLib.ReceiverPackLib(Id,onData, onEnd, null, onMsgRead4Tx);
        }
        void OnTransationEnd(object param)
        {
        }
        void OnMsgRead4Tx(object param, byte[] msg)
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
            if (receiverPackLib != null)
            {
                return receiverPackLib.GetTotalDataSaved();
            }
            return base.GetSaved();
        }
        protected override void OnProprietarySegmentMsgReceived()
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
                        //receiverPackLib.OnDataByteMode(rxStateMachine.GetMsgBody(), 0);
                        //rxStateMachine.ClearMsgBody();
                        LogUtility.LogUtility.LogFile("SHOULD NOT OCCUR!!!!!!!!!!!!", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        break;
                    case (byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_MSG_KIND:
                        EnterProprietaryLibCriticalArea();
                        try
                        {
                            receiverPackLib.OnDataByteMode(rxStateMachine.GetMsgBody(), 0);
                            LeaveProprietaryLibCriticalArea();
                        }
                        catch (Exception exc)
                        {
                            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                            LeaveProprietaryLibCriticalArea();
                        }
                        rxStateMachine.ClearMsgBody();
                        break;
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                throw new Exception("Exception in OnProprietarySegmentMsgReceived", exc);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving OnProprietarySegmentMsgReceived", LogUtility.LogLevels.LEVEL_LOG_MEDIUM);
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
                rxStateMachine.GetDebugInfo() + " " + txStateMachine.GetDebugInfo() + receiverPackLib.GetDebugInfo();
            return debufInfo;
        }
        public override object GetResults()
        {
            object[] res = new object[3];
            res[0] = receiverPackLib.GetTotalData();
            res[1] = receiverPackLib.GetTotalDataSaved();          
            res[2] = GenerateDebugInfo();
            return res;
        }
        protected override void Disposing()
        {
            if (onGotResults != null)
            {
                object[] res = new object[3];
                res[0] = receiverPackLib.GetTotalData();
                res[1] = receiverPackLib.GetTotalDataSaved();
                res[2] = GenerateDebugInfo();
                onGotResults(res);
            }
            //LogUtility.LogUtility.LogFile(receiverPackLib.GetDebugInfo(), LogUtility.LogLevels.LEVEL_LOG_HIGH2);
            //LogUtility.LogUtility.LogFile("TotalData " + Convert.ToString(receiverPackLib.GetTotalData()) + " TotalDataSaved " + Convert.ToString(receiverPackLib.GetTotalDataSaved()), LogUtility.LogLevels.LEVEL_LOG_HIGH3);
            receiverPackLib.OnDispose();
            base.Disposing();
        }

        public static new void Flush()
        {
            ReceiverPackLib.ReceiverPackLib.Flush();
        }
    }
}
