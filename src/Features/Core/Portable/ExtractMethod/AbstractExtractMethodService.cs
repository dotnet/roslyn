// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod;

/// <summary>
/// Core service that tries to share as much extract-method logic across C# and VB.  Note: TStatementSyntax and
/// TExecutableStatementSyntax exist to model VB's inheritance model there (where StatementSyntax is used liberally
/// (including for signatures of members, while ExecutableStatementSyntax generally corresponds to a code statement
/// found within a method body).  In C# these will be the same StatementSyntax type as C# has a much stronger split
/// between executable code statements and symbol signatures.
/// </summary>
internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax> : IExtractMethodService
    where TStatementSyntax : SyntaxNode
    where TExecutableStatementSyntax : TStatementSyntax
    where TExpressionSyntax : SyntaxNode
{
    protected abstract SelectionValidator CreateSelectionValidator(SemanticDocument document, TextSpan textSpan, bool localFunction);
    protected abstract MethodExtractor CreateMethodExtractor(SelectionResult selectionResult, ExtractMethodGenerationOptions options, bool localFunction);

    public async Task<ExtractMethodResult> ExtractMethodAsync(
        Document document,
        TextSpan textSpan,
        bool localFunction,
        ExtractMethodGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var validator = CreateSelectionValidator(semanticDocument, textSpan, localFunction);

        var (selectionResult, status) = await validator.GetValidSelectionAsync(cancellationToken).ConfigureAwait(false);
        if (selectionResult is null)
            return ExtractMethodResult.Fail(status);

        cancellationToken.ThrowIfCancellationRequested();

        // extract method
        var extractor = CreateMethodExtractor(selectionResult, options, localFunction);
        return extractor.ExtractMethod(status, cancellationToken);
    }
}
