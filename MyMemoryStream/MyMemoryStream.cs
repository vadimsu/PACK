using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MyMemoryStream
{
    public class MyMemoryStream
    {
        uint current_offset;
        MemoryStream memoryStream;

        public MyMemoryStream()
        {
            current_offset = 0;
            memoryStream = new MemoryStream();
        }

        public byte[] GetBytes()
        {
            byte[] buff = new byte[memoryStream.Length - current_offset];
            long position = memoryStream.Position;
            memoryStream.Position = current_offset;
            int read = memoryStream.Read(buff, 0, buff.Length);
            memoryStream.Position = position;
            if (read != buff.Length)
            {
                return null;
            }
            return buff;
        }
        public byte[] GetBytesLimited(int Limit)
        {
            int bufSize;
            if((memoryStream.Length - current_offset) > Limit)
            {
                bufSize = Limit;
            }
            else
            {
                bufSize = (int)(memoryStream.Length - current_offset);
            }
            byte[] buff = new byte[bufSize];
            long position = memoryStream.Position;
            memoryStream.Position = current_offset;
            int read = memoryStream.Read(buff, 0, buff.Length);
            memoryStream.Position = position;
            if (read != buff.Length)
            {
                return null;
            }
            return buff;
        }

        public void AddBytes(byte[] buff)
        {
            memoryStream.Write(buff, 0, buff.Length);
        }

        public long Length
        {
            get
            {
                return (memoryStream.Length - current_offset);
            }
        }

        public long WholeLength
        {
            get
            {
                return memoryStream.Length;
            }
        }

        public void GetBytes(byte []buff,int idx,int length)
        {
            long position = memoryStream.Position;
            memoryStream.Position = current_offset;
            int read = memoryStream.Read(buff, idx, length);
            memoryStream.Position = position;
        }

        public byte[] GetBytes(int position)
        {
            long current_position = memoryStream.Position;
            if (current_position < position)
            {
                return null;
            }
            try
            {
                memoryStream.Position -= position;
                byte[] buff = new byte[memoryStream.Length - memoryStream.Position];
                memoryStream.Read(buff, 0, (int)memoryStream.Length - (int)memoryStream.Position);
                memoryStream.Position = current_position;
                return buff;
            }
            catch (Exception exc)
            {
                memoryStream.Position = current_position;
                LogUtility.LogUtility.LogFile("EXCEPTION: position " + Convert.ToString(position) + " Length " + Convert.ToString(memoryStream.Length) + " pos " + Convert.ToString(memoryStream.Position),LogUtility.LogLevels.LEVEL_LOG_HIGH);
                return null;
            }
        }

        public void IncrementOffset(uint increment)
        {
            current_offset += increment;
            if (memoryStream.Length == current_offset)
            {
                memoryStream.Dispose();
                memoryStream = null;
                memoryStream = new MemoryStream();
                current_offset = 0;
            }
        }
        public void Reset()
        {
            current_offset = 0;
        }
        public void Clear()
        {
            current_offset = 0;
            memoryStream.Dispose();
            memoryStream = null;
        }
    }
}
