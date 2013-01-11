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
        protected Socket socket;
        protected ArrayList clientsList;
        byte ProxyType;
        IPEndPoint remoteEndpoint;
        public delegate void OnGotResultsCbk(object o);
        OnGotResultsCbk onGotResults;
        protected object listLock;
#if true
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
            ProxyType = proxyType;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ExclusiveAddressUse = true;
            socket.Bind(ipEndPoint);
            clientsList = new ArrayList(100);
            remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
            onGotResults = null;
            listLock = new object();
#if true
            m_OnAccept = new AsyncCallback(OnAccept);
#else
            m_AcceptArgs = new SocketAsyncEventArgs();
            m_AcceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_AcceptArgs_Completed);
#endif
        }

        public void SetRemoteEndpoint(IPEndPoint ipEndPoint)
        {
            remoteEndpoint = ipEndPoint;
        }
        public virtual void SetOnGotResults(OnGotResultsCbk cbk)
        {
            onGotResults = cbk;
        }
#if true
        public virtual void OnAccept(IAsyncResult ar)
        {
            Socket newClientSocket = socket.EndAccept(ar);
            Proxy serverSideProxy = null;
            switch (ProxyType)
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
                serverSideProxy.SetRemoteEndpoint(remoteEndpoint);
                serverSideProxy.Start();
            }
            IAsyncResult ar2 = socket.BeginAccept(m_OnAccept, null);
            if (ar2.CompletedSynchronously)
            {
                OnAccept(ar2);
            }
        }
#else
        protected virtual void m_AcceptArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            Proxy serverSideProxy = null;
            Socket newClientSocket = m_AcceptArgs.AcceptSocket;
            switch (ProxyType)
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
                serverSideProxy.SetRemoteEndpoint(remoteEndpoint);
                serverSideProxy.Start();
            }
            socket.AcceptAsync(m_AcceptArgs);
        }
#endif
        public virtual ArrayList GetResults()
        {
            ArrayList resList = new ArrayList();
            Monitor.Enter(listLock);
            foreach (Proxy p in clientsList)
            {
                resList.Add(p.GetResults());
            }
            Monitor.Exit(listLock);
            return resList;
        }
        public void OnGotResults(object res)
        {
            if (onGotResults != null)
            {
                onGotResults(res);
            }
        }
        public virtual void OnDisposed(Proxy client)
        {
            Monitor.Enter(listLock);
            clientsList.Remove(client);
            Monitor.Exit(listLock);
        }
        public virtual void Start()
        {
            socket.Listen(100);
#if true
            IAsyncResult ar = socket.BeginAccept(m_OnAccept, null);
            if (ar.CompletedSynchronously)
            {
                OnAccept(ar);
            }
#else
            socket.AcceptAsync(m_AcceptArgs);
#endif
        }

        public void Stop()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception exc)
            {
            }
            try
            {
                socket.Close();
            }
            catch (Exception exc)
            {
            }
            Monitor.Enter(listLock);
            foreach (Proxy proxy in clientsList)
            {
                proxy.Dispose();
            }
            clientsList.Clear();
            clientsList = null;
            //Monitor.Exit(listLock);
        }
    }
}
