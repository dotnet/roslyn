using System;
using System.Collections.Generic;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal interface IOrderedReadOnlySet<T> : IReadOnlySet<T>, IReadOnlyList<T>
    {
    }
}
