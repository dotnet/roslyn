// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal interface IImplementInterfaceService : ILanguageService
{
    Task<Document> ImplementInterfaceAsync(Document document, ImplementTypeGenerationOptions options, SyntaxNode node, CancellationToken cancellationToken);
    ImmutableArray<CodeAction> GetCodeActions(Document document, ImplementTypeGenerationOptions options, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken);
}
