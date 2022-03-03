using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class VariableMap : ClassMap<Variable>
    {
        public VariableMap()
        {
            Table("amur_dfo.variable");
            Id(x => x.Id);
            Map(x => x.Name);
            HasMany(x => x.MeteoData).Cascade.All().Inverse();
            HasMany(x => x.ForecastData).Cascade.All().Inverse();
            HasMany(x => x.VarCodes).Cascade.All().Inverse();
        }
    }
}
