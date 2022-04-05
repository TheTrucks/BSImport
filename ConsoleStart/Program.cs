using System;
using BSImport;

namespace ConsoleStart
{
    class Program
    {
        static void Main(string[] args)
        {
            string ParamsFile = InputFilename("Enter file name containing parameters list (leave empty to default on 'params.ff')", "params.ff");

            string StationsFile = InputFilename("Enter file name containing stations list (leave empty to default on 'restricts.ff')", "restricts.ff");

            string StartTime = InputDateTime("Enter start date in format \"yyyy-MM-dd HH:mm\" (leave empty to default on current UTC time - 1 hour");

            var Imp = new Importer(ParamsFile, StationsFile, "nullcache", 1);
            Imp.StartImport(StartTime);
        }

        private static string InputFilename(string Message, string Default)
        {
            string Result;
            while (true)
            {
                Console.WriteLine(Message);
                Result = Console.ReadLine().Trim();
                if (String.IsNullOrEmpty(Result))
                {
                    Console.WriteLine($"Default filename \'{Default}\' used");
                    Result = Default;
                    break;
                }
                
                if (!System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Result)))
                {
                    Console.WriteLine($"Error: There is no file named \'{Result}\' in the base directory.");
                    continue;
                }
                break;
            }
            return Result;
        }

        private static string InputDateTime(string Message)
        {
            string Result;
            while (true)
            {
                Console.WriteLine(Message);
                Result = Console.ReadLine().Trim();
                if (String.IsNullOrEmpty(Result))
                {
                    Console.WriteLine($"Current UTC -1 hour used");
                    break;
                }

                if (!DateTime.TryParseExact(Result, "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                {
                    Console.WriteLine("Inadequate date\\time format");
                    continue;
                }
                break;
            }
            return Result;
        }
    }
}
