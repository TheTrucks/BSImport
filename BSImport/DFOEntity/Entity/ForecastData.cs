using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class ForecastData
    {
        public virtual long? Id { get; set; }
        public virtual Station Station { get; set; }
        public virtual Variable Variable { get; set; }
        public virtual DateTime DateFrom { get; set; }
        public virtual int Depths { get; set; }
        public virtual double? Value { get; set; }
        public virtual string Addition { get; set; }
    }
}
