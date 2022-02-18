using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class Station
    {
        public virtual int Id
        {
            get; set;
        }
        public virtual string Code { get; set; }
        public virtual string Name
        {
            get; set;
        }
        public virtual StationType StationType { get; set; }
        public virtual double? Altitude { get; set; }
        public virtual double? Longtitude { get; set; }
        public virtual double? Latitude { get; set; }
        public virtual Addr Region { get; set; }
}
}
