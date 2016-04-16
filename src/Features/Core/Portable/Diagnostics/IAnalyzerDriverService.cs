// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
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
        void ComputeDeclarationsInSpan(SemanticModel model, TextSpan span, bool getSymbol, List<DeclarationInfo> builder, CancellationToken cancellationToken);
    }
}
