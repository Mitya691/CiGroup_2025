using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sharp7
{
    public class S7MultiVar
    {
        private S7Client _fClient;
        private GCHandle[] _handles = new GCHandle[S7Client.MaxVars];
        private int _count;
        private S7Client.S7DataItem[] _items = new S7Client.S7DataItem[S7Client.MaxVars];


        public int[] Results { get; } = new int[S7Client.MaxVars];

        private bool AdjustWordLength(int area, ref int wordLen, ref int amount, ref int start)
        {
            // Calc Word size          
            int wordSize = S7.DataSizeByte(wordLen);
            if (wordSize == 0)
                return false;

            if (area == S7Consts.S7AreaC)
                wordLen = S7Consts.S7WlCounter;
            if (area == S7Consts.S7AreaT)
                wordLen = S7Consts.S7WlTimer;

            if (wordLen == S7Consts.S7WlBit)
                amount = 1;  // Only 1 bit can be transferred at time
            else
            {
                if ((wordLen != S7Consts.S7WlCounter) && (wordLen != S7Consts.S7WlTimer))
                {
                    amount = amount * wordSize;
                    start = start * 8;
                    wordLen = S7Consts.S7WlByte;
                }
            }
            return true;
        }

        public S7MultiVar(S7Client client)
        {
            _fClient = client;
            for (int c = 0; c < S7Client.MaxVars; c++)
                Results[c] = S7Consts.ErrCliItemNotAvailable;
        }
        ~S7MultiVar()
        {
            Clear();
        }

        public bool Add<T>(S7Consts.S7Tag tag, ref T[] buffer, int offset)
        {
            return Add(tag.Area, tag.WordLen, tag.DBNumber, tag.Start, tag.Elements, ref buffer, offset);
        }

        public bool Add<T>(S7Consts.S7Tag tag, ref T[] buffer)
        {
            return Add(tag.Area, tag.WordLen, tag.DBNumber, tag.Start, tag.Elements, ref buffer);
        }

        public bool Add<T>(int area, int wordLen, int dbNumber, int start, int amount, ref T[] buffer)
        {
            return Add(area, wordLen, dbNumber, start, amount, ref buffer, 0);
        }

        public bool Add<T>(int area, int wordLen, int dbNumber, int start, int amount, ref T[] buffer, int offset)
        {
            if (_count < S7Client.MaxVars)
            {
                if (AdjustWordLength(area, ref wordLen, ref amount, ref start))
                {
                    _items[_count].Area = area;
                    _items[_count].WordLen = wordLen;
                    _items[_count].Result = S7Consts.ErrCliItemNotAvailable;
                    _items[_count].DbNumber = dbNumber;
                    _items[_count].Start = start;
                    _items[_count].Amount = amount;
                    GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    if (IntPtr.Size == 4)
                        _items[_count].PData = (IntPtr)(handle.AddrOfPinnedObject().ToInt32() + offset * Marshal.SizeOf(typeof(T)));
                    else
                        _items[_count].PData = (IntPtr)(handle.AddrOfPinnedObject().ToInt64() + offset * Marshal.SizeOf(typeof(T)));

                    _handles[_count] = handle;
                    _count++;
                    return true;
                }

                return false;
            }

            return false;
        }

        public int Read()
        {
            int globalResult;
            try
            {
                if (_count > 0)
                {
                    int functionResult = _fClient.ReadMultiVars(_items, _count);
                    if (functionResult == 0)
                        for (int c = 0; c < S7Client.MaxVars; c++)
                            Results[c] = _items[c].Result;
                    globalResult = functionResult;
                }
                else
                    globalResult = S7Consts.ErrCliFunctionRefused;
            }
            finally
            {
                Clear(); // handles are no more needed and MUST be freed
            }
            return globalResult;
        }

        public int Write()
        {
            int globalResult;
            try
            {
                if (_count > 0)
                {
                    int functionResult = _fClient.WriteMultiVars(_items, _count);
                    if (functionResult == 0)
                        for (int c = 0; c < S7Client.MaxVars; c++)
                            Results[c] = _items[c].Result;
                    globalResult = functionResult;
                }
                else
                    globalResult = S7Consts.ErrCliFunctionRefused;
            }
            finally
            {
                Clear(); // handles are no more needed and MUST be freed
            }
            return globalResult;
        }

        public void Clear()
        {
            for (int c = 0; c < _count; c++)
            {
                if (_handles[c] != null)
                    _handles[c].Free();
            }
            _count = 0;
        }
    }
}