using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSImport.DFOEntity.Entity
{
    public class VarCode
    {
        public virtual Variable Variable { get; set; }
        public virtual int Code { get; set; }
        public virtual string Name { get; set; }
        public virtual string NameShort { get; set; }
        public virtual string Description { get; set; }

        public override bool Equals(object obj)
        {
            var typeObj = (VarCode)obj;

            return this.Variable.Id == typeObj.Variable.Id &&
                this.Code == typeObj.Code;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = GetType().GetHashCode();
                hash = (hash * 13) ^ Variable.Id;
                hash = (hash * 13) ^ Code;

                return hash;
            }
        }
    }
}
