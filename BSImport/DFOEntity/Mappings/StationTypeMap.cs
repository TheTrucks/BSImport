using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class StationTypeMap : ClassMap<StationType>
    {
        public StationTypeMap()
        {
            Table("amur_dfo.station_type");
            Id(x => x.Id);
            Map(x => x.Name);
            HasMany(x => x.Stations).Cascade.All().Inverse();
        }
    }
}
