// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class INamespaceOrTypeSymbolExtensions
    {
        private static readonly SymbolDisplayFormat ShortNameFormat = new SymbolDisplayFormat(
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

        public static string GetShortName(this INamespaceOrTypeSymbol symbol)
        {
            return symbol.ToDisplayString(ShortNameFormat);
        }

        public static IEnumerable<IPropertySymbol> GetIndexers(this INamespaceOrTypeSymbol symbol)
        {
            return symbol == null
                ? SpecializedCollections.EmptyEnumerable<IPropertySymbol>()
                : symbol.GetMembers(WellKnownMemberNames.Indexer).OfType<IPropertySymbol>().Where(p => p.IsIndexer);
        }
    }
}