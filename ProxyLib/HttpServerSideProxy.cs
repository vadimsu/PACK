using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ProxyLibTypes;

namespace ProxyLib
{
    public class HttpServerSideProxy : ServerSideProxy
    {
        // private variables
        /// <summary>Holds the value of the HttpQuery property.</summary>
        private string m_HttpQuery;
        /// <summary>Holds the value of the RequestedPath property.</summary>
        private string m_RequestedPath;
        /// <summary>Holds the value of the HeaderFields property.</summary>
        private StringDictionary m_HeaderFields;
        /// <summary>Holds the value of the HttpVersion property.</summary>
        private string m_HttpVersion;
        /// <summary>Holds the value of the HttpRequestType property.</summary>
        private string m_HttpRequestType;
        /// <summary>Holds the POST data</summary>
        private string m_HttpPost;
        public HttpServerSideProxy(Socket sock)
            : base(sock)
        {
            Id = sock.RemoteEndPoint;
            m_RequestedPath = null;
            HttpReqCleanUp();
        }
        void HttpReqCleanUp()
        {
            m_HttpQuery = "";
            m_HeaderFields = null;
            m_HttpVersion = "";
            m_HttpRequestType = "";
            m_HttpPost = null;
        }
        public string GetRequestPath()
        {
            if (m_RequestedPath == null)
            {
                return "PATH IS EMPTY, QUERY: " + m_HttpQuery;
            }
            return m_RequestedPath;
        }
        public override byte []GetFirstBuffToTransmitDestination()
        {
            byte[] buff = rxStateMachine.GetMsgBody();
            m_HttpQuery += Encoding.ASCII.GetString(buff, 0, buff.Length);
            if (IsValidQuery())
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Valid query:  " + m_HttpQuery, LogUtility.LogLevels.LEVEL_LOG_HIGH2);
                ProcessQuery();
            }
            return null;
        }
        public override void ProcessUpStreamDataKind()
        {
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Entering ProcessUpStreamDataKind", ModuleLogLevel);
            try
            {
                if ((destinationSideSocket != null) && (destinationSideSocket.Connected))
                {
                    LogUtility.LogUtility.LogFile("destination connected, submit " + Convert.ToString(rxStateMachine.GetMsgBody().Length), ModuleLogLevel);
                    NonProprietarySegmentSubmitStream4Tx(rxStateMachine.GetMsgBody());
                    //NonProprietarySegmentTransmit();
                }
                else if (destinationSideSocket == null)
                {
                    LogUtility.LogUtility.LogFile("destination is not connected and no attempt " + Convert.ToString(rxStateMachine.GetMsgBody().Length), LogUtility.LogLevels.LEVEL_LOG_HIGH);
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
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
            LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Leaving ProcessUpStreamDataKind", ModuleLogLevel);
        }
        public override void ProcessUpStreamMsgKind()
        {
        }
        public override void ProcessDownStreamData(byte []data)
        {
            ProprietarySegmentSubmitStream4Tx(data);
            //ProprietarySegmentTransmit();
        }
        void SendBadRequest()
        {
            try
            {
#if false
                QueueElement queueElement = new QueueElement();
                queueElement.MsgType = (byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_MSG;
                queueElement.MsgBody = Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>");
                LogUtility.LogUtility.LogFile("Submit BAD request to tx to client");
                ClientTransmit(queueElement);
#else
                ProprietarySegmentSubmitStream4Tx(Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>"));
                //ProprietarySegmentTransmit();
#endif
                ErrorSent = true;
            }
            catch(Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        bool IsValidQuery()
        {
            int index = m_HttpQuery.IndexOf("\r\n\r\n");
            if (index == -1)
                return false;
            m_HeaderFields = ParseQuery();
            if (m_HttpRequestType.ToUpper().Equals("POST"))
            {
                try
                {
                    int length = int.Parse((string)m_HeaderFields["Content-Length"]);
                    return m_HttpQuery.Length >= index + 6 + length;
                }
                catch(Exception exc)
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    SendBadRequest();
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        string RebuildQuery()
        {
            string ret = m_HttpRequestType + " " + m_RequestedPath + " " + m_HttpVersion + "\r\n";
            if (m_HeaderFields != null)
            {
                foreach (string sc in m_HeaderFields.Keys)
                {
                    if (sc.Length < 6 || !sc.Substring(0, 6).Equals("proxy-"))
                        ret += System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sc) + ": " + (string)m_HeaderFields[sc] + "\r\n";
                }
                ret += "\r\n";
                if (m_HttpPost != null)
                    ret += m_HttpPost;
            }
            return ret;
        }
        void ProcessQuery()
        {
            m_HeaderFields = ParseQuery();
            if (m_HeaderFields == null || !m_HeaderFields.ContainsKey("Host"))
            {
                SendBadRequest();
                return;
            }
            int Port;
            string Host;
            int Ret;
            if (m_HttpRequestType.ToUpper().Equals("CONNECT"))
            { //HTTPS
                Ret = m_RequestedPath.IndexOf(":");
                if (Ret >= 0)
                {
                    Host = m_RequestedPath.Substring(0, Ret);
                    if (m_RequestedPath.Length > Ret + 1)
                        Port = int.Parse(m_RequestedPath.Substring(Ret + 1));
                    else
                        Port = 443;
                }
                else
                {
                    Host = m_RequestedPath;
                    Port = 443;
                }
            }
            else
            { //Normal HTTP
                Ret = ((string)m_HeaderFields["Host"]).IndexOf(":");
                if (Ret > 0)
                {
                    Host = ((string)m_HeaderFields["Host"]).Substring(0, Ret);
                    Port = int.Parse(((string)m_HeaderFields["Host"]).Substring(Ret + 1));
                }
                else
                {
                    Host = (string)m_HeaderFields["Host"];
                    Port = 80;
                }
                if (m_HttpRequestType.ToUpper().Equals("POST"))
                {
                    int index = m_HttpQuery.IndexOf("\r\n\r\n");
                    m_HttpPost = m_HttpQuery.Substring(index + 4);
                }
            }
            try
            {
                IPAddress destIp;
                IPEndPoint DestinationEndPoint;
                if (!IPAddress.TryParse(Host, out destIp))
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " trying to retrieve IP for " + Host, LogUtility.LogLevels.LEVEL_LOG_HIGH2);
                    DestinationEndPoint = new IPEndPoint(Dns.GetHostEntry(Host).AddressList[0], Port);
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " host name is the IP address " + Convert.ToString(destIp), LogUtility.LogLevels.LEVEL_LOG_HIGH2);
                    DestinationEndPoint = new IPEndPoint(destIp, Port);
                }
                destinationSideSocket = new Socket(DestinationEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //destinationSideSocket.LingerState.Enabled = true;
                //destinationSideSocket.LingerState.LingerTime = 5000;
                if (m_HeaderFields.ContainsKey("Proxy-Connection") && m_HeaderFields["Proxy-Connection"].ToLower().Equals("keep-alive"))
                {
                    destinationSideSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                }
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Trying to connect destination " + Convert.ToString(DestinationEndPoint), LogUtility.LogLevels.LEVEL_LOG_HIGH2);
                destinationSideSocket.Connect(DestinationEndPoint);
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " destination connected ", LogUtility.LogLevels.LEVEL_LOG_HIGH2);
                m_OnceConnected = true;
                QueueElement queueElement = new QueueElement();
                if (m_HttpRequestType.ToUpper().Equals("CONNECT"))
                { //HTTPS
#if false
                    queueElement.MsgBody = Encoding.ASCII.GetBytes(m_HttpVersion + " 200 Connection established\r\nProxy-Agent: Vadim Suraev Proxy Server\r\n\r\n");
                    queueElement.MsgType = (byte)PackEnvelopeKinds.PACK_ENVELOPE_DOWNSTREAM_MSG;
                    LogUtility.LogUtility.LogFile("Submit to tx to client " + Convert.ToString(queueElement.MsgBody.Length));
                    ClientTransmit(queueElement);
#else
                    ProprietarySegmentSubmitStream4Tx(Encoding.ASCII.GetBytes(m_HttpVersion + " 200 Connection established\r\nProxy-Agent: Vadim Suraev Proxy Server\r\n\r\n"));
                    //ProprietarySegmentTransmit();
#endif
                }
                else
                {
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Submit to tx to destination ", ModuleLogLevel);
                    NonProprietarySegmentSubmitStream4Tx(Encoding.ASCII.GetBytes(RebuildQuery()));
                    //NonProprietarySegmentTransmit();
                }
                m_RequestedPath = Host + " " + m_RequestedPath;
                HttpReqCleanUp();
            }
            catch(Exception exc)
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                SendBadRequest();
                return;
            }
        }
        StringDictionary ParseQuery()
        {
            StringDictionary retdict = new StringDictionary();
            string[] Lines = m_HttpQuery.Replace("\r\n", "\n").Split('\n');
            int Cnt, Ret;
            //Extract requested URL
            if (Lines.Length > 0)
            {
                //Parse the Http Request Type
                Ret = Lines[0].IndexOf(' ');
                if (Ret > 0)
                {
                    m_HttpRequestType = Lines[0].Substring(0, Ret);
                    Lines[0] = Lines[0].Substring(Ret).Trim();
                }
                //Parse the Http Version and the Requested Path
                Ret = Lines[0].LastIndexOf(' ');
                if (Ret > 0)
                {
                    m_HttpVersion = Lines[0].Substring(Ret).Trim();
                    m_RequestedPath = Lines[0].Substring(0, Ret);
                }
                else
                {
                    m_RequestedPath = Lines[0];
                }
                //Remove http:// if present
                if (m_RequestedPath.Length >= 7 && m_RequestedPath.Substring(0, 7).ToLower().Equals("http://"))
                {
                    Ret = m_RequestedPath.IndexOf('/', 7);
                    if (Ret == -1)
                        m_RequestedPath = "/";
                    else
                        m_RequestedPath = m_RequestedPath.Substring(Ret);
                }
            }
            for (Cnt = 1; Cnt < Lines.Length; Cnt++)
            {
                Ret = Lines[Cnt].IndexOf(":");
                if (Ret > 0 && Ret < Lines[Cnt].Length - 1)
                {
                    try
                    {
                        retdict.Add(Lines[Cnt].Substring(0, Ret), Lines[Cnt].Substring(Ret + 1).Trim());
                    }
                    catch(Exception exc)
                    {
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " EXCEPTION " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    }
                }
            }
            return retdict;
        }
    }
}
