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
        AsyncCallback m_OnAccept;

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
            m_OnAccept = new AsyncCallback(OnAccept);
        }
        public void SetRemoteEndpoint(IPEndPoint ipEndPoint)
        {
            remoteEndpoint = ipEndPoint;
        }
        public virtual void SetOnGotResults(OnGotResultsCbk cbk)
        {
            onGotResults = cbk;
        }
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
                Monitor.Enter(listLock);
                clientsList.Add(serverSideProxy);
                Monitor.Exit(listLock);
                serverSideProxy.SetRemoteEndpoint(remoteEndpoint);
                serverSideProxy.Start();
            }
            socket.BeginAccept(m_OnAccept, null);
        }
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
            socket.Listen(1000);
            socket.BeginAccept(m_OnAccept, null);
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
                proxy.Dispose2(false);
            }
            clientsList.Clear();
            clientsList = null;
            //Monitor.Exit(listLock);
        }
    }
}
