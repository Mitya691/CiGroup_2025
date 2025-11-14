using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharp7
{
    public class S7Timer
    {
        #region S7Timer
        TimeSpan _pt;
        TimeSpan _et;
        bool _input;
        bool _q;
        public S7Timer(byte[] buff, int position)
        {
            if (position + 12 < buff.Length)
            {
                SetTimer(new List<byte>(buff).GetRange(position, 12).ToArray());
            }
        }

        public S7Timer(byte[] buff)
        {
            SetTimer(buff);
        }

        private void SetTimer(byte[] buff)
        {
            if (buff.Length != 12)
            {
                this._pt = new TimeSpan(0);
                this._et = new TimeSpan(0);
            }
            else
            {
                int resPt;
                resPt = buff[0]; resPt <<= 8;
                resPt += buff[1]; resPt <<= 8;
                resPt += buff[2]; resPt <<= 8;
                resPt += buff[3];
                this._pt = new TimeSpan(0, 0, 0, 0, resPt);

                int resEt;
                resEt = buff[4]; resEt <<= 8;
                resEt += buff[5]; resEt <<= 8;
                resEt += buff[6]; resEt <<= 8;
                resEt += buff[7];
                this._et = new TimeSpan(0, 0, 0, 0, resEt);

                this._input = (buff[8] & 0x01) == 0x01;
                this._q = (buff[8] & 0x02) == 0x02;
            }
        }
        public TimeSpan Pt
        {
            get
            {
                return _pt;
            }
        }
        public TimeSpan Et
        {
            get
            {
                return _et;
            }
        }
        public bool In
        {
            get
            {
                return _input;
            }
        }
        public bool Q
        {
            get
            {
                return _q;
            }
        }
        #endregion
    }
}