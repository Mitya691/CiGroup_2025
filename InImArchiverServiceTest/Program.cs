using NanoXLSX;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InImArchiverService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Globals.Init();
            Server server = new Server(Globals.LogFile);
            Globals.Log.Register(server); //тут произошли подписки на события для логов
            server.Start();
            Console.WriteLine("Соединения созданы.");
            Console.ReadLine();
            server.Stop();
            Console.WriteLine("Соединения разорваны.");
            Console.ReadLine();
        }

    }
}
