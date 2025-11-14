using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopClient.Config;

namespace DesktopClient.Config
{
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    public sealed class MailSettingsStore : IMailSettingsStore
    {
        private readonly object _sync = new();

        public MailSettings Settings { get; private set; } = new();
        public string FilePath { get; }

        public MailSettingsStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ElevatorReports");
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "MailSettings.xml");
            Reload();
        }

        public void Reload()
        {
            lock (_sync)
            {
                if (!File.Exists(FilePath))
                {
                    Settings = new MailSettings(); // дефолтные значения из модели
                    Save();                         // обычная сериализация — без ручных шаблонов
                    return;
                }

                var ser = new XmlSerializer(typeof(MailSettings));
                using var fs = File.OpenRead(FilePath);
                Settings = (MailSettings)ser.Deserialize(fs)!;
            }
        }

        public void Save()
        {
            lock (_sync)
            {
                var ser = new XmlSerializer(typeof(MailSettings));

                // Хотим просто “красивый” XML. Никаких ручных хаков, строгих деклараций и т.п.
                var xws = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) // UTF-8 без BOM
                };

                using var fs = File.Create(FilePath);
                using var xw = XmlWriter.Create(fs, xws);
                ser.Serialize(xw, Settings); // стандартная сериализация добавит нужные xmlns сама
            }
        }
    }
}
