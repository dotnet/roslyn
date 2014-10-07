using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Services.Options
{
    internal struct OptionInfo : IEquatable<OptionInfo>
    {
        internal readonly string Feature;
        internal readonly string Name;
        internal readonly Type Type;

        public OptionInfo(IOption option)
            : this(option.Feature, option.Name, option.Type)
        {
        }

        public OptionInfo(string feature, string name, Type type)
        {
            this.Feature = feature;
            this.Name = name;
            this.Type = type;
        }

        public bool Equals(OptionInfo other)
        {
            return this.Feature == other.Feature && this.Name == other.Name && this.Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (obj is OptionInfo)
            {
                return Equals((OptionInfo)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Feature.GetHashCode() + this.Name.GetHashCode() + this.Type.GetHashCode();
        }
    }
}
