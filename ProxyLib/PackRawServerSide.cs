using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProxyLibTypes;
using RxTxStateMachine;
using SenderPackLib;
using Stream2Message;
using System.Net;
using System.Net.Sockets;
using ChunkChainDataTypes;

namespace ProxyLib
{
    public class PackRawServerSide : RawServerSideProxy
    {
        SenderPackLib.SenderPackLib senderPackLib;
        OnMessageReadyToTx onMessageReadyToTx;

        public PackRawServerSide(Socket sock)
            : base(sock)
        {
            onMessageReadyToTx = new OnMessageReadyToTx(OnMsgRead4Tx);
            Id = sock.RemoteEndPoint;
            senderPackLib = new SenderPackLib.SenderPackLib(Id, onMessageReadyToTx);
        }
        void OnMsgRead4Tx(object param, byte[] msg)
        {
            LogUtility.LogUtility.LogFile("Entering OnMsgReady4Tx", ModuleLogLevel);
            ProprietarySegmentSubmitMsg4Tx(msg);
            //ProprietarySegmentTransmit();
            LogUtility.LogUtility.LogFile("Leaving OnMsgReady4Tx", ModuleLogLevel);
        }
        public override void ProcessUpStreamMsgKind()
        {
            LogUtility.LogUtility.LogFile("Entering ProcessUpStreamMsgKind", ModuleLogLevel);
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
        public override void OnDownStreamTransmissionOpportunity()
        {
        }
        void Flush()
        {
        }
        
        protected override void OnBeginShutdown()
        {
            LogUtility.LogUtility.LogFile("Entering OnBeginShutdown", ModuleLogLevel);
            Flush();
            LogUtility.LogUtility.LogFile("Leaving OnBeginShutdown", ModuleLogLevel);
        }
        public override void ProcessDownStreamData(byte[] data)
        {
            byte[] buff = null;
            LogUtility.LogUtility.LogFile("Entering ProcessDownStreamData", ModuleLogLevel);
            EnterProprietaryLibCriticalArea();
            try
            {
                senderPackLib.AddData(data);
                
                LeaveProprietaryLibCriticalArea();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                LeaveProprietaryLibCriticalArea();
            }
            LogUtility.LogUtility.LogFile("Leaving ProcessDownStreamData", ModuleLogLevel);
        }
    }
}
