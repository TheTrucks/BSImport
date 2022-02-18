using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class StationMap : ClassMap<Station>
    {
        public StationMap()
        {
            Table("amur_dfo.stations");
            Id(x => x.Id);
            Map(x => x.Name);
            Map(x => x.Code);
            References(x => x.StationType, "station_type_id");
            Map(x => x.Altitude);
            Map(x => x.Latitude);
            Map(x => x.Longtitude, "longitude");
            References(x => x.Region, "region_id");
        }
    }
}
