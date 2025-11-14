using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sharp7;

namespace InImArchiverService
{
    /// <summary>
    /// Делегат для обработки данных, полученных от ОРС-сервера.
    /// </summary>
    public delegate void EventDataReaded(List<Tag> tags);

    public class S7Reader : ILoggable
    {
        /// <summary>
        /// Поток, в котором выполняется чтение тегов ОРС-сервера.
        /// </summary>
        private Thread _th;

        /// <summary>
        /// Объект для доступа к контроллеру S7.
        /// </summary>
        private S7Client _client;

        /// <summary>
        /// Чтение переменных из контроллера S7 разрешено.
        /// </summary>
        private bool _readEnable = true;

        /// <summary>
        /// Период чтения переменных из контроллера S7.
        /// </summary>
        private int _readingPeriod;

        /// <summary>
        /// Событие завершения чтения данных из ОРС-сервера.
        /// </summary>
        public event EventDataReaded EventDataReaded;
        
        /// <summary>
        /// Краткое имя файла лога для этого объекта, без расширения.
        /// </summary>
        private readonly string _logFile;

        /// <summary>
        /// Событие генерации сообщения потоком обмена с ОРС-сервером.
        /// </summary>
        public event LogMessage LogMessageEvent;

        /// <summary>
        /// Время чтения тегов ОРС-сервера.
        /// </summary>
        public long ReadingTime = 0;

        /// <summary>
        /// Полное время исполнения итерации цикла чтения тегов ОРС-сервера.
        /// </summary>
        public long CompletionTime = 0;

        private object _clientLock;

        /// <summary>
        /// IP-адрес контроллера S7.
        /// </summary>
        private string _address;

        /// <summary>
        /// Коризна контроллера S7.
        /// </summary>
        private int _rack;

        /// <summary>
        /// Гнездо контроллера S7.
        /// </summary>
        private int _slot;

        /// <summary>
        /// Порт контроллера S7.
        /// </summary>
        private int _port;

        /// <summary>
        /// Объект для синхронизации доступа к свойству IsWork.
        /// </summary>
        private readonly object _isWorkLock = new object();

        /// <summary>
        /// Читатель работает.
        /// </summary>
        private bool _isWork;

        /// <summary>
        /// Читатель работает.
        /// </summary>
        public bool IsWork
        {
            get
            {
                lock (_isWorkLock)
                {
                    return _isWork;
                }
            }
            set
            {
                lock (_isWorkLock)
                {
                    _isWork = value;
                }
            }
        }

        /// <summary>
        /// Группы тегов по размеру PDU.
        /// </summary>
        private List<List<Tag>> _pduGroups;

        /// <summary>
        /// Иницализация нового объекта S7Reader.
        /// </summary>
        /// <param name="tagsForRead">Теги для чтения из контроллера.</param>
        /// <param name="address">IP-адрес контроллера S7.</param>
        /// <param name="rack">Корзина контроллера S7.</param>
        /// <param name="slot">Гнездо контроллера S7.</param>
        /// <param name="port">Порт контроллера S7.</param>
        /// <param name="readingPeriod">Период чтения переменных из контроллера S7.</param>
        public S7Reader(List<Tag> tagsForRead, string address, int rack, int slot, int port=102, int readingPeriod = 100,string logFile = "S7Reader")
        {
            _clientLock = new object();
            _pduGroups= new List<List<Tag>>();
            List<Tag> tags = new List<Tag>();
            foreach (Tag tag in tagsForRead)
            {
                tags.Add(new Tag
                {
                    ID = tag.ID,
                    Address = tag.Address,
                    Type = tag.Type,
                    Description = tag.Description,
                    Value = tag.Value
                });
            }
            _pduGroups.Add(tags);

            _address = address;
            _rack = rack;
            _slot = slot;
            _port = port;

            _logFile = logFile;

            lock (_clientLock)
            {
                //создать объект для подключения контроллеру
                _client = new S7Client();
                _client.SetConnectionType(S7Client.ConntypeBasic);
                //у контроллера S7 порт всегда 102
                _client.PlcPort = _port;
            }

            //проинициализировать период чтения
            _readingPeriod = readingPeriod;

            //создать поток для чтения тегов контроллера и запустить его на выполнение.
            _th = new Thread(ExchangeCycle);
            _th.Start();
        }

