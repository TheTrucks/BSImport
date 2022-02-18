using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using BSImport.DFOEntity.Entity;

namespace BSImport.DFOEntity.Mappings
{
    public class VarCodeMap : ClassMap<VarCode>
    {
        public VarCodeMap()
        {
            Table("amur_dfo.variable_code");
            CompositeId()
                .KeyReference(x => x.Variable, "variable_id")
                .KeyProperty(x => x.Code);
            Map(x => x.Name);
            Map(x => x.NameShort, "name_short");
            Map(x => x.Description);
        }
    }
}
