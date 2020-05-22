// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract partial class AbstractAnonymousTypeDisplayService : IAnonymousTypeDisplayService
    {
        public abstract IEnumerable<SymbolDisplayPart> GetAnonymousTypeParts(
            INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position);

        public AnonymousTypeDisplayInfo GetNormalAnonymousTypeDisplayInfo(
            ISymbol orderSymbol,
            IEnumerable<INamedTypeSymbol> directNormalAnonymousTypeReferences,
            SemanticModel semanticModel,
            int position)
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
            anonymousTypeParts.Add(PlainText(FeaturesResources.Anonymous_Types_colon));
            anonymousTypeParts.AddRange(LineBreak());

            for (var i = 0; i < transitiveNormalAnonymousTypeReferences.Count; i++)
            {
                if (i != 0)
                {
                    anonymousTypeParts.AddRange(LineBreak());
                }

                var anonymousType = transitiveNormalAnonymousTypeReferences[i];
                anonymousTypeParts.AddRange(Space(count: 4));
                anonymousTypeParts.Add(Part(SymbolDisplayPartKind.ClassName, anonymousType, anonymousType.Name));
                anonymousTypeParts.AddRange(Space());
                anonymousTypeParts.Add(PlainText(FeaturesResources.is_));
                anonymousTypeParts.AddRange(Space());
                anonymousTypeParts.AddRange(GetAnonymousTypeParts(anonymousType, semanticModel, position));
            }

            // Now, inline any delegate anonymous types we've got.
            anonymousTypeParts = this.InlineDelegateAnonymousTypes(anonymousTypeParts, semanticModel, position);

            // Finally, assign a name to all the anonymous types.
            var anonymousTypeToName = GenerateAnonymousTypeNames(transitiveNormalAnonymousTypeReferences);
            anonymousTypeParts = AnonymousTypeDisplayInfo.ReplaceAnonymousTypes(anonymousTypeParts, anonymousTypeToName);

            return new AnonymousTypeDisplayInfo(anonymousTypeToName, anonymousTypeParts);
        }

        private static Dictionary<INamedTypeSymbol, string> GenerateAnonymousTypeNames(
            IList<INamedTypeSymbol> anonymousTypes)
        {
            var current = 0;
            var anonymousTypeToName = new Dictionary<INamedTypeSymbol, string>();
            foreach (var type in anonymousTypes)
            {
                anonymousTypeToName[type] = GenerateAnonymousTypeName(current);
                current++;
            }

            return anonymousTypeToName;
        }

        private static string GenerateAnonymousTypeName(int current)
        {
            var c = (char)('a' + current);
            if (c >= 'a' && c <= 'z')
            {
                return "'" + c.ToString();
            }

            return "'" + current.ToString();
        }

        private static IList<INamedTypeSymbol> OrderAnonymousTypes(
            IList<INamedTypeSymbol> transitiveAnonymousTypeReferences,
            ISymbol symbol)
        {
            if (symbol is IMethodSymbol method)
            {
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
            else if (symbol is IPropertySymbol property)
            {
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

        protected static IEnumerable<SymbolDisplayPart> LineBreak(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
            }
        }

        protected static SymbolDisplayPart PlainText(string text)
            => Part(SymbolDisplayPartKind.Text, text);

        private static SymbolDisplayPart Part(SymbolDisplayPartKind kind, string text)
            => Part(kind, null, text);

        private static SymbolDisplayPart Part(SymbolDisplayPartKind kind, ISymbol symbol, string text)
            => new SymbolDisplayPart(kind, symbol, text);

        protected static IEnumerable<SymbolDisplayPart> Space(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
            }
        }

        protected static SymbolDisplayPart Punctuation(string text)
            => Part(SymbolDisplayPartKind.Punctuation, text);

        protected static SymbolDisplayPart Keyword(string text)
            => Part(SymbolDisplayPartKind.Keyword, text);
    }
}
