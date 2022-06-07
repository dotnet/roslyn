// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal interface IAddParameterService
    {
        /// <summary>
        /// Checks if there are indications that there might be more than one declarations that need to be fixed.
        /// The check does not look-up if there are other declarations (this is done later in the CodeAction).
        /// </summary>
        bool HasCascadingDeclarations(IMethodSymbol method);

        /// <summary>
        /// Adds a parameter to a method.
        /// </summary>
        /// <param name="newParameterIndex"><see langword="null"/> to add as the final parameter</param>
        /// <returns></returns>
        Task<Solution> AddParameterAsync(
            Document invocationDocument,
            IMethodSymbol method,
            ITypeSymbol newParamaterType,
            RefKind refKind,
            string parameterName,
            int? newParameterIndex,
            bool fixAllReferences,
            CancellationToken cancellationToken);
    }
}
