using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProxyLib
{
    public class ClientListener : Listener
    {
        IPEndPoint m_serverSideEndPoint;
        byte m_ProxyType;
#if false
        AsyncCallback m_OnAccept;
#else
        SocketAsyncEventArgs m_AcceptArgs;
#endif
        public ClientListener(IPEndPoint myEndPoint, IPEndPoint remoteEndPoint,byte proxyType)
            : base(myEndPoint,0)
        {
            m_serverSideEndPoint = remoteEndPoint;
            m_ProxyType = proxyType;
#if false
            m_OnAccept = new AsyncCallback(OnAccept);
#else
            m_AcceptArgs = new SocketAsyncEventArgs();
            m_AcceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_AcceptArgs_Completed);
#endif
        }
#if false
        public override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket newClientSocket = m_socket.EndAccept(ar);
                ClientSideProxy clientSideProxy = null;
                switch (m_ProxyType)
                {
                    case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_PACK:
                        clientSideProxy = new PackClientSide(newClientSocket, m_serverSideEndPoint);
                        break;
                    case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_GENERAL:
                        clientSideProxy = new ClientSideProxy(newClientSocket, m_serverSideEndPoint);
                        break;
                }
                Proxy.OnGotResults cbk = new Proxy.OnGotResults(OnGotResults);
                clientSideProxy.SetOnGotResults(cbk);
                Proxy.OnDisposed ondisp = new Proxy.OnDisposed(OnDisposed);
                clientSideProxy.SetOnDisposed(ondisp);
                /*Monitor.Enter(listLock);
                clientsList.Add(clientSideProxy);
                Monitor.Exit(listLock);*/
                clientSideProxy.Start();
                IAsyncResult ar2 = m_socket.BeginAccept(m_OnAccept, null);
                if (ar2.CompletedSynchronously)
                {
                    LogUtility.LogUtility.LogFile(" completed synchronously ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    OnAccept(ar2);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
#else
        protected override void m_AcceptArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket listenSocket = (Socket)sender;
            do
            {
                try
                {
                    if (e.SocketError != SocketError.Success)
                    {
                        LogUtility.LogUtility.LogFile("Error in Accept " + Convert.ToString(e.SocketError), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                        continue;
                    }
                    Socket newClientSocket = e.AcceptSocket;
                    LogUtility.LogUtility.LogFile("Accepted " + Convert.ToString(e.BytesTransferred) + " " + Convert.ToString(newClientSocket != null), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    ClientSideProxy clientSideProxy = null;
                    switch (m_ProxyType)
                    {
                        case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_PACK:
                            clientSideProxy = new PackClientSide(newClientSocket, m_serverSideEndPoint);
                            break;
                        case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_GENERAL:
                            clientSideProxy = new ClientSideProxy(newClientSocket, m_serverSideEndPoint);
                            break;
                    }
                    Proxy.OnGotResults cbk = new Proxy.OnGotResults(OnGotResults);
                    clientSideProxy.SetOnGotResults(cbk);
                    Proxy.OnDisposed ondisp = new Proxy.OnDisposed(OnDisposed);
                    clientSideProxy.SetOnDisposed(ondisp);
                    /*Monitor.Enter(listLock);
                    clientsList.Add(clientSideProxy);
                    Monitor.Exit(listLock);*/
                    clientSideProxy.Start();
                }
                catch (Exception exc)
                {
                     LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " +  exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                finally
                {
                    e.AcceptSocket = null; // to enable reuse
                }
            } while (!listenSocket.AcceptAsync(e));
        }
#endif
    }
}
