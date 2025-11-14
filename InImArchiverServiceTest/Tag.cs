using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InImArchiverService
{
    public class Tag : INotifyPropertyChanged
    {
        private long _id;
        
        /// <summary>
        /// ID тега в базе данных.
        /// </summary>
        public long ID
        {
            get => _id;
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ID)));
            }
        }

        private string _address;
        /// <summary>
        /// Адрес тега в контроллере.
        /// </summary>
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Address)));
            }
        }

        /// <summary>
        /// Тип тега.
        /// </summary>
        public TagType Type;

        private string _description;
        /// <summary>
        /// Описание тега.
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

        private object _value;
        /// <summary>
        /// Прочитанное значение тега.
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}