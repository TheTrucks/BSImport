using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class ForecastDataMap : ClassMap<ForecastData>
    {
        public ForecastDataMap()
        {
            Table("amur_dfo.forecast_data");
            Id(x => x.Id).GeneratedBy.Sequence("amur_dfo.forecast_data_seq");
            References(x => x.Station, "station_id");
            References(x => x.Variable, "variable_id");
            Map(x => x.DateFrom, "date_from");
            Map(x => x.Depths);
            Map(x => x.Value);
            Map(x => x.Addition);
        }
    }
}
