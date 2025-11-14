using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using System.Data.SqlClient;

namespace InImArchiverService
{
    /// <summary>
    /// Подключение к контроллеру S7
    /// </summary>
    public class S7Connection : ILoggable
    {
        /// <summary>
        /// Имя соединения с контроллером S7.
        /// </summary>
        public string Name;

        /// <summary>
        /// MySQL сервер для сохранения результатов.
        /// </summary>
        private MySQL _mySql;

        /// <summary>
        /// MS SQL Server для сохранения результатов.
        /// </summary>
        private MSSQL _mssql;

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
        /// Период чтения из контроллера S7.
        /// </summary>
        private int _readingPeriod;

        /// <summary>
        /// Объект для чтения из контроллера S7.
        /// </summary>
        private S7Reader _reader;

        /// <summary>
        /// Список тегов для чтения из контроллера S7.
        /// </summary>
        private List<Tag> _tagsForRead = new List<Tag>();

        /// <summary>
        /// Краткое имя файла лога для этого объекта, без расширения.
        /// </summary>
        private readonly string _logFile;

        /// <summary>
        /// Событие генерации сообщения потоком обмена с ОРС-сервером.
        /// </summary>
        public event LogMessage LogMessageEvent;

        /// <summary>
        /// Подключение работает.
        /// </summary>
        public bool IsWork => _reader != null && _reader.IsWork;

        /// <summary>
        /// Создание подключения к контроллеру S7.
        /// </summary>
        /// <param name="name">Имя подключения.</param>
        /// <param name="address">IP-адрес контроллера S7.</param>
        /// <param name="port">Порт контроллера S7.</param>
        /// <param name="rack">Корзина контроллера S7.</param>
        /// <param name="slot">Гнездо контроллера S7.</param>
        /// <param name="readingPeriod">Период чтения тегов ОРС-сервера, мс.</param>
        /// <param name="tags">Теги, которые нужно читать из ОРС-сервера, и их идентификаторы в базе данных.</param>
        public S7Connection(string name, string address, int port, int rack, int slot, int readingPeriod, List<Tag> tags, string logFile = "S7Connection")
        {
            _logFile = logFile;
            Globals.Log.Register(this);
            LogMessageEvent?.Invoke(_logFile, "S7Connection", $"Connection to {name} ({address}:{port} R{rack}S{slot})\ntags count = " + tags.Count);
            Name = name;

            switch (Globals.Settings.SqlServer)
            {
                case SqlServer.MySql:
                    _mySql = new MySQL(Globals.Settings.ConnectionString, "S7Connection", _logFile);
                    Globals.Log.Register(_mySql);
                    _mssql = null;
                    break;
                case SqlServer.MsSql:
                    _mssql = new MSSQL(Globals.Settings.ConnectionString, "S7Connection", _logFile);
                    Globals.Log.Register(_mssql);
                    _mySql = null;
                    break;
            }

            _address = address;
            _rack = rack;
            _slot = slot;
            _port = port;
            _readingPeriod = readingPeriod;
            _tagsForRead = tags;
            _reader = new S7Reader(_tagsForRead, _address, _rack, _slot, _port, _readingPeriod);
            _reader.EventDataReaded += DataReaded;
            Globals.Log.Register(_reader);
        }

