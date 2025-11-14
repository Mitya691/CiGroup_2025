using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DesktopClient.Model;

namespace DesktopClient.Config
{
    public class SettingsStore : ISettingsStore
    {
        public Settings Settings { get; private set; }

        /// <summary>
        /// Папка с общими рабочими данными.
        /// </summary>
        internal const string ProgramDataFolder = "ElevatorReports";

        /// <summary>
        /// Короткое имя файла настроек.
        /// </summary>
        public const string SettingsFile = "DBStore.ini";

        public void Init()
        {
            //инициализация настроек программы
            LoadSettings();
        }

        /// <summary>
        /// Чтение настроек из файла настроек.
        /// </summary>
        /// <returns></returns>
        public void LoadSettings()
        {
            //попытаться прочитать настройки из папки в ProgramData
            string path = FullSettingsPath(ProgramDataFolder);
            string filename = Path.Combine(path, SettingsFile);
            if (File.Exists(filename))
            {
                try
                {
                    //попытаться десериализовать настройки из файла
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    using (Stream fstream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        Settings = (Settings)serializer.Deserialize(fstream);
                    }
                }
                catch (Exception)
                {
                    //настройки десериализовать не получилось - удалить неправильный файл настроек и создать заново с настройками по умолчанию
                    Settings = new Settings();
                    SaveSettings();
                }
            }
            else
            {
                //файла настроек в папке ProgramData не существует - попытаться прочитать файл настроек из папки программы
                path = Directory.GetCurrentDirectory();
                filename = Path.Combine(path, SettingsFile);
                if (File.Exists(filename))
                {
                    try
                    {
                        //попытаться десериализовать настройки из файла
                        XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                        using (Stream fstream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                        {
                            Settings = (Settings)serializer.Deserialize(fstream);
                        }
                        SaveSettings();
                    }
                    catch (Exception)
                    {
                        //настройки десериализовать не получилось - удалить неправильный файл настроек и создать заново с настройками по умолчанию
                        Settings = new Settings();
                        SaveSettings();
                    }
                }
                else
                {
                    //файлы настроек отсутствуют - создать файл с примером настроек
                    Settings = new Settings();
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Сохранение настроек в файл настроек.
        /// </summary>
        public void SaveSettings()
        {
            //сформировать путь к файлу настроек
            string path = FullSettingsPath(ProgramDataFolder);
            string filename = Path.Combine(path, SettingsFile);

            //создать папку с настройками
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            //сереализовать настройки в файл
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            if (File.Exists(filename)) File.Delete(filename);
            using (Stream fstream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                using (XmlWriter writer = XmlWriter.Create(fstream, new XmlWriterSettings { Indent = true }))
                {
                    serializer.Serialize(writer, Settings);
                }
            }
        }

        /// <summary>
        /// Полный путь к папке InIm в каталоге ProgramData.
        /// </summary>
        public string FullSettingsPath(string inImFolder)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), inImFolder);
        }
    }
}
