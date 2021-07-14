// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal interface IAnalyzerDriverService : ILanguageService
    {
        /// <summary>
        /// Computes the <see cref="DeclarationInfo"/> for all the declarations whose span overlaps with the given <paramref name="span"/>.
        /// </summary>
        /// <param name="model">The semantic model for the document.</param>
        /// <param name="span">Span to get declarations.</param>
        /// <param name="getSymbol">Flag indicating whether <see cref="DeclarationInfo.DeclaredSymbol"/> should be computed for the returned declaration infos.
        /// If false, then <see cref="DeclarationInfo.DeclaredSymbol"/> is always null.</param>
        /// <param name="builder">Builder to add computed declarations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        void ComputeDeclarationsInSpan(SemanticModel model, TextSpan span, bool getSymbol, ArrayBuilder<DeclarationInfo> builder, CancellationToken cancellationToken);
    }
}
