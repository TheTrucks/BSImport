using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class OffsetTypeMap : ClassMap<OffsetType>
    {
        public OffsetTypeMap()
        {
            Table("amur_dfo.offset_type");
            Id(x => x.Id);
            Map(x => x.Name);
            HasMany(x => x.MeteoData).Cascade.All().Inverse();
        }
    }
}
