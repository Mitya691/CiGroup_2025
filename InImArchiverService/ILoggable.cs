using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InImArchiverService
{
    /// <summary>
    /// Делегат для обработки сообщений, сохраняемых в логи.
    /// </summary>
    /// <param name="title">Заголовок сообщения.</param>
    /// <param name="mes">Текст сообщения.</param>
    public delegate void LogMessage(string logfile, string title, string mes);

    /// <summary>
    /// Интерфейс для записи сообщений в лог.
    /// </summary>
    public interface ILoggable
    {
        /// <summary>
        /// Событие, инициирующее запись в лог.
        /// </summary>
        event LogMessage LogMessageEvent;
    }
}