using System;
using BSImport;

namespace ConsoleStart
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Enter file name containing parameters list (leave empty to default on 'params.ff')");
            string ParamsFile = Console.ReadLine();
            if (String.IsNullOrEmpty(ParamsFile))
                ParamsFile = "params.ff";

            Console.WriteLine($"Enter file name containing stations list (leave empty to default on 'restricts.ff')");
            string StationsFile = Console.ReadLine();
            if (String.IsNullOrEmpty(StationsFile))
                StationsFile = "restricts.ff";

            Console.WriteLine($"Enter start date (leave empty to default on current UTC time - 1 hour");
            string StartTime = Console.ReadLine();

            var Imp = new Importer(ParamsFile, StationsFile, "nullcache", 1);
            Imp.StartImport(StartTime);
        }
    }
}
