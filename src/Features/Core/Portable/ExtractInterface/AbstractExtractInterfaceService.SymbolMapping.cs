// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal abstract partial class AbstractExtractInterfaceService
    {
        private readonly struct SymbolMapping
        {
            public SymbolMapping(
                Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
                Solution annotatedSolution,
                ImmutableArray<DocumentId> documentIds,
                SyntaxAnnotation typeNodeAnnotation)
            {
                SymbolToDeclarationAnnotationMap = symbolToDeclarationAnnotationMap;
                AnnotatedSolution = annotatedSolution;
                DocumentIds = documentIds;
                TypeNodeAnnotation = typeNodeAnnotation;
            }

            public Dictionary<ISymbol, SyntaxAnnotation> SymbolToDeclarationAnnotationMap { get; }
            public Solution AnnotatedSolution { get; }
            public ImmutableArray<DocumentId> DocumentIds { get; }
            public SyntaxAnnotation TypeNodeAnnotation { get; }
        }
    }
}
