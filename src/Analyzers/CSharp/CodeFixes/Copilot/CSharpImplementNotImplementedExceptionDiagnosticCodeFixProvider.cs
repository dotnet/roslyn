// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementNotImplementedException), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpImplementNotImplementedExceptionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Implement_with_Copilot, nameof(CSharpAnalyzersResources.Implement_with_Copilot));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
            await FixOneAsync(editor, document, diagnostic, cancellationToken).ConfigureAwait(false);
    }

    private static async Task FixOneAsync(
        SyntaxEditor editor, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // Find the throw statement node
        var throwStatement = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken).AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
        if (throwStatement == null)
        {
            return;
        }

        // Find the containing method
        var containingMethod = throwStatement.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod == null)
        {
            return;
        }

        var containingMethodName = containingMethod.Identifier.Text;
        var referencedMethods = new List<string>();

        // Traverse the syntax tree to find all method invocations
        var root = containingMethod.SyntaxTree.GetRoot(cancellationToken);
        var methodInvocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in methodInvocations)
        {
            string? invokedMethodName = null;

            // Check if the invocation is a simple identifier
            if (invocation.Expression is IdentifierNameSyntax identifierName)
            {
                invokedMethodName = identifierName.Identifier.Text;
            }
            // Check if the invocation is a member access expression (e.g., this.MethodName or ClassName.MethodName)
            else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                invokedMethodName = memberAccess.Name.Identifier.Text;
            }

            if (invokedMethodName == containingMethodName)
            {
                var invokingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (invokingMethod != null)
                {
                    referencedMethods.Add(invokingMethod.Identifier.Text);
                }
            }
        }

        // Initialize the comment with a basic TODO message
        var referencesComment = "// TODO: Implement this method\n";
        if (referencedMethods.Any())
        {
            referencesComment += "// Referenced by methods:\n";
            foreach (var method in referencedMethods)
            {
                referencesComment += $"// - {method}\n";
            }
        }

        // Split the comment into individual lines
        var commentLines = referencesComment.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Get the leading trivia of the throw statement
        var leadingTrivia = throwStatement.GetLeadingTrivia();

        // Create a new trivia list with the comment lines, preserving indentation
        var newLeadingTrivia = leadingTrivia;
        foreach (var line in commentLines)
        {
            newLeadingTrivia = newLeadingTrivia.Add(SyntaxFactory.Comment(line)).Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
        }

        // Replace the throw statement with the new leading trivia
        editor.ReplaceNode(throwStatement, (currentNode, generator) =>
        {
            return currentNode.WithLeadingTrivia(newLeadingTrivia);
        });

        // Remove the throw statement but keep its leading trivia
        editor.RemoveNode(throwStatement, SyntaxRemoveOptions.KeepLeadingTrivia);
    }
}
