using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityNH.Hibernate;

namespace BSImport
{
    class Program
    {
        static void Main(string[] args)
        {
            var Imp = new Importer(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "params.ff")); // to support being win service with its own scheduler in the future
            Imp.StartImport();
        }
    }
}
