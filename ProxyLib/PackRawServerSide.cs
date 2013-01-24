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
            m_Id = sock.RemoteEndPoint;
            senderPackLib = new SenderPackLib.SenderPackLib(m_Id, onMessageReadyToTx);
        }
        void OnMsgRead4Tx(object param, byte[] msg,bool dummy)
        {
            LogUtility.LogUtility.LogFile("Entering OnMsgReady4Tx", ModuleLogLevel);
            ProprietarySegmentSubmitMsg4Tx(msg,dummy);
            //ProprietarySegmentTransmit();
            LogUtility.LogUtility.LogFile("Leaving OnMsgReady4Tx", ModuleLogLevel);
        }
        public override void ProcessUpStreamMsgKind()
        {
            LogUtility.LogUtility.LogFile("Entering ProcessUpStreamMsgKind", ModuleLogLevel);
            EnterProprietaryLibCriticalArea();
            try
            {
                senderPackLib.OnDataByteMode(m_rxStateMachine.GetMsgBody(), 0);
                LeaveProprietaryLibCriticalArea();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
        public override bool ProcessDownStreamData(byte[] data, bool isInvokedOnTransmit)
        {
            byte[] buff = null;
            bool ret = false;
            LogUtility.LogUtility.LogFile("Entering ProcessDownStreamData", ModuleLogLevel);
            EnterProprietaryLibCriticalArea();
            try
            {
                ret = senderPackLib.AddData(data,isInvokedOnTransmit);
                
                LeaveProprietaryLibCriticalArea();
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                LeaveProprietaryLibCriticalArea();
            }
            LogUtility.LogUtility.LogFile("Leaving ProcessDownStreamData", ModuleLogLevel);
            return ret;
        }
    }
}
