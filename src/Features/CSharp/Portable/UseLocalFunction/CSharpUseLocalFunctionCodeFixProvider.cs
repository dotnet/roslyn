// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseLocalFunction
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseLocalFunctionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private static TypeSyntax s_voidType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        private static TypeSyntax s_objectType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseLocalFunctionDiagnosticId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => diagnostic.Severity != DiagnosticSeverity.Hidden;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var localDeclarationToLambda = new Dictionary<LocalDeclarationStatementSyntax, LambdaExpressionSyntax>();
            var nodesToTrack = new HashSet<SyntaxNode>();
            foreach (var diagnostic in diagnostics)
            {
                var localDeclaration = (LocalDeclarationStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var lambda = (LambdaExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(cancellationToken);

                localDeclarationToLambda[localDeclaration] = lambda;

                nodesToTrack.Add(localDeclaration);
                nodesToTrack.Add(lambda);
            }

            var root = editor.OriginalRoot;
            var currentRoot = root.TrackNodes(nodesToTrack);

            // Process declarations in reverse order so that we see the effects of nested 
            // declarations befor processing the outer decls.
            foreach (var (originalLocalDeclaration, originalLambda) in localDeclarationToLambda.OrderByDescending(kvp => kvp.Value.SpanStart))
            {
                var delegateType = (INamedTypeSymbol)semanticModel.GetTypeInfo(originalLambda, cancellationToken).ConvertedType;
                var parameterList = GenerateParameterList(semanticModel, originalLambda, cancellationToken);

                var currentLocalDeclaration = currentRoot.GetCurrentNode(originalLocalDeclaration);
                var currentLambda = currentRoot.GetCurrentNode(originalLambda);

                currentRoot = ReplaceAnonymousWithLocalFunction(
                    document.Project.Solution.Workspace, currentRoot,
                    currentLocalDeclaration, currentLambda,
                    delegateType, parameterList,
                    cancellationToken);
            }

            editor.ReplaceNode(root, currentRoot);
        }

        private SyntaxNode ReplaceAnonymousWithLocalFunction(
            Workspace workspace, SyntaxNode currentRoot,
            LocalDeclarationStatementSyntax localDeclaration, LambdaExpressionSyntax lambda,
            INamedTypeSymbol delegateType, ParameterListSyntax parameterList,
            CancellationToken cancellationToken)
        {
            var newLocalFunctionStatement = CreateLocalFunctionStatement(
                localDeclaration, lambda, delegateType, parameterList, cancellationToken);

            newLocalFunctionStatement = newLocalFunctionStatement.WithTriviaFrom(localDeclaration)
                                                                 .WithAdditionalAnnotations(Formatter.Annotation);

            var editor = new SyntaxEditor(currentRoot, workspace);
            editor.ReplaceNode(localDeclaration, newLocalFunctionStatement);

            var lambdaStatement = lambda.GetAncestor<StatementSyntax>();
            if (lambdaStatement != localDeclaration)
            {
                // This is the split decl+init form.  Remove the second statement as we're
                // merging into the first one.
                editor.RemoveNode(lambdaStatement);
            }

            return editor.GetChangedRoot();
        }

        private LocalFunctionStatementSyntax CreateLocalFunctionStatement(
            LocalDeclarationStatementSyntax localDeclaration,
            LambdaExpressionSyntax lambda,
            INamedTypeSymbol delegateType,
            ParameterListSyntax parameterList,
            CancellationToken cancellationToken)
        {
            var modifiers = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)
                ? new SyntaxTokenList(lambda.AsyncKeyword)
                : default;

            var invokeMethod = delegateType.DelegateInvokeMethod;

            var returnType = invokeMethod.ReturnsVoid
                ? s_voidType
                : invokeMethod.ReturnType.GenerateTypeSyntax();

            var identifier = localDeclaration.Declaration.Variables[0].Identifier;
            var typeParameterList = default(TypeParameterListSyntax);

            var constraintClauses = default(SyntaxList<TypeParameterConstraintClauseSyntax>);

            var body = lambda.Body.IsKind(SyntaxKind.Block)
                ? (BlockSyntax)lambda.Body
                : null;

            var expressionBody = lambda.Body is ExpressionSyntax expression
                ? SyntaxFactory.ArrowExpressionClause(lambda.ArrowToken, expression)
                : null;

            var semicolonToken = lambda.Body is ExpressionSyntax
                ? localDeclaration.SemicolonToken
                : default;

            return SyntaxFactory.LocalFunctionStatement(
                modifiers, returnType, identifier, typeParameterList, parameterList,
                constraintClauses, body, expressionBody, semicolonToken);
        }

        private ParameterListSyntax GenerateParameterList(
            SemanticModel semanticModel, AnonymousFunctionExpressionSyntax anonymousFunction, CancellationToken cancellationToken)
        {
            switch (anonymousFunction)
            {
                case SimpleLambdaExpressionSyntax simpleLambda:
                    return GenerateSimpleLambdaParameterList(semanticModel, simpleLambda, cancellationToken);
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    return GenerateParenthesizedLambdaParameterList(semanticModel, parenthesizedLambda, cancellationToken);
                default:
                    throw ExceptionUtilities.UnexpectedValue(anonymousFunction);
            }
        }

        private ParameterListSyntax GenerateSimpleLambdaParameterList(
            SemanticModel semanticModel, SimpleLambdaExpressionSyntax lambdaExpression, CancellationToken cancellationToken)
        {
            var parameter = semanticModel.GetDeclaredSymbol(lambdaExpression.Parameter, cancellationToken);
            var type = parameter?.Type.GenerateTypeSyntax() ?? s_objectType;

            return SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList<ParameterSyntax>().Add(
                    SyntaxFactory.Parameter(lambdaExpression.Parameter.Identifier).WithType(type)));
        }

        private ParameterListSyntax GenerateParenthesizedLambdaParameterList(
            SemanticModel semanticModel, ParenthesizedLambdaExpressionSyntax lambdaExpression, CancellationToken cancellationToken)
        {
            return lambdaExpression.ParameterList.ReplaceNodes(
                lambdaExpression.ParameterList.Parameters,
                (parameterNode, _) =>
                {
                    if (parameterNode.Type != null)
                    {
                        return parameterNode;
                    }

                    var parameter = semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
                    return parameterNode.WithType(parameter?.Type.GenerateTypeSyntax() ?? s_objectType);
                });
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Use_local_function, createChangedDocument, FeaturesResources.Use_local_function)
            {
            }
        }
    }
}
