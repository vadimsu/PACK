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
        AsyncCallback m_OnAccept;
        public ClientListener(IPEndPoint myEndPoint, IPEndPoint remoteEndPoint,byte proxyType)
            : base(myEndPoint,0)
        {
            serverSideEndPoint = remoteEndPoint;
            ProxyType = proxyType;
            m_OnAccept = new AsyncCallback(OnAccept);
        }
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
            Monitor.Enter(listLock);
            clientsList.Add(clientSideProxy);
            Monitor.Exit(listLock);
            clientSideProxy.Start();
            socket.BeginAccept(m_OnAccept, null);
        }
    }
}
