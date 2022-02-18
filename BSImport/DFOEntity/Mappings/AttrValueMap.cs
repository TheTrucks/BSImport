using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class AttrValueMap : ClassMap<AttrValue>
    {
        public AttrValueMap()
        {
            Table("amur_dfo.attr_value");
            CompositeId()
                .KeyReference(x => x.Station, "station_id")
                .KeyReference(x => x.Attribute, "attribute_id")
                .KeyProperty(x => x.DateStart, "date_start");
            Map(x => x.Value);
        }
    }
}
