using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProxyLib
{
    public class Listener
    {
        protected Socket m_socket;
        protected ArrayList m_clientsList;
        byte m_ProxyType;
        IPEndPoint m_remoteEndpoint;
        public delegate void OnGotResultsCbk(object o);
        OnGotResultsCbk m_onGotResults;
        protected object m_listLock;
#if false
        AsyncCallback m_OnAccept;
#else
        SocketAsyncEventArgs m_AcceptArgs;
#endif

        public static void InitGlobalObjects()
        {
            Proxy.InitGlobalObjects();
        }

        public Listener(IPEndPoint ipEndPoint,byte proxyType)
        {
            m_ProxyType = proxyType;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.ExclusiveAddressUse = true;
            m_socket.Bind(ipEndPoint);
            m_clientsList = new ArrayList(100);
            m_remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
            m_onGotResults = null;
            m_listLock = new object();
#if false
            m_OnAccept = new AsyncCallback(OnAccept);
#else
            m_AcceptArgs = new SocketAsyncEventArgs();
            m_AcceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_AcceptArgs_Completed);
#endif
        }

        public void SetRemoteEndpoint(IPEndPoint ipEndPoint)
        {
            m_remoteEndpoint = ipEndPoint;
        }
        public virtual void SetOnGotResults(OnGotResultsCbk cbk)
        {
            m_onGotResults = cbk;
        }
#if false
        public virtual void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket newClientSocket = m_socket.EndAccept(ar);
                Proxy serverSideProxy = null;
                switch (m_ProxyType)
                {
                    case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_RAW:
                        serverSideProxy = new RawServerSideProxy(newClientSocket);
                        break;
                    case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_HTTP:
                        serverSideProxy = new HttpServerSideProxy(newClientSocket);
                        break;
                    case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_PACK_HTTP:
                        serverSideProxy = new PackHttpServerSide(newClientSocket);
                        break;
                    case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_PACK_RAW:
                        serverSideProxy = new PackRawServerSide(newClientSocket);
                        break;
                }
                if (serverSideProxy != null)
                {
                    Proxy.OnGotResults cbk = new Proxy.OnGotResults(OnGotResults);
                    serverSideProxy.SetOnGotResults(cbk);
                    Proxy.OnDisposed ondisp = new Proxy.OnDisposed(OnDisposed);
                    serverSideProxy.SetOnDisposed(ondisp);
                    /*Monitor.Enter(listLock);
                    clientsList.Add(serverSideProxy);
                    Monitor.Exit(listLock);*/
                    serverSideProxy.SetRemoteEndpoint(m_remoteEndpoint);
                    serverSideProxy.Start();
                }
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
        protected virtual void m_AcceptArgs_Completed(object sender, SocketAsyncEventArgs e)
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
                    Proxy serverSideProxy = null;
                    Socket newClientSocket = e.AcceptSocket;
                    LogUtility.LogUtility.LogFile("Accepted " + Convert.ToString(e.BytesTransferred) + " " + Convert.ToString(newClientSocket != null), LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    switch (m_ProxyType)
                    {
                        case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_RAW:
                            serverSideProxy = new RawServerSideProxy(newClientSocket);
                            break;
                        case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_HTTP:
                            serverSideProxy = new HttpServerSideProxy(newClientSocket);
                            break;
                        case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_PACK_HTTP:
                            serverSideProxy = new PackHttpServerSide(newClientSocket);
                            break;
                        case (byte)ProxyLibTypes.ServerSideProxyTypes.SERVER_SIDE_PROXY_PACK_RAW:
                            serverSideProxy = new PackRawServerSide(newClientSocket);
                            break;
                    }
                    if (serverSideProxy != null)
                    {
                        Proxy.OnGotResults cbk = new Proxy.OnGotResults(OnGotResults);
                        serverSideProxy.SetOnGotResults(cbk);
                        Proxy.OnDisposed ondisp = new Proxy.OnDisposed(OnDisposed);
                        serverSideProxy.SetOnDisposed(ondisp);
                        /*Monitor.Enter(listLock);
                        clientsList.Add(serverSideProxy);
                        Monitor.Exit(listLock);*/
                        serverSideProxy.SetRemoteEndpoint(m_remoteEndpoint);
                        serverSideProxy.Start();
                    }
                }
                catch (Exception exc)
                {
                    LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                }
                finally
                {
                    e.AcceptSocket = null; // to enable reuse
                }
            } while (!listenSocket.AcceptAsync(e));
        }
#endif
        public virtual ArrayList GetResults()
        {
            ArrayList resList = new ArrayList();
            Monitor.Enter(m_listLock);
            foreach (Proxy p in m_clientsList)
            {
                resList.Add(p.GetResults());
            }
            Monitor.Exit(m_listLock);
            return resList;
        }
        public void OnGotResults(object res)
        {
            if (m_onGotResults != null)
            {
                m_onGotResults(res);
            }
        }
        public virtual void OnDisposed(Proxy client)
        {
            try
            {
                Monitor.Enter(m_listLock);
                m_clientsList.Remove(client);
                Monitor.Exit(m_listLock);
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
        }
        public virtual void Start()
        {
            m_socket.Listen(100);
#if false
            try
            {
                IAsyncResult ar = m_socket.BeginAccept(m_OnAccept, null);
                if (ar.CompletedSynchronously)
                {
                    LogUtility.LogUtility.LogFile(" completed synchronously ", LogUtility.LogLevels.LEVEL_LOG_HIGH);
                    OnAccept(ar);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
#else
            try
            {
                if (!m_socket.AcceptAsync(m_AcceptArgs))
                {
                    m_AcceptArgs_Completed(null, m_AcceptArgs);
                }
            }
            catch (Exception exc)
            {
                LogUtility.LogUtility.LogFile("EXCEPTION: " + exc.Message + " " + exc.StackTrace, LogUtility.LogLevels.LEVEL_LOG_HIGH);
            }
#endif
        }

        public void Stop()
        {
            try
            {
                m_socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception exc)
            {
            }
            try
            {
                m_socket.Close();
            }
            catch (Exception exc)
            {
            }
            Monitor.Enter(m_listLock);
            foreach (Proxy proxy in m_clientsList)
            {
                proxy.Dispose();
            }
            m_clientsList.Clear();
            m_clientsList = null;
            //Monitor.Exit(listLock);
        }
    }
}
