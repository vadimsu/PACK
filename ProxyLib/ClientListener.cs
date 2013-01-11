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
        IPEndPoint serverSideEndPoint;
        byte ProxyType;
#if true
        AsyncCallback m_OnAccept;
#else
        SocketAsyncEventArgs m_AcceptArgs;
#endif
        public ClientListener(IPEndPoint myEndPoint, IPEndPoint remoteEndPoint,byte proxyType)
            : base(myEndPoint,0)
        {
            serverSideEndPoint = remoteEndPoint;
            ProxyType = proxyType;
#if true
            m_OnAccept = new AsyncCallback(OnAccept);
#else
            m_AcceptArgs = new SocketAsyncEventArgs();
            m_AcceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(m_AcceptArgs_Completed);
#endif
        }
#if true
        public override void OnAccept(IAsyncResult ar)
        {
            Socket newClientSocket = socket.EndAccept(ar);
            ClientSideProxy clientSideProxy = null;
            switch (ProxyType)
            {
                case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_PACK:
                    clientSideProxy = new PackClientSide(newClientSocket, serverSideEndPoint);
                    break;
                case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_GENERAL:
                    clientSideProxy = new ClientSideProxy(newClientSocket, serverSideEndPoint);
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
            IAsyncResult ar2 = socket.BeginAccept(m_OnAccept, null);
            if (ar2.CompletedSynchronously)
            {
                OnAccept(ar2);
            }
        }
#else
        protected override void m_AcceptArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket newClientSocket = m_AcceptArgs.AcceptSocket;
            ClientSideProxy clientSideProxy = null;
            switch (ProxyType)
            {
                case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_PACK:
                    clientSideProxy = new PackClientSide(newClientSocket, serverSideEndPoint);
                    break;
                case (byte)ProxyLibTypes.ClientSideProxyTypes.CLIENT_SIDE_PROXY_GENERAL:
                    clientSideProxy = new ClientSideProxy(newClientSocket, serverSideEndPoint);
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
            socket.AcceptAsync(m_AcceptArgs);
        }
#endif
    }
}
