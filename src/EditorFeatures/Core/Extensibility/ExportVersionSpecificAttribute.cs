using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Note: This need to be in ascending order, since we compare values in
    /// <see cref="VersionSelector.SelectHighest{T}(IEnumerable{Lazy{T, Extensibility.Composition.VisualStudioVersionMetadata}})"/>.
    /// </summary>
    internal enum VisualStudioVersion
    {
        /// <summary>VS Version 14, aka 'VS 2015'</summary>
        Dev14 = 14,
        /// <summary>VS Version 15, aka 'VS "15"'</summary>
        Dev15 = 15,
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