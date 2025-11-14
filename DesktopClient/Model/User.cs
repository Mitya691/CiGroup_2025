using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Model
{
    public class User
    {
        public string Name { get; set; }
        public string Family { get; set; }
        public string Patronymic { get; set; }
        public string Email { get; set; }
        public string Post { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; } //случайная генерация и приклеивание к паролю, затем хэширование происходит

        public string FullName =>
           string.Join(" ", new[] { Family, Name, Patronymic }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
