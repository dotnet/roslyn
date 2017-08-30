using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    public static class CompilationExtensions
    {
        /// <summary>
        /// Gets the low-level operation corresponding to the method's body.
        /// </summary>
        /// <param name="compilation">The compilation containing the method symbol.</param>
        /// <param name="method">The method symbol.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The low-level operation corresponding to the method's body.</returns>
        public static IOperation GetOperation(this Compilation compilation, IMethodSymbol method, CancellationToken cancellationToken = default)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (!compilation.IsIOperationFeatureEnabled())
            {
                throw new InvalidOperationException(CodeAnalysisResources.IOperationFeatureDisabled);
            }

            return compilation.GetOperation(method, cancellationToken);
        }
    }
}
