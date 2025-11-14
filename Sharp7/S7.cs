using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp7
{
    public static class S7
    {
        #region [Help Functions]

        private static long
            _bias = 621355968000000000; // "decimicros" between 0001-01-01 00:00:00 and 1970-01-01 00:00:00

        private static int BcDtoByte(byte b)
        {
            return ((b >> 4) * 10) + (b & 0x0F);
        }

        private static byte ByteToBcd(int value)
        {
            return (byte)(((value / 10) << 4) | (value % 10));
        }

        public static int DataSizeByte(this int wordLength)
        {
            switch (wordLength)
            {
                case S7Consts.S7WlBit: return 1; // S7 sends 1 byte per bit
                case S7Consts.S7WlByte: return 1;
                case S7Consts.S7WlChar: return 1;
                case S7Consts.S7WlWord: return 2;
                case S7Consts.S7WldWord: return 4;
                case S7Consts.S7WlInt: return 2;
                case S7Consts.S7WldInt: return 4;
                case S7Consts.S7WlReal: return 4;
                case S7Consts.S7WlCounter: return 2;
                case S7Consts.S7WlTimer: return 2;
                default: return 0;
            }
        }

        #region Get/Set the bit at Pos.Bit

        public static bool GetBitAt(this byte[] buffer, int pos, int bit)
        {
            byte[] mask = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
            if (bit < 0) bit = 0;
            if (bit > 7) bit = 7;
            return (buffer[pos] & mask[bit]) != 0;
        }

        public static void SetBitAt(ref byte[] buffer, int pos, int bit, bool value)
        {
            byte[] mask = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };
            if (bit < 0) bit = 0;
            if (bit > 7) bit = 7;

            if (value)
                buffer[pos] = (byte)(buffer[pos] | mask[bit]);
            else
                buffer[pos] = (byte)(buffer[pos] & ~mask[bit]);
        }

        #endregion

        #region Get/Set 8 bit signed value (S7 SInt) -128..127

        public static int GetSIntAt(this byte[] buffer, int pos)
        {
            int value = buffer[pos];
            if (value < 128)
                return value;

            return (value - 256);
        }

        public static void SetSIntAt(this byte[] buffer, int pos, int value)
        {
            if (value < -128) value = -128;
            if (value > 127) value = 127;
            buffer[pos] = (byte)value;
        }

        #endregion

        #region Get/Set 16 bit signed value (S7 int) -32768..32767

        public static int GetIntAt(this byte[] buffer, int pos)
        {
            return (short)((buffer[pos] << 8) | buffer[pos + 1]);
        }

        public static void SetIntAt(this byte[] buffer, int pos, short value)
        {
            buffer[pos] = (byte)(value >> 8);
            buffer[pos + 1] = (byte)(value & 0x00FF);
        }

        #endregion

        #region Get/Set 32 bit signed value (S7 DInt) -2147483648..2147483647

        public static int GetDIntAt(this byte[] buffer, int pos)
        {
            int result;
            result = buffer[pos];
            result <<= 8;
            result += buffer[pos + 1];
            result <<= 8;
            result += buffer[pos + 2];
            result <<= 8;
            result += buffer[pos + 3];
            return result;
        }

        public static void SetDIntAt(this byte[] buffer, int pos, int value)
        {
            buffer[pos + 3] = (byte)(value & 0xFF);
            buffer[pos + 2] = (byte)((value >> 8) & 0xFF);
            buffer[pos + 1] = (byte)((value >> 16) & 0xFF);
            buffer[pos] = (byte)((value >> 24) & 0xFF);
        }

        #endregion

        #region Get/Set 64 bit signed value (S7 LInt) -9223372036854775808..9223372036854775807

        public static long GetLIntAt(this byte[] buffer, int pos)
        {
            long result;
            result = buffer[pos];
            result <<= 8;
            result += buffer[pos + 1];
            result <<= 8;
            result += buffer[pos + 2];
            result <<= 8;
            result += buffer[pos + 3];
            result <<= 8;
            result += buffer[pos + 4];
            result <<= 8;
            result += buffer[pos + 5];
            result <<= 8;
            result += buffer[pos + 6];
            result <<= 8;
            result += buffer[pos + 7];
            return result;
        }

        public static void SetLIntAt(this byte[] buffer, int pos, long value)
        {
            buffer[pos + 7] = (byte)(value & 0xFF);
            buffer[pos + 6] = (byte)((value >> 8) & 0xFF);
            buffer[pos + 5] = (byte)((value >> 16) & 0xFF);
            buffer[pos + 4] = (byte)((value >> 24) & 0xFF);
            buffer[pos + 3] = (byte)((value >> 32) & 0xFF);
            buffer[pos + 2] = (byte)((value >> 40) & 0xFF);
            buffer[pos + 1] = (byte)((value >> 48) & 0xFF);
            buffer[pos] = (byte)((value >> 56) & 0xFF);
        }

        #endregion

        #region Get/Set 8 bit unsigned value (S7 USInt) 0..255

        public static byte GetUsIntAt(this byte[] buffer, int pos)
        {
            return buffer[pos];
        }

        public static void SetUsIntAt(this byte[] buffer, int pos, byte value)
        {
            buffer[pos] = value;
        }

        #endregion

        #region Get/Set 16 bit unsigned value (S7 UInt) 0..65535

        public static ushort GetUIntAt(this byte[] buffer, int pos)
        {
            return (ushort)((buffer[pos] << 8) | buffer[pos + 1]);
        }

        public static void SetUIntAt(this byte[] buffer, int pos, ushort value)
        {
            buffer[pos] = (byte)(value >> 8);
            buffer[pos + 1] = (byte)(value & 0x00FF);
        }

        #endregion

        #region Get/Set 32 bit unsigned value (S7 UDInt) 0..4294967296

        public static uint GetUdIntAt(this byte[] buffer, int pos)
        {
            uint result;
            result = buffer[pos];
            result <<= 8;
            result |= buffer[pos + 1];
            result <<= 8;
            result |= buffer[pos + 2];
            result <<= 8;
            result |= buffer[pos + 3];
            return result;
        }

        public static void SetUdIntAt(this byte[] buffer, int pos, uint value)
        {
            buffer[pos + 3] = (byte)(value & 0xFF);
            buffer[pos + 2] = (byte)((value >> 8) & 0xFF);
            buffer[pos + 1] = (byte)((value >> 16) & 0xFF);
            buffer[pos] = (byte)((value >> 24) & 0xFF);
        }

        #endregion

        #region Get/Set 64 bit unsigned value (S7 ULint) 0..18446744073709551616

        public static ulong GetUlIntAt(this byte[] buffer, int pos)
        {
            ulong result;
            result = buffer[pos];
            result <<= 8;
            result |= buffer[pos + 1];
            result <<= 8;
            result |= buffer[pos + 2];
            result <<= 8;
            result |= buffer[pos + 3];
            result <<= 8;
            result |= buffer[pos + 4];
            result <<= 8;
            result |= buffer[pos + 5];
            result <<= 8;
            result |= buffer[pos + 6];
            result <<= 8;
            result |= buffer[pos + 7];
            return result;
        }

        public static void SetULintAt(this byte[] buffer, int pos, ulong value)
        {
            buffer[pos + 7] = (byte)(value & 0xFF);
            buffer[pos + 6] = (byte)((value >> 8) & 0xFF);
            buffer[pos + 5] = (byte)((value >> 16) & 0xFF);
            buffer[pos + 4] = (byte)((value >> 24) & 0xFF);
            buffer[pos + 3] = (byte)((value >> 32) & 0xFF);
            buffer[pos + 2] = (byte)((value >> 40) & 0xFF);
            buffer[pos + 1] = (byte)((value >> 48) & 0xFF);
            buffer[pos] = (byte)((value >> 56) & 0xFF);
        }

        #endregion

        #region Get/Set 8 bit word (S7 Byte) 16#00..16#FF

        public static byte GetByteAt(this byte[] buffer, int pos)
        {
            return buffer[pos];
        }

        public static void SetByteAt(this byte[] buffer, int pos, byte value)
        {
            buffer[pos] = value;
        }

        #endregion

        #region Get/Set 16 bit word (S7 Word) 16#0000..16#FFFF

        public static ushort GetWordAt(this byte[] buffer, int pos)
        {
            return GetUIntAt(buffer, pos);
        }

        public static void SetWordAt(this byte[] buffer, int pos, ushort value)
        {
            SetUIntAt(buffer, pos, value);
        }

        #endregion

        #region Get/Set 32 bit word (S7 DWord) 16#00000000..16#FFFFFFFF

        public static uint GetDWordAt(this byte[] buffer, int pos)
        {
            return GetUdIntAt(buffer, pos);
        }

        public static void SetDWordAt(this byte[] buffer, int pos, uint value)
        {
            SetUdIntAt(buffer, pos, value);
        }

        #endregion

        #region Get/Set 64 bit word (S7 LWord) 16#0000000000000000..16#FFFFFFFFFFFFFFFF

        public static ulong GetLWordAt(this byte[] buffer, int pos)
        {
            return GetUlIntAt(buffer, pos);
        }

        public static void SetLWordAt(this byte[] buffer, int pos, ulong value)
        {
            SetULintAt(buffer, pos, value);
        }

        #endregion

        #region Get/Set 32 bit floating point number (S7 Real) (Range of Single)

        public static float GetRealAt(this byte[] buffer, int pos)
        {
            uint value = GetUdIntAt(buffer, pos);
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static void SetRealAt(this byte[] buffer, int pos, float value)
        {
            byte[] floatArray = BitConverter.GetBytes(value);
            buffer[pos] = floatArray[3];
            buffer[pos + 1] = floatArray[2];
            buffer[pos + 2] = floatArray[1];
            buffer[pos + 3] = floatArray[0];
        }

        #endregion

        #region Get/Set 64 bit floating point number (S7 LReal) (Range of Double)

        public static double GetLRealAt(this byte[] buffer, int pos)
        {
            ulong value = GetUlIntAt(buffer, pos);
            byte[] bytes = BitConverter.GetBytes(value);
            return BitConverter.ToDouble(bytes, 0);
        }

        public static void SetLRealAt(this byte[] buffer, int pos, double value)
        {
            byte[] floatArray = BitConverter.GetBytes(value);
            buffer[pos] = floatArray[7];
            buffer[pos + 1] = floatArray[6];
            buffer[pos + 2] = floatArray[5];
            buffer[pos + 3] = floatArray[4];
            buffer[pos + 4] = floatArray[3];
            buffer[pos + 5] = floatArray[2];
            buffer[pos + 6] = floatArray[1];
            buffer[pos + 7] = floatArray[0];
        }

        #endregion

        #region Get/Set DateTime (S7 DATE_AND_TIME)

        public static DateTime GetDateTimeAt(this byte[] buffer, int pos)
        {
            int year, month, day, hour, min, sec, mSec;

            year = BcDtoByte(buffer[pos]);
            if (year < 90)
                year += 2000;
            else
                year += 1900;

            month = BcDtoByte(buffer[pos + 1]);
            day = BcDtoByte(buffer[pos + 2]);
            hour = BcDtoByte(buffer[pos + 3]);
            min = BcDtoByte(buffer[pos + 4]);
            sec = BcDtoByte(buffer[pos + 5]);
            mSec = (BcDtoByte(buffer[pos + 6]) * 10) + (BcDtoByte(buffer[pos + 7]) / 10);
            try
            {
                return new DateTime(year, month, day, hour, min, sec, mSec);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetDateTimeAt(this byte[] buffer, int pos, DateTime value)
        {
            int year = value.Year;
            int month = value.Month;
            int day = value.Day;
            int hour = value.Hour;
            int min = value.Minute;
            int sec = value.Second;
            int dow = (int)value.DayOfWeek + 1;
            // MSecH = First two digits of miliseconds 
            int msecH = value.Millisecond / 10;
            // MSecL = Last digit of miliseconds
            int msecL = value.Millisecond % 10;
            if (year > 1999)
                year -= 2000;

            buffer[pos] = ByteToBcd(year);
            buffer[pos + 1] = ByteToBcd(month);
            buffer[pos + 2] = ByteToBcd(day);
            buffer[pos + 3] = ByteToBcd(hour);
            buffer[pos + 4] = ByteToBcd(min);
            buffer[pos + 5] = ByteToBcd(sec);
            buffer[pos + 6] = ByteToBcd(msecH);
            buffer[pos + 7] = ByteToBcd(msecL * 10 + dow);
        }

        #endregion

        #region Get/Set DATE (S7 DATE)

        public static DateTime GetDateAt(this byte[] buffer, int pos)
        {
            try
            {
                return new DateTime(1990, 1, 1).AddDays(GetIntAt(buffer, pos));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetDateAt(this byte[] buffer, int pos, DateTime value)
        {
            SetIntAt(buffer, pos, (short)(value - new DateTime(1990, 1, 1)).Days);
        }

        #endregion

        #region Get/Set TOD (S7 TIME_OF_DAY)

        public static DateTime GetTodAt(this byte[] buffer, int pos)
        {
            try
            {
                return new DateTime(0).AddMilliseconds(S7.GetDIntAt(buffer, pos));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetTodAt(this byte[] buffer, int pos, DateTime value)
        {
            TimeSpan time = value.TimeOfDay;
            SetDIntAt(buffer, pos, (int)Math.Round(time.TotalMilliseconds));
        }

        #endregion

        #region Get/Set LTOD (S7 1500 LONG TIME_OF_DAY)

        public static DateTime GetLtodAt(this byte[] buffer, int pos)
        {
            // .NET Tick = 100 ns, S71500 Tick = 1 ns
            try
            {
                return new DateTime(Math.Abs(GetLIntAt(buffer, pos) / 100));
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetLtodAt(this byte[] buffer, int pos, DateTime value)
        {
            TimeSpan time = value.TimeOfDay;
            SetLIntAt(buffer, pos, (long)time.Ticks * 100);
        }

        #endregion

        #region GET/SET LDT (S7 1500 Long Date and Time)

        public static DateTime GetLdtAt(this byte[] buffer, int pos)
        {
            try
            {
                return new DateTime((GetLIntAt(buffer, pos) / 100) + _bias);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetLdtAt(this byte[] buffer, int pos, DateTime value)
        {
            SetLIntAt(buffer, pos, (value.Ticks - _bias) * 100);
        }

        #endregion

        #region Get/Set DTL (S71200/1500 Date and Time)

        // Thanks to Johan Cardoen for GetDTLAt
        public static DateTime GetDtlAt(this byte[] buffer, int pos)
        {
            int year, month, day, hour, min, sec, mSec;

            year = buffer[pos] * 256 + buffer[pos + 1];
            month = buffer[pos + 2];
            day = buffer[pos + 3];
            hour = buffer[pos + 5];
            min = buffer[pos + 6];
            sec = buffer[pos + 7];
            mSec = (int)GetUdIntAt(buffer, pos + 8) / 1000000;

            try
            {
                return new DateTime(year, month, day, hour, min, sec, mSec);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return new DateTime(0);
            }
        }

        public static void SetDtlAt(this byte[] buffer, int pos, DateTime value)
        {
            short year = (short)value.Year;
            byte month = (byte)value.Month;
            byte day = (byte)value.Day;
            byte hour = (byte)value.Hour;
            byte min = (byte)value.Minute;
            byte sec = (byte)value.Second;
            byte dow = (byte)(value.DayOfWeek + 1);

            int nanoSecs = value.Millisecond * 1000000;

            var bytesShort = BitConverter.GetBytes(year);

            buffer[pos] = bytesShort[1];
            buffer[pos + 1] = bytesShort[0];
            buffer[pos + 2] = month;
            buffer[pos + 3] = day;
            buffer[pos + 4] = dow;
            buffer[pos + 5] = hour;
            buffer[pos + 6] = min;
            buffer[pos + 7] = sec;
            SetDIntAt(buffer, pos + 8, nanoSecs);
        }

        #endregion

        #region Get/Set String (S7 String)

        // Thanks to Pablo Agirre 
        public static string GetStringAt(this byte[] buffer, int pos)
        {
            int size = (int)buffer[pos + 1];
            return Encoding.UTF8.GetString(buffer, pos + 2, size);
        }

        public static void SetStringAt(this byte[] buffer, int pos, int maxLen, string value)
        {
            int size = value.Length;
            buffer[pos] = (byte)maxLen;
            buffer[pos + 1] = (byte)size;
            Encoding.UTF8.GetBytes(value, 0, size, buffer, pos + 2);
        }

        #endregion

        #region Get/Set Array of char (S7 ARRAY OF CHARS)

        public static string GetCharsAt(this byte[] buffer, int pos, int size)
        {
            return Encoding.UTF8.GetString(buffer, pos, size);
        }

        public static void SetCharsAt(this byte[] buffer, int pos, string value)
        {
            int maxLen = buffer.Length - pos;
            // Truncs the string if there's no room enough        
            if (maxLen > value.Length) maxLen = value.Length;
            Encoding.UTF8.GetBytes(value, 0, maxLen, buffer, pos);
        }

        #endregion

        #region Get/Set Counter

        public static int GetCounter(this ushort value)
        {
            return BcDtoByte((byte)value) * 100 + BcDtoByte((byte)(value >> 8));
        }

        public static int GetCounterAt(this ushort[] buffer, int index)
        {
            return GetCounter(buffer[index]);
        }

        public static ushort ToCounter(this int value)
        {
            return (ushort)(ByteToBcd(value / 100) + (ByteToBcd(value % 100) << 8));
        }

        public static void SetCounterAt(this ushort[] buffer, int pos, int value)
        {
            buffer[pos] = ToCounter(value);
        }

        #endregion

        #region Get/Set Timer

        public static S7Timer GetS7TimerAt(this byte[] buffer, int pos)
        {
            return new S7Timer(new List<byte>(buffer).GetRange(pos, 12).ToArray());
        }

        public static void SetS7TimespanAt(this byte[] buffer, int pos, TimeSpan value)
        {
            SetDIntAt(buffer, pos, (int)value.TotalMilliseconds);
        }

        public static TimeSpan GetS7TimespanAt(this byte[] buffer, int pos)
        {
            if (buffer.Length < pos + 4)
            {
                return new TimeSpan();
            }

            int a;
            a = buffer[pos + 0];
            a <<= 8;
            a += buffer[pos + 1];
            a <<= 8;
            a += buffer[pos + 2];
            a <<= 8;
            a += buffer[pos + 3];
            TimeSpan sp = new TimeSpan(0, 0, 0, 0, a);

            return sp;
        }

        #endregion

        #endregion [Help Functions]
    }
}