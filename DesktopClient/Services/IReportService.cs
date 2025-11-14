using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Services
{
    public interface IReportService
    {
        Task<string> NewReport(DateTime? Start, DateTime? Stop, string shiftOperator);
        Task<string> NewDailyReport(DateTime? Start, DateTime? Stop);
        Task SendReportAsync(string reportPath, CancellationToken ct = default);
    }
}
