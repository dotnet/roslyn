using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    public abstract class LocalBinding : SyntaxBinding
    {
        public abstract IList<LocalSymbol> Locals { get; }
        internal abstract ImmutableMap<SyntaxNode, BlockBaseBinderContext> BlockMap { get; }
    }
}