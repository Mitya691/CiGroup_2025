using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DesktopClient.Model
{
    public class Settings
    {
        /// <summary>
        /// Имя сервера базы данных.
        /// </summary>
        public string DbServer { get; set; }

        /// <summary>
        /// Порт подключения к серверу базы данных.
        /// </summary>
        public int DbPort { get; set; }

        /// <summary>
        /// База данных на сервере.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Пользователь базы данных.
        /// </summary>
        public string DbUser { get; set; }

        /// <summary>
        /// Пользователь базы данных.
        /// </summary>
        public string DbPassword { get; set; }

        /// <summary>
        /// Строка подключения к базе данных.
        /// </summary>
        [XmlIgnore]
        public string DbConnectionString => $"Server={DbServer};Port={DbPort};User={DbUser};Password={DbPassword};Database={Database};AllowLoadLocalInfile=true;";

        /// <summary>
        /// Создать настройки со значениями по-умолчанию.
        /// </summary>
        public Settings()
        {
            DbServer = "127.0.0.1";
            DbPort = 3306;
            Database = "elevatordb";
            DbUser = "elevator_user";
            DbPassword = "123456";
        }

        /// <summary>
        /// Создать настройки, скопировав их значения из других настроек.
        /// </summary>
        /// <param name="settings"></param>
        public Settings(Settings settings)
        {
            Copy(settings);
        }

        /// <summary>
        /// Скопировать значения других настроек в эти.
        /// </summary>
        /// <param name="settings"></param>
        public void Copy(Settings settings)
        {
            DbServer = settings.DbServer;
            DbPort = settings.DbPort;
            Database = settings.Database;
            DbUser = settings.DbUser;
            DbPassword = settings.DbPassword;
        }
    }
}
