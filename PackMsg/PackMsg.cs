using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ByteArrayScalarTypeConversionLib;

namespace PackMsg
{
    public class PackMsg
    {
        public static uint PackPreamble = 0x5AA57FF7;
        public static byte LastChunkFlag = 0x1;

        public enum MsgKind_e
        {
            PACK_DUMMY_MSG_KIND,
            PACK_PRED_MSG_KIND,
            PACK_PRED_ACK_MSG_KIND,
            PACK_DATA_MSG_KIND,
            PACK_MSG_NUMBER
        };

        public static byte[] AllocateMsgAndBuildHeader(uint DataSize,byte Flags,byte MsgKind,out int offset)
        {
            byte[] msg = new byte[sizeof(uint) + sizeof(byte) + sizeof(byte) + sizeof(uint) + DataSize];
            offset = 0;
            offset += (int)ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(msg, (uint)offset, PackPreamble);
            msg[offset++] = MsgKind;
            msg[offset++] = Flags;
            offset += (int)ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.Uint2ByteArray(msg, (uint)offset, DataSize);
            return msg;
        }
        public static uint GetMsgHeaderSize()
        {
            return (sizeof(uint) + sizeof(byte) + sizeof(byte) + sizeof(uint));
        }
        public static int DecodeMsg(byte []msg,out uint DataSize, out byte Flags,out byte MsgKind, out int offset)
        {
            uint packPreamble;
            offset = 0;
            offset += (int)ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(msg, (uint)offset, out packPreamble);
            if (packPreamble != PackPreamble)
            {
                MsgKind = (byte)MsgKind_e.PACK_DUMMY_MSG_KIND;
                DataSize = 0;
                Flags = 0;
                return 1;
            }
            MsgKind = msg[offset++];
            Flags = msg[offset++];
            switch (MsgKind)
            {
                case (byte)MsgKind_e.PACK_DATA_MSG_KIND:
                case (byte)MsgKind_e.PACK_PRED_ACK_MSG_KIND:
                case (byte)MsgKind_e.PACK_PRED_MSG_KIND:
                    break;
                default:
                    DataSize = 0;
                    return 2;
            }
            offset += (int)ByteArrayScalarTypeConversionLib.ByteArrayScalarTypeConversionLib.ByteArray2Uint(msg, (uint)offset, out DataSize);
            return 0;
        }
    }
}
