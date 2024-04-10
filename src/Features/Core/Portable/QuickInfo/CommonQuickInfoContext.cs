// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal readonly struct CommonQuickInfoContext(
    SolutionServices services,
    SemanticModel semanticModel,
    int position,
    SymbolDescriptionOptions options,
    CancellationToken cancellationToken)
{
    public readonly SolutionServices Services = services;
    public readonly SemanticModel SemanticModel = semanticModel;
    public readonly int Position = position;
    public readonly SymbolDescriptionOptions Options = options;
    public readonly CancellationToken CancellationToken = cancellationToken;
}
