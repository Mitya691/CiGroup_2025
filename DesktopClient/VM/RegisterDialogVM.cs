using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DesktopClient.Helpers;
using DesktopClient.Model;
using DesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopClient.VM
{
    public class RegisterDialogVM : ViewModelBase
    {
        public User User { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set 
            {
                if (Set(ref _name, value))
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _family;
        public string Family
        {
            get { return _family; }
            set
            { 
                if (!Set(ref _family, value))
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _patronymic;
        public string Patronymic
        {
            get { return _patronymic; }
            set
            {
                if(!Set(ref _patronymic, value))
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                if(!Set(ref _email, value))
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _post;
        public string Post
        {
            get { return _post; }
            set
            {
                if(!Set(ref _post, value))
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _login;
        public string Login
        {
            get { return _login; }
            set
            {
                if(!Set(ref _login, value))
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (!Set(ref _password, value)) 
                    RegistrationCommand?.RaiseCanExecuteChanged();
            }
        }

        

        private readonly MainWindowVM _shell;
        private readonly IRegistrationService _registration;
        public RelayCommand CloseCommand { get; }
        public AsyncRelayCommand RegistrationCommand { get; }

        public RegisterDialogVM(MainWindowVM shell, IRegistrationService registration) 
        {
            _shell = shell;
            _registration = registration;
            RegistrationCommand = new AsyncRelayCommand(DoRegistration, CanRegistration);
            CloseCommand = new RelayCommand(() => _shell.NavigateTo(App.Services.GetRequiredService<AutorizationDialogVM>()));
        }

        private async Task DoRegistration()
        {
            var req = new SignUpRequest(Name, Family, Patronymic, Email, Post, Login, Password);

            var res = await _registration.SignUpAsync(req);
            //тут логика регистрации
        }

        private bool CanRegistration() 
        {
           return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Family)
                && !string.IsNullOrWhiteSpace(Patronymic) && !string.IsNullOrWhiteSpace(Email)
                && !string.IsNullOrWhiteSpace(Post) && !string.IsNullOrWhiteSpace(Login)
                && !string.IsNullOrWhiteSpace(Password);
        }
    }  
}
