using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class ImportStationMap : ClassMap<ImportStation>
    {
        public ImportStationMap()
        {
            Table("amur_dfo.import_station");
            Id(x => x.Id);
            Map(x => x.StationType, "station_type");
            Map(x => x.Code);
            Map(x => x.IsStrong, "is_strong");
        }
    }
}
