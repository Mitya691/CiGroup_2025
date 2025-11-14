using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using MySqlConnector;

namespace InImArchiverService
{
    /// <summary>
    /// Класс для доступа к БД MSSQL.
    /// 07 апреля 2021
    /// Добавлено: переменная препроцессора FULLOG для отображения текстов ошибочных запросов.
    /// 25 октября 2021
    /// Добавлено: функция BulkInsert для массовой вставки данных в таблицу.
    /// 11 ноября 2022
    /// Добавлено: функция ExecuteCommand для запуска на сервере настроенной команды MS SQL.
    /// Удалено: код для форматирования строк запросов.
    /// </summary>
    internal class MSSQL : ILoggable
    {
        /// <summary>
        /// Строка подключения к базе данных.
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// Объект для подключения к базе данных.
        /// </summary>
        private SqlConnection _connnection;

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
        internal MSSQL(string connectionString, string parentThread = null, string logFile = "MSSQL")
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
                _connnection = new SqlConnection(_connectionString);
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
                        _connnection = new SqlConnection(_connectionString);
                        _connnection.Open();
                    }
                    catch (Exception ex)
                    {
#if FULLOG
                        LogMessageEvent?.Invoke(_logFile, "MSSQL.Connect" + ParentToString() + ": ", ex.Message + "\n" + _connectionString);
#else
                        LogMessageEvent?.Invoke(_logFile, "MSSQL.Connect" + ParentToString() + ": ", ex.Message);
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
                LogMessageEvent?.Invoke(_logFile, "MSSQL.Disconnect" + ParentToString() + ": ", ex.Message);
#else
                LogMessageEvent?.Invoke(_logFile, "MSSQL.Disconnect" + ParentToString() + ": ", ex.Message);
#endif
            }
        }

        /// <summary>
        /// Полученые таблицы из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal DataTable ReadTable(string sql)
        {
            DataTable dt = null;
            if (Connect())
            {
                try
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, _connnection))
                    {
                        dt = new DataTable();
                        da.SelectCommand.CommandTimeout = 25;
                        da.Fill(dt);
                    }
                }
                catch (Exception ex)
                {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.ReadTable" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.ReadTable" + ParentToString() + ": ", ex.Message);
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
        internal DataRow ReadFirstRow(string sql)
        {
            DataTable dt = new DataTable();
            if (Connect())
            {
                try
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, _connnection))
                    {
                        da.SelectCommand.CommandTimeout = 25;
                        da.Fill(dt);
                    }
                }
                catch (Exception ex)
                {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.ReadFirstRow" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.ReadFirstRow" + ParentToString() + ": ", ex.Message);
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
        internal object ReadScalar(string sql)
        {
            if (!Connect()) return DBNull.Value;
            try
            {
                SqlCommand cmd = new SqlCommand(sql, _connnection) { CommandTimeout = 20 };
                return cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.ReadScalar" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                LogMessageEvent?.Invoke(_logFile, "MSSQL.ReadScalar" + ParentToString() + ": ", ex.Message);
#endif

                return DBNull.Value;
            }
        }

        /// <summary>
        /// Получение результата строкового скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal string ReadScalarString(string sql)
        {
            string ret = "";

            try
            {
                ret = Convert.ToString(ReadScalar(sql));
            }
            catch { }

            return ret;

        }

        /// <summary>
        /// Получение результата с плавающей точкой скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal double ReadScalarDouble(string sql)
        {
            double ret = 0;

            try
            {
                ret = Convert.ToDouble(ReadScalar(sql));
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Получение результата 32-битного целочисленного скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal int ReadScalarInt32(string sql)
        {
            int ret = 0;

            try
            {
                ret = Convert.ToInt32(ReadScalar(sql));
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Получение результата 16-битного целочисленного скалярного выражения или процедуры из базы данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal short ReadScalarInt16(string sql)
        {
            short ret = 0;

            try
            {
                ret = Convert.ToInt16(ReadScalar(sql));
            }
            catch { }

            return ret;
        }

        /// <summary>
        /// Запись данных в базу данных.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal void Write(string sql)
        {
            if (Connect())
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(sql, _connnection) { CommandTimeout = 2 };
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.Write" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.Write" + ParentToString() + ": ", ex.Message);
#endif

                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Выполнение команды базы данных, не возвращающей никакого результата.
        /// В случае ошибки возвращает ложь.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal bool Execute(string sql)
        {
            if (!Connect()) return false;
            try
            {
                SqlCommand cmd = new SqlCommand(sql, _connnection) { CommandTimeout = 2 };
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.Execute" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                LogMessageEvent?.Invoke(_logFile, "MSSQL.Execute" + ParentToString() + ": ", ex.Message);
#endif

                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Выполнение команды базы данных, не возвращающей никакого результата, с большим тайм-аутом.
        /// </summary>
        /// <param name="sql">Строка SQL-запроса с символами форматирования.</param>
        internal bool LongExecute(string sql)
        {
            if (!Connect()) return false;
            try
            {
                SqlCommand cmd = new SqlCommand(sql, _connnection) { CommandTimeout = 9999 };
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
#if FULLOG
                    LogMessageEvent?.Invoke(_logFile, "MSSQL.LongExecute" + ParentToString() + ": ", ex.Message + "\n" + sql);
#else
                LogMessageEvent?.Invoke(_logFile, "MSSQL.LongExecute" + ParentToString() + ": ", ex.Message);
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
        /// <param name="columnMappings">Переопределение колонок таблицы данных.</param>
        /// <returns></returns>
        internal bool BulkInsert(string tableName, DataTable data, List<SqlBulkCopyColumnMapping> columnMappings = null)
        {
            if (!Connect()) return false;

            SqlBulkCopy bk = new SqlBulkCopy(_connnection) {DestinationTableName = tableName};

            if (columnMappings != null)
            {
                bk.ColumnMappings.Clear();
                foreach (SqlBulkCopyColumnMapping mapping in columnMappings) bk.ColumnMappings.Add(mapping);
            }
            
            try
            {
                bk.WriteToServer(data);
                return true;
            }
            catch (Exception ex)
            {
                LogMessageEvent?.Invoke(_logFile, "MSSQL.BulkCopy" + ParentToString() + ": ", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Запуск на этом сервере команды SQL.
        /// </summary>
        /// <param name="command">Не возвращающая результат команда SQL, которую нужно запустить.</param>
        /// <returns></returns>
        internal bool ExecuteCommand(SqlCommand command)
        {
            if (command == null || !Connect()) return false;

            try
            {
                command.Connection = _connnection;
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
#if FULLOG
                LogMessageEvent?.Invoke(_logFile, "MSSQL.LongExecute" + ParentToString() + ": ", ex.Message + "\n" + command.CommandText);
#else
                LogMessageEvent?.Invoke(_logFile, "MSSQL.LongExecute" + ParentToString() + ": ", ex.Message);
#endif

                Disconnect();
                return false;
            }
        }
    }
}
