// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal abstract partial class AbstractExtractInterfaceService
    {
        private readonly struct SymbolMapping
        {
            public SymbolMapping(
                Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
                Solution annotatedSolution,
                List<DocumentId> documentIds,
                SyntaxAnnotation typeNodeAnnotation)
            {
                SymbolToDeclarationAnnotationMap = symbolToDeclarationAnnotationMap;
                AnnotatedSolution = annotatedSolution;
                DocumentIds = documentIds;
                TypeNodeAnnotation = typeNodeAnnotation;
            }

            public Dictionary<ISymbol, SyntaxAnnotation> SymbolToDeclarationAnnotationMap { get; }
            public Solution AnnotatedSolution { get; }
            public List<DocumentId> DocumentIds { get; }
            public SyntaxAnnotation TypeNodeAnnotation { get; }
        }
    }
}
