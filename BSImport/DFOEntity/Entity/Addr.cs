using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class Addr
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string NameShort { get; set; }
        public virtual AddrType AddrType { get; set; }
        public virtual Addr Parent { get; set; }
        public virtual int? UTCOffset { get; set; }
    }
}
