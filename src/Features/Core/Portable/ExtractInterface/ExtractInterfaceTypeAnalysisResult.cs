// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal class ExtractInterfaceTypeAnalysisResult
    {
        public readonly bool CanExtractInterface;
        public readonly Document DocumentToExtractFrom;
        public readonly SyntaxNode TypeNode;
        public readonly INamedTypeSymbol TypeToExtractFrom;
        public readonly IEnumerable<ISymbol> ExtractableMembers;
        public readonly string ErrorMessage;

        public ExtractInterfaceTypeAnalysisResult(
            AbstractExtractInterfaceService extractInterfaceService,
            Document documentToExtractFrom,
            SyntaxNode typeNode,
            INamedTypeSymbol typeToExtractFrom,
            IEnumerable<ISymbol> extractableMembers)
        {
            CanExtractInterface = true;
            DocumentToExtractFrom = documentToExtractFrom;
            TypeNode = typeNode;
            TypeToExtractFrom = typeToExtractFrom;
            ExtractableMembers = extractableMembers;
        }

        public ExtractInterfaceTypeAnalysisResult(string errorMessage)
        {
            CanExtractInterface = false;
            ErrorMessage = errorMessage;
        }
    }
}
