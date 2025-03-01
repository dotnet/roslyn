// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal interface IExternalCSharpCopilotGenerateImplementationService
    {
        Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> ImplementNotImplementedMethodAsync(
            Document document,
            TextSpan? span,
            SyntaxNode memberDeclaration,
            ISymbol memberSymbol,
            SemanticModel semanticModel,
            ImmutableArray<ReferencedSymbol> references,
            CancellationToken cancellationToken);
    }
}
