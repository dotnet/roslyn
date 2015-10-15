// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal struct AnonymousTypeDisplayInfo
    {
        public IDictionary<INamedTypeSymbol, string> AnonymousTypeToName { get; }
        public IList<SymbolDisplayPart> AnonymousTypesParts { get; }

        public AnonymousTypeDisplayInfo(
            IDictionary<INamedTypeSymbol, string> anonymousTypeToName,
            IList<SymbolDisplayPart> anonymousTypesParts)
            : this()
        {
            this.AnonymousTypeToName = anonymousTypeToName;
            this.AnonymousTypesParts = anonymousTypesParts;
        }

        public IList<SymbolDisplayPart> ReplaceAnonymousTypes(IList<SymbolDisplayPart> parts)
        {
            return ReplaceAnonymousTypes(parts, this.AnonymousTypeToName);
        }

        public static IList<SymbolDisplayPart> ReplaceAnonymousTypes(
            IList<SymbolDisplayPart> parts,
            IDictionary<INamedTypeSymbol, string> anonymousTypeToName)
        {
            var result = parts;
            for (int i = 0; i < result.Count; i++)
            {
                var part = result[i];
                var type = part.Symbol as INamedTypeSymbol;
                string name;
                if (type != null && anonymousTypeToName.TryGetValue(type, out name) && part.ToString() != name)
                {
                    result = result == parts ? new List<SymbolDisplayPart>(parts) : result;
                    result[i] = new SymbolDisplayPart(part.Kind, part.Symbol, name);
                }
            }

            return result;
        }
    }
}