        /// <summary>
        /// Подключение к контроллеру.
        /// </summary>
        public bool Connect()
        {
            lock (_clientLock)
            {
                if (!_client.Connected)
                {
                    int result = _client.ConnectTo(_address, _rack, _slot);
                    if (result != 0)
                        LogMessageEvent?.Invoke(_logFile, "S7Reader.Connect", $"Connect, ошибка: {S7Client.ErrorText(result)}\n_address={_address}; _client.PlcPort={_client.PlcPort}; _rack={_rack}; _slot={_slot}");
                }
                return _client.Connected;
            }
        }

        /// <summary>
        /// Подача команды на отключение от сервера.
        /// </summary>
        public void Disconnect()
        {
            _readEnable = false;
        }

        /// <summary>
        /// Завершение чтения тегов ОРС-сервера.
        /// </summary>
        public void Abort()
        {
            Disconnect();
        }

        /// <summary>
        /// Цикл обмена данными с ОРС-сервером.
        /// </summary>
        void ExchangeCycle()
        {
            IsWork = true;

            //выполнять до тех пор, пока разрешено чтение из ОРС-сервера
            while (_readEnable)
            {
                //зафиксировать начало итерации цикла чтения
                Stopwatch iterationTimer = Stopwatch.StartNew();

                //проверить, есть ли соединение с сервером
                if (!Connect())
                {
                    //соединения нет
                    //отправить сообщение об ошибке связи
                    EventDataReaded?.Invoke(null);
                    //ждать 30 секунд
                    LogMessageEvent?.Invoke(_logFile, "S7Reader.Read", "Connected = false >>> Sleep 30 сек");
                    //частота проверок - 100 раз в секунду
                    for (int i = 0; i < 3000; i++)
                    {
                        Thread.Sleep(10);

                        if (!_readEnable)
                            break;
                    }
                }
                else
                {
                    //соединение есть - прочитать переменные из контроллера S7

                    //зафиксировать начало операции чтения
                    Stopwatch readingTimer = Stopwatch.StartNew();

                    //объект для обмена данными с контроллером
                    S7MultiVar multiVar = new S7MultiVar(_client);

                    //потокобезопасно получить переменные, которые нужно прочитать из контроллера S7.
                    List<List<Tag>> pduGroups = new List<List<Tag>>();
                    lock (_pduGroups)
                    {
                        if (_pduGroups != null)
                        {
                            foreach (List<Tag> tags in _pduGroups)
                            {
                                List<Tag> group = new List<Tag>();
                                foreach (Tag tag in tags)
                                {
                                    group.Add(new Tag
                                    {
                                        ID = tag.ID,
                                        Address = tag.Address,
                                        Type = tag.Type,
                                        Description = tag.Description,
                                        Value = tag.Value
                                    });
                                }
                                pduGroups.Add(group);
                            }
                        }
                    }

                    //прочитать теги в каждой группе
                    bool pduGroupsChanged = false; //состав PDU групп тегов изменился
                    List<Tag> readedTags= new List<Tag>();
                    for (int j = 0; j < pduGroups.Count; j++)
                    {
                        if (pduGroups[j].Count > 0)
                        {
                            bool work = true;
                            while (work)
                            {
                                //сформировать буфер для читаемых значений
                                int size = 0;

                                foreach (Tag tag in pduGroups[j])
                                {
                                    //тип BOOL в дотнете длиной 4 байта, а в Sharp7 - 1 байт
                                    size += tag.Type.DotNetType == typeof(bool)
                                        ? 1
                                        : Marshal.SizeOf(tag.Type.DotNetType);
                                }

                                byte[] buffer = new byte[size];
                                //сформировать данные для чтения
                                int offset = 0;
                                foreach (Tag tag in pduGroups[j])
                                {
                                    int[] fields = S7Utils.S7ToSharp7Address(tag.Address, tag.Type.DotNetType);
                                    multiVar.Add(fields[0], fields[1], fields[2], fields[3], 1, ref buffer, offset);
                                    //тип BOOL в дотнете длиной 4 байта, а в Sharp7 - 1 байт
                                    offset += tag.Type.DotNetType == typeof(bool)
                                        ? 1
                                        : Marshal.SizeOf(tag.Type.DotNetType);
                                }

                                //выполнить чтение
                                int result = multiVar.Read();
                                if (result == 0)
                                {
                                    //чтение успешно - расшифровать полученные данные
                                    offset = 0;
                                    for (int i = 0; i < pduGroups[j].Count; i++)
                                    {
                                        if (multiVar.Results[i] == 0)
                                        {
                                            pduGroups[j][i].Value =
                                                S7Utils.S7BytesToValue(buffer, pduGroups[j][i].Type.DotNetType, offset);
                                        }
                                        else
                                            pduGroups[j][i].Value = "Read Error";

                                        //тип BOOL в дотнете длиной 4 байта, а в Sharp7 - 1 байт
                                        offset += pduGroups[j][i].Type.DotNetType == typeof(bool)
                                            ? 1
                                            : Marshal.SizeOf(pduGroups[j][i].Type.DotNetType);
                                    }
                                    work = false;
                                }
                                else
                                {
                                    if (result == S7Consts.ErrCliSizeOverPdu && pduGroups[j].Count > 1)
                                    {
                                        //ошибка превышения размера PDU, которую можно попытаться исправить
                                        //переместить последний тег из группы в следующую группу и повторить чтение из контроллера
                                        if (j < pduGroups.Count - 1)
                                        {
                                            //после этой группы есть ещё одна - переместить последний тег в неё, чтобы уменьшить PDU текущей группы
                                            Tag tag = pduGroups[j][pduGroups[j].Count - 1];
                                            pduGroups[j].RemoveAt(pduGroups[j].Count - 1);
                                            pduGroups[j + 1].Insert(0, tag);
                                            pduGroupsChanged = true;
                                        }
                                        else
                                        {
                                            //это последняя группа - создать новую и переместить последний тег в неё, чтобы уменьшить PDU текущей группы
                                            Tag tag = pduGroups[j][pduGroups[j].Count - 1];
                                            pduGroups[j].RemoveAt(pduGroups[j].Count - 1);
                                            List<Tag> newGroup = new List<Tag> { tag };
                                            pduGroups.Add(newGroup);
                                        }
                                    }
                                    else
                                    {
                                        //неустранимая ошибка - установить у всех тегов группы флаг "Ошибка чтения"
                                        foreach (Tag tag in pduGroups[j])
                                            tag.Value = "Read Error";
                                        LogMessageEvent?.Invoke(_logFile, "S7Reader.ExchangeCycle.", $"Ошибка чтения переменных из контроллера:\n{S7Client.ErrorText(result)}.");
                                        work = false;
                                    }
                                }
                            }
                            //сохранить результаты чтения
                            readedTags.AddRange(pduGroups[j]);
                        }
                    }
                    //если выполнялась перестойка групп PDU - сохранить изменения
                    if (pduGroupsChanged)
                    {
                        _pduGroups.Clear();
                        foreach (List<Tag> tags in pduGroups)
                        {
                            List<Tag> group = new List<Tag>();
                            foreach (Tag tag in tags)
                            {
                                group.Add(new Tag
                                {
                                    ID = tag.ID,
                                    Address = tag.Address,
                                    Type = tag.Type,
                                    Description = tag.Description,
                                    Value = tag.Value
                                });
                            }

                            _pduGroups.Add(group);
                        }
                    }

                    //вернуть результаты чтения
                    EventDataReaded?.Invoke(readedTags);
                    
                    //остановить таймер и прочитать продолжительность операции чтения
                    readingTimer.Stop();
                    ReadingTime = readingTimer.ElapsedMilliseconds;

                    //рассчитать остаток времени до конца периода чтения
                    long timeRemainder = _readingPeriod - ReadingTime;
                    //если конец периода ещё не наступил - ждать до конца периода
                    if (timeRemainder > 0)
                    {
                        int decims = Convert.ToInt32(timeRemainder / 10);
                        int remainder = Convert.ToInt32(timeRemainder % 10);
                        //частота проверок - 100 раз в секунду
                        for (int i = 0; i < decims; i++)
                        {
                            Thread.Sleep(10);

                            if (!_readEnable)
                                break;
                        }
                        if(remainder > 0)
                            Thread.Sleep(remainder);
                    }
                }

                //рассчитать полную продолжительность итерации цикла
                iterationTimer.Stop();
                CompletionTime = iterationTimer.ElapsedMilliseconds;
            }

            //отключение от контроллера
            if (_client != null)
            {
                if (_client.Connected)
                    _client.Disconnect();
            }

            IsWork = false;
        }
    }
}