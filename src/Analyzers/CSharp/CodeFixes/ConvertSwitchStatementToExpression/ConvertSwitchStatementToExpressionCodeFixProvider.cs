// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;

using Constants = ConvertSwitchStatementToExpressionConstants;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertSwitchStatementToExpression), Shared]
internal sealed partial class ConvertSwitchStatementToExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ConvertSwitchStatementToExpressionCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var switchLocation = context.Diagnostics.First().AdditionalLocations[0];
        var switchStatement = (SwitchStatementSyntax)switchLocation.FindNode(getInnermostNodeForTie: true, context.CancellationToken);
        if (switchStatement.ContainsDirectives)
        {
            // Avoid providing code fixes for switch statements containing directives
            return Task.CompletedTask;
        }

        RegisterCodeFix(context, CSharpAnalyzersResources.Convert_switch_statement_to_expression, nameof(CSharpAnalyzersResources.Convert_switch_statement_to_expression));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<TextSpan>.GetInstance(diagnostics.Length, out var spans);
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var switchLocation = diagnostic.AdditionalLocations[0];
            if (spans.Any((s, nodeSpan) => s.Contains(nodeSpan), switchLocation.SourceSpan))
            {
                // Skip nested switch expressions in case of a fix-all operation.
                continue;
            }

            spans.Add(switchLocation.SourceSpan);

            var properties = diagnostic.Properties;
            var nodeToGenerate = (SyntaxKind)int.Parse(properties[Constants.NodeToGenerateKey]!);
            var shouldRemoveNextStatement = bool.Parse(properties[Constants.ShouldRemoveNextStatementKey]!);

            var declaratorToRemoveLocation = diagnostic.AdditionalLocations.ElementAtOrDefault(1);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            VariableDeclaratorSyntax? declaratorToRemoveNode = null;
            ITypeSymbol? declaratorToRemoveType = null;

            if (declaratorToRemoveLocation != null)
            {
                declaratorToRemoveNode = (VariableDeclaratorSyntax)declaratorToRemoveLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);
                declaratorToRemoveType = semanticModel.GetDeclaredSymbol(declaratorToRemoveNode, cancellationToken).GetSymbolType();
            }

            var switchStatement = (SwitchStatementSyntax)switchLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            var switchExpressionStatement = Rewriter.Rewrite(
               switchStatement, semanticModel, declaratorToRemoveType, nodeToGenerate,
               shouldMoveNextStatementToSwitchExpression: shouldRemoveNextStatement,
               generateDeclaration: declaratorToRemoveLocation is not null,
               cancellationToken);

            if (declaratorToRemoveNode is not null)
            {
                editor.RemoveNode(declaratorToRemoveNode);

                // If we are removing the declarator statement entirely, transfer its leading trivia to the
                // expression-statement are converting to.
                if (declaratorToRemoveNode.Parent is VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax declStatement, Variables.Count: 1 })
                    switchExpressionStatement = switchExpressionStatement.WithPrependedLeadingTrivia(declStatement.GetLeadingTrivia());
            }

            editor.ReplaceNode(switchStatement, switchExpressionStatement.WithAdditionalAnnotations(Formatter.Annotation));

            if (shouldRemoveNextStatement)
            {
                // Already morphed into the top-level switch expression.
                var nextStatement = switchStatement.GetNextStatement();
                Contract.ThrowIfNull(nextStatement);
                Debug.Assert(nextStatement.Kind() is SyntaxKind.ThrowStatement or SyntaxKind.ReturnStatement);
                editor.RemoveNode(nextStatement.IsParentKind(SyntaxKind.GlobalStatement) ? nextStatement.GetRequiredParent() : nextStatement);
            }
        }
    }
}
