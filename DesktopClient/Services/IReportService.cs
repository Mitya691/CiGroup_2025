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
        Task<string> NewDailyReport(DateTime? Start, DateTime? Stop, string firstOperator, string secondOperator);
        Task SendReportAsync(string reportPath, DateTime? date, DateTime? date1, CancellationToken ct = default);
    }
}
