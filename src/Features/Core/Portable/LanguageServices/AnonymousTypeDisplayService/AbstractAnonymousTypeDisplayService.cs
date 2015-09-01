// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract partial class AbstractAnonymousTypeDisplayService : IAnonymousTypeDisplayService
    {
        public abstract IEnumerable<SymbolDisplayPart> GetAnonymousTypeParts(
            INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position,
            ISymbolDisplayService displayService);

        public AnonymousTypeDisplayInfo GetNormalAnonymousTypeDisplayInfo(
            ISymbol orderSymbol,
            IEnumerable<INamedTypeSymbol> directNormalAnonymousTypeReferences,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService displayService)
        {
            if (!directNormalAnonymousTypeReferences.Any())
            {
                return new AnonymousTypeDisplayInfo(
                    SpecializedCollections.EmptyDictionary<INamedTypeSymbol, string>(),
                    SpecializedCollections.EmptyList<SymbolDisplayPart>());
            }

            var transitiveNormalAnonymousTypeReferences = GetTransitiveNormalAnonymousTypeReferences(directNormalAnonymousTypeReferences.ToSet());
            transitiveNormalAnonymousTypeReferences = OrderAnonymousTypes(transitiveNormalAnonymousTypeReferences, orderSymbol);

            IList<SymbolDisplayPart> anonymousTypeParts = new List<SymbolDisplayPart>();
            anonymousTypeParts.Add(PlainText(FeaturesResources.AnonymousTypes));
            anonymousTypeParts.AddRange(LineBreak());

            for (int i = 0; i < transitiveNormalAnonymousTypeReferences.Count; i++)
            {
                if (i != 0)
                {
                    anonymousTypeParts.AddRange(LineBreak());
                }

                var anonymousType = transitiveNormalAnonymousTypeReferences[i];
                anonymousTypeParts.AddRange(Space(count: 4));
                anonymousTypeParts.Add(Part(SymbolDisplayPartKind.ClassName, anonymousType, anonymousType.Name));
                anonymousTypeParts.AddRange(Space());
                anonymousTypeParts.Add(PlainText(FeaturesResources.Is));
                anonymousTypeParts.AddRange(Space());
                anonymousTypeParts.AddRange(GetAnonymousTypeParts(anonymousType, semanticModel, position, displayService));
            }

            // Now, inline any delegate anonymous types we've got.
            anonymousTypeParts = this.InlineDelegateAnonymousTypes(anonymousTypeParts, semanticModel, position, displayService);

            // Finally, assign a name to all the anonymous types.
            var anonymousTypeToName = GenerateAnonymousTypeNames(transitiveNormalAnonymousTypeReferences);
            anonymousTypeParts = AnonymousTypeDisplayInfo.ReplaceAnonymousTypes(anonymousTypeParts, anonymousTypeToName);

            return new AnonymousTypeDisplayInfo(anonymousTypeToName, anonymousTypeParts);
        }

        private Dictionary<INamedTypeSymbol, string> GenerateAnonymousTypeNames(
            IList<INamedTypeSymbol> anonymousTypes)
        {
            int current = 0;
            var anonymousTypeToName = new Dictionary<INamedTypeSymbol, string>();
            foreach (var type in anonymousTypes)
            {
                anonymousTypeToName[type] = GenerateAnonymousTypeName(current);
                current++;
            }

            return anonymousTypeToName;
        }

        private string GenerateAnonymousTypeName(int current)
        {
            char c = (char)('a' + current);
            if (c >= 'a' && c <= 'z')
            {
                return "'" + c.ToString();
            }

            return "'" + current.ToString();
        }

        private IList<INamedTypeSymbol> OrderAnonymousTypes(
            IList<INamedTypeSymbol> transitiveAnonymousTypeReferences,
            ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                var method = (IMethodSymbol)symbol;
                return transitiveAnonymousTypeReferences.OrderBy(
                    (n1, n2) =>
                    {
                        var index1 = method.TypeArguments.IndexOf(n1);
                        var index2 = method.TypeArguments.IndexOf(n2);
                        index1 = index1 < 0 ? int.MaxValue : index1;
                        index2 = index2 < 0 ? int.MaxValue : index2;

                        return index1 - index2;
                    }).ToList();
            }
            else if (symbol is IPropertySymbol)
            {
                var property = (IPropertySymbol)symbol;
                return transitiveAnonymousTypeReferences.OrderBy(
                    (n1, n2) =>
                    {
                        if (n1.Equals(property.ContainingType) && !n2.Equals(property.ContainingType))
                        {
                            return -1;
                        }
                        else if (!n1.Equals(property.ContainingType) && n2.Equals(property.ContainingType))
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToList();
            }

            return transitiveAnonymousTypeReferences;
        }

        private static IList<INamedTypeSymbol> GetTransitiveNormalAnonymousTypeReferences(
            ISet<INamedTypeSymbol> anonymousTypeReferences)
        {
            var transitiveReferences = new List<INamedTypeSymbol>();
            var visitor = new NormalAnonymousTypeCollectorVisitor(transitiveReferences);

            foreach (var type in anonymousTypeReferences)
            {
                type.Accept(visitor);
            }

            return transitiveReferences;
        }

        protected IEnumerable<SymbolDisplayPart> LineBreak(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
            }
        }

        protected SymbolDisplayPart PlainText(string text)
        {
            return Part(SymbolDisplayPartKind.Text, text);
        }

        private SymbolDisplayPart Part(SymbolDisplayPartKind kind, string text)
        {
            return Part(kind, null, text);
        }

        private SymbolDisplayPart Part(SymbolDisplayPartKind kind, ISymbol symbol, string text)
        {
            return new SymbolDisplayPart(kind, symbol, text);
        }

        protected IEnumerable<SymbolDisplayPart> Space(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
            }
        }

        protected SymbolDisplayPart Punctuation(string text)
        {
            return Part(SymbolDisplayPartKind.Punctuation, text);
        }

        protected SymbolDisplayPart Keyword(string text)
        {
            return Part(SymbolDisplayPartKind.Keyword, text);
        }
    }
}
