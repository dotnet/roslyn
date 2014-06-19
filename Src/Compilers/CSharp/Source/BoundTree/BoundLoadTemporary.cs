using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class BoundLoadTemporary
    {
        public BoundStoreTemporary Store { get; private set; }
        public BoundLoadTemporary(BoundStoreTemporary temporary)
            : this(temporary.Syntax, temporary.Cookie)
        {
            this.Store = temporary;
        }
    }
}