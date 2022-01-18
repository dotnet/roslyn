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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    [ExportLanguageService(typeof(IStructuralTypeDisplayService), LanguageNames.CSharp), Shared]
    internal class CSharpStructuralTypeDisplayService : AbstractStructuralTypeDisplayService
    {
        private static readonly SymbolDisplayFormat s_minimalWithoutContainingType =
            s_minimalWithoutExpandedTuples.WithMemberOptions(s_minimalWithoutExpandedTuples.MemberOptions & ~SymbolDisplayMemberOptions.IncludeContainingType);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpStructuralTypeDisplayService()
        {
        }

        protected override ImmutableArray<SymbolDisplayPart> GetNormalAnonymousTypeParts(
            INamedTypeSymbol anonymousType,
            SemanticModel semanticModel,
            int position)
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
                members.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, property, property.Name));
            }

            members.AddRange(Space());
            members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CloseBraceToken)));

            return members.ToImmutable();
        }

        protected override ImmutableArray<SymbolDisplayPart> GetDelegateAnonymousTypeParts(
            INamedTypeSymbol anonymousType,
            SemanticModel semanticModel,
            int position)
        {
            using var _ = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var parts);

            parts.AddRange(anonymousType.DelegateInvokeMethod.ToMinimalDisplayParts(semanticModel, position, s_minimalWithoutContainingType));

            // change the display from `bool Invoke(int x, string y)` to `bool delegate(int x, string y)`. `delegate`
            // helps make it clear what sort of signature we're showing, and the lack of the name is appropriate as this
            // is an anonymous delegate.
            var namePart = parts.FirstOrNull(p => p.Kind == SymbolDisplayPartKind.MethodName && p.ToString() == anonymousType.DelegateInvokeMethod.Name);
            if (namePart != null)
            {
                var index = parts.IndexOf(namePart.Value);
                parts[index] = new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "delegate");
            }

            return parts.ToImmutable();
        }
    }
}
