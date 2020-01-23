// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaDocumentationCommentFormatting
    {
        public static IEnumerable<TaggedText> GetDocumentationParts(ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter, CancellationToken cancellationToken)
            => Shared.Extensions.ISymbolExtensions2.GetDocumentationParts(symbol, semanticModel, position, formatter, cancellationToken);
    }
}
