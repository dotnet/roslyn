using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    // During overload resolution we need to know the origin of a method group. 
    internal enum MethodGroupOrigin
    {
        Other,
        SimpleName,
        MemberAccessThroughType,
        MemberAccessThroughValue
    }
}