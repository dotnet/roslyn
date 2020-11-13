// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Simplification
{
    /// <summary>
    /// An annotation that holds onto information about a type or namespace symbol.
    /// </summary>
    internal class SymbolAnnotation
    {
        public const string Kind = "SymbolId";

        public static SyntaxAnnotation Create(ISymbol symbol)
            => new(Kind, DocumentationCommentId.CreateReferenceId(symbol));

        public static ISymbol? GetSymbol(SyntaxAnnotation annotation, Compilation compilation)
            => GetSymbols(annotation, compilation).FirstOrDefault();

        public static ImmutableArray<ISymbol> GetSymbols(SyntaxAnnotation annotation, Compilation compilation)
            => DocumentationCommentId.GetSymbolsForReferenceId(annotation.Data!, compilation);
    }
}
