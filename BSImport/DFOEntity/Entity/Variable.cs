using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class Variable
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<MeteoData> MeteoData { get; set; }
        public virtual IList<ForecastData> ForecastData { get; set; }
        public virtual IList<VarCode> VarCodes { get; set; }
    }
}
