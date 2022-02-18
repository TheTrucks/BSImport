using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class AddrTypeMap : ClassMap<AddrType>
    {
        public AddrTypeMap()
        {
            Table("amur_dfo.addr_type");
            Id(x => x.Id);
            Map(x => x.Name);
            Map(x => x.NameShort, "name_short");
        }
    }
}
