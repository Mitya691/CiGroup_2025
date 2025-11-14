using NanoXLSX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InImArchiverService
{
    /// <summary>
    /// Серевер архивации данных.
    /// </summary>
    public class Server : ILoggable
    {
        /// <summary>
        /// Список подключений к разным контроллерам.
        /// </summary>
        private List<S7Connection> _сonnections;
        
        /// <summary>
        /// Событие записи сообщения в лог.
        /// </summary>
        public event LogMessage LogMessageEvent;
        
        /// <summary>
        /// Событие, что сервер успешно запустился.
        /// </summary>
        public event LogMessage EventStarted;

        /// <summary>
        /// Событие, что сервер не смог запуститься.
        /// </summary>
        public event LogMessage EventNotStarted;

        /// <summary>
        /// Краткое имя файла лога без расширения.
        /// </summary>
        private string _logFile;
        
        /// <summary>
        /// Сервер работает.
        /// </summary>
        public bool IsWork
        {
            get
            {
                if (_сonnections == null || _сonnections.Count == 0) return false;

                foreach (S7Connection connection in _сonnections)
                {
                    if (connection.IsWork) return true;
                }

                return false;
            }
        }

        public Server(string logFile = "Server")
        {
            _logFile = logFile;
        }

        /// <summary>
        /// Создание подключений к контроллерам и запуск чтения данных.
        /// </summary>
        public void Start()
        {
            //настройки подключения хранятся в XLSX-файле
            //открыть книгу эксель со списком архивируемых тегов (по умолчанию называется "tags.xlsx")
            string sourceName = Path.Combine(Globals.FullPath(Globals.ProgramDataFolder), "tags.xlsx");
            if (!File.Exists(sourceName))
            {
                LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Не найден файл списка тегов {sourceName}");
                EventNotStarted?.Invoke(null, null, null);
                return;
            }
            string wbName = Path.Combine(Path.GetTempPath(), "tags.xlsx");
            try
            {
                File.Copy(sourceName, wbName, true);
            }
            catch (Exception)
            {
                LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Ошибка копирования файла тегов во папку Temp: {sourceName}, {wbName}");
                EventNotStarted?.Invoke(null, null, null);
                return;
            }
            Workbook wb = Workbook.Load(wbName);

            //загрузить список контроллеров
            //лист "PLC" - список контроллеров
            //строка 1 - заголовок, дальше данные до первой пустой строки
            //колонки: ID (1, целочисленное), Name (2), Address (3), Port (4, целочисленное), Rack (5, целочисленное), Slot(6, целочисленное),
            //Description (7), Enable (8: "1" - теги этого контроллера тег нужно архивировать, что-то другое - нет)
            Worksheet plcWorksheet = wb.GetWorksheet("PLC");
            int rowNum = 1;
            Dictionary<long, PlcData> tagGroups = new Dictionary<long, PlcData>();
            while (plcWorksheet != null && plcWorksheet.HasCell(0, rowNum) && plcWorksheet.GetCell(0, rowNum).Value != null)
            {
                try
                {
                    //проверить, нужно ли архивировать теги этого контроллера
                    string enable = Convert.ToString(plcWorksheet.GetCell(7, rowNum).Value);
                    if(enable != "1") continue;
                    //прочитать данные контроллера и добавить их к словарю
                    PlcData plc = new PlcData
                    {
                        ID = Convert.ToInt64(plcWorksheet.GetCell(0, rowNum).Value),
                        Name = Convert.ToString(plcWorksheet.GetCell(1, rowNum).Value),
                        Address = Convert.ToString(plcWorksheet.GetCell(2, rowNum).Value),
                        Port = Convert.ToInt32(plcWorksheet.GetCell(3, rowNum).Value),
                        Rack = Convert.ToInt32(plcWorksheet.GetCell(4, rowNum).Value),
                        Slot = Convert.ToInt32(plcWorksheet.GetCell(5, rowNum).Value),
                        Description = Convert.ToString(plcWorksheet.GetCell(6, rowNum).Value)
                    };
                    //если ID контроллеров дублируются - дубли не добаавлять
                    if(!tagGroups.ContainsKey(plc.ID)) 
                        tagGroups.Add(plc.ID, plc);
                    else
                        LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Дублирование идентификатора контроллера в строке {rowNum} списка контроллеров.");
                }
                catch (Exception e)
                {
                    LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Ошибка расшифровки строки {rowNum} списка контроллеров.\n{e.Message}");
                }

                rowNum++;
            }

            //загрузить список тегов
            //лист "TAGS" - список тегов
            //строка 1 - заголовок, дальше данные до первой пустой строки
            //колонки: ID (1), PlcID (2), Address (3), TagType (4), Description (5),
            //Enable (6: "1" - тег нужно архивировать, что-то другое - нет)
            Worksheet tagsWorksheet = wb.GetWorksheet("TAGS");
            rowNum = 1;
            while (tagsWorksheet != null && tagsWorksheet.HasCell(0, rowNum) && tagsWorksheet.GetCell(0, rowNum).Value != null)
            {
                try
                {
                    //получить идентификатор контроллера
                    long plcId = Convert.ToInt64(tagsWorksheet.GetCell(1, rowNum).Value);
                    if(!tagGroups.ContainsKey(plcId)) continue;
                    //проверка, нужно ли архивировать этот тег
                    string enable = Convert.ToString(tagsWorksheet.GetCell(5, rowNum).Value);
                    if (enable != "1")
                    {
                        rowNum++;
                        continue;
                    }
                    //прочитать данные тега
                    Tag tag = new Tag
                    {
                        ID = Convert.ToInt64(tagsWorksheet.GetCell(0, rowNum).Value),
                        Address = Convert.ToString(tagsWorksheet.GetCell(2, rowNum).Value),
                        Type = GetTagType(Convert.ToString(tagsWorksheet.GetCell(3, rowNum).Value)),
                        Description = Convert.ToString(tagsWorksheet.GetCell(4, rowNum).Value)
                    };
                    //проверить, корректны ли прочитанные данные
                    if(tag.Type == null)
                    {
                        LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Некорректный тип тега в строке {rowNum} списка тегов.");
                        rowNum++;
                        continue;
                    }
                    if(!S7Utils.S7AddressCheck(tag.Address))
                    {
                        LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Некорректный адрес PLC в строке {rowNum} списка тегов.");
                        rowNum++;
                        continue;
                    }
                    if(!S7Utils.S7TypeCheck(tag.Address, tag.Type.DotNetType))
                    {
                        LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Несовпадение типа адреса и типа тега в строке {rowNum} списка тегов.");
                        rowNum++;
                        continue;
                    }
                    //добавить тег в список
                    if(!tagGroups[plcId].Tags.ContainsKey(tag.ID))
                        tagGroups[plcId].Tags.Add(tag.ID, tag);
                    else
                        LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Дублирование идентификатора тега в строке {rowNum} списка тегов.");
                }
                catch (Exception e)
                {
                    LogMessageEvent?.Invoke(_logFile, "Server.Connect", $"Ошибка расшифровки строки {rowNum} списка тегов.\n{e.Message}");
                }

                rowNum++;
            }

            //настроить подключения к контроллерам
            _сonnections = new List<S7Connection>();
            foreach (KeyValuePair<long, PlcData> tags in tagGroups)
            {
                //для "пустых" контроллеров подключения не создавать, потому что незачем
                if(tags.Value.Tags.Count == 0) continue;
                //создать подключение к контроллеру и добавить его в список подключений
                //ВАЖНО: Чтение данных из подключения запускается сразу после создания подключения.
                S7Connection connection = new S7Connection(tags.Value.Name, tags.Value.Address, tags.Value.Port, tags.Value.Rack, tags.Value.Slot, 
                    Globals.Settings.Period, tags.Value.Tags.Values.ToList(), _logFile);
                _сonnections.Add(connection);
            }

            EventStarted?.Invoke(null, null, null);
        }

        /// <summary>
        /// Получить тип тега по имени типа дотнет.
        /// </summary>
        /// <param name="name">Краткое имя типа дотнет.</param>
        private static TagType GetTagType(string name)
        {
            foreach (TagType tagType in Globals.TagTypes)
            {
                if (tagType.DotNetType.Name.ToLower() == name.ToLower())
                    return tagType;
            }

            return null;
        }

        /// <summary>
        /// Отключение от контроллеров и остановка чтения данных.
        /// </summary>
        public void Stop()
        {
            if(_сonnections == null || _сonnections.Count == 0) return;

            //отключить все подключения
            foreach (S7Connection connection in _сonnections) connection.Disconnect();
            //удалить все подключения
            _сonnections.Clear();
        }
    }
}
