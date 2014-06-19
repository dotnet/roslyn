using System.Diagnostics;
using Roslyn.Compilers.Scripting;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Creates a binder that looks symbols up in a chain of compilations.
    /// </summary>
    /// <remarks>
    /// Compilations built for interactive submissions are chained. The binder created by this factory 
    /// implements the same lookup as if the compilations were nested scopes, the first compilation 
    /// of the chain being the outermost scope.
    /// </remarks>
    internal sealed partial class ChainedCompilationsBinderFactory : ExternalBinderFactory
    {
        private readonly CommonSubmission previousInteraction;
        private readonly bool hasHostObject;

        public ChainedCompilationsBinderFactory(CommonSubmission previousInteraction, bool hasHostObject)
        {
            this.previousInteraction = previousInteraction;
            this.hasHostObject = hasHostObject;
        }

        internal override Binder CreateBinder(Binder next)
        {
            if (hasHostObject)
            {
                next = new HostObjectModelBinder(next);
            }

            if (previousInteraction != null)
            {
                next = new ChainedCompilationsBinder(previousInteraction, next);
            }

            return next;
        }
    }
}
