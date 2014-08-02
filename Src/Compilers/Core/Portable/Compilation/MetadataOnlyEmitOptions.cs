using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Options that can be used to control metadata-only emit.
    /// </summary>
    [Flags]
    public enum MetadataOnlyEmitOptions : int
    {
        /// <summary>
        /// Tolerate errors, producing a PE stream and a success result even in the presence of (some) errors. This parameter is currently ignored.
        /// </summary>
        TolerateErrors = 1 << 0,

        /// <summary>
        /// If not set, exclude (some) private members from the generated assembly when they do not
        /// affect the language semantics of the resulting assembly. This option is currently ignored.
        /// </summary>
        IncludePrivateMembers = 1 << 1,
    }

}
