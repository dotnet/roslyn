// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices;

internal partial class CSharpSymbolDisplayService(Host.LanguageServices services) : AbstractSymbolDisplayService(services)
{
    protected override AbstractSymbolDescriptionBuilder CreateDescriptionBuilder(SemanticModel semanticModel, int position, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        => new SymbolDescriptionBuilder(semanticModel, position, LanguageServices, options, cancellationToken);
}
