using DesktopClient.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Helpers
{
    public class CurrentUserStore : INotifyPropertyChanged
    {
        private User? _currentUser;
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    PropertyChanged?.Invoke(this, new(nameof(CurrentUser)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
