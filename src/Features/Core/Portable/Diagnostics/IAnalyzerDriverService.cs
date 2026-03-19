// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal interface IAnalyzerDriverService : ILanguageService
{
    /// <summary>
    /// Computes the <see cref="DeclarationInfo"/> for all the declarations whose span overlaps with the given <paramref name="span"/>.
    /// </summary>
    /// <param name="model">The semantic model for the document.</param>
    /// <param name="span">Span to get declarations.</param>
    ImmutableArray<DeclarationInfo> ComputeDeclarationsInSpan(SemanticModel model, TextSpan span, CancellationToken cancellationToken);
}
