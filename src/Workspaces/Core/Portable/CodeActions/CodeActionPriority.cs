using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal enum CodeActionPriority
    {
        //
        // Summary:
        //     No particular priority.
        None = 0,
        //
        // Summary:
        //     Low priority suggestion.
        Low = 1,
        //
        // Summary:
        //     Medium priority suggestion.
        Medium = 2,
        //
        // Summary:
        //     High priority suggestion.
        High = 3
    }
}
