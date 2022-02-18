using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class AttrValue
    {
        public virtual Attr Attribute { get; set; }
        public virtual Station Station { get; set; }
        public virtual DateTime DateStart { get; set; }
        public virtual string Value { get; set; }

        public override bool Equals(object obj)
        {
            var typeObj = (AttrValue)obj;

            return this.Station.Id == typeObj.Station.Id &&
                this.Attribute.Id == typeObj.Attribute.Id &&
                this.DateStart == typeObj.DateStart;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Station.Id;
                hash += (hash * 7) ^ Attribute.Id;
                hash += (hash * 7) ^ DateStart.GetHashCode();
                hash -= Value.GetHashCode();

                return hash;
            }
        }
    }
}
