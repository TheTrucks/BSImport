using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class MeteoDataMap : ClassMap<MeteoData>
    {
        public MeteoDataMap()
        {
            Table("amur_dfo.meteo_data");
            Id(x => x.Id).GeneratedBy.Sequence("amur_dfo.meteo_data_seq");
            References(x => x.Station, "station_id");
            References(x => x.Variable, "variable_id");
            Map(x => x.DateLoc, "date_loc");
            Map(x => x.DateUtc, "date_utc");
            References(x => x.OffsetType, "offset_type");
            Map(x => x.OffsetValue, "offset_value");
            Map(x => x.Value);
        }
    }
}
