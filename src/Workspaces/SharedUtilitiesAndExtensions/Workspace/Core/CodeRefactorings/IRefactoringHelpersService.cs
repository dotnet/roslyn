// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal interface IRefactoringHelpersService : IRefactoringHelpers, ILanguageService
{
}

internal static class IRefactoringHelpersServiceExtensions
{
    public static void AddRelevantNodes<TSyntaxNode>(
        this IRefactoringHelpersService service, ParsedDocument document, TextSpan selection, bool allowEmptyNodes, int maxCount, ref TemporaryArray<TSyntaxNode> result, CancellationToken cancellationToken) where TSyntaxNode : SyntaxNode
    {
        service.AddRelevantNodes(document.Text, document.Root, selection, allowEmptyNodes, maxCount, ref result, cancellationToken);
    }
}
