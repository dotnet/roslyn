using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    internal enum PreselectionPriority
    {
        Highest = 2,
        Optional = 1,
        Default = 0
    }
}
