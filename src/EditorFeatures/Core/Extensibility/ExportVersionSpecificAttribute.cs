using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor
{
    internal enum VisualStudioVersion
    {
        VS2015 = 14,
        VS15 = 15,
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportVersionSpecificAttribute : ExportAttribute
    {
        public VisualStudioVersion Version { get; }
        public ExportVersionSpecificAttribute(Type contractType, VisualStudioVersion version)
            : base(contractType)
        {
            this.Version = version;
        }
    }
}
