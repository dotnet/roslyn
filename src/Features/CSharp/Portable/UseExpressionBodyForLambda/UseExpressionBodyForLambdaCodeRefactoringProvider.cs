// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseExpressionBodyForLambda), Shared]
internal sealed class UseExpressionBodyForLambdaCodeRefactoringProvider : CodeRefactoringProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public UseExpressionBodyForLambdaCodeRefactoringProvider()
    {
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        var optionProvider = await document.GetAnalyzerOptionsProviderAsync(cancellationToken).ConfigureAwait(false);
        var optionValue = UseExpressionBodyForLambdaHelpers.GetCodeStyleOption(optionProvider);

        var severity = UseExpressionBodyForLambdaHelpers.GetOptionSeverity(optionValue);
        switch (severity)
        {
            case ReportDiagnostic.Suppress:
            case ReportDiagnostic.Hidden:
                // if the severity is Hidden that's equivalent to 'refactoring only', so we want
                // to try to compute the refactoring here.
                //
                // If the severity is 'suppress', that means the user doesn't want the actual
                // analyzer to run here.  However, we can still check to see if we could offer
                // the feature here as a refactoring.
                await ComputeRefactoringsAsync(context, optionValue.Value, analyzerActive: false).ConfigureAwait(false);
                return;

            case ReportDiagnostic.Error:
            case ReportDiagnostic.Warn:
            case ReportDiagnostic.Info:
                // User has this option set at a level where we want it checked by the
                // DiagnosticAnalyser and not the CodeRefactoringProvider.  However, we still
                // want to check if we want to offer the *reverse* refactoring here in this
                // single location.
                //
                // For example, say this is the "use expression body" feature.  If the user says
                // they always prefer expression-bodies (with warning level), then we want the
                // analyzer to always be checking for that.  However, we still want to offer the
                // refactoring to flip their code to use a block body here, just in case that
                // was something they wanted to do as a one off (i.e. before adding new
                // statements.
                //
                // TODO(cyrusn): Should we only do this for warn/info?  Argument could be made
                // that we shouldn't even offer to refactor in the reverse direction if it will
                // just cause an error.  That said, maybe this is just an intermediary step, and
                // we shouldn't really be blocking the user from making it.
                await ComputeRefactoringsAsync(context, optionValue.Value, analyzerActive: true).ConfigureAwait(false);
                return;
        }
    }

    private static async Task ComputeRefactoringsAsync(
        CodeRefactoringContext context, ExpressionBodyPreference option, bool analyzerActive)
    {
        var document = context.Document;
        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var computationTask = analyzerActive
            ? ComputeOpposingRefactoringsWhenAnalyzerActiveAsync(document, span, option, cancellationToken)
            : ComputeAllRefactoringsWhenAnalyzerInactiveAsync(document, span, cancellationToken);

        var codeActions = await computationTask.ConfigureAwait(false);
        context.RegisterRefactorings(codeActions);
    }

    private static async Task<ImmutableArray<CodeAction>> ComputeOpposingRefactoringsWhenAnalyzerActiveAsync(
        Document document, TextSpan span, ExpressionBodyPreference option, CancellationToken cancellationToken)
    {
        if (option == ExpressionBodyPreference.Never)
        {
            // the user wants block-bodies (and the analyzer will be trying to enforce that). So
            // the reverse of this is that we want to offer the refactoring to convert a
            // block-body to an expression-body whenever possible.
            return await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.WhenPossible, cancellationToken).ConfigureAwait(false);
        }
        else if (option == ExpressionBodyPreference.WhenPossible)
        {
            // the user likes expression-bodies whenever possible, and the analyzer will be
            // trying to enforce that.  So the reverse of this is that we want to offer the
            // refactoring to convert an expression-body to a block-body whenever possible.
            return await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.Never, cancellationToken).ConfigureAwait(false);
        }
        else if (option == ExpressionBodyPreference.WhenOnSingleLine)
        {
            // the user likes expression-bodies *if* the body would be on a single line. this
            // means if we hit an block-body with an expression on a single line, then the
            // analyzer will handle it for us.

            // So we need to handle the cases of either hitting an expression-body and wanting
            // to convert it to a block-body *or* hitting an block-body over *multiple* lines and
            // wanting to offer to convert to an expression-body.

            // Always offer to convert an expression to a block since the analyzer will never
            // offer that. For this option setting.
            var useBlockRefactorings = await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.Never, cancellationToken).ConfigureAwait(false);

            var whenOnSingleLineRefactorings = await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.WhenOnSingleLine, cancellationToken).ConfigureAwait(false);
            if (whenOnSingleLineRefactorings.Length > 0)
            {
                // this block lambda would be converted to an expression lambda based on the
                // analyzer alone.  So we don't want to offer that as a refactoring ourselves.
                return useBlockRefactorings;
            }

            // The lambda block statement wasn't on a single line.  So the analyzer would
            // not offer to convert it to an expression body.  So we should can offer that
            // as a refactoring if possible.
            var whenPossibleRefactorings = await ComputeRefactoringsAsync(
                document, span, ExpressionBodyPreference.WhenPossible, cancellationToken).ConfigureAwait(false);
            return useBlockRefactorings.AddRange(whenPossibleRefactorings);
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(option);
        }
    }

    private static async Task<ImmutableArray<CodeAction>> ComputeAllRefactoringsWhenAnalyzerInactiveAsync(
        Document document, TextSpan span, CancellationToken cancellationToken)
    {
        // If the analyzer is inactive, then we want to offer refactorings in any viable
        // direction.  So we want to offer to convert expression-bodies to block-bodies, and
        // vice-versa if applicable.

        var toExpressionBodyRefactorings = await ComputeRefactoringsAsync(
            document, span, ExpressionBodyPreference.WhenPossible, cancellationToken).ConfigureAwait(false);

        var toBlockBodyRefactorings = await ComputeRefactoringsAsync(
            document, span, ExpressionBodyPreference.Never, cancellationToken).ConfigureAwait(false);

        return toExpressionBodyRefactorings.AddRange(toBlockBodyRefactorings);
    }

    private static async Task<ImmutableArray<CodeAction>> ComputeRefactoringsAsync(
        Document document, TextSpan span, ExpressionBodyPreference option, CancellationToken cancellationToken)
    {
        var lambdaNode = await document.TryGetRelevantNodeAsync<LambdaExpressionSyntax>(span, cancellationToken).ConfigureAwait(false);
        if (lambdaNode == null)
        {
            return [];
        }

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var resultDisposer = ArrayBuilder<CodeAction>.GetInstance(out var result);
        if (UseExpressionBodyForLambdaHelpers.CanOfferUseExpressionBody(option, lambdaNode, root.GetLanguageVersion(), cancellationToken))
        {
            var title = UseExpressionBodyForLambdaHelpers.UseExpressionBodyTitle.ToString();
            result.Add(CodeAction.Create(
                title,
                cancellationToken => UpdateDocumentAsync(document, root, lambdaNode, cancellationToken),
                title));
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (UseExpressionBodyForLambdaHelpers.CanOfferUseBlockBody(semanticModel, option, lambdaNode, cancellationToken))
        {
            var title = UseExpressionBodyForLambdaHelpers.UseBlockBodyTitle.ToString();
            result.Add(CodeAction.Create(
                title,
                cancellationToken => UpdateDocumentAsync(document, root, lambdaNode, cancellationToken),
                title));
        }

        return result.ToImmutable();
    }

    private static async Task<Document> UpdateDocumentAsync(
        Document document, SyntaxNode root, LambdaExpressionSyntax declaration, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // We're only replacing a single declaration in the refactoring.  So pass 'declaration'
        // as both the 'original' and 'current' declaration.
        var updatedDeclaration = UseExpressionBodyForLambdaCodeActionHelpers.Update(semanticModel, declaration, declaration, cancellationToken);

        var newRoot = root.ReplaceNode(declaration, updatedDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }
}
