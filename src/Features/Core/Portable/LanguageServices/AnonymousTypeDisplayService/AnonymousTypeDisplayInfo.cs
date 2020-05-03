// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal readonly struct AnonymousTypeDisplayInfo
    {
        public IDictionary<INamedTypeSymbol, string> AnonymousTypeToName { get; }
        public IList<SymbolDisplayPart> AnonymousTypesParts { get; }

        public AnonymousTypeDisplayInfo(
            IDictionary<INamedTypeSymbol, string> anonymousTypeToName,
            IList<SymbolDisplayPart> anonymousTypesParts)
            : this()
        {
            AnonymousTypeToName = anonymousTypeToName;
            AnonymousTypesParts = anonymousTypesParts;
        }

        public IList<SymbolDisplayPart> ReplaceAnonymousTypes(IList<SymbolDisplayPart> parts)
            => ReplaceAnonymousTypes(parts, AnonymousTypeToName);

        public static IList<SymbolDisplayPart> ReplaceAnonymousTypes(
            IList<SymbolDisplayPart> parts,
            IDictionary<INamedTypeSymbol, string> anonymousTypeToName)
        {
            var result = parts;
            for (var i = 0; i < result.Count; i++)
            {
                var part = result[i];
                if (part.Symbol is INamedTypeSymbol type && anonymousTypeToName.TryGetValue(type, out var name) && part.ToString() != name)
                {
                    result = result == parts ? new List<SymbolDisplayPart>(parts) : result;
                    result[i] = new SymbolDisplayPart(part.Kind, part.Symbol, name);
                }
            }

            return result;
        }
    }
}
