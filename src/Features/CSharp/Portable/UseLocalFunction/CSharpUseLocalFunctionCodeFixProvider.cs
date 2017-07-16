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

            var localDeclarationToInfo = new Dictionary<LocalDeclarationStatementSyntax, (LambdaExpressionSyntax, string form)>();
            foreach (var diagnostic in diagnostics)
            {
                var localDeclaration = (LocalDeclarationStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var anonymousFunction = (LambdaExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(cancellationToken);
                var form = diagnostic.Properties[CSharpUseLocalFunctionDiagnosticAnalyzer.Form];

                localDeclarationToInfo[localDeclaration] = (anonymousFunction, form);
            }

            // Process declarations backwards, that way we see the effects of any nested changes
            // when we process the outer change.
            foreach (var (localDeclaration, (anonymousFunction, form)) in localDeclarationToInfo.OrderByDescending(kvp => kvp.Key.SpanStart))
            {
                ReplaceAnonymousWithLocalFunction(
                    semanticModel, editor, localDeclaration, anonymousFunction, form, cancellationToken);
            }
        }

        private void ReplaceAnonymousWithLocalFunction(
            SemanticModel semanticModel, SyntaxEditor editor,
            LocalDeclarationStatementSyntax localDeclaration,
            LambdaExpressionSyntax anonymousFunction,
            string form, CancellationToken cancellationToken)
        {
            switch (form)
            {
                case CSharpUseLocalFunctionDiagnosticAnalyzer.SimpleLocalDeclarationForm:
                    ReplaceSimpleLocalDeclaration(semanticModel, editor, localDeclaration, anonymousFunction, cancellationToken);
                    return;

                case CSharpUseLocalFunctionDiagnosticAnalyzer.CastedLocalDeclarationForm:
                    ReplaceCastedLocalDeclaration(semanticModel, editor, localDeclaration, anonymousFunction, cancellationToken);
                    return;

                default:
                    throw ExceptionUtilities.UnexpectedValue(form);
            }
        }

        private void ReplaceSimpleLocalDeclaration(
            SemanticModel semanticModel, SyntaxEditor editor,
            LocalDeclarationStatementSyntax localDeclaration,
            LambdaExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken)
        {
            // Type t = <anonymous function>
            var localFunctionStatement = CreateLocalFunctionStatement(
                semanticModel, localDeclaration, anonymousFunction, cancellationToken);

            localFunctionStatement = localFunctionStatement.WithTriviaFrom(localDeclaration)
                                                           .WithAdditionalAnnotations(Formatter.Annotation);
            editor.ReplaceNode(localDeclaration, localFunctionStatement);
        }

        private void ReplaceCastedLocalDeclaration(
            SemanticModel semanticModel, SyntaxEditor editor,
            LocalDeclarationStatementSyntax localDeclaration,
            LambdaExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken)
        {
            // var t = (Type)(<anonymous function>);
            var localFunctionStatement = CreateLocalFunctionStatement(
                semanticModel, localDeclaration, anonymousFunction, cancellationToken);

            localFunctionStatement = localFunctionStatement.WithTriviaFrom(localDeclaration)
                                                           .WithAdditionalAnnotations(Formatter.Annotation);
            editor.ReplaceNode(localDeclaration, localFunctionStatement);
        }

        private LocalFunctionStatementSyntax CreateLocalFunctionStatement(
            SemanticModel semanticModel,
            LocalDeclarationStatementSyntax localDeclaration,
            LambdaExpressionSyntax anonymousFunction,
            CancellationToken cancellationToken)
        {
            var modifiers = anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)
                ? new SyntaxTokenList(anonymousFunction.AsyncKeyword)
                : default;

            var delegateType = (INamedTypeSymbol)semanticModel.GetTypeInfo(anonymousFunction, cancellationToken).ConvertedType;
            var invokeMethod = delegateType.DelegateInvokeMethod;

            var returnType = invokeMethod.ReturnsVoid
                ? s_voidType
                : invokeMethod.ReturnType.GenerateTypeSyntax();

            var identifier = localDeclaration.Declaration.Variables[0].Identifier;
            var typeParameterList = default(TypeParameterListSyntax);

            var parameterList = GenerateParameterList(semanticModel, anonymousFunction, cancellationToken);
            var constraintClauses = default(SyntaxList<TypeParameterConstraintClauseSyntax>);

            var body = anonymousFunction.Body.IsKind(SyntaxKind.Block)
                ? (BlockSyntax)anonymousFunction.Body
                : null;

            var expressionBody = anonymousFunction.Body is ExpressionSyntax expression
                ? SyntaxFactory.ArrowExpressionClause(anonymousFunction.ArrowToken, expression)
                : null;

            var semicolonToken = anonymousFunction.Body is ExpressionSyntax
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
            var newParameterList = lambdaExpression.ParameterList.ReplaceNodes(
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

            //var sourceText = semanticModel.SyntaxTree.GetText(cancellationToken);
            //if (sourceText.AreOnSameLine(lambdaExpression.ParameterList.CloseParenToken, lambdaExpression.ArrowToken))
            //{
            //    newParameterList = newParameterList.WithAppendedTrailingTrivia(lambdaExpression.ArrowToken.TrailingTrivia);
            //}

            return newParameterList;
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
