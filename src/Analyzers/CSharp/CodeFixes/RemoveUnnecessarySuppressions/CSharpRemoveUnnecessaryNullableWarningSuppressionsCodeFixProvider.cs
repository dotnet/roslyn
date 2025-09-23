// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableWarningSuppressions), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider()
    : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression];

    public override FixAllProvider? GetFixAllProvider()
        => new RemoveUnnecessaryNullableWarningSuppressionsFixAllProvider();

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var diagnostic = context.Diagnostics[0];
        var node = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
        if (node is PostfixUnaryExpressionSyntax postfixUnary)
        {
            context.RegisterCodeFix(CodeAction.Create(
                AnalyzersResources.Remove_unnecessary_suppression,
                cancellationToken => FixDocumentAsync(context.Document, postfixUnary, cancellationToken),
                nameof(AnalyzersResources.Remove_unnecessary_suppression)),
                context.Diagnostics);
        }

        return Task.CompletedTask;
    }

    private Task<Document> FixDocumentAsync(Document document, PostfixUnaryExpressionSyntax postFixUnary, CancellationToken cancellationToken)
    {
        var root = postFixUnary.SyntaxTree.GetRoot(cancellationToken);
        var newRoot = root.ReplaceNode(
            postFixUnary,
            postFixUnary.Operand.WithTriviaFrom(postFixUnary));
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private sealed class RemoveUnnecessaryNullableWarningSuppressionsFixAllProvider : FixAllProvider
    {
#if !CODE_STYLE
        internal override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;
#endif

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            fixAllContext.get
        }
    }
}
