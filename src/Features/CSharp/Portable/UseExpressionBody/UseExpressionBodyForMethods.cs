// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyForMethodsDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        public bool OpenFileOnly(Workspace workspace) => true;

        public UseExpressionBodyForMethodsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_expression_body_for_methods), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var optionSet = context.Options.GetOptionSet();
            var preferExpressionBodiedMethods = optionSet.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods);

            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            if (preferExpressionBodiedMethods.Value)
            {
                if (methodDeclaration.ExpressionBody == null)
                {
                    // They want expression bodies and they don't have one.  See if we can
                    // convert this to have an expression body.
                    var expressionBody = methodDeclaration.Body.TryConvertToExpressionBody();
                    if (expressionBody != null)
                    {
                        var additionalLocations = ImmutableArray.Create(methodDeclaration.GetLocation());
                        context.ReportDiagnostic(Diagnostic.Create(
                            CreateDescriptor(this.DescriptorId, FeaturesResources.Use_expression_body_for_methods, preferExpressionBodiedMethods.Notification.Value),
                            methodDeclaration.Body.Statements[0].GetLocation(),
                            additionalLocations: additionalLocations));
                    }
                }
            }
            else
            {
                // They don't want expression bodies but they have one.  Offer to conver this to a normal block
                if (methodDeclaration.ExpressionBody != null)
                {
                    var additionalLocations = ImmutableArray.Create(methodDeclaration.GetLocation());
                    context.ReportDiagnostic(Diagnostic.Create(
                        CreateDescriptor(this.DescriptorId, FeaturesResources.Use_block_body_for_methods, preferExpressionBodiedMethods.Notification.Value),
                        methodDeclaration.ExpressionBody.GetLocation(),
                        additionalLocations: additionalLocations));
                }
            }
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForMethodsCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var option = context.Document.Project.Solution.Workspace.Options.GetOption(
                CSharpCodeStyleOptions.PreferExpressionBodiedMethods);
            var title = option.Value
                ? FeaturesResources.Use_expression_body_for_methods
                : FeaturesResources.Use_block_body_for_methods;

            context.RegisterCodeFix(
                new MyCodeAction(title, c => FixAsync(context.Document, diagnostic, c)),
                diagnostic);

            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var option = document.Project.Solution.Workspace.Options.GetOption(
                CSharpCodeStyleOptions.PreferExpressionBodiedMethods);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(root, editor, diagnostic, option.Value, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private void AddEdits(
            SyntaxNode root, SyntaxEditor editor, Diagnostic diagnostic,
            bool preferExpressionBody, CancellationToken cancellationToken)
        {
            var methodLocation = diagnostic.AdditionalLocations[0];
            var methodDeclaration = (MethodDeclarationSyntax)methodLocation.FindNode(cancellationToken);

            var updatedMethodDeclaration = Update(methodDeclaration, preferExpressionBody).WithAdditionalAnnotations(
                Formatter.Annotation);

            editor.ReplaceNode(methodDeclaration, updatedMethodDeclaration);
        }

        private MethodDeclarationSyntax Update(MethodDeclarationSyntax methodDeclaration, bool preferExpressionBody)
        {
            if (preferExpressionBody)
            {
                return methodDeclaration.WithBody(null)
                                        .WithExpressionBody(methodDeclaration.Body.TryConvertToExpressionBody())
                                        .WithSemicolonToken(GetFirstStatementSemicolon(methodDeclaration.Body));
            }
            else
            {
                var block = methodDeclaration.ExpressionBody.ConvertToBlock(
                    methodDeclaration.SemicolonToken, 
                    createReturnStatementForExpression: !methodDeclaration.ReturnType.IsVoid());
                return methodDeclaration.WithBody(block)
                                        .WithExpressionBody(null)
                                        .WithSemicolonToken(default(SyntaxToken));
            }
        }

        private SyntaxToken GetFirstStatementSemicolon(BlockSyntax body)
        {
            var firstStatement = body.Statements[0];
            if (firstStatement.IsKind(SyntaxKind.ExpressionStatement))
            {
                return ((ExpressionStatementSyntax)firstStatement).SemicolonToken;
            }
            else if (firstStatement.IsKind(SyntaxKind.ReturnStatement))
            {
                return ((ReturnStatementSyntax)firstStatement).SemicolonToken;
            }
            else if (firstStatement.IsKind(SyntaxKind.ThrowStatement))
            {
                return ((ThrowStatementSyntax)firstStatement).SemicolonToken;
            }
            else
            {
                return SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}