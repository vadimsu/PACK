using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProxyLibTypes
{
    public enum PackEnvelopeKinds
    {
        PACK_ENVELOPE_DUMMY_KIND,
        PACK_ENVELOPE_UPSTREAM_MSG_KIND,
        PACK_ENVELOPE_UPSTREAM_DATA_KIND,
        PACK_ENVELOPE_DOWNSTREAM_MSG_KIND,
        PACK_ENVELOPE_DOWNSTREAM_DATA_KIND
    };

    public class QueueElement
    {
        public byte[] MsgBody;
        public byte MsgType;
    };

    public enum ServerSideProxyTypes
    {
        SERVER_SIDE_PROXY_RAW,
        SERVER_SIDE_PROXY_HTTP,
        SERVER_SIDE_PROXY_PACK_HTTP,
        SERVER_SIDE_PROXY_PACK_RAW
    };

    public enum ClientSideProxyTypes
    {
        CLIENT_SIDE_PROXY_GENERAL,
        CLIENT_SIDE_PROXY_PACK
    };
}
