// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaDocumentExtensions
    {
        public static Task<SemanticModel> GetSemanticModelForNodeAsync(this Document document, SyntaxNode? node, CancellationToken cancellationToken)
            => DocumentExtensions.GetSemanticModelForNodeAsync(document, node, cancellationToken);
    }
}
