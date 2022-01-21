// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertAnonymousType
{
    internal abstract class AbstractConvertAnonymousTypeCodeRefactoringProvider<TAnonymousObjectCreationExpressionSyntax>
        : CodeRefactoringProvider
        where TAnonymousObjectCreationExpressionSyntax : SyntaxNode
    {
        protected static async Task<(TAnonymousObjectCreationExpressionSyntax?, INamedTypeSymbol?)> TryGetAnonymousObjectAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            // Gets a `TAnonymousObjectCreationExpressionSyntax` for current selection.
            // Due to the way `TryGetSelectedNodeAsync` works and how `TAnonymousObjectCreationExpressionSyntax` is e.g. for C# constructed
            // it matches even when caret is next to some tokens within the anonymous object creation node.
            // E.g.: `var a = new [||]{ b=1,[||] c=2 };` both match due to the caret being next to `,` and `{`.
            var anonymousObject = await document.TryGetRelevantNodeAsync<TAnonymousObjectCreationExpressionSyntax>(
                span, cancellationToken).ConfigureAwait(false);
            if (anonymousObject == null)
                return default;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var anonymousType = semanticModel.GetTypeInfo(anonymousObject, cancellationToken).Type as INamedTypeSymbol;

            return (anonymousObject, anonymousType);
        }
    }
}
