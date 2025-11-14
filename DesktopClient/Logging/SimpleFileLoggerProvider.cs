using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Logging
{
    public sealed class SimpleFileLoggerProvider : ILoggerProvider
    {
        private readonly string _path;
        private readonly object _lock = new();

        public SimpleFileLoggerProvider(string path)
        {
            _path = path;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public ILogger CreateLogger(string categoryName)
            => new SimpleFileLogger(_path, categoryName, _lock);

        public void Dispose() { }
    }

    internal sealed class SimpleFileLogger : ILogger
    {
        private readonly string _path;
        private readonly string _category;
        private readonly object _lock;

        public SimpleFileLogger(string path, string category, object @lock)
        {
            _path = path;
            _category = category;
            _lock = @lock;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var line =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_category} - {formatter(state, exception)}";

            if (exception != null)
                line += Environment.NewLine + exception;

            lock (_lock)
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
        }
    }
}
