using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InImArchiverService
{
    [Serializable]
    public enum SqlServer
    {
        /// <summary>
        /// Данные сохраняются в MS SQL Server.
        /// </summary>
        MsSql,

        /// <summary>
        /// Данные сохраняются в MySQL.
        /// </summary>
        MySql
    }

    /// <summary>
    /// Структура таблицы для архивации.
    /// </summary>
    [Serializable]
    public class ArchiveTableStruct
    {
        /// <summary>
        /// Имя архивной таблицы.
        /// </summary>
        public string Name;

        /// <summary>
        /// Имя поля идентификатора графика.
        /// </summary>
        public string TrendId;

        /// <summary>
        /// Имя поля даты и времени архивации значения (поле типа DateTime).
        /// </summary>
        public string DateSet;

        /// <summary>
        /// Имя поля значения.
        /// </summary>
        public string TagValue;

        /// <summary>
        /// Инициализация значениями по-умолчанию.
        /// </summary>
        public ArchiveTableStruct()
        {
            Name = "int_archive";
            TrendId = "TrendID";
            DateSet = "DateSet";
            TagValue = "TagValue";
        }
    }

    [Serializable]
    public class Settings
    {
        /// <summary>
        /// Тип сервера базы данных, в который нужно записывать данные.
        /// </summary>
        public SqlServer SqlServer;

        /// <summary>
        /// Строка подключения к базе данных MySQL.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Период обмена данными, мс.
        /// </summary>
        public int Period;

        /// <summary>
        /// Таблица для архивации целочисленных знаковых значений.
        /// </summary>
        public ArchiveTableStruct IntArchive;

        /// <summary>
        /// Таблица для архивации целочисленных 64-разрядных беззнаковых значений.
        /// </summary>
        public ArchiveTableStruct UlongArchive;

        /// <summary>
        /// Таблица для архивации значений с плавающей точкой.
        /// </summary>
        public ArchiveTableStruct DoubleArchive;
        
        /// <summary>
        /// Инициализация значениями по-умолчанию.
        /// </summary>
        public Settings()
        {
            SqlServer = SqlServer.MySql;
            ConnectionString = "Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;";
            Period = 60000;
            IntArchive = new ArchiveTableStruct { Name = "int_archive", TrendId = "TrendId", DateSet = "DateSet", TagValue = "TagValue" };
            UlongArchive = new ArchiveTableStruct { Name = "ulong_archive", TrendId = "TrendId", DateSet = "DateSet", TagValue = "TagValue" };
            DoubleArchive = new ArchiveTableStruct { Name = "double_archive", TrendId = "TrendId", DateSet = "DateSet", TagValue = "TagValue" };
        }

        /// <summary>
        /// Чтение настроек из файла.
        /// </summary>
        /// <param name="path">Путь к файлу настроек.</param>
        /// <param name="filename">Полное имя файла настроек.</param>
        public static Settings Load(string path, string filename)
        {
            Settings settings;

            //проверить, существует ли файл настроек
            if (File.Exists(filename))
            {
                try
                {
                    //попытаться десериализовать настройки из файла
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    using (Stream fstream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        settings = (Settings)serializer.Deserialize(fstream);
                    }
                }
                catch (Exception)
                {
                    //настройки десериализовать не получилось - удалить неправильный файл настроек и создать заново с настройками по умолчанию
                    if (File.Exists(path)) File.Delete(path);
                    settings = new Settings();
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings), new []{typeof(SqlServer), typeof(ArchiveTableStruct)});
                    using (Stream fstream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        serializer.Serialize(fstream, settings);
                    }
                }
            }
            else
            {
                //создать папку с настройками
                Directory.CreateDirectory(path);
                //создать файл с настройками
                settings = new Settings();
                XmlSerializer serializer = new XmlSerializer(typeof(Settings), new []{typeof(SqlServer), typeof(ArchiveTableStruct)});
                using (Stream fstream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(fstream, settings);
                }
            }

            return settings;
        }

        /// <summary>
        /// Сохранение настроек в файл.
        /// </summary>
        /// <param name="settings">Настройки, которые нужно сохранить.</param>
        /// <param name="path">Путь к файлу настроек.</param>
        /// <param name="filename">Полное имя файла настроек.</param>
        public static void Save(Settings settings, string path, string filename)
        {
            //если файл настроек существует - удалить его
            if (File.Exists(filename)) File.Delete(filename);
            //создать папку с настройками
            Directory.CreateDirectory(path);
            //сериализовать настройки в файл
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            using (Stream fstream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                serializer.Serialize(fstream, settings);
            }
        }
    }
}
