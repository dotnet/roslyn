using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal struct PackageReference
    {
        internal string Name { get; }
        internal string Version { get; }
        internal bool IsVersionAttribute { get; }

        internal PackageReference(string name, string version, bool isVersionAttribute)
        {
            Name = name;
            Version = version;
            IsVersionAttribute = isVersionAttribute;
        }

        public override string ToString() => $"{Name} - {Version}";
    }
}
