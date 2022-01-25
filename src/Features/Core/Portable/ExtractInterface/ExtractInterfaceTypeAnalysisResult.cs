﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
