using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Config
{
    public interface IMailSettingsStore
    {
        MailSettings Settings { get; }
        string FilePath { get; }
        void Reload();
        void Save();
    }
}
