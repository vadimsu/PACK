using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChunkChainDataTypes;

namespace Stream2Message
{
    public class Stream2Message
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_HIGH;
        enum Receiver_State_e
        {
            RECEIVER_HEADER_STATE,
            RECEIVER_MSG_BODY_STATE
        };

        protected OnMessageReadyToTx onMessageReadyToTx;
        protected object onTxMessageParam;
        byte mState;
        uint mBufferIdx;
        byte[] mBuff;
        uint mExpectedNumberOfBytes;
        byte mMsgKind;
        byte mFlags;
        ChunkChainDataTypes.OnMessageReceived[] msgHandlers;

        public Stream2Message(OnMessageReadyToTx onMsgReadyToTx)
        {
            onMessageReadyToTx = onMsgReadyToTx;
            onTxMessageParam = null;
            msgHandlers = new OnMessageReceived[(int)PackMsg.PackMsg.MsgKind_e.PACK_MSG_NUMBER];
            InitRxStateMachine();
        }

        void InitRxStateMachine()
        {
            mState = (byte)Receiver_State_e.RECEIVER_HEADER_STATE;
            mBufferIdx = 0;
            mBuff = null;
            mExpectedNumberOfBytes = 0;
            mMsgKind = (byte)PackMsg.PackMsg.MsgKind_e.PACK_DUMMY_MSG_KIND;
            mFlags = 0;
        }

        public void SetOnMsgReady4TxParam(object onTxMsgParam)
        {
            onTxMessageParam = onTxMsgParam;
        }

        public void SetCallback(int MsgType, OnMessageReceived onMsgReceived)
        {
            if (MsgType >= (int)PackMsg.PackMsg.MsgKind_e.PACK_MSG_NUMBER)
            {
                return;
            }
            msgHandlers[MsgType] = onMsgReceived;
        }

        uint CopyBytes(byte[] src, byte[] dst, uint src_offset, uint dst_offset, uint number_of_bytes_2_copy)
        {
            uint idx = 0;

            for (idx = 0; ((dst_offset + idx) < dst.Length) && ((src_offset + idx) < src.Length) && (idx < number_of_bytes_2_copy); idx++)
            {
                dst[dst_offset + idx] = src[src_offset + idx];
            }
            return idx;
        }

        public void OnDataByteMode(byte[] packet, int room_space)
        {
            uint bytes_copied = 0;
            uint overall_bytes_copied = 0;
            int offset = 0;
            int rc;
            byte[] returnMsg;

            LogUtility.LogUtility.LogFile("processing " + Convert.ToString(packet.Length) + " bytes", ModuleLogLevel);
            while (overall_bytes_copied < packet.Length)
            {
                LogUtility.LogUtility.LogFile("remain " + Convert.ToString(packet.Length - overall_bytes_copied) + " bytes", ModuleLogLevel);
                switch (mState)
                {
                    case (byte)Receiver_State_e.RECEIVER_HEADER_STATE:
                        LogUtility.LogUtility.LogFile("HEADER STATE", ModuleLogLevel);
                        if (mBuff == null)
                        {
                            mBuff = new byte[PackMsg.PackMsg.GetMsgHeaderSize()];
                            mBufferIdx = 0;
                        }
                        bytes_copied = CopyBytes(packet, mBuff, overall_bytes_copied, mBufferIdx, PackMsg.PackMsg.GetMsgHeaderSize() - mBufferIdx);
                        overall_bytes_copied += bytes_copied;
                        mBufferIdx += bytes_copied;
                        LogUtility.LogUtility.LogFile("copied " + Convert.ToString(bytes_copied) + " bytes", ModuleLogLevel);
                        if (mBufferIdx != PackMsg.PackMsg.GetMsgHeaderSize())
                        {
                            LogUtility.LogUtility.LogFile("still missing " + Convert.ToString(PackMsg.PackMsg.GetMsgHeaderSize() - mBufferIdx) + " bytes", ModuleLogLevel);
                            break;
                        }
                        if ((rc = PackMsg.PackMsg.DecodeMsg(mBuff, out mExpectedNumberOfBytes, out mFlags, out mMsgKind, out offset)) != 0)
                        {
                            InitRxStateMachine();
                            LogUtility.LogUtility.LogFile("unable to decode message", ModuleLogLevel);
                            // process the rest of buffer, write above lines as a function
                            break;
                        }
                        mBuff = null;
                        mBuff = new byte[mExpectedNumberOfBytes];
                        mBufferIdx = 0;
                        mState = (byte)Receiver_State_e.RECEIVER_MSG_BODY_STATE;
                        break;
                    case (byte)Receiver_State_e.RECEIVER_MSG_BODY_STATE:
                        bytes_copied = CopyBytes(packet, mBuff, overall_bytes_copied, mBufferIdx, mExpectedNumberOfBytes - mBufferIdx);
                        LogUtility.LogUtility.LogFile("BODY STATE  " + Convert.ToString(bytes_copied) + " bytes copied", ModuleLogLevel);
                        mBufferIdx += bytes_copied;
                        overall_bytes_copied += bytes_copied;
                        // calculate remaining number of bytes in packet
                        if (mBufferIdx == mExpectedNumberOfBytes)
                        {
                            if (msgHandlers[mMsgKind] == null)
                            {
                                break;
                            }
                            returnMsg = msgHandlers[mMsgKind](mBuff, 0,mFlags, room_space);
                            if (returnMsg != null)
                            {
                                onMessageReadyToTx(onTxMessageParam, returnMsg,false);
                            }
                            InitRxStateMachine();
                        }
                        break;
                    default:
                        overall_bytes_copied += bytes_copied;
                        LogUtility.LogUtility.LogFile("Default case state", ModuleLogLevel);
                        break;
                }
            }
        }
        public string GetDebugInfo()
        {
            string debugInfo = "";
            debugInfo += "mBufferIdx " + Convert.ToString(mBufferIdx) + " mExpectedNumberOfBytes " + Convert.ToString(mExpectedNumberOfBytes) + " mFlags " + Convert.ToString(mFlags) + " mMsgKind " + Convert.ToString(mMsgKind) + " mState " + Convert.ToString(mState);
            return debugInfo;
        }
    }
}
