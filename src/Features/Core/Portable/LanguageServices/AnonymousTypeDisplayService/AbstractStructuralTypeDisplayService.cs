// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract partial class AbstractStructuralTypeDisplayService : IStructuralTypeDisplayService
    {
        public abstract ImmutableArray<SymbolDisplayPart> GetTypeParts(
            INamedTypeSymbol anonymousType, SemanticModel semanticModel, int position);

        public StructuralTypeDisplayInfo GetTypeDisplayInfo(
            ISymbol orderSymbol,
            IEnumerable<INamedTypeSymbol> directStructuralTypeReferences,
            SemanticModel semanticModel,
            int position)
        {
            if (!directStructuralTypeReferences.Any())
            {
                return new StructuralTypeDisplayInfo(
                    SpecializedCollections.EmptyDictionary<INamedTypeSymbol, string>(),
                    SpecializedCollections.EmptyList<SymbolDisplayPart>());
            }

            var transitiveStructuralTypeReferences = GetTransitiveNormalAnonymousTypeReferences(directStructuralTypeReferences.ToSet());
            transitiveStructuralTypeReferences = OrderAnonymousTypes(transitiveStructuralTypeReferences, orderSymbol);

            IList<SymbolDisplayPart> typeParts = new List<SymbolDisplayPart>();
            typeParts.Add(PlainText(FeaturesResources.Structural_Types_colon));
            typeParts.AddRange(LineBreak());

            for (var i = 0; i < transitiveStructuralTypeReferences.Count; i++)
            {
                if (i != 0)
                {
                    typeParts.AddRange(LineBreak());
                }

                var structuralType = transitiveStructuralTypeReferences[i];
                typeParts.AddRange(Space(count: 4));
                typeParts.Add(Part(SymbolDisplayPartKind.ClassName, structuralType, structuralType.Name));
                typeParts.AddRange(Space());
                typeParts.Add(PlainText(FeaturesResources.is_));
                typeParts.AddRange(Space());
                typeParts.AddRange(GetTypeParts(structuralType, semanticModel, position));
            }

            // Now, inline any delegate anonymous types we've got.
            typeParts = this.InlineDelegateAnonymousTypes(typeParts, semanticModel, position);

            // Finally, assign a name to all the anonymous types.
            var anonymousTypeToName = GenerateAnonymousTypeNames(transitiveStructuralTypeReferences);
            typeParts = StructuralTypeDisplayInfo.ReplaceStructuralTypes(typeParts, anonymousTypeToName);

            return new StructuralTypeDisplayInfo(anonymousTypeToName, typeParts);
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
            if (c is >= 'a' and <= 'z')
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

        private static SymbolDisplayPart Part(SymbolDisplayPartKind kind, ISymbol? symbol, string text)
            => new(kind, symbol, text);

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
