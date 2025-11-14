using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace InImArchiverService
{
    /// <summary>
    /// Глобальные константы, переменные и экземпляры.
    /// </summary>
    internal static class Globals
    {
        private const string _assemblyName = "InImArchiverService";

        internal const string Iso8601DateTime = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Объект для ведения логов.
        /// </summary>
        internal static Log Log { get; private set; }

        /// <summary>
        /// Имя файлал лога.
        /// </summary>
        internal const string LogFile = _assemblyName;

        /// <summary>
        /// Настройки программы.
        /// </summary>
        internal static Settings Settings { get; private set; }

        /// <summary>
        /// Папка с общими рабочими данными.
        /// </summary>
        internal const string ProgramDataFolder = _assemblyName;

        /// <summary>
        /// Папка с лог-файлами.
        /// </summary>
        internal static readonly string LogFolder = $@"{_assemblyName}\Logs";
        
        /// <summary>
        /// Список типов тегов.
        /// </summary>
        public static readonly List<TagType> TagTypes= new List<TagType>
        {
            new TagType(1, typeof(bool), "1 бит"),
            new TagType(2, typeof(byte), "1 байт, беззнаковый"),
            new TagType(3, typeof(sbyte), "1 байт, знаковый"),
            new TagType(4, typeof(double), "Число с плавающей точкой двойной точности"),
            new TagType(5, typeof(float), "Число с плавающей точкой одинарной точности"),
            new TagType(6, typeof(int), "4 байта, знаковый"),
            new TagType(7, typeof(uint), "4 байта, беззнаковый"),
            new TagType(8, typeof(long), "8 байт, знаковый"),
            new TagType(9, typeof(ulong), "8 байт, беззнаковый"),
            new TagType(10, typeof(short), "2 байта, знаковый"),
            new TagType(11, typeof(ushort), "2 байта, беззнаковый")
        };

        internal static void Init()
        {
            //инициализация системы логирования
            Log = new Log(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                LogFolder));

            //инициализация настроек программы
            LoadSettings();
        }

        /// <summary>
        /// Чтение настроек из файла настроек.
        /// </summary>
        /// <returns></returns>
        private static void LoadSettings()
        {
            //сформировать путь к файлу настроек
            string path = FullPath(ProgramDataFolder);
            string filename = Path.Combine(path, $"{_assemblyName}.ini");

            Settings = Settings.Load(path, filename);
        }

        /// <summary>
        /// Сохранение настроек в файл настроек.
        /// </summary>
        internal static void SaveSettings()
        {
            //сформировать путь к файлу настроек
            string path = FullPath(ProgramDataFolder);
            string filename = Path.Combine(path, $"{_assemblyName}.ini");

            Settings.Save(Settings, path, filename);
        }

        /// <summary>
        /// XML-сериализация объекта в строку.
        /// Исходник: http://qaru.site/questions/427691/serialization-of-object-to-xml-and-string-without-rn-special-characters
        /// </summary>
        /// <param name="object">Объект, который нужно сериализовать.</param>
        /// <param name="types">Типы объектов, входящих в этот объект.</param>
        internal static string SerealizeToXmlString(object @object, Type[] types = null)
        {
            string xmlString;

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = true,
                NewLineChars = string.Empty,
                NewLineHandling = NewLineHandling.None
            };

            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                    namespaces.Add(string.Empty, string.Empty);

                    XmlSerializer serializer = types == null || types.Length == 0
                        ? new XmlSerializer(@object.GetType())
                        : new XmlSerializer(@object.GetType(), types);
                    serializer.Serialize(xmlWriter, @object, namespaces);

                    xmlString = stringWriter.ToString();
                    xmlWriter.Close();
                }

                stringWriter.Close();
            }

            return xmlString;
        }

        /// <summary>
        /// Десериализация XML-строки в объект.
        /// Исходник: http://qaru.site/questions/57123/generic-deserialization-of-an-xml-string
        /// </summary>
        /// <param name="xmlString">Строка, которую нужно десереализовать.</param>
        /// <param name="types">Типы объектов, входящих в этот объект.</param>
        internal static T DeserealizeFromXmlString<T>(string xmlString, Type[] types = null)
        {
            XmlSerializer serializer = types == null || types.Length == 0
                ? new XmlSerializer(typeof(T))
                : new XmlSerializer(typeof(T), types);
            using (StringReader sr = new StringReader(xmlString))
                return (T)serializer.Deserialize(sr);
        }

        /// <summary>
        /// Полный путь к указанной папке CecServer.
        /// </summary>
        internal static string FullPath(string folder)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), folder);
        }
    }
}
