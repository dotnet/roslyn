// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices;

[ExportLanguageService(typeof(IStructuralTypeDisplayService), LanguageNames.CSharp), Shared]
internal class CSharpStructuralTypeDisplayService : AbstractStructuralTypeDisplayService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpStructuralTypeDisplayService()
    {
    }

    protected override ISyntaxFacts SyntaxFactsService => CSharpSyntaxFacts.Instance;

    protected override ImmutableArray<SymbolDisplayPart> GetNormalAnonymousTypeParts(
        INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position)
    {
        using var _ = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var members);

        members.Add(Keyword(SyntaxFacts.GetText(SyntaxKind.NewKeyword)));
        members.AddRange(Space());
        members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.OpenBraceToken)));
        members.AddRange(Space());

        var first = true;
        foreach (var property in anonymousType.GetValidAnonymousTypeProperties())
        {
            if (!first)
            {
                members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CommaToken)));
                members.AddRange(Space());
            }

            first = false;
            members.AddRange(property.Type.ToMinimalDisplayParts(semanticModel, position, s_minimalWithoutExpandedTuples).Select(p => p.MassageErrorTypeNames("?")));
            members.AddRange(Space());
            members.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, property, CSharpSyntaxFacts.Instance.EscapeIdentifier(property.Name)));
        }

        members.AddRange(Space());
        members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CloseBraceToken)));

        return members.ToImmutableAndClear();
    }
}
