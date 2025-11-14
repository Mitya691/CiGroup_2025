using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DesktopClient.Config
{
    [XmlRoot("Settings")]
    public class MailSettings
    {
        [XmlElement(Order = 1)]
        public string SmtpServer { get; set; } = "smtp.yandex.ru";

        [XmlElement(Order = 2)]
        public int SmtpPort { get; set; } = 465;

        [XmlElement(Order = 3)]
        public string SmtpLogin { get; set; } = "PtiAutoMailer@yandex.ru";

        [XmlElement(Order = 4)]
        public string SmtpPassword { get; set; } = "rxyjejizmbietyla";

        [XmlElement(Order = 5)]
        public Person Sender { get; set; } = new();

        [XmlArray("Recipients", Order = 6)]
        [XmlArrayItem("MailAddress")]
        public List<Person> Recipients { get; set; } = new() { new Person { Name = "Алексей Оводков", Email = "mop3e@mail.ru" } };
    }

    public class Person
    {
        [XmlElement(Order = 1)]
        public string Name { get; set; } = "";

        [XmlElement(Order = 2)]
        public string Email { get; set; } = "";
    }
}
