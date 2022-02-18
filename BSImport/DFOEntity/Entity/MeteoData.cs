using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class MeteoData
    {
        public virtual long? Id { get; set; }
        public virtual Station Station { get; set; }
        public virtual Variable Variable { get; set; }
        public virtual DateTime DateLoc { get; set; }
        public virtual DateTime DateUtc { get; set; }
        public virtual OffsetType OffsetType { get; set; }
        public virtual double OffsetValue { get; set; }
        public virtual double? Value { get; set; }
    }
}
