using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ERP6.Helpers
{
    public class Logger
    {
        private readonly string logDirectory;

        public Logger(string logDir)
        {
            logDirectory = logDir;
            EnsureLogDirectorExists();
        }

        private void EnsureLogDirectorExists()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public void WriteLog(string fileName, string message)
        {
            string logFilePath = Path.Combine(logDirectory, $"{fileName}.txt");
            using (var writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}:{message}");
            }
        }
    }
}
