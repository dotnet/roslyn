// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion;

internal abstract class AbstractCSharpBraceCompletionService : AbstractBraceCompletionService
{
    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    protected static bool IsLegalExpressionLocation(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        var leftToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        return tree.IsExpressionContext(position, leftToken, attributes: true, cancellationToken)
            || tree.IsStatementContext(position, leftToken, cancellationToken)
            || tree.IsGlobalStatementContext(position, cancellationToken);
    }
}
