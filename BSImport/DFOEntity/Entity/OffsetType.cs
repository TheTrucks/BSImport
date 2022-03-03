using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class OffsetType
    {
        public virtual int? Id { get; set; } // since nhibernate distinguishes int id == 0 as new value always, it should be nullable to avoid conflict with existing offset_type == 0
        public virtual string Name { get; set; }
        public virtual IList<MeteoData> MeteoData { get; set; }
    }
}
