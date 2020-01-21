// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal static class IAnonymousTypeDisplayExtensions
    {
        public static IList<SymbolDisplayPart> InlineDelegateAnonymousTypes(
            this IAnonymousTypeDisplayService service, IList<SymbolDisplayPart> parts, SemanticModel semanticModel, int position, ISymbolDisplayService displayService)
        {
            var result = parts;
            while (true)
            {
                var delegateAnonymousType = result.Select(p => p.Symbol).OfType<INamedTypeSymbol>().FirstOrDefault(s => s.IsAnonymousDelegateType());
                if (delegateAnonymousType == null)
                {
                    break;
                }

                result = result == parts ? new List<SymbolDisplayPart>(parts) : result;
                ReplaceAnonymousType(result, delegateAnonymousType,
                    service.GetAnonymousTypeParts(delegateAnonymousType, semanticModel, position, displayService));
            }

            return result;
        }

        private static void ReplaceAnonymousType(
            IList<SymbolDisplayPart> list,
            INamedTypeSymbol anonymousType,
            IEnumerable<SymbolDisplayPart> parts)
        {
            var index = list.IndexOf(p => anonymousType.Equals(p.Symbol));
            if (index >= 0)
            {
                var result = list.Take(index).Concat(parts).Concat(list.Skip(index + 1)).ToList();
                list.Clear();
                list.AddRange(result);
            }
        }
    }
}
