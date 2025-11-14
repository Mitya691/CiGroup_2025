using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopClient.Model;

namespace DesktopClient.Services
{
    public interface IPollingService
    {
        int LagSeconds { get; set; }
        event Action<IReadOnlyList<Card>>? CardsCreated;
        Task StartAsync(CancellationToken ct = default);
        void Stop();
    }
}
