// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
