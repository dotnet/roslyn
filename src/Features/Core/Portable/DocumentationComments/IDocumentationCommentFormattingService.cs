// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal interface IDocumentationCommentFormattingService : ILanguageService
{
    string? Format(string? rawXmlText, Compilation? compilation = null);
    ImmutableArray<TaggedText> Format(
        string? rawXmlText, ISymbol symbol, SemanticModel semanticModel, int position,
        SymbolDisplayFormat format, StructuralTypeDisplayInfo typeDisplayInfo, CancellationToken cancellationToken);
}
