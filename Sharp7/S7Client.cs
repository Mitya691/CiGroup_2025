using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sharp7
{
    public class S7Client
    {
        #region [Constants and TypeDefs]

        // Block type
        public const int BlockOb = 0x38;
        public const int BlockDb = 0x41;
        public const int BlockSdb = 0x42;
        public const int BlockFc = 0x43;
        public const int BlockSfc = 0x44;
        public const int BlockFb = 0x45;
        public const int BlockSfb = 0x46;

        // Sub Block Type 
        public const byte SubBlkOb = 0x08;
        public const byte SubBlkDb = 0x0A;
        public const byte SubBlkSdb = 0x0B;
        public const byte SubBlkFc = 0x0C;
        public const byte SubBlkSfc = 0x0D;
        public const byte SubBlkFb = 0x0E;
        public const byte SubBlkSfb = 0x0F;

        // Block languages
        public const byte BlockLangAwl = 0x01;
        public const byte BlockLangKop = 0x02;
        public const byte BlockLangFup = 0x03;
        public const byte BlockLangScl = 0x04;
        public const byte BlockLangDb = 0x05;
        public const byte BlockLangGraph = 0x06;

        // Max number of vars (multiread/write)
        public static readonly int MaxVars = 20;

        // Result transport size
        const byte TsResBit = 0x03;
        const byte TsResByte = 0x04;
        const byte TsResInt = 0x05;
        const byte TsResReal = 0x07;
        const byte TsResOctet = 0x09;

        const ushort Code7Ok = 0x0000;
        const ushort Code7AddressOutOfRange = 0x0005;
        const ushort Code7InvalidTransportSize = 0x0006;
        const ushort Code7WriteDataSizeMismatch = 0x0007;
        const ushort Code7ResItemNotAvailable = 0x000A;
        const ushort Code7ResItemNotAvailable1 = 0xD209;
        const ushort Code7InvalidValue = 0xDC01;
        const ushort Code7NeedPassword = 0xD241;
        const ushort Code7InvalidPassword = 0xD602;
        const ushort Code7NoPasswordToClear = 0xD604;
        const ushort Code7NoPasswordToSet = 0xD605;
        const ushort Code7FunNotAvailable = 0x8104;
        const ushort Code7DataOverPdu = 0x8500;

        // Client Connection Type
        public static readonly ushort ConntypePg = 0x01;  // Connect to the PLC as a PG
        public static readonly ushort ConntypeOp = 0x02;  // Connect to the PLC as an OP
        public static readonly ushort ConntypeBasic = 0x03;  // Basic connection 

        public int _LastError = 0;

        public struct S7DataItem
        {
            public int Area;
            public int WordLen;
            public int Result;
            public int DbNumber;
            public int Start;
            public int Amount;
            public IntPtr PData;
        }

        // Order Code + Version
        public struct S7OrderCode
        {
            public string Code; // such as "6ES7 151-8AB01-0AB0"
            public byte V1;     // Version 1st digit
            public byte V2;     // Version 2nd digit
            public byte V3;     // Version 3th digit
        };

        // CPU Info
        public struct S7CpuInfo
        {
            public string ModuleTypeName;
            public string SerialNumber;
            public string AsName;
            public string Copyright;
            public string ModuleName;
        }

        public struct S7CpInfo
        {
            public int MaxPduLength;
            public int MaxConnections;
            public int MaxMpiRate;
            public int MaxBusRate;
        };

        // Block List
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7BlocksList
        {
            public int OBCount;
            public int FBCount;
            public int FCCount;
            public int SFBCount;
            public int SFCCount;
            public int DBCount;
            public int SDBCount;
        };

        // Managed Block Info
        public struct S7BlockInfo
        {
            public int BlkType;
            public int BlkNumber;
            public int BlkLang;
            public int BlkFlags;
            public int Mc7Size;  // The real size in bytes
            public int LoadSize;
            public int LocalData;
            public int SbbLength;
            public int CheckSum;
            public int Version;
            // Chars info
            public string CodeDate;
            public string IntfDate;
            public string Author;
            public string Family;
            public string Header;
        };

        // See §33.1 of "System Software for S7-300/400 System and Standard Functions"
        // and see SFC51 description too
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SzlHeader
        {
            public ushort LENTHDR;
            public ushort N_DR;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7Szl
        {
            public SzlHeader Header;
            [MarshalAs(UnmanagedType.ByValArray)]
            public byte[] Data;
        };

        // SZL List of available SZL IDs : same as SZL but List items are big-endian adjusted
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct S7SzlList
        {
            public SzlHeader Header;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x2000 - 2)]
            public ushort[] Data;
        };

        // S7 Protection
        // See §33.19 of "System Software for S7-300/400 System and Standard Functions"
        public struct S7Protection
        {
            public ushort SchSchal;
            public ushort SchPar;
            public ushort SchRel;
            public ushort BartSch;
            public ushort AnlSch;
        };

        #endregion

        #region [S7 Telegrams]

        // ISO Connection Request telegram (contains also ISO Header and COTP Header)
        byte[] _isoCr = {
            // TPKT (RFC1006 Header)
            0x03, // RFC 1006 ID (3) 
            0x00, // Reserved, always 0
            0x00, // High part of packet lenght (entire frame, payload and TPDU included)
            0x16, // Low part of packet lenght (entire frame, payload and TPDU included)
            // COTP (ISO 8073 Header)
            0x11, // PDU Size Length
            0xE0, // CR - Connection Request ID
            0x00, // Dst Reference HI
            0x00, // Dst Reference LO
            0x00, // Src Reference HI
            0x01, // Src Reference LO
            0x00, // Class + Options Flags
            0xC0, // PDU Max Length ID
            0x01, // PDU Max Length HI
            0x0A, // PDU Max Length LO
            0xC1, // Src TSAP Identifier
            0x02, // Src TSAP Length (2 bytes)
            0x01, // Src TSAP HI (will be overwritten)
            0x00, // Src TSAP LO (will be overwritten)
            0xC2, // Dst TSAP Identifier
            0x02, // Dst TSAP Length (2 bytes)
            0x01, // Dst TSAP HI (will be overwritten)
            0x02  // Dst TSAP LO (will be overwritten)
        };

        // TPKT + ISO COTP Header (Connection Oriented Transport Protocol)
        byte[] _tpktIso = { // 7 bytes
            0x03,0x00,
            0x00,0x1f,      // Telegram Length (Data Size + 31 or 35)
            0x02,0xf0,0x80  // COTP (see above for info)
        };

        // S7 PDU Negotiation Telegram (contains also ISO Header and COTP Header)
        byte[] _s7Pn = {
            0x03, 0x00, 0x00, 0x19,
            0x02, 0xf0, 0x80, // TPKT + COTP (see above for info)
            0x32, 0x01, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x08,
            0x00, 0x00, 0xf0, 0x00,
            0x00, 0x01, 0x00, 0x01,
            0x00, 0x1e        // PDU Length Requested = HI-LO Here Default 480 bytes
        };

        // S7 Read/Write Request Header (contains also ISO Header and COTP Header)
        byte[] _s7Rw = { // 31-35 bytes
            0x03,0x00,
            0x00,0x1f,       // Telegram Length (Data Size + 31 or 35)
            0x02,0xf0, 0x80, // COTP (see above for info)
            0x32,            // S7 Protocol ID 
            0x01,            // Job Type
            0x00,0x00,       // Redundancy identification
            0x05,0x00,       // PDU Reference
            0x00,0x0e,       // Parameters Length
            0x00,0x00,       // Data Length = Size(bytes) + 4      
            0x04,            // Function 4 Read Var, 5 Write Var  
            0x01,            // Items count
            0x12,            // Var spec.
            0x0a,            // Length of remaining bytes
            0x10,            // Syntax ID 
            (byte)S7Consts.S7WlByte,  // Transport Size idx=22                       
            0x00,0x00,       // Num Elements                          
            0x00,0x00,       // DB Number (if any, else 0)            
            0x84,            // Area Type                            
            0x00,0x00,0x00,  // Area Offset                     
            // WR area
            0x00,            // Reserved 
            0x04,            // Transport size
            0x00,0x00,       // Data Length * 8 (if not bit or timer or counter) 
        };
        private static int _sizeRd = 31; // Header Size when Reading 
        private static int _sizeWr = 35; // Header Size when Writing

        // S7 Variable MultiRead Header
        byte[] _s7MrdHeader = {
            0x03,0x00,
            0x00,0x1f,       // Telegram Length 
            0x02,0xf0, 0x80, // COTP (see above for info)
            0x32,            // S7 Protocol ID 
            0x01,            // Job Type
            0x00,0x00,       // Redundancy identification
            0x05,0x00,       // PDU Reference
            0x00,0x0e,       // Parameters Length
            0x00,0x00,       // Data Length = Size(bytes) + 4      
            0x04,            // Function 4 Read Var, 5 Write Var  
            0x01             // Items count (idx 18)
        };

        // S7 Variable MultiRead Item
        byte[] _s7MrdItem = {
            0x12,            // Var spec.
            0x0a,            // Length of remaining bytes
            0x10,            // Syntax ID 
            (byte)S7Consts.S7WlByte,  // Transport Size idx=3                   
            0x00,0x00,       // Num Elements                          
            0x00,0x00,       // DB Number (if any, else 0)            
            0x84,            // Area Type                            
            0x00,0x00,0x00   // Area Offset                     
        };

        // S7 Variable MultiWrite Header
        byte[] _s7MwrHeader = {
            0x03,0x00,
            0x00,0x1f,       // Telegram Length 
            0x02,0xf0, 0x80, // COTP (see above for info)
            0x32,            // S7 Protocol ID 
            0x01,            // Job Type
            0x00,0x00,       // Redundancy identification
            0x05,0x00,       // PDU Reference
            0x00,0x0e,       // Parameters Length (idx 13)
            0x00,0x00,       // Data Length = Size(bytes) + 4 (idx 15)     
            0x05,            // Function 5 Write Var  
            0x01             // Items count (idx 18)
        };

        // S7 Variable MultiWrite Item (Param)
        byte[] _s7MwrParam = {
            0x12,            // Var spec.
            0x0a,            // Length of remaining bytes
            0x10,            // Syntax ID 
            (byte)S7Consts.S7WlByte,  // Transport Size idx=3                      
            0x00,0x00,       // Num Elements                          
            0x00,0x00,       // DB Number (if any, else 0)            
            0x84,            // Area Type                            
            0x00,0x00,0x00,  // Area Offset                     
        };

        // SZL First telegram request   
        byte[] _s7SzlFirst = {
            0x03, 0x00, 0x00, 0x21,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00,
            0x05, 0x00, // Sequence out
            0x00, 0x08, 0x00,
            0x08, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x44, 0x01,
            0x00, 0xff, 0x09, 0x00,
            0x04,
            0x00, 0x00, // ID (29)
            0x00, 0x00  // Index (31)
        };

        // SZL Next telegram request 
        byte[] _s7SzlNext = {
            0x03, 0x00, 0x00, 0x21,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x06,
            0x00, 0x00, 0x0c, 0x00,
            0x04, 0x00, 0x01, 0x12,
            0x08, 0x12, 0x44, 0x01,
            0x01, // Sequence
            0x00, 0x00, 0x00, 0x00,
            0x0a, 0x00, 0x00, 0x00
        };

        // Get Date/Time request
        byte[] _s7GetDt = {
            0x03, 0x00, 0x00, 0x1d,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x38,
            0x00, 0x00, 0x08, 0x00,
            0x04, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x47, 0x01,
            0x00, 0x0a, 0x00, 0x00,
            0x00
        };

        // Set Date/Time command
        byte[] _s7SetDt = {
            0x03, 0x00, 0x00, 0x27,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x89,
            0x03, 0x00, 0x08, 0x00,
            0x0e, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x47, 0x02,
            0x00, 0xff, 0x09, 0x00,
            0x0a, 0x00,
            0x19, // Hi part of Year (idx=30)
            0x13, // Lo part of Year
            0x12, // Month
            0x06, // Day
            0x17, // Hour
            0x37, // Min
            0x13, // Sec
            0x00, 0x01 // ms + Day of week   
        };

        // S7 Set Session Password 
        byte[] _s7SetPwd = {
            0x03, 0x00, 0x00, 0x25,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x27,
            0x00, 0x00, 0x08, 0x00,
            0x0c, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x45, 0x01,
            0x00, 0xff, 0x09, 0x00,
            0x08, 
            // 8 Char Encoded Password
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        // S7 Clear Session Password 
        byte[] _s7ClrPwd = {
            0x03, 0x00, 0x00, 0x1d,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x29,
            0x00, 0x00, 0x08, 0x00,
            0x04, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x45, 0x02,
            0x00, 0x0a, 0x00, 0x00,
            0x00
        };

        // S7 STOP request
        byte[] _s7Stop = {
            0x03, 0x00, 0x00, 0x21,
            0x02, 0xf0, 0x80, 0x32,
            0x01, 0x00, 0x00, 0x0e,
            0x00, 0x00, 0x10, 0x00,
            0x00, 0x29, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x09,
            0x50, 0x5f, 0x50, 0x52,
            0x4f, 0x47, 0x52, 0x41,
            0x4d
        };

        // S7 HOT Start request
        byte[] _s7HotStart = {
            0x03, 0x00, 0x00, 0x25,
            0x02, 0xf0, 0x80, 0x32,
            0x01, 0x00, 0x00, 0x0c,
            0x00, 0x00, 0x14, 0x00,
            0x00, 0x28, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xfd, 0x00, 0x00, 0x09,
            0x50, 0x5f, 0x50, 0x52,
            0x4f, 0x47, 0x52, 0x41,
            0x4d
        };

        // S7 COLD Start request
        byte[] _s7ColdStart = {
            0x03, 0x00, 0x00, 0x27,
            0x02, 0xf0, 0x80, 0x32,
            0x01, 0x00, 0x00, 0x0f,
            0x00, 0x00, 0x16, 0x00,
            0x00, 0x28, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xfd, 0x00, 0x02, 0x43,
            0x20, 0x09, 0x50, 0x5f,
            0x50, 0x52, 0x4f, 0x47,
            0x52, 0x41, 0x4d
        };
        const byte PduStart = 0x28;   // CPU start
        const byte PduStop = 0x29;   // CPU stop
        const byte PduAlreadyStarted = 0x02;   // CPU already in run mode
        const byte PduAlreadyStopped = 0x07;   // CPU already in stop mode

        // S7 Get PLC Status 
        byte[] _s7GetStat = {
            0x03, 0x00, 0x00, 0x21,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x2c,
            0x00, 0x00, 0x08, 0x00,
            0x08, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x44, 0x01,
            0x00, 0xff, 0x09, 0x00,
            0x04, 0x04, 0x24, 0x00,
            0x00
        };

        // S7 Get Block Info Request Header (contains also ISO Header and COTP Header)
        byte[] _s7Bi = {
            0x03, 0x00, 0x00, 0x25,
            0x02, 0xf0, 0x80, 0x32,
            0x07, 0x00, 0x00, 0x05,
            0x00, 0x00, 0x08, 0x00,
            0x0c, 0x00, 0x01, 0x12,
            0x04, 0x11, 0x43, 0x03,
            0x00, 0xff, 0x09, 0x00,
            0x08, 0x30,
            0x41, // Block Type
            0x30, 0x30, 0x30, 0x30, 0x30, // ASCII Block Number
            0x41
        };

        #endregion

        #region [Internals]

        // Defaults
        private static int _isotcp = 102; // ISOTCP Port
        private static int _minPduSize = 16;
        private static int _minPduSizeToRequest = 240;
        private static int _maxPduSizeToRequest = 960;
        private static int _defaultTimeout = 2000;
        private static int _isoHSize = 7; // TPKT+COTP Header Size

        // Properties
        private int _pduLength = 0;
        private int _pduSizeRequested = 480;
        private int _plcPort = _isotcp;

        // Privates
        private string _ipAddress;
        private byte _localTsapHi;
        private byte _localTsapLo;
        private byte _remoteTsapHi;
        private byte _remoteTsapLo;
        private byte _lastPduType;
        private ushort _connType = ConntypePg;
        private byte[] _pdu = new byte[2048];
        private MsgSocket _socket = null;
        private int _timeMs = 0;

        private void CreateSocket()
        {
            _socket = new MsgSocket();
            _socket.ConnectTimeout = _defaultTimeout;
            _socket.ReadTimeout = _defaultTimeout;
            _socket.WriteTimeout = _defaultTimeout;
        }

        private int TcpConnect()
        {
            if (_LastError == 0)
            {
                try
                {
                    _LastError = _socket.Connect(_ipAddress, _plcPort);
                }
                catch
                {
                    _LastError = S7Consts.ErrTcpConnectionFailed;
                }
            }
            return _LastError;
        }

        private void RecvPacket(byte[] buffer, int start, int size)
        {
            if (Connected)
                _LastError = _socket.Receive(buffer, start, size);
            else
                _LastError = S7Consts.ErrTcpNotConnected;
        }

        private void SendPacket(byte[] buffer, int len)
        {
            if (Connected)
                _LastError = _socket.Send(buffer, len);
            else
                _LastError = S7Consts.ErrTcpNotConnected;
        }

        private void SendPacket(byte[] buffer)
        {
            SendPacket(buffer, buffer.Length);
        }

        private int RecvIsoPacket()
        {
            bool done = false;
            int size = 0;
            while ((_LastError == 0) && !done)
            {
                // Get TPKT (4 bytes)
                RecvPacket(_pdu, 0, 4);
                if (_LastError == 0)
                {
                    size = S7.GetWordAt(_pdu, 2);
                    // Check 0 bytes Data Packet (only TPKT+COTP = 7 bytes)
                    if (size == _isoHSize)
                        RecvPacket(_pdu, 4, 3); // Skip remaining 3 bytes and Done is still false
                    else
                    {
                        if ((size > _pduSizeRequested + _isoHSize) || (size < _minPduSize))
                            _LastError = S7Consts.ErrIsoInvalidPdu;
                        else
                            done = true; // a valid Length !=7 && >16 && <247
                    }
                }
            }
            if (_LastError == 0)
            {
                RecvPacket(_pdu, 4, 3); // Skip remaining 3 COTP bytes
                _lastPduType = _pdu[5];   // Stores PDU Type, we need it 
                // Receives the S7 Payload          
                RecvPacket(_pdu, 7, size - _isoHSize);
            }

            if (_LastError == 0)
            {
                return size;
            }

            return 0;
        }

        private int IsoConnect()
        {
            int size;
            _isoCr[16] = _localTsapHi;
            _isoCr[17] = _localTsapLo;
            _isoCr[20] = _remoteTsapHi;
            _isoCr[21] = _remoteTsapLo;

            // Sends the connection request telegram      
            SendPacket(_isoCr);
            if (_LastError == 0)
            {
                // Gets the reply (if any)
                size = RecvIsoPacket();
                if (_LastError == 0)
                {
                    if (size == 22)
                    {
                        if (_lastPduType != (byte)0xD0) // 0xD0 = CC Connection confirm
                            _LastError = S7Consts.ErrIsoConnect;
                    }
                    else
                        _LastError = S7Consts.ErrIsoInvalidPdu;
                }
            }
            return _LastError;
        }

        private int NegotiatePduLength()
        {
            int length;
            // Set PDU Size Requested
            S7.SetWordAt(_s7Pn, 23, (ushort)_pduSizeRequested);
            // Sends the connection request telegram
            SendPacket(_s7Pn);
            if (_LastError == 0)
            {
                length = RecvIsoPacket();
                if (_LastError == 0)
                {
                    // check S7 Error
                    if ((length == 27) && (_pdu[17] == 0) && (_pdu[18] == 0))  // 20 = size of Negotiate Answer
                    {
                        // Get PDU Size Negotiated
                        _pduLength = S7.GetWordAt(_pdu, 25);
                        if (_pduLength <= 0)
                            _LastError = S7Consts.ErrCliNegotiatingPdu;
                    }
                    else
                        _LastError = S7Consts.ErrCliNegotiatingPdu;
                }
            }
            return _LastError;
        }

        private int CpuError(ushort error)
        {
            switch (error)
            {
                case 0: return 0;
                case Code7AddressOutOfRange: return S7Consts.ErrCliAddressOutOfRange;
                case Code7InvalidTransportSize: return S7Consts.ErrCliInvalidTransportSize;
                case Code7WriteDataSizeMismatch: return S7Consts.ErrCliWriteDataSizeMismatch;
                case Code7ResItemNotAvailable:
                case Code7ResItemNotAvailable1: return S7Consts.ErrCliItemNotAvailable;
                case Code7DataOverPdu: return S7Consts.ErrCliSizeOverPdu;
                case Code7InvalidValue: return S7Consts.ErrCliInvalidValue;
                case Code7FunNotAvailable: return S7Consts.ErrCliFunNotAvailable;
                case Code7NeedPassword: return S7Consts.ErrCliNeedPassword;
                case Code7InvalidPassword: return S7Consts.ErrCliInvalidPassword;
                case Code7NoPasswordToSet:
                case Code7NoPasswordToClear: return S7Consts.ErrCliNoPasswordToSetOrClear;
                default:
                    return S7Consts.ErrCliFunctionRefused;
            };
        }

        #endregion

        #region [Class Control]

        public S7Client()
        {
            CreateSocket();
        }

        ~S7Client()
        {
            Disconnect();
        }

        public int Connect()
        {
            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;
            if (!Connected)
            {
                TcpConnect(); // First stage : TCP Connection
                if (_LastError == 0)
                {
                    IsoConnect(); // Second stage : ISOTCP (ISO 8073) Connection
                    if (_LastError == 0)
                    {
                        _LastError = NegotiatePduLength(); // Third stage : S7 PDU negotiation
                    }
                }
            }
            if (_LastError != 0)
                Disconnect();
            else
                _timeMs = Environment.TickCount - elapsed;

            return _LastError;
        }

        public int ConnectTo(string address, int rack, int slot)
        {
            ushort remoteTsap = (ushort)((_connType << 8) + (rack * 0x20) + slot);
            SetConnectionParams(address, 0x0100, remoteTsap);
            return Connect();
        }

        public int SetConnectionParams(string address, ushort localTsap, ushort remoteTsap)
        {
            int locTsap = localTsap & 0x0000FFFF;
            int remTsap = remoteTsap & 0x0000FFFF;
            _ipAddress = address;
            _localTsapHi = (byte)(locTsap >> 8);
            _localTsapLo = (byte)(locTsap & 0x00FF);
            _remoteTsapHi = (byte)(remTsap >> 8);
            _remoteTsapLo = (byte)(remTsap & 0x00FF);
            return 0;
        }

        public int SetConnectionType(ushort connectionType)
        {
            _connType = connectionType;
            return 0;
        }

        public int Disconnect()
        {
            _socket.Close();
            return 0;
        }

        public int GetParam(int paramNumber, ref int value)
        {
            int result = 0;
            switch (paramNumber)
            {
                case S7Consts.PU16RemotePort:
                {
                    value = PlcPort;
                    break;
                }
                case S7Consts.PI32PingTimeout:
                {
                    value = ConnTimeout;
                    break;
                }
                case S7Consts.PI32SendTimeout:
                {
                    value = SendTimeout;
                    break;
                }
                case S7Consts.PI32RecvTimeout:
                {
                    value = RecvTimeout;
                    break;
                }
                case S7Consts.PI32PduRequest:
                {
                    value = PduSizeRequested;
                    break;
                }
                default:
                {
                    result = S7Consts.ErrCliInvalidParamNumber;
                    break;
                }
            }
            return result;
        }

        // Set Properties for compatibility with Snap7.net.cs
        public int SetParam(int paramNumber, ref int value)
        {
            int result = 0;
            switch (paramNumber)
            {
                case S7Consts.PU16RemotePort:
                {
                    PlcPort = value;
                    break;
                }
                case S7Consts.PI32PingTimeout:
                {
                    ConnTimeout = value;
                    break;
                }
                case S7Consts.PI32SendTimeout:
                {
                    SendTimeout = value;
                    break;
                }
                case S7Consts.PI32RecvTimeout:
                {
                    RecvTimeout = value;
                    break;
                }
                case S7Consts.PI32PduRequest:
                {
                    PduSizeRequested = value;
                    break;
                }
                default:
                {
                    result = S7Consts.ErrCliInvalidParamNumber;
                    break;
                }
            }
            return result;
        }

        public delegate void S7CliCompletion(IntPtr usrPtr, int opCode, int opResult);
        public int SetAsCallBack(S7CliCompletion completion, IntPtr usrPtr)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        #endregion

        #region [Data I/O main functions]

        public int ReadArea(int area, int dbNumber, int start, int amount, int wordLen, byte[] buffer)
        {
            int bytesRead = 0;
            return ReadArea(area, dbNumber, start, amount, wordLen, buffer, ref bytesRead);
        }

        public int ReadArea(int area, int dbNumber, int start, int amount, int wordLen, byte[] buffer, ref int bytesRead)
        {
            int address;
            int numElements;
            int maxElements;
            int totElements;
            int sizeRequested;
            int length;
            int offset = 0;
            int wordSize = 1;

            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;
            // Some adjustment
            if (area == S7Consts.S7AreaC)
                wordLen = S7Consts.S7WlCounter;
            if (area == S7Consts.S7AreaT)
                wordLen = S7Consts.S7WlTimer;

            // Calc Word size          
            wordSize = S7.DataSizeByte(wordLen);
            if (wordSize == 0)
                return S7Consts.ErrCliInvalidWordLen;

            if (wordLen == S7Consts.S7WlBit)
                amount = 1;  // Only 1 bit can be transferred at time
            else
            {
                if ((wordLen != S7Consts.S7WlCounter) && (wordLen != S7Consts.S7WlTimer))
                {
                    amount = amount * wordSize;
                    wordSize = 1;
                    wordLen = S7Consts.S7WlByte;
                }
            }

            maxElements = (_pduLength - 18) / wordSize; // 18 = Reply telegram header
            totElements = amount;

            while ((totElements > 0) && (_LastError == 0))
            {
                numElements = totElements;
                if (numElements > maxElements)
                    numElements = maxElements;

                sizeRequested = numElements * wordSize;

                // Setup the telegram
                Array.Copy(_s7Rw, 0, _pdu, 0, _sizeRd);
                // Set DB Number
                _pdu[27] = (byte)area;
                // Set Area
                if (area == S7Consts.S7AreaDb)
                    S7.SetWordAt(_pdu, 25, (ushort)dbNumber);

                // Adjusts Start and word length
                if ((wordLen == S7Consts.S7WlBit) || (wordLen == S7Consts.S7WlCounter) || (wordLen == S7Consts.S7WlTimer))
                {
                    address = start;
                    _pdu[22] = (byte)wordLen;
                }
                else
                    address = start << 3;

                // Num elements
                S7.SetWordAt(_pdu, 23, (ushort)numElements);

                // Address into the PLC (only 3 bytes)           
                _pdu[30] = (byte)(address & 0x0FF);
                address = address >> 8;
                _pdu[29] = (byte)(address & 0x0FF);
                address = address >> 8;
                _pdu[28] = (byte)(address & 0x0FF);

                SendPacket(_pdu, _sizeRd);
                if (_LastError == 0)
                {
                    length = RecvIsoPacket();
                    if (_LastError == 0)
                    {
                        if (length < 25)
                            _LastError = S7Consts.ErrIsoInvalidDataSize;
                        else
                        {
                            if (_pdu[21] != 0xFF)
                                _LastError = CpuError(_pdu[21]);
                            else
                            {
                                Array.Copy(_pdu, 25, buffer, offset, sizeRequested);
                                offset += sizeRequested;
                            }
                        }
                    }
                }
                totElements -= numElements;
                start += numElements * wordSize;
            }

            if (_LastError == 0)
            {
                bytesRead = offset;
                _timeMs = Environment.TickCount - elapsed;
            }
            else
                bytesRead = 0;
            return _LastError;
        }

        public int WriteArea(int area, int dbNumber, int start, int amount, int wordLen, byte[] buffer)
        {
            int bytesWritten = 0;
            return WriteArea(area, dbNumber, start, amount, wordLen, buffer, ref bytesWritten);
        }

        public int WriteArea(int area, int dbNumber, int start, int amount, int wordLen, byte[] buffer, ref int bytesWritten)
        {
            int address;
            int numElements;
            int maxElements;
            int totElements;
            int dataSize;
            int isoSize;
            int length;
            int offset = 0;
            int wordSize = 1;

            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;
            // Some adjustment
            if (area == S7Consts.S7AreaC)
                wordLen = S7Consts.S7WlCounter;
            if (area == S7Consts.S7AreaT)
                wordLen = S7Consts.S7WlTimer;

            // Calc Word size          
            wordSize = S7.DataSizeByte(wordLen);
            if (wordSize == 0)
                return S7Consts.ErrCliInvalidWordLen;

            if (wordLen == S7Consts.S7WlBit) // Only 1 bit can be transferred at time
                amount = 1;
            else
            {
                if ((wordLen != S7Consts.S7WlCounter) && (wordLen != S7Consts.S7WlTimer))
                {
                    amount = amount * wordSize;
                    wordSize = 1;
                    wordLen = S7Consts.S7WlByte;
                }
            }

            maxElements = (_pduLength - 35) / wordSize; // 35 = Reply telegram header
            totElements = amount;

            while ((totElements > 0) && (_LastError == 0))
            {
                numElements = totElements;
                if (numElements > maxElements)
                    numElements = maxElements;

                dataSize = numElements * wordSize;
                isoSize = _sizeWr + dataSize;

                // Setup the telegram
                Array.Copy(_s7Rw, 0, _pdu, 0, _sizeWr);
                // Whole telegram Size
                S7.SetWordAt(_pdu, 2, (ushort)isoSize);
                // Data Length
                length = dataSize + 4;
                S7.SetWordAt(_pdu, 15, (ushort)length);
                // Function
                _pdu[17] = (byte)0x05;
                // Set DB Number
                _pdu[27] = (byte)area;
                if (area == S7Consts.S7AreaDb)
                    S7.SetWordAt(_pdu, 25, (ushort)dbNumber);


                // Adjusts Start and word length
                if ((wordLen == S7Consts.S7WlBit) || (wordLen == S7Consts.S7WlCounter) || (wordLen == S7Consts.S7WlTimer))
                {
                    address = start;
                    length = dataSize;
                    _pdu[22] = (byte)wordLen;
                }
                else
                {
                    address = start << 3;
                    length = dataSize << 3;
                }

                // Num elements
                S7.SetWordAt(_pdu, 23, (ushort)numElements);
                // Address into the PLC
                _pdu[30] = (byte)(address & 0x0FF);
                address = address >> 8;
                _pdu[29] = (byte)(address & 0x0FF);
                address = address >> 8;
                _pdu[28] = (byte)(address & 0x0FF);

                // Transport Size
                switch (wordLen)
                {
                    case S7Consts.S7WlBit:
                        _pdu[32] = TsResBit;
                        break;
                    case S7Consts.S7WlCounter:
                    case S7Consts.S7WlTimer:
                        _pdu[32] = TsResOctet;
                        break;
                    default:
                        _pdu[32] = TsResByte; // byte/word/dword etc.
                        break;
                };
                // Length
                S7.SetWordAt(_pdu, 33, (ushort)length);

                // Copies the Data
                Array.Copy(buffer, offset, _pdu, 35, dataSize);

                SendPacket(_pdu, isoSize);
                if (_LastError == 0)
                {
                    length = RecvIsoPacket();
                    if (_LastError == 0)
                    {
                        if (length == 22)
                        {
                            if (_pdu[21] != (byte)0xFF)
                                _LastError = CpuError(_pdu[21]);
                        }
                        else
                            _LastError = S7Consts.ErrIsoInvalidPdu;
                    }
                }
                offset += dataSize;
                totElements -= numElements;
                start += numElements * wordSize;
            }

            if (_LastError == 0)
            {
                bytesWritten = offset;
                _timeMs = Environment.TickCount - elapsed;
            }
            else
                bytesWritten = 0;

            return _LastError;
        }

        public int ReadMultiVars(S7DataItem[] items, int itemsCount)
        {
            int offset;
            int length;
            int itemSize;
            byte[] s7Item = new byte[12];
            byte[] s7ItemRead = new byte[1024];

            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;

            // Checks items
            if (itemsCount > MaxVars)
                return S7Consts.ErrCliTooManyItems;

            // Fills Header
            Array.Copy(_s7MrdHeader, 0, _pdu, 0, _s7MrdHeader.Length);
            S7.SetWordAt(_pdu, 13, (ushort)(itemsCount * s7Item.Length + 2));
            _pdu[18] = (byte)itemsCount;
            // Fills the Items
            offset = 19;
            for (int c = 0; c < itemsCount; c++)
            {
                Array.Copy(_s7MrdItem, s7Item, s7Item.Length);
                s7Item[3] = (byte)items[c].WordLen;
                S7.SetWordAt(s7Item, 4, (ushort)items[c].Amount);
                if (items[c].Area == S7Consts.S7AreaDb)
                    S7.SetWordAt(s7Item, 6, (ushort)items[c].DbNumber);
                s7Item[8] = (byte)items[c].Area;

                // Address into the PLC
                int address = items[c].Start;
                s7Item[11] = (byte)(address & 0x0FF);
                address = address >> 8;
                s7Item[10] = (byte)(address & 0x0FF);
                address = address >> 8;
                s7Item[09] = (byte)(address & 0x0FF);

                Array.Copy(s7Item, 0, _pdu, offset, s7Item.Length);
                offset += s7Item.Length;
            }

            if (offset > _pduLength)
                return S7Consts.ErrCliSizeOverPdu;

            S7.SetWordAt(_pdu, 2, (ushort)offset); // Whole size
            SendPacket(_pdu, offset);

            if (_LastError != 0)
                return _LastError;
            // Get Answer
            length = RecvIsoPacket();
            if (_LastError != 0)
                return _LastError;
            // Check ISO Length
            if (length < 22)
            {
                _LastError = S7Consts.ErrIsoInvalidPdu; // PDU too Small
                return _LastError;
            }
            // Check Global Operation Result
            _LastError = CpuError(S7.GetWordAt(_pdu, 17));
            if (_LastError != 0)
                return _LastError;
            // Get true ItemsCount
            int itemsRead = S7.GetByteAt(_pdu, 20);
            if ((itemsRead != itemsCount) || (itemsRead > MaxVars))
            {
                _LastError = S7Consts.ErrCliInvalidPlcAnswer;
                return _LastError;
            }
            // Get Data
            offset = 21;
            for (int c = 0; c < itemsCount; c++)
            {
                // Get the Item
                Array.Copy(_pdu, offset, s7ItemRead, 0, length - offset);
                if (s7ItemRead[0] == 0xff)
                {
                    itemSize = (int)S7.GetWordAt(s7ItemRead, 2);
                    if ((s7ItemRead[1] != TsResOctet) && (s7ItemRead[1] != TsResReal) && (s7ItemRead[1] != TsResBit))
                        itemSize = itemSize >> 3;
                    Marshal.Copy(s7ItemRead, 4, items[c].PData, itemSize);
                    items[c].Result = 0;
                    if (itemSize % 2 != 0)
                        itemSize++; // Odd size are rounded
                    offset = offset + 4 + itemSize;
                }
                else
                {
                    items[c].Result = CpuError(s7ItemRead[0]);
                    offset += 4; // Skip the Item header                           
                }
            }
            _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int WriteMultiVars(S7DataItem[] items, int itemsCount)
        {
            int offset;
            int parLength;
            int dataLength;
            int itemDataSize;
            byte[] s7ParItem = new byte[_s7MwrParam.Length];
            byte[] s7DataItem = new byte[1024];

            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;

            // Checks items
            if (itemsCount > MaxVars)
                return S7Consts.ErrCliTooManyItems;
            // Fills Header
            Array.Copy(_s7MwrHeader, 0, _pdu, 0, _s7MwrHeader.Length);
            parLength = itemsCount * _s7MwrParam.Length + 2;
            S7.SetWordAt(_pdu, 13, (ushort)parLength);
            _pdu[18] = (byte)itemsCount;
            // Fills Params
            offset = _s7MwrHeader.Length;
            for (int c = 0; c < itemsCount; c++)
            {
                Array.Copy(_s7MwrParam, 0, s7ParItem, 0, _s7MwrParam.Length);
                s7ParItem[3] = (byte)items[c].WordLen;
                s7ParItem[8] = (byte)items[c].Area;
                S7.SetWordAt(s7ParItem, 4, (ushort)items[c].Amount);
                S7.SetWordAt(s7ParItem, 6, (ushort)items[c].DbNumber);
                // Address into the PLC
                int address = items[c].Start;
                s7ParItem[11] = (byte)(address & 0x0FF);
                address = address >> 8;
                s7ParItem[10] = (byte)(address & 0x0FF);
                address = address >> 8;
                s7ParItem[09] = (byte)(address & 0x0FF);
                Array.Copy(s7ParItem, 0, _pdu, offset, s7ParItem.Length);
                offset += _s7MwrParam.Length;
            }
            // Fills Data
            dataLength = 0;
            for (int c = 0; c < itemsCount; c++)
            {
                s7DataItem[0] = 0x00;
                switch (items[c].WordLen)
                {
                    case S7Consts.S7WlBit:
                        s7DataItem[1] = TsResBit;
                        break;
                    case S7Consts.S7WlCounter:
                    case S7Consts.S7WlTimer:
                        s7DataItem[1] = TsResOctet;
                        break;
                    default:
                        s7DataItem[1] = TsResByte; // byte/word/dword etc.
                        break;
                };
                if ((items[c].WordLen == S7Consts.S7WlTimer) || (items[c].WordLen == S7Consts.S7WlCounter))
                    itemDataSize = items[c].Amount * 2;
                else
                    itemDataSize = items[c].Amount;

                if ((s7DataItem[1] != TsResOctet) && (s7DataItem[1] != TsResBit))
                    S7.SetWordAt(s7DataItem, 2, (ushort)(itemDataSize * 8));
                else
                    S7.SetWordAt(s7DataItem, 2, (ushort)itemDataSize);

                Marshal.Copy(items[c].PData, s7DataItem, 4, itemDataSize);
                if (itemDataSize % 2 != 0)
                {
                    s7DataItem[itemDataSize + 4] = 0x00;
                    itemDataSize++;
                }
                Array.Copy(s7DataItem, 0, _pdu, offset, itemDataSize + 4);
                offset = offset + itemDataSize + 4;
                dataLength = dataLength + itemDataSize + 4;
            }

            // Checks the size
            if (offset > _pduLength)
                return S7Consts.ErrCliSizeOverPdu;

            S7.SetWordAt(_pdu, 2, (ushort)offset); // Whole size
            S7.SetWordAt(_pdu, 15, (ushort)dataLength); // Whole size
            SendPacket(_pdu, offset);

            RecvIsoPacket();
            if (_LastError == 0)
            {
                // Check Global Operation Result
                _LastError = CpuError(S7.GetWordAt(_pdu, 17));
                if (_LastError != 0)
                    return _LastError;
                // Get true ItemsCount
                int itemsWritten = S7.GetByteAt(_pdu, 20);
                if ((itemsWritten != itemsCount) || (itemsWritten > MaxVars))
                {
                    _LastError = S7Consts.ErrCliInvalidPlcAnswer;
                    return _LastError;
                }

                for (int c = 0; c < itemsCount; c++)
                {
                    if (_pdu[c + 21] == 0xFF)
                        items[c].Result = 0;
                    else
                        items[c].Result = CpuError((ushort)_pdu[c + 21]);
                }
                _timeMs = Environment.TickCount - elapsed;
            }
            return _LastError;
        }

        #endregion

        #region [Data I/O lean functions]

        public int DbRead(int dbNumber, int start, int size, byte[] buffer)
        {
            return ReadArea(S7Consts.S7AreaDb, dbNumber, start, size, S7Consts.S7WlByte, buffer);
        }

        public int DbWrite(int dbNumber, int start, int size, byte[] buffer)
        {
            return WriteArea(S7Consts.S7AreaDb, dbNumber, start, size, S7Consts.S7WlByte, buffer);
        }

        public int MbRead(int start, int size, byte[] buffer)
        {
            return ReadArea(S7Consts.S7AreaM, 0, start, size, S7Consts.S7WlByte, buffer);
        }

        public int MbWrite(int start, int size, byte[] buffer)
        {
            return WriteArea(S7Consts.S7AreaM, 0, start, size, S7Consts.S7WlByte, buffer);
        }

        public int EbRead(int start, int size, byte[] buffer)
        {
            return ReadArea(S7Consts.S7AreaI, 0, start, size, S7Consts.S7WlByte, buffer);
        }

        public int EbWrite(int start, int size, byte[] buffer)
        {
            return WriteArea(S7Consts.S7AreaI, 0, start, size, S7Consts.S7WlByte, buffer);
        }

        public int AbRead(int start, int size, byte[] buffer)
        {
            return ReadArea(S7Consts.S7AreaQ, 0, start, size, S7Consts.S7WlByte, buffer);
        }

        public int AbWrite(int start, int size, byte[] buffer)
        {
            return WriteArea(S7Consts.S7AreaQ, 0, start, size, S7Consts.S7WlByte, buffer);
        }

        public int TmRead(int start, int amount, ushort[] buffer)
        {
            byte[] sBuffer = new byte[amount * 2];
            int result = ReadArea(S7Consts.S7AreaT, 0, start, amount, S7Consts.S7WlTimer, sBuffer);
            if (result == 0)
            {
                for (int c = 0; c < amount; c++)
                {
                    buffer[c] = (ushort)((sBuffer[c * 2 + 1] << 8) + (sBuffer[c * 2]));
                }
            }
            return result;
        }

        public int TmWrite(int start, int amount, ushort[] buffer)
        {
            byte[] sBuffer = new byte[amount * 2];
            for (int c = 0; c < amount; c++)
            {
                sBuffer[c * 2 + 1] = (byte)((buffer[c] & 0xFF00) >> 8);
                sBuffer[c * 2] = (byte)(buffer[c] & 0x00FF);
            }
            return WriteArea(S7Consts.S7AreaT, 0, start, amount, S7Consts.S7WlTimer, sBuffer);
        }

        public int CtRead(int start, int amount, ushort[] buffer)
        {
            byte[] sBuffer = new byte[amount * 2];
            int result = ReadArea(S7Consts.S7AreaC, 0, start, amount, S7Consts.S7WlCounter, sBuffer);
            if (result == 0)
            {
                for (int c = 0; c < amount; c++)
                {
                    buffer[c] = (ushort)((sBuffer[c * 2 + 1] << 8) + (sBuffer[c * 2]));
                }
            }
            return result;
        }

        public int CtWrite(int start, int amount, ushort[] buffer)
        {
            byte[] sBuffer = new byte[amount * 2];
            for (int c = 0; c < amount; c++)
            {
                sBuffer[c * 2 + 1] = (byte)((buffer[c] & 0xFF00) >> 8);
                sBuffer[c * 2] = (byte)(buffer[c] & 0x00FF);
            }
            return WriteArea(S7Consts.S7AreaC, 0, start, amount, S7Consts.S7WlCounter, sBuffer);
        }

        #endregion

        #region [Directory functions]

        public int ListBlocks(ref S7BlocksList list)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        private string SiemensTimestamp(long encodedDate)
        {
            DateTime dt = new DateTime(1984, 1, 1).AddSeconds(encodedDate * 86400);
#if WINDOWS_UWP || NETFX_CORE || CORE_CLR
            return DT.ToString(System.Globalization.DateTimeFormatInfo.CurrentInfo.ShortDatePattern);
#else
            return dt.ToShortDateString();
#endif
        }

        public int GetAgBlockInfo(int blockType, int blockNum, ref S7BlockInfo info)
        {
            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;

            _s7Bi[30] = (byte)blockType;
            // Block Number
            _s7Bi[31] = (byte)((blockNum / 10000) + 0x30);
            blockNum = blockNum % 10000;
            _s7Bi[32] = (byte)((blockNum / 1000) + 0x30);
            blockNum = blockNum % 1000;
            _s7Bi[33] = (byte)((blockNum / 100) + 0x30);
            blockNum = blockNum % 100;
            _s7Bi[34] = (byte)((blockNum / 10) + 0x30);
            blockNum = blockNum % 10;
            _s7Bi[35] = (byte)((blockNum / 1) + 0x30);

            SendPacket(_s7Bi);

            if (_LastError == 0)
            {
                int length = RecvIsoPacket();
                if (length > 32) // the minimum expected
                {
                    ushort result = S7.GetWordAt(_pdu, 27);
                    if (result == 0)
                    {
                        info.BlkFlags = _pdu[42];
                        info.BlkLang = _pdu[43];
                        info.BlkType = _pdu[44];
                        info.BlkNumber = S7.GetWordAt(_pdu, 45);
                        info.LoadSize = S7.GetDIntAt(_pdu, 47);
                        info.CodeDate = SiemensTimestamp(S7.GetWordAt(_pdu, 59));
                        info.IntfDate = SiemensTimestamp(S7.GetWordAt(_pdu, 65));
                        info.SbbLength = S7.GetWordAt(_pdu, 67);
                        info.LocalData = S7.GetWordAt(_pdu, 71);
                        info.Mc7Size = S7.GetWordAt(_pdu, 73);
                        info.Author = S7.GetCharsAt(_pdu, 75, 8).Trim(new char[] { (char)0 });
                        info.Family = S7.GetCharsAt(_pdu, 83, 8).Trim(new char[] { (char)0 });
                        info.Header = S7.GetCharsAt(_pdu, 91, 8).Trim(new char[] { (char)0 });
                        info.Version = _pdu[99];
                        info.CheckSum = S7.GetWordAt(_pdu, 101);
                    }
                    else
                        _LastError = CpuError(result);
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;

            return _LastError;

        }

        public int GetPgBlockInfo(ref S7BlockInfo info, byte[] buffer, int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int ListBlocksOfType(int blockType, ushort[] list, ref int itemsCount)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        #endregion

        #region [Blocks functions]

        public int Upload(int blockType, int blockNum, byte[] usrData, ref int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int FullUpload(int blockType, int blockNum, byte[] usrData, ref int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int Download(int blockNum, byte[] usrData, int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int Delete(int blockType, int blockNum)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int DbGet(int dbNumber, byte[] usrData, ref int size)
        {
            S7BlockInfo bi = new S7BlockInfo();
            int elapsed = Environment.TickCount;
            _timeMs = 0;

            _LastError = GetAgBlockInfo(BlockDb, dbNumber, ref bi);

            if (_LastError == 0)
            {
                int dbSize = bi.Mc7Size;
                if (dbSize <= usrData.Length)
                {
                    size = dbSize;
                    _LastError = DbRead(dbNumber, 0, dbSize, usrData);
                    if (_LastError == 0)
                        size = dbSize;
                }
                else
                    _LastError = S7Consts.ErrCliBufferTooSmall;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int DbFill(int dbNumber, int fillChar)
        {
            S7BlockInfo bi = new S7BlockInfo();
            int elapsed = Environment.TickCount;
            _timeMs = 0;

            _LastError = GetAgBlockInfo(BlockDb, dbNumber, ref bi);

            if (_LastError == 0)
            {
                byte[] buffer = new byte[bi.Mc7Size];
                for (int c = 0; c < bi.Mc7Size; c++)
                    buffer[c] = (byte)fillChar;
                _LastError = DbWrite(dbNumber, 0, bi.Mc7Size, buffer);
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        #endregion

        #region [Date/Time functions]

        public int GetPlcDateTime(ref DateTime dt)
        {
            int length;
            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;

            SendPacket(_s7GetDt);
            if (_LastError == 0)
            {
                length = RecvIsoPacket();
                if (length > 30) // the minimum expected
                {
                    if ((S7.GetWordAt(_pdu, 27) == 0) && (_pdu[29] == 0xFF))
                    {
                        dt = S7.GetDateTimeAt(_pdu, 35);
                    }
                    else
                        _LastError = S7Consts.ErrCliInvalidPlcAnswer;
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }

            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;

            return _LastError;
        }

        public int SetPlcDateTime(DateTime dt)
        {
            int length;
            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;

            S7.SetDateTimeAt(_s7SetDt, 31, dt);
            SendPacket(_s7SetDt);
            if (_LastError == 0)
            {
                length = RecvIsoPacket();
                if (length > 30) // the minimum expected
                {
                    if (S7.GetWordAt(_pdu, 27) != 0)
                        _LastError = S7Consts.ErrCliInvalidPlcAnswer;
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;

            return _LastError;
        }

        public int SetPlcSystemDateTime()
        {
            return SetPlcDateTime(DateTime.Now);
        }

        #endregion

        #region [System Info functions]

        public int GetOrderCode(ref S7OrderCode info)
        {
            S7Szl szl = new S7Szl();
            int size = 1024;
            szl.Data = new byte[size];
            int elapsed = Environment.TickCount;
            _LastError = ReadSzl(0x0011, 0x000, ref szl, ref size);
            if (_LastError == 0)
            {
                info.Code = S7.GetCharsAt(szl.Data, 2, 20);
                info.V1 = szl.Data[size - 3];
                info.V2 = szl.Data[size - 2];
                info.V3 = szl.Data[size - 1];
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int GetCpuInfo(ref S7CpuInfo info)
        {
            S7Szl szl = new S7Szl();
            int size = 1024;
            szl.Data = new byte[size];
            int elapsed = Environment.TickCount;
            _LastError = ReadSzl(0x001C, 0x000, ref szl, ref size);
            if (_LastError == 0)
            {
                info.ModuleTypeName = S7.GetCharsAt(szl.Data, 172, 32);
                info.SerialNumber = S7.GetCharsAt(szl.Data, 138, 24);
                info.AsName = S7.GetCharsAt(szl.Data, 2, 24);
                info.Copyright = S7.GetCharsAt(szl.Data, 104, 26);
                info.ModuleName = S7.GetCharsAt(szl.Data, 36, 24);
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int GetCpInfo(ref S7CpInfo info)
        {
            S7Szl szl = new S7Szl();
            int size = 1024;
            szl.Data = new byte[size];
            int elapsed = Environment.TickCount;
            _LastError = ReadSzl(0x0131, 0x001, ref szl, ref size);
            if (_LastError == 0)
            {
                info.MaxPduLength = S7.GetIntAt(_pdu, 2);
                info.MaxConnections = S7.GetIntAt(_pdu, 4);
                info.MaxMpiRate = S7.GetDIntAt(_pdu, 6);
                info.MaxBusRate = S7.GetDIntAt(_pdu, 10);
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int ReadSzl(int id, int index, ref S7Szl szl, ref int size)
        {
            int length;
            int dataSzl;
            int offset = 0;
            bool done = false;
            bool first = true;
            byte seqIn = 0x00;
            ushort seqOut = 0x0000;

            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;
            szl.Header.LENTHDR = 0;

            do
            {
                if (first)
                {
                    S7.SetWordAt(_s7SzlFirst, 11, ++seqOut);
                    S7.SetWordAt(_s7SzlFirst, 29, (ushort)id);
                    S7.SetWordAt(_s7SzlFirst, 31, (ushort)index);
                    SendPacket(_s7SzlFirst);
                }
                else
                {
                    S7.SetWordAt(_s7SzlNext, 11, ++seqOut);
                    _pdu[24] = (byte)seqIn;
                    SendPacket(_s7SzlNext);
                }
                if (_LastError != 0)
                    return _LastError;

                length = RecvIsoPacket();
                if (_LastError == 0)
                {
                    if (first)
                    {
                        if (length > 32) // the minimum expected
                        {
                            if ((S7.GetWordAt(_pdu, 27) == 0) && (_pdu[29] == (byte)0xFF))
                            {
                                // Gets Amount of this slice
                                dataSzl = S7.GetWordAt(_pdu, 31) - 8; // Skips extra params (ID, Index ...)
                                done = _pdu[26] == 0x00;
                                seqIn = (byte)_pdu[24]; // Slice sequence
                                szl.Header.LENTHDR = S7.GetWordAt(_pdu, 37);
                                szl.Header.N_DR = S7.GetWordAt(_pdu, 39);
                                Array.Copy(_pdu, 41, szl.Data, offset, dataSzl);
                                //                                SZL.Copy(PDU, 41, Offset, DataSZL);
                                offset += dataSzl;
                                szl.Header.LENTHDR += szl.Header.LENTHDR;
                            }
                            else
                                _LastError = S7Consts.ErrCliInvalidPlcAnswer;
                        }
                        else
                            _LastError = S7Consts.ErrIsoInvalidPdu;
                    }
                    else
                    {
                        if (length > 32) // the minimum expected
                        {
                            if ((S7.GetWordAt(_pdu, 27) == 0) && (_pdu[29] == (byte)0xFF))
                            {
                                // Gets Amount of this slice
                                dataSzl = S7.GetWordAt(_pdu, 31);
                                done = _pdu[26] == 0x00;
                                seqIn = (byte)_pdu[24]; // Slice sequence
                                Array.Copy(_pdu, 37, szl.Data, offset, dataSzl);
                                offset += dataSzl;
                                szl.Header.LENTHDR += szl.Header.LENTHDR;
                            }
                            else
                                _LastError = S7Consts.ErrCliInvalidPlcAnswer;
                        }
                        else
                            _LastError = S7Consts.ErrIsoInvalidPdu;
                    }
                }
                first = false;
            }
            while (!done && (_LastError == 0));
            if (_LastError == 0)
            {
                size = szl.Header.LENTHDR;
                _timeMs = Environment.TickCount - elapsed;
            }
            return _LastError;
        }

        public int ReadSzlList(ref S7SzlList list, ref int itemsCount)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        #endregion

        #region [Control functions]

        public int PlcHotStart()
        {
            _LastError = 0;
            int elapsed = Environment.TickCount;

            SendPacket(_s7HotStart);
            if (_LastError == 0)
            {
                int length = RecvIsoPacket();
                if (length > 18) // 18 is the minimum expected
                {
                    if (_pdu[19] != PduStart)
                        _LastError = S7Consts.ErrCliCannotStartPlc;
                    else
                    {
                        if (_pdu[20] == PduAlreadyStarted)
                            _LastError = S7Consts.ErrCliAlreadyRun;
                        else
                            _LastError = S7Consts.ErrCliCannotStartPlc;
                    }
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int PlcColdStart()
        {
            _LastError = 0;
            int elapsed = Environment.TickCount;

            SendPacket(_s7ColdStart);
            if (_LastError == 0)
            {
                int length = RecvIsoPacket();
                if (length > 18) // 18 is the minimum expected
                {
                    if (_pdu[19] != PduStart)
                        _LastError = S7Consts.ErrCliCannotStartPlc;
                    else
                    {
                        if (_pdu[20] == PduAlreadyStarted)
                            _LastError = S7Consts.ErrCliAlreadyRun;
                        else
                            _LastError = S7Consts.ErrCliCannotStartPlc;
                    }
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int PlcStop()
        {
            _LastError = 0;
            int elapsed = Environment.TickCount;

            SendPacket(_s7Stop);
            if (_LastError == 0)
            {
                int length = RecvIsoPacket();
                if (length > 18) // 18 is the minimum expected
                {
                    if (_pdu[19] != PduStop)
                        _LastError = S7Consts.ErrCliCannotStopPlc;
                    else
                    {
                        if (_pdu[20] == PduAlreadyStopped)
                            _LastError = S7Consts.ErrCliAlreadyStop;
                        else
                            _LastError = S7Consts.ErrCliCannotStopPlc;
                    }
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int PlcCopyRamToRom(uint timeout)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int PlcCompress(uint timeout)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int PlcGetStatus(ref int status)
        {
            _LastError = 0;
            int elapsed = Environment.TickCount;

            SendPacket(_s7GetStat);
            if (_LastError == 0)
            {
                int length = RecvIsoPacket();
                if (length > 30) // the minimum expected
                {
                    ushort result = S7.GetWordAt(_pdu, 27);
                    if (result == 0)
                    {
                        switch (_pdu[44])
                        {
                            case S7Consts.S7CpuStatusUnknown:
                            case S7Consts.S7CpuStatusRun:
                            case S7Consts.S7CpuStatusStop:
                            {
                                status = _pdu[44];
                                break;
                            }
                            default:
                            {
                                // Since RUN status is always 0x08 for all CPUs and CPs, STOP status
                                // sometime can be coded as 0x03 (especially for old cpu...)
                                status = S7Consts.S7CpuStatusStop;
                                break;
                            }
                        }
                    }
                    else
                        _LastError = CpuError(result);
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        #endregion

        #region [Security functions]
        public int SetSessionPassword(string password)
        {
            byte[] pwd = { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            int length;
            _LastError = 0;
            int elapsed = Environment.TickCount;
            // Encodes the Password
            S7.SetCharsAt(pwd, 0, password);
            pwd[0] = (byte)(pwd[0] ^ 0x55);
            pwd[1] = (byte)(pwd[1] ^ 0x55);
            for (int c = 2; c < 8; c++)
            {
                pwd[c] = (byte)(pwd[c] ^ 0x55 ^ pwd[c - 2]);
            }
            Array.Copy(pwd, 0, _s7SetPwd, 29, 8);
            // Sends the telegrem
            SendPacket(_s7SetPwd);
            if (_LastError == 0)
            {
                length = RecvIsoPacket();
                if (length > 32) // the minimum expected
                {
                    ushort result = S7.GetWordAt(_pdu, 27);
                    if (result != 0)
                        _LastError = CpuError(result);
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            return _LastError;
        }

        public int ClearSessionPassword()
        {
            int length;
            _LastError = 0;
            int elapsed = Environment.TickCount;
            SendPacket(_s7ClrPwd);
            if (_LastError == 0)
            {
                length = RecvIsoPacket();
                if (length > 30) // the minimum expected
                {
                    ushort result = S7.GetWordAt(_pdu, 27);
                    if (result != 0)
                        _LastError = CpuError(result);
                }
                else
                    _LastError = S7Consts.ErrIsoInvalidPdu;
            }
            return _LastError;
        }

        public int GetProtection(ref S7Protection protection)
        {
            S7Client.S7Szl szl = new S7Client.S7Szl();
            int size = 256;
            szl.Data = new byte[size];
            _LastError = ReadSzl(0x0232, 0x0004, ref szl, ref size);
            if (_LastError == 0)
            {
                protection.SchSchal = S7.GetWordAt(szl.Data, 2);
                protection.SchPar = S7.GetWordAt(szl.Data, 4);
                protection.SchRel = S7.GetWordAt(szl.Data, 6);
                protection.BartSch = S7.GetWordAt(szl.Data, 8);
                protection.AnlSch = S7.GetWordAt(szl.Data, 10);
            }
            return _LastError;
        }
        #endregion

        #region [Low Level]

        public int IsoExchangeBuffer(byte[] buffer, ref int size)
        {
            _LastError = 0;
            _timeMs = 0;
            int elapsed = Environment.TickCount;
            Array.Copy(_tpktIso, 0, _pdu, 0, _tpktIso.Length);
            S7.SetWordAt(_pdu, 2, (ushort)(size + _tpktIso.Length));
            try
            {
                Array.Copy(buffer, 0, _pdu, _tpktIso.Length, size);
            }
            catch
            {
                return S7Consts.ErrIsoInvalidPdu;
            }
            SendPacket(_pdu, _tpktIso.Length + size);
            if (_LastError == 0)
            {
                int length = RecvIsoPacket();
                if (_LastError == 0)
                {
                    Array.Copy(_pdu, _tpktIso.Length, buffer, 0, length - _tpktIso.Length);
                    size = length - _tpktIso.Length;
                }
            }
            if (_LastError == 0)
                _timeMs = Environment.TickCount - elapsed;
            else
                size = 0;
            return _LastError;
        }

        #endregion

        #region [Async functions (not implemented)]

        public int AsReadArea(int area, int dbNumber, int start, int amount, int wordLen, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsWriteArea(int area, int dbNumber, int start, int amount, int wordLen, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsDbRead(int dbNumber, int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsDbWrite(int dbNumber, int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsMbRead(int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsMbWrite(int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsEbRead(int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsEbWrite(int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsAbRead(int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsAbWrite(int start, int size, byte[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsTmRead(int start, int amount, ushort[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsTmWrite(int start, int amount, ushort[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsCtRead(int start, int amount, ushort[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsCtWrite(int start, int amount, ushort[] buffer)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsListBlocksOfType(int blockType, ushort[] list)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsReadSzl(int id, int index, ref S7Szl data, ref int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsReadSzlList(ref S7SzlList list, ref int itemsCount)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsUpload(int blockType, int blockNum, byte[] usrData, ref int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsFullUpload(int blockType, int blockNum, byte[] usrData, ref int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsDownload(int blockNum, byte[] usrData, int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsPlcCopyRamToRom(uint timeout)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsPlcCompress(uint timeout)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsDbGet(int dbNumber, byte[] usrData, ref int size)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public int AsDbFill(int dbNumber, int fillChar)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        public bool CheckAsCompletion(ref int opResult)
        {
            opResult = 0;
            return false;
        }

        public int WaitAsCompletion(int timeout)
        {
            return S7Consts.ErrCliFunctionNotImplemented;
        }

        #endregion

        #region [Info Functions / Properties]

        public static string ErrorText(int error)
        {
            switch (error)
            {
                case 0: return "OK";
                case S7Consts.ErrTcpSocketCreation: return "SYS: Error creating the Socket";
                case S7Consts.ErrTcpConnectionTimeout: return "TCP: Connection Timeout";
                case S7Consts.ErrTcpConnectionFailed: return "TCP: Connection Error";
                case S7Consts.ErrTcpReceiveTimeout: return "TCP: Data receive Timeout";
                case S7Consts.ErrTcpDataReceive: return "TCP: Error receiving Data";
                case S7Consts.ErrTcpSendTimeout: return "TCP: Data send Timeout";
                case S7Consts.ErrTcpDataSend: return "TCP: Error sending Data";
                case S7Consts.ErrTcpConnectionReset: return "TCP: Connection reset by the Peer";
                case S7Consts.ErrTcpNotConnected: return "CLI: Client not connected";
                case S7Consts.ErrTcpUnreachableHost: return "TCP: Unreachable host";
                case S7Consts.ErrIsoConnect: return "ISO: Connection Error";
                case S7Consts.ErrIsoInvalidPdu: return "ISO: Invalid PDU received";
                case S7Consts.ErrIsoInvalidDataSize: return "ISO: Invalid Buffer passed to Send/Receive";
                case S7Consts.ErrCliNegotiatingPdu: return "CLI: Error in PDU negotiation";
                case S7Consts.ErrCliInvalidParams: return "CLI: Invalid param(s) supplied";
                case S7Consts.ErrCliJobPending: return "CLI: Job pending";
                case S7Consts.ErrCliTooManyItems: return "CLI: Too many items (>20) in multi read/write";
                case S7Consts.ErrCliInvalidWordLen: return "CLI: Invalid WordLength";
                case S7Consts.ErrCliPartialDataWritten: return "CLI: Partial data written";
                case S7Consts.ErrCliSizeOverPdu: return "CPU: Total data exceeds the PDU size";
                case S7Consts.ErrCliInvalidPlcAnswer: return "CLI: Invalid CPU answer";
                case S7Consts.ErrCliAddressOutOfRange: return "CPU: Address out of range";
                case S7Consts.ErrCliInvalidTransportSize: return "CPU: Invalid Transport size";
                case S7Consts.ErrCliWriteDataSizeMismatch: return "CPU: Data size mismatch";
                case S7Consts.ErrCliItemNotAvailable: return "CPU: Item not available";
                case S7Consts.ErrCliInvalidValue: return "CPU: Invalid value supplied";
                case S7Consts.ErrCliCannotStartPlc: return "CPU: Cannot start PLC";
                case S7Consts.ErrCliAlreadyRun: return "CPU: PLC already RUN";
                case S7Consts.ErrCliCannotStopPlc: return "CPU: Cannot stop PLC";
                case S7Consts.ErrCliCannotCopyRamToRom: return "CPU: Cannot copy RAM to ROM";
                case S7Consts.ErrCliCannotCompress: return "CPU: Cannot compress";
                case S7Consts.ErrCliAlreadyStop: return "CPU: PLC already STOP";
                case S7Consts.ErrCliFunNotAvailable: return "CPU: Function not available";
                case S7Consts.ErrCliUploadSequenceFailed: return "CPU: Upload sequence failed";
                case S7Consts.ErrCliInvalidDataSizeRecvd: return "CLI: Invalid data size received";
                case S7Consts.ErrCliInvalidBlockType: return "CLI: Invalid block type";
                case S7Consts.ErrCliInvalidBlockNumber: return "CLI: Invalid block number";
                case S7Consts.ErrCliInvalidBlockSize: return "CLI: Invalid block size";
                case S7Consts.ErrCliNeedPassword: return "CPU: Function not authorized for current protection level";
                case S7Consts.ErrCliInvalidPassword: return "CPU: Invalid password";
                case S7Consts.ErrCliNoPasswordToSetOrClear: return "CPU: No password to set or clear";
                case S7Consts.ErrCliJobTimeout: return "CLI: Job Timeout";
                case S7Consts.ErrCliFunctionRefused: return "CLI: Function refused by CPU (Unknown error)";
                case S7Consts.ErrCliPartialDataRead: return "CLI: Partial data read";
                case S7Consts.ErrCliBufferTooSmall: return "CLI: The buffer supplied is too small to accomplish the operation";
                case S7Consts.ErrCliDestroying: return "CLI: Cannot perform (destroying)";
                case S7Consts.ErrCliInvalidParamNumber: return "CLI: Invalid Param Number";
                case S7Consts.ErrCliCannotChangeParam: return "CLI: Cannot change this param now";
                case S7Consts.ErrCliFunctionNotImplemented: return "CLI: Function not implemented";
                default: return "CLI: Unknown error (0x" + Convert.ToString(error, 16) + ")";
            };
        }

        public int LastError()
        {
            return _LastError;
        }

        public int RequestedPduLength()
        {
            return _pduSizeRequested;
        }

        public int NegotiatedPduLength()
        {
            return _pduLength;
        }

        public int ExecTime()
        {
            return _timeMs;
        }

        public int ExecutionTime
        {
            get
            {
                return _timeMs;
            }
        }

        public int PduSizeNegotiated
        {
            get
            {
                return _pduLength;
            }
        }

        public int PduSizeRequested
        {
            get
            {
                return _pduSizeRequested;
            }
            set
            {
                if (value < _minPduSizeToRequest)
                    value = _minPduSizeToRequest;
                if (value > _maxPduSizeToRequest)
                    value = _maxPduSizeToRequest;
                _pduSizeRequested = value;
            }
        }

        public int PlcPort
        {
            get
            {
                return _plcPort;
            }
            set
            {
                _plcPort = value;
            }
        }

        public int ConnTimeout
        {
            get
            {
                return _socket.ConnectTimeout;
            }
            set
            {
                _socket.ConnectTimeout = value;
            }
        }

        public int RecvTimeout
        {
            get
            {
                return _socket.ReadTimeout;
            }
            set
            {
                _socket.ReadTimeout = value;
            }
        }

        public int SendTimeout
        {
            get
            {
                return _socket.WriteTimeout;
            }
            set
            {
                _socket.WriteTimeout = value;
            }
        }

        public bool Connected
        {
            get
            {
                return (_socket != null) && (_socket.Connected);
            }
        }


        #endregion
    }
}