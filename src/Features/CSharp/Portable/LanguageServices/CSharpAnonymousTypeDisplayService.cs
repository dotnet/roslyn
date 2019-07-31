// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    [ExportLanguageService(typeof(IAnonymousTypeDisplayService), LanguageNames.CSharp), Shared]
    internal class CSharpAnonymousTypeDisplayService : AbstractAnonymousTypeDisplayService
    {
        [ImportingConstructor]
        public CSharpAnonymousTypeDisplayService()
        {
        }

        public override IEnumerable<SymbolDisplayPart> GetAnonymousTypeParts(
            INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position, ISymbolDisplayService displayService)
        {
            var members = new List<SymbolDisplayPart>();

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
                members.AddRange(displayService.ToMinimalDisplayParts(semanticModel, position, property.Type).Select(p => p.MassageErrorTypeNames("?")));
                members.AddRange(Space());
                members.Add(new SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, property, property.Name));
            }

            members.AddRange(Space());
            members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CloseBraceToken)));

            return members;
        }
    }
}
