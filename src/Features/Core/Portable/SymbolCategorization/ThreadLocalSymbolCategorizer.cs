// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.SymbolCategorization
{
    // Motivating example for ISymbolCategorizer
    // [ExportSymbolCategorizer, Shared]
    internal class ThreadLocalSymbolCategorizer : ISymbolCategorizer
    {
        private const string _threadLocalCategoryName = "[ThreadStatic]";
        private readonly ImmutableArray<string> matchingCategoryArray = new[] { _threadLocalCategoryName }.ToImmutableArray();

        public ImmutableArray<string> SupportedCategories
        {
            get
            {
                return matchingCategoryArray;
            }
        }

        public ImmutableArray<string> Categorize(ISymbol symbol)
        {
            return symbol.GetAttributes().Any(a => a.AttributeClass.MetadataName.Contains("ThreadStatic"))
                ? matchingCategoryArray
                : ImmutableArray<string>.Empty;
        }
    }
}
