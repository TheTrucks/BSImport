using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class AddrMap : ClassMap<Addr>
    {
        public AddrMap()
        {
            Table("amur_dfo.addr");
            Id(x => x.Id);
            Map(x => x.Name);
            Map(x => x.NameShort, "name_short");
            References(x => x.AddrType, "addr_type_id");
            References(x => x.Parent, "parent_id");
            Map(x => x.UTCOffset, "utc_offset");
            HasMany(x => x.Stations).Cascade.All().Inverse();
        }
    }
}
