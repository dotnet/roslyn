// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    internal interface ISemanticModelReuseLanguageService : ILanguageService
    {
        SyntaxNode? TryGetContainingMethodBodyForSpeculation(SyntaxNode node);
        Task<SemanticModel?> TryGetSpeculativeSemanticModelAsync(SemanticModel previousSemanticModel, SyntaxNode currentBodyNode, CancellationToken cancellationToken);
    }
}
