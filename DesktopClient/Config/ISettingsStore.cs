using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopClient.Model;

namespace DesktopClient.Config
{
    public interface ISettingsStore
    {
        Settings Settings { get; }
        void LoadSettings();
        void SaveSettings();
    }
}
