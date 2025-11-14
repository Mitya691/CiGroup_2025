using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using System.Data;
using System.Data.SqlClient;

namespace InImArchiverService
{
    /// <summary>
    /// Класс для доступа к БД MySQL.
    /// 07 апреля 2021
    /// Добавлено: переменная препроцессора FULLOG для отображения текстов ошибочных запросов.
    /// 25 октября 2021
    /// Добавлено: функция BulkInsert для массовой вставки данных в таблицу.
    /// 30 мая 2022
    /// Исправление ошибок в функции BulkInsert.
    /// </summary>
    internal class MySQL : ILoggable
    {
        /// <summary>
        /// Строка подключения к базе данных.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Объект для подключения к базе данных.
        /// </summary>
        private MySqlConnection _connnection;

        /// <summary>
        /// Имя потока, вызвавшего класс.
        /// </summary>
        private readonly string _parentThread;

        /// <summary>
        /// Краткое имя файла лога для этого объекта, без расширения.
        /// </summary>
        private readonly string _logFile;

        /// <summary>
        /// Событие для передачи сообщений в лог.
        /// </summary>
        public event LogMessage LogMessageEvent;

        /// <summary>
        /// Инициализация объекта.
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных.</param>
        /// <param name="logFile">Краткое имя файла лога для этого объекта, без расширения.</param>
        /// <param name="parentThread">Имя потока, вызвавшего класс.</param>
        internal MySQL(string connectionString, string parentThread = null, string logFile = "MySQL")
        {
            _logFile = logFile;
            _parentThread = parentThread;
            _connectionString = connectionString;

            //подключиться к базе данных
            Connect();
        }

        private string ParentToString()
        {
            return string.IsNullOrWhiteSpace(_parentThread) ? string.Empty : " " + _parentThread;
        }

        /// <summary>
        /// Подключение к базе данных. Если подключиться получилось - возвращает истину.
        /// </summary>
        private bool Connect()
        {
            //если соедиенение с базой данных не существует - создать его
            if (_connnection == null)
            {
                _connnection = new MySqlConnection(_connectionString);
            }

            //при необходимости открыть соединение с базой данных
            if (_connnection.State != ConnectionState.Open)
            {
                try
                {
                    //попытаться открыть существующее соединение с базой данных
                    _connnection.Open();
                }
                catch (Exception)
                {
                    //существующее соедиенние не открылось
                    try
                    {
                        //попытаться создать и открыть новое соединение с базой данных
                        _connnection = new MySqlConnection(_connectionString);
                        _connnection.Open();
                    }
                    catch (Exception ex)
                    {
#if FULLOG
                        LogMessageEvent?.Invoke(_logFile, "MySQL.Connect" + ParentToString() + ": ", ex.Message + "\n" + _connectionString);
#else
                        LogMessageEvent?.Invoke(_logFile, "MySQL.Connect" + ParentToString() + ": ", ex.Message);
#endif
                    }
                }
            }

            return _connnection.State == ConnectionState.Open;
        }

        /// <summary>
        /// Отключение от базы данных.
        /// </summary>
        internal void Disconnect()
        {
            try
            {
                if (_connnection != null && _connnection.State == ConnectionState.Open)
                    _connnection.Close();
            }
            catch (Exception ex)
            {
#if FULLOG
                LogMessageEvent?.Invoke(_logFile, "MySQL.Disconnect" + ParentToString() + ": ", ex.Message);
#else
                LogMessageEvent?.Invoke(_logFile, "MySQL.Disconnect" + ParentToString() + ": ", ex.Message);
#endif
            }
        }

        /// <summary>
        /// Полученые таблицы из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal DataTable ReadTable(string sql, params object[] param)
        {
            sql = string.Format(sql, param);

            DataTable dt = null;
            if (Connect())
            {
                try
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter(sql, _connnection))
                    {
                        dt = new DataTable();
                        da.SelectCommand.CommandTimeout = 25;
                        da.Fill(dt);
                    }
                }
                catch (Exception ex)
                {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.ReadTable" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MySQL.ReadTable" + ParentToString() + ": ", ex.Message);
#endif

                    dt = null;
                }
            }

            return dt;
        }

        /// <summary>
        /// Получение пеовой строки таблицы из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal DataRow ReadFirstRow(string sql, params object[] param)
        {
            sql = string.Format(sql, param);

            DataTable dt = new DataTable();
            if (Connect())
            {
                try
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter(sql, _connnection))
                    {
                        da.SelectCommand.CommandTimeout = 25;
                        da.Fill(dt);
                    }
                }
                catch (Exception ex)
                {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.ReadFirstRow" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MySQL.ReadFirstRow" + ParentToString() + ": ", ex.Message);
