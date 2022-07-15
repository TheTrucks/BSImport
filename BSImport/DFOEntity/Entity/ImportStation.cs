using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class ImportStation
    {
        public virtual int Id { get; set; }
        public virtual int StationType { get; set; }
        public virtual string Code { get; set; }
        public virtual bool IsStrong { get; set; }
    }
}
