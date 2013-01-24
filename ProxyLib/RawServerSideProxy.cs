using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ProxyLibTypes;

namespace ProxyLib
{
    public class RawServerSideProxy : ServerSideProxy
    {
        public RawServerSideProxy(Socket sock)
            : base(sock)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"),8888);
        }
        IPEndPoint remoteEndPoint;
        public override void SetRemoteEndpoint(IPEndPoint ipEndpoint)
        {
            remoteEndPoint = ipEndpoint;
        }
        public override byte []GetFirstBuffToTransmitDestination()
        {
            try
            {
                IPEndPoint DestinationEndPoint = remoteEndPoint;
                m_destinationSideSocket = new Socket(DestinationEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //destinationSideSocket.LingerState.Enabled = true;
                //destinationSideSocket.LingerState.LingerTime = 10000;
                m_destinationSideSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                LogUtility.LogUtility.LogFile("Trying to connect destination " + Convert.ToString(DestinationEndPoint), ModuleLogLevel);
                m_destinationSideSocket.Connect(DestinationEndPoint);
                LogUtility.LogUtility.LogFile("destination connected ", ModuleLogLevel);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return null;
            }
            return m_rxStateMachine.GetMsgBody();
        }
        public override void ProcessUpStreamDataKind()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Entering ProcessUpStreamDataKind", ModuleLogLevel);
            try
            {
                if ((m_destinationSideSocket != null) && (m_destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile("destination connected, submit " + Convert.ToString(m_rxStateMachine.GetMsgBody().Length), ModuleLogLevel);
                    NonProprietarySegmentSubmitStream4Tx(m_rxStateMachine.GetMsgBody());
                    //NonProprietarySegmentTransmit();
                }
                else if (m_destinationSideSocket == null)
                {
                    LogUtility.LogUtility.LogFile("destination is not connected and no attempt " + Convert.ToString(m_rxStateMachine.GetMsgBody().Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    //ShutDownFlag = false;
                    byte []data = GetFirstBuffToTransmitDestination();
                    try
                    {
                        if (data != null)
                        {
                            NonProprietarySegmentSubmitStream4Tx(data);
                            //NonProprietarySegmentTransmit();
                        }
                    }
                    catch (Exception exc)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(m_Id) + " Leaving ProcessUpStreamDataKind", ModuleLogLevel);
        }
        public override void ProcessUpStreamMsgKind()
        {
        }
        public override bool ProcessDownStreamData(byte[] data, bool isInvokedOnTransmit)
        {
            if (!isInvokedOnTransmit)
            {
                ProprietarySegmentSubmitStream4Tx(data);
                return true;
            }
            return false;
        }
    }
}
