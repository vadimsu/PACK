using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;

namespace RxTxStateMachine
{
    enum PackTxRxState_e
    {
        PACK_DUMMY_STATE,
        PACK_HEADER_STATE,
        PACK_BODY_STATE,
        PACK_WHOLE_PACKET_STATE
    };

    public delegate void OnMsgReceived();

    public class FramingStateMachine
    {
        public static LogUtility.LogLevels ModuleLogLevel = LogUtility.LogLevels.LEVEL_LOG_MEDIUM;
        protected const uint PackPreamble = 0xA57F5AF7;

        protected uint m_ByteCounter;
        protected byte[] m_Header;
        protected byte[] m_Body;
        protected byte m_State;
        protected EndPoint Id;

        public FramingStateMachine(EndPoint id)
        {
            m_ByteCounter = 0;
            m_Body = null;
            m_Header = new byte[9];
            m_State = (byte)PackTxRxState_e.PACK_DUMMY_STATE;
            Id = id;
        }

        public bool IsTransactionCompleted()
        {
            if ((m_State == (byte)PackTxRxState_e.PACK_BODY_STATE) &&
                (m_Body != null) &&
                (m_ByteCounter == m_Body.Length))
            {
                return true;
            }
            else if ((m_State == (byte)PackTxRxState_e.PACK_WHOLE_PACKET_STATE) &&
                    (m_Body != null) &&
                    (m_ByteCounter == (m_Header.Length + m_Body.Length)))
            {
                return true;
            }
            return false;
        }

        public void ClearMsgBody()
        {
            m_Body = null;
            m_ByteCounter = 0;
            m_State = (byte)PackTxRxState_e.PACK_DUMMY_STATE;
        }

        public bool IsBusy()
        {
            return (m_State != (byte)PackTxRxState_e.PACK_DUMMY_STATE);
        }
        public virtual string GetDebugInfo()
        {
            return " State " + Convert.ToString(m_State) + " byte counter " + Convert.ToString(m_ByteCounter);
        }
    }
    public class TxStateMachine : FramingStateMachine
    {
        
        public TxStateMachine(EndPoint id) : base(id)
        {
            m_Header[0] = (byte)(PackPreamble & 0xFF);
            m_Header[1] = (byte)((PackPreamble >> 8) & 0xFF);
            m_Header[2] = (byte)((PackPreamble >> 16) & 0xFF);
            m_Header[3] = (byte)((PackPreamble >> 24) & 0xFF);
        }

        void GoHeaderStateIfDummy()
        {
            if (m_State == (byte)PackTxRxState_e.PACK_DUMMY_STATE)
            {
                m_State = (byte)PackTxRxState_e.PACK_HEADER_STATE;
            }
        }

        public void SetLength(uint length)
        {
            m_Header[4] = (byte)(length & 0xFF);
            m_Header[5] = (byte)((length >> 8) & 0xFF);
            m_Header[6] = (byte)((length >> 16) & 0xFF);
            m_Header[7] = (byte)((length >> 24) & 0xFF);
            GoHeaderStateIfDummy();
        }

        public void SetKind(byte kind)
        {
            m_Header[8] = kind;
            GoHeaderStateIfDummy();
        }

