using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ByteArrayScalarTypeConversionLib
{
    public class ByteArrayScalarTypeConversionLib
    {
        public static uint ByteArray2Long(byte[] array, uint offset, out long l)
        {
            l = BitConverter.ToInt64(array,(int)offset);
            return 8;
        }
        public static uint ByteArray2Uint(byte[] array, uint offset, out uint ui)
        {
            ui = BitConverter.ToUInt32(array, (int)offset);
            return 4;
        }
        public static uint Long2ByteArray(byte[] array, uint offset, long l)
        {
            BitConverter.GetBytes(l).CopyTo(array, (int)offset);
            return 8;
        }
        public static uint Uint2ByteArray(byte[] array, uint offset, uint ui)
        {
            BitConverter.GetBytes(ui).CopyTo(array, (int)offset);
            return 4;
        }
    }
}
