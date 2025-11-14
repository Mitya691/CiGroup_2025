using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InImArchiverService
{
    /// <summary>
    /// Данные контроллера.
    /// </summary>
    internal class PlcData
    {
        /// <summary>
        /// Идентификатор контроллера.
        /// </summary>
        public long ID;

        /// <summary>
        /// Наименование контроллера.
        /// </summary>
        public string Name;

        /// <summary>
        /// IP-адрес контроллера.
        /// </summary>
        public string Address;

        /// <summary>
        /// Порт контроллера.
        /// </summary>
        public int Port;

        /// <summary>
        /// Корзина контроллера.
        /// </summary>
        public int Rack;

        /// <summary>
        /// Разъём контроллера.
        /// </summary>
        public int Slot;

        /// <summary>
        /// Описание контроллера.
        /// </summary>
        public string Description;

        /// <summary>
        /// Список тегов контроллера.
        /// </summary>
        public Dictionary<long, Tag> Tags = new Dictionary<long, Tag>();
    }
}