        public byte[] GetBytes()
        {
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " GetBytes Tx sm: state " + Convert.ToString(m_State) + " byte counter " + Convert.ToString(m_ByteCounter), ModuleLogLevel);
                //LogUtility.LogUtility.LogFile(Environment.StackTrace);
                switch (m_State)
                {
                    case (byte)PackTxRxState_e.PACK_DUMMY_STATE:
                        m_State = (byte)PackTxRxState_e.PACK_HEADER_STATE;
                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Went header state", ModuleLogLevel);
                        return m_Header;
                    case (byte)PackTxRxState_e.PACK_HEADER_STATE:
                        return m_Header;
                    case (byte)PackTxRxState_e.PACK_BODY_STATE:
                        if (m_ByteCounter == m_Body.Length)
                        {
                            return null;
                        }
                        return m_Body;
                }
                return null;
            }
            catch (Exception exc)
            {
                throw new Exception("Excetion in GetBytes " + exc.Message + " " + exc.StackTrace);
            }
        }

        public byte[] GetBytes(int limit)
        {
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " GetBytes (limited) Tx sm: state " + Convert.ToString(m_State) + " byte counter " + Convert.ToString(m_ByteCounter), ModuleLogLevel);
                //LogUtility.LogUtility.LogFile(Environment.StackTrace);
                if (m_State == (byte)PackTxRxState_e.PACK_DUMMY_STATE)
                {
                    m_State = (byte)PackTxRxState_e.PACK_HEADER_STATE;
                    LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " Went header state", ModuleLogLevel);
                }
                if(m_State == (byte)PackTxRxState_e.PACK_HEADER_STATE)
                {
                    byte []buff = new byte[m_Header.Length+m_Body.Length];
                    m_Header.CopyTo(buff,0);
                    m_Body.CopyTo(buff,m_Header.Length);
                    m_State = (byte)PackTxRxState_e.PACK_WHOLE_PACKET_STATE;
                    return buff;
                }
                if (m_ByteCounter == m_Body.Length)
                {
                    return null;
                }
                return m_Body;
            }
            catch (Exception exc)
            {
                throw new Exception("Excetion in GetBytes " + exc.Message + " " + exc.StackTrace);
            }
        }
        public bool IsInBody()
        {
            return ((m_State == (byte)PackTxRxState_e.PACK_BODY_STATE)||(m_State == (byte)PackTxRxState_e.PACK_WHOLE_PACKET_STATE));
        }
        public bool IsWholeMessage()
        {
            return (m_State == (byte)PackTxRxState_e.PACK_WHOLE_PACKET_STATE);
        }
        public int GetHeaderLength()
        {
            return m_Header.Length;
        }
        public bool OnTxComplete(uint bytes_count)
        {
            try
            {
                LogUtility.LogUtility.LogFile(Convert.ToString(Id) + " OnTxComplete Tx sm: state " + Convert.ToString(m_State) + " byte counter " + Convert.ToString(m_ByteCounter), ModuleLogLevel);
                switch (m_State)
                {
                    case (byte)PackTxRxState_e.PACK_HEADER_STATE:
                        //m_ByteCounter += bytes_count;
                        if (bytes_count == m_Header.Length)
                        {
                            m_State = (byte)PackTxRxState_e.PACK_BODY_STATE;
                            //m_ByteCounter = 0;
                        }
                        else
                        {
                            m_State = (byte)PackTxRxState_e.PACK_DUMMY_STATE;
                        }
                        break;
                    case (byte)PackTxRxState_e.PACK_BODY_STATE:
                        if (bytes_count != m_Body.Length)
                        {
                            return false;
                        }
                        m_ByteCounter += bytes_count;
                        if (m_ByteCounter == m_Body.Length)
                        {
                            // m_State = (byte)PackTxRxState_e.PACK_DUMMY_STATE;
                        }
                        break;
                    case (byte)PackTxRxState_e.PACK_WHOLE_PACKET_STATE:
                        if (bytes_count != (m_Header.Length + m_Body.Length))
                        {
                            return false;
                        }
                        m_ByteCounter += bytes_count;
                        break;
                }
                return true;
            }
            catch (Exception exc)
            {
                throw new Exception("exception in OnTxComplete " + exc.Message + " " + exc.StackTrace);
            }
        }

        public void SetMsgBody(byte[] body)
        {
            m_Body = body;
        }
    };

    public class RxStateMachine : FramingStateMachine
    {
        OnMsgReceived onMsgReceived;
        public RxStateMachine(EndPoint id) : base(id)
        {
            onMsgReceived = null;
        }

        uint GetLength()
        {
            uint length = m_Header[4];
            length |= (uint)(m_Header[5] << 8);
            length |= (uint)(m_Header[6] << 16);
            length |= (uint)(m_Header[7] << 24);
            return length;
        }
        public void SetCallback(OnMsgReceived onMessageReceived)
        {
            onMsgReceived = onMessageReceived;
        }
        public byte GetKind()
        {
            return m_Header[8];
        }
        void CopyBytes(byte []src,byte []dst,int src_offset,int dst_offset,int bytes2copy)
        {
            for(int idx = 0;idx < bytes2copy;idx++)
            {
                dst[dst_offset + idx] = src[src_offset + idx];
            }
        }
        public void OnRxComplete(byte []buff,int RealSize)
        {
            try
            {
                int bytes2process = RealSize;
                int remaining_bytes;
                LogUtility.LogUtility.LogFile("OnRxComplete " + Convert.ToString(RealSize), ModuleLogLevel);
                while (bytes2process > 0)
                {
                    LogUtility.LogUtility.LogFile("State " + Convert.ToString(m_State) + " bytes2process " + Convert.ToString(bytes2process) + " byte counter " + Convert.ToString(m_ByteCounter), ModuleLogLevel);
                    switch (m_State)
                    {
                        case (byte)PackTxRxState_e.PACK_DUMMY_STATE:
                        case (byte)PackTxRxState_e.PACK_HEADER_STATE:
                            m_State = (byte)PackTxRxState_e.PACK_HEADER_STATE;
                            remaining_bytes = (int)(m_Header.Length - m_ByteCounter);
                            if (remaining_bytes >= bytes2process)
                            {
                                CopyBytes(buff, m_Header, RealSize - bytes2process, (int)m_ByteCounter, bytes2process);
                                m_ByteCounter += (uint)bytes2process;
                                bytes2process = 0;
                                LogUtility.LogUtility.LogFile("All bytes processed ", ModuleLogLevel);
                            }
                            else
                            {
                                CopyBytes(buff, m_Header, RealSize - bytes2process, (int)m_ByteCounter, remaining_bytes);
                                m_ByteCounter += (uint)remaining_bytes;
                                bytes2process -= remaining_bytes;
                                LogUtility.LogUtility.LogFile("After copying remain " + Convert.ToString(bytes2process), ModuleLogLevel);
                            }

                            if (m_ByteCounter == m_Header.Length)
                            {
                                uint preamble = m_Header[0];
                                preamble |= (uint)(m_Header[1] << 8);
                                preamble |= (uint)(m_Header[2] << 16);
                                preamble |= (uint)(m_Header[3] << 24);
                                LogUtility.LogUtility.LogFile("Header received ", ModuleLogLevel);
                                if (preamble != PackPreamble)
                                {
                                    m_ByteCounter = 0;
                                    m_State = (byte)PackTxRxState_e.PACK_DUMMY_STATE;
                                    LogUtility.LogUtility.LogFile("PREAMBLE MISMATCH!!!!!!! ", ModuleLogLevel);
                                    return;
                                }
                                m_State = (byte)PackTxRxState_e.PACK_BODY_STATE;
                                if (m_Body == null)
                                {
                                    try
                                    {
                                        LogUtility.LogUtility.LogFile("Allocating body " + Convert.ToString(GetLength()), ModuleLogLevel);
                                        m_Body = new byte[GetLength()];
                                    }
                                    catch (Exception exc)
                                    {
                                        LogUtility.LogUtility.LogFile(Convert.ToString(Id) + "EXCEPTION " + exc.Message, LogUtility.LogLevels.LEVEL_LOG_HIGH);
                                    }
                                }
                                m_ByteCounter = 0;
                            }
                            break;
                        case (byte)PackTxRxState_e.PACK_BODY_STATE:
                            remaining_bytes = (int)(m_Body.Length - m_ByteCounter);
                            if (remaining_bytes >= bytes2process)
                            {
                                CopyBytes(buff, m_Body, RealSize - bytes2process, (int)m_ByteCounter, bytes2process);
                                m_ByteCounter += (uint)bytes2process;
                                bytes2process = 0;
                                LogUtility.LogUtility.LogFile("All bytes copyied ", ModuleLogLevel);
                            }
                            else
                            {
                                CopyBytes(buff, m_Body, RealSize - bytes2process, (int)m_ByteCounter, remaining_bytes);
                                m_ByteCounter += (uint)remaining_bytes;
                                bytes2process -= remaining_bytes;
                                LogUtility.LogUtility.LogFile("After copying remain " + Convert.ToString(bytes2process), ModuleLogLevel);
                            }
                            if (m_ByteCounter == m_Body.Length)
                            {
                                //m_State = (byte)PackTxRxState_e.PACK_DUMMY_STATE;
                                LogUtility.LogUtility.LogFile("Message received", ModuleLogLevel);
                                if (onMsgReceived != null)
                                {
                                    onMsgReceived();
                                }
                            }
                            break;
                    }
                }
                LogUtility.LogUtility.LogFile("Leaving OnRxComplete", ModuleLogLevel);
            }
            catch (Exception exc)
            {
                throw new Exception("Exception in OnRxComplete", exc);
            }
        }

        public byte[] GetMsgBody()
        {
            return m_Body;
        }

        public uint GetByteCounter()
        {
            return m_ByteCounter;
        }
        public uint GetExpectedBytes()
        {
            if (m_Body == null)
            {
                return 0;
            }
            return (uint)m_Body.Length;
        }
    };
}
