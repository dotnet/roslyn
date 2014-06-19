using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Creates binders external to the host object model.
    /// </summary>
    internal sealed partial class HostObjectModelBinderFactory : ExternalBinderFactory
    {
        private readonly CompilationChain chain;

        public HostObjectModelBinderFactory(CompilationChain chain)
        {
            this.chain = chain;
        }

        internal override Binder CreateBinder(Binder next)
        {
            return new ExternalBinder(chain, next);
        }
    }
}
