using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Helpers
{
    public class FioProcess
    {
        public static string ToShortFio(string fio)
        {
            if (string.IsNullOrWhiteSpace(fio)) return string.Empty;

            var parts = fio
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            string surname = parts[0];
            string i1 = parts.Length > 1 ? char.ToUpper(parts[1][0]) + "." : "";
            string i2 = parts.Length > 2 ? char.ToUpper(parts[2][0]) + "." : "";

            return string.IsNullOrEmpty(i1) ? surname : $"{surname} {i1}{i2}";
        }
    }
}