#endif

                    return null;
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        /// <summary>
        /// Получение результата скалярного выражения или процедуры из базы данных. Результат имеет тип object.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal object ReadScalar(string sql, params object[] param)
        {
            sql = string.Format(sql, param);

            if (!Connect()) return DBNull.Value;
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, _connnection) { CommandTimeout = 20 };
                return cmd.ExecuteScalar();
            }
            catch (MySqlException ex)
            {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.ReadScalar" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                LogMessageEvent?.Invoke(_logFile, "MySQL.ReadScalar" + ParentToString() + ": ", ex.Message);
#endif

                return DBNull.Value;
            }
        }

        /// <summary>
        /// Получение результата строкового скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal string ReadScalarString(string sql, params object[] param)
        {
            string ret = "";

            try
            {
                ret = Convert.ToString(ReadScalar(sql, param));
            }
            catch { }

            return ret;

        }

        /// <summary>
        /// Получение результата с плавающей точкой скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal double ReadScalarDouble(string sql, params object[] param)
        {
            double ret = 0;

            try
            {
                ret = Convert.ToDouble(ReadScalar(sql, param));
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Получение результата 32-битного целочисленного скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal int ReadScalarInt32(string sql, params object[] param)
        {
            int ret = 0;

            try
            {
                ret = Convert.ToInt32(ReadScalar(sql, param));
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Получение результата 16-битного целочисленного скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal short ReadScalarInt16(string sql, params object[] param)
        {
            short ret = 0;

            try
            {
                ret = Convert.ToInt16(ReadScalar(sql, param));
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Запись данных в базу данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal void Write(string sql, params object[] param)
        {
            sql = string.Format(sql, param);

            if (Connect())
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, _connnection) { CommandTimeout = 2 };
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.Write" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MySQL.Write" + ParentToString() + ": ", ex.Message);
#endif

                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Запись данных в базу данных с возвратом диагностического сообщения.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal string DebugWrite(string sql, params object[] param)
        {
            string message = "";
            sql = string.Format(sql, param);

            if (Connect())
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, _connnection) { CommandTimeout = 2 };
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    message = ex.Message;
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.DebugWrite" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MySQL.DebugWrite" + ParentToString() + ": ", ex.Message);
#endif

                    Disconnect();
                }
            }

            return message;
        }

        /// <summary>
        /// Выполнение команды базы данных, не возвращающей никакого результата.
        /// В случае ошибки возвращает ложь.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal bool Execute(string sql, params object[] param)
        {
            sql = string.Format(sql, param);

            if (!Connect()) return false;
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, _connnection) { CommandTimeout = 2 };
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.Execute" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                LogMessageEvent?.Invoke(_logFile, "MySQL.Execute" + ParentToString() + ": ", ex.Message);
#endif

                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Выполнение команды базы данных, не возвращающей никакого результата, с большим тайм-аутом.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        /// <param name="param">Параметры строки запроса.</param>
        internal bool LongExecute(string sql, params object[] param)
        {
            sql = string.Format(sql, param);

            if (!Connect()) return false;
            try
            {
                MySqlCommand cmd = new MySqlCommand(sql, _connnection) { CommandTimeout = 9999 };
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MySQL.LongExecute" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                LogMessageEvent?.Invoke(_logFile, "MySQL.LongExecute" + ParentToString() + ": ", ex.Message);
#endif

                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Массовая вставка в базу данных.
        /// </summary>
        /// <param name="tableName">Имя таблицы для вставки.</param>
        /// <param name="data">Данные для вставки. Имена колонок должны соответствовать именам колонок таблицы.</param>
        /// <param name="columnMappings">Переопределение колонок, если нужно.</param>
        internal bool BulkInsert(string tableName, DataTable data, List<MySqlBulkCopyColumnMapping> columnMappings = null)
        {
            if (!Connect()) return false;
            MySqlBulkCopy bk = new MySqlBulkCopy(_connnection) {DestinationTableName = tableName};

            if (columnMappings != null)
            {
                bk.ColumnMappings.Clear();
                foreach (MySqlBulkCopyColumnMapping mapping in columnMappings) bk.ColumnMappings.Add(mapping);
            }

            try
            {
                bk.WriteToServer(data);
                return true;
            }
            catch (Exception ex)
            {
                LogMessageEvent?.Invoke(_logFile, "MySQL.BulkCopy" + ParentToString() + ": ", ex.Message);
                return false;
            }
        }


        /// <summary>
        /// Экранизация опасных симоволов в строке.
        /// </summary>
        internal static string EscapeString(string s)
        {
            if (s == null) return null;
            return MySqlHelper.EscapeString(s);
        }
    }
}