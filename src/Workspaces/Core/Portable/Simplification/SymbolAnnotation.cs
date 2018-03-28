// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return new SyntaxAnnotation(Kind, DocumentationCommentId.CreateReferenceId(symbol));
        }

        public static ISymbol GetSymbol(SyntaxAnnotation annotation, Compilation compilation)
        {
            return GetSymbols(annotation, compilation).FirstOrDefault();
        }

        public static ImmutableArray<ISymbol> GetSymbols(SyntaxAnnotation annotation, Compilation compilation)
        {
            return DocumentationCommentId.GetSymbolsForReferenceId(annotation.Data, compilation);
        }
    }
}
