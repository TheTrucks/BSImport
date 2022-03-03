using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class StationType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Station> Stations { get; set; }
    }
}
