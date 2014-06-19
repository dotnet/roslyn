using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class BoundStoreTemporary
    {
        public BoundStoreTemporary(BoundExpression value, object cookie)
            : this(value.Syntax, value, cookie, RefKind.None)
        {
        }
    }
}