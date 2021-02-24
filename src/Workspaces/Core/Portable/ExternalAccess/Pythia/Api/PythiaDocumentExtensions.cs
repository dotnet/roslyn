// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaDocumentExtensions
    {
        public static Task<SemanticModel> GetSemanticModelForNodeAsync(this Document document, SyntaxNode? node, CancellationToken cancellationToken)
            => DocumentExtensions.ReuseExistingSpeculativeModelAsync(document, node, cancellationToken).AsTask();
    }
}