        /// <summary>
        /// Функция для обработки прочитанных данных.
        /// </summary>
        /// <param name="tags">Теги, значения которых прочитаны из контроллера S7.</param>
        public void DataReaded(List<Tag> tags)
        {
            if (tags == null || tags.Count == 0)
                return;

            DataTable intData = new DataTable();
            intData.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("TagID", typeof(long)),
                new DataColumn("DateSet", typeof(DateTime)),
                new DataColumn("TagValue", typeof(long))
            });
            DataTable doubleData = new DataTable();
            doubleData.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("TagID", typeof(long)),
                new DataColumn("DateSet", typeof(DateTime)),
                new DataColumn("TagValue", typeof(long))
            });
            DataTable uilongData = new DataTable();
            uilongData.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("TagID", typeof(long)),
                new DataColumn("DateSet", typeof(DateTime)),
                new DataColumn("TagValue", typeof(long))
            });

            object currentDateTime = DateTime.Now;

            //запретить на время обработки изменение списка переменных
            lock (_tagsForRead)
            {
                foreach (Tag readedTag in tags)
                {
                    foreach (Tag tag in _tagsForRead)
                    {
                        if (tag.ID == readedTag.ID)
                        {
                            //проверить, не изменилось ли значение тега
                            if (tag.Value == null || !tag.Value.Equals(readedTag.Value))
                            {
                                tag.Value = readedTag.Value;

                                //тег читается впервые либо его значение изменилось с прошлого  раза
                                if (readedTag.Value is string)
                                {
                                    //это сообщение об ошибке
                                    continue;
                                }

                                if (readedTag.Value is bool || readedTag.Value is sbyte || readedTag.Value is byte ||
                                    readedTag.Value is short || readedTag.Value is ushort || readedTag.Value is int ||
                                    readedTag.Value is uint || readedTag.Value is long)
                                {
                                    intData.Rows.Add(
                                        readedTag.ID, currentDateTime, Convert.ToInt64(readedTag.Value));
                                    continue;
                                }

                                if (readedTag.Value is float || readedTag.Value is double)
                                {
                                    doubleData.Rows.Add(readedTag.ID, currentDateTime, Convert.ToDouble(readedTag.Value));
                                    continue;
                                }

                                if (readedTag.Value is ulong)
                                {
                                    uilongData.Rows.Add(readedTag.ID, currentDateTime, Convert.ToUInt64(readedTag.Value));
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            //выполнить сохранение изменившихся тегов в базу данных
            SaveToDb(intData, Globals.Settings.IntArchive);
            SaveToDb(uilongData, Globals.Settings.UlongArchive);
            SaveToDb(doubleData, Globals.Settings.DoubleArchive);
        }

        /// <summary>
        /// Сохранение таблицы данных в базу данных.
        /// </summary>
        /// <param name="dt">Таблица данных.</param>
        /// <param name="table">Структура таблицы в базе данных, в которую нужно сохранить данные.</param>
        private void SaveToDb(DataTable dt, ArchiveTableStruct table)
        {
            if (dt == null || dt.Rows.Count == 0) return;

            switch (Globals.Settings.SqlServer)
            {
                case SqlServer.MsSql:
                    //вставка данных в базу данных MSSQL
                    List<SqlBulkCopyColumnMapping> sqlMappings = new List<SqlBulkCopyColumnMapping>()
                    {
                        new SqlBulkCopyColumnMapping(0, table.TrendId),
                        new SqlBulkCopyColumnMapping(1, table.DateSet),
                        new SqlBulkCopyColumnMapping(2, table.TagValue)
                    };
                    if (!_mssql.BulkInsert(table.Name, dt, sqlMappings))
                        LogMessageEvent?.Invoke(_logFile, "S7Connection.DataReaded", $"Error saving table {table.Name}.");
                    break;
                case SqlServer.MySql:
                    //вставка данных в базу данных MySQL
                    List<MySqlBulkCopyColumnMapping> mappings = new List<MySqlBulkCopyColumnMapping>
                    {
                        new MySqlBulkCopyColumnMapping(0, table.TrendId),
                        new MySqlBulkCopyColumnMapping(1, table.DateSet),
                        new MySqlBulkCopyColumnMapping(2, table.TagValue)
                    };
                    if (!_mySql.BulkInsert(table.Name, dt, mappings))
                        LogMessageEvent?.Invoke(_logFile, "S7Connection.DataReaded", $"Error saving table {table.Name}.");
                    break;
            }
        }

        /// <summary>
        /// Завершение работы.
        /// </summary>
        public void Disconnect()
        {
            _reader.Disconnect();
            _reader = null;

            if (_mssql != null)
            {
                _mssql.Disconnect();
                _mssql = null;
            }

            if (_mySql != null)
            {
                _mySql.Disconnect();
                _mssql = null;
            }
        }
    }
}
