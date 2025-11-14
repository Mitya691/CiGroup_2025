using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sharp7
{
    public static class S7Consts
    {
        #region [Exported Consts]
        // Error codes
        //------------------------------------------------------------------------------
        //                                     ERRORS                 
        //------------------------------------------------------------------------------
        public const int ErrTcpSocketCreation = 0x00000001;
        public const int ErrTcpConnectionTimeout = 0x00000002;
        public const int ErrTcpConnectionFailed = 0x00000003;
        public const int ErrTcpReceiveTimeout = 0x00000004;
        public const int ErrTcpDataReceive = 0x00000005;
        public const int ErrTcpSendTimeout = 0x00000006;
        public const int ErrTcpDataSend = 0x00000007;
        public const int ErrTcpConnectionReset = 0x00000008;
        public const int ErrTcpNotConnected = 0x00000009;
        public const int ErrTcpUnreachableHost = 0x00002751;

        public const int ErrIsoConnect = 0x00010000; // Connection error
        public const int ErrIsoInvalidPdu = 0x00030000; // Bad format
        public const int ErrIsoInvalidDataSize = 0x00040000; // Bad Datasize passed to send/recv : buffer is invalid

        public const int ErrCliNegotiatingPdu = 0x00100000;
        public const int ErrCliInvalidParams = 0x00200000;
        public const int ErrCliJobPending = 0x00300000;
        public const int ErrCliTooManyItems = 0x00400000;
        public const int ErrCliInvalidWordLen = 0x00500000;
        public const int ErrCliPartialDataWritten = 0x00600000;
        public const int ErrCliSizeOverPdu = 0x00700000;
        public const int ErrCliInvalidPlcAnswer = 0x00800000;
        public const int ErrCliAddressOutOfRange = 0x00900000;
        public const int ErrCliInvalidTransportSize = 0x00A00000;
        public const int ErrCliWriteDataSizeMismatch = 0x00B00000;
        public const int ErrCliItemNotAvailable = 0x00C00000;
        public const int ErrCliInvalidValue = 0x00D00000;
        public const int ErrCliCannotStartPlc = 0x00E00000;
        public const int ErrCliAlreadyRun = 0x00F00000;
        public const int ErrCliCannotStopPlc = 0x01000000;
        public const int ErrCliCannotCopyRamToRom = 0x01100000;
        public const int ErrCliCannotCompress = 0x01200000;
        public const int ErrCliAlreadyStop = 0x01300000;
        public const int ErrCliFunNotAvailable = 0x01400000;
        public const int ErrCliUploadSequenceFailed = 0x01500000;
        public const int ErrCliInvalidDataSizeRecvd = 0x01600000;
        public const int ErrCliInvalidBlockType = 0x01700000;
        public const int ErrCliInvalidBlockNumber = 0x01800000;
        public const int ErrCliInvalidBlockSize = 0x01900000;
        public const int ErrCliNeedPassword = 0x01D00000;
        public const int ErrCliInvalidPassword = 0x01E00000;
        public const int ErrCliNoPasswordToSetOrClear = 0x01F00000;
        public const int ErrCliJobTimeout = 0x02000000;
        public const int ErrCliPartialDataRead = 0x02100000;
        public const int ErrCliBufferTooSmall = 0x02200000;
        public const int ErrCliFunctionRefused = 0x02300000;
        public const int ErrCliDestroying = 0x02400000;
        public const int ErrCliInvalidParamNumber = 0x02500000;
        public const int ErrCliCannotChangeParam = 0x02600000;
        public const int ErrCliFunctionNotImplemented = 0x02700000;
        //------------------------------------------------------------------------------
        //        PARAMS LIST FOR COMPATIBILITY WITH Snap7.net.cs           
        //------------------------------------------------------------------------------
        public const int PU16LocalPort = 1;  // Not applicable here
        public const int PU16RemotePort = 2;
        public const int PI32PingTimeout = 3;
        public const int PI32SendTimeout = 4;
        public const int PI32RecvTimeout = 5;
        public const int PI32WorkInterval = 6;  // Not applicable here
        public const int PU16SrcRef = 7;  // Not applicable here
        public const int PU16DstRef = 8;  // Not applicable here
        public const int PU16SrcTSap = 9;  // Not applicable here
        public const int PI32PduRequest = 10;
        public const int PI32MaxClients = 11; // Not applicable here
        public const int PI32BSendTimeout = 12; // Not applicable here
        public const int PI32BRecvTimeout = 13; // Not applicable here
        public const int PU32RecoveryTime = 14; // Not applicable here
        public const int PU32KeepAliveTime = 15; // Not applicable here
        // Идентификаторы зон памяти контроллера.
        /// <summary>
        /// Периферийные входы-выходы.
        /// </summary>
        public const byte S7AreaP = 0x80;
        /// <summary>
        /// Входы процесса.
        /// </summary>
        public const byte S7AreaI = 0x81;
        /// <summary>
        /// Выходы процесса.
        /// </summary>
        public const byte S7AreaQ = 0x82;
        /// <summary>
        /// Глобальные переменные (меркеры).
        /// </summary>
        public const byte S7AreaM = 0x83;
        /// <summary>
        /// Блоки данных.
        /// </summary>
        public const byte S7AreaDb = 0x84;
        /// <summary>
        /// Счётчики.
        /// </summary>
        public const byte S7AreaC = 0x1C;
        /// <summary>
        /// Таймеры.
        /// </summary>
        public const byte S7AreaT = 0x1D;
        // Идентфикаторы размера переменной.
        /// <summary>
        /// Бит.
        /// </summary>
        public const int S7WlBit = 0x01;
        /// <summary>
        /// Байт.
        /// </summary>
        public const int S7WlByte = 0x02;
        /// <summary>
        /// Символ.
        /// </summary>
        public const int S7WlChar = 0x03;
        /// <summary>
        /// Слово беззнаковое.
        /// </summary>
        public const int S7WlWord = 0x04;
        /// <summary>
        /// Слово знаковое.
        /// </summary>
        public const int S7WlInt = 0x05;
        /// <summary>
        /// Двойное слово беззнаковое.
        /// </summary>
        public const int S7WldWord = 0x06;
        /// <summary>
        /// Двойное слово знаковое.
        /// </summary>
        public const int S7WldInt = 0x07;
        /// <summary>
        /// Число с плавающей точкой одиночной точности.
        /// </summary>
        public const int S7WlReal = 0x08;
        /// <summary>
        /// Счётчик.
        /// </summary>
        public const int S7WlCounter = 0x1C;
        /// <summary>
        /// Таймер.
        /// </summary>
        public const int S7WlTimer = 0x1D;
        // Коды состояния контроллера.
        /// <summary>
        /// Состояние контроллера неизвестно.
        /// </summary>
        public const int S7CpuStatusUnknown = 0x00;
        /// <summary>
        /// Контроллер работает по программе.
        /// </summary>
        public const int S7CpuStatusRun = 0x08;
        /// <summary>
        /// Контроллер остановлен.
        /// </summary>
        public const int S7CpuStatusStop = 0x04;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7Tag
        {
            public int Area;
            public int DBNumber;
            public int Start;
            public int Elements;
            public int WordLen;
        }
        #endregion
    }
}