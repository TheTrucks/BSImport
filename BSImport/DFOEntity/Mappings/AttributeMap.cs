using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class AttributeMap : ClassMap<Attr>
    {
        public AttributeMap()
        {
            Table("amur_dfo.attribute");
            Id(x => x.Id);
            Map(x => x.Name);
            HasMany(x => x.AttrValues).Cascade.All();
        }
    }
}
