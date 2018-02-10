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
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
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

            var localDeclarationToFunction = new Dictionary<LocalDeclarationStatementSyntax, AnonymousFunctionExpressionSyntax>();
            var nodesToTrack = new HashSet<SyntaxNode>();
            var explicitInvokeCalls = new List<MemberAccessExpressionSyntax>();
            foreach (var diagnostic in diagnostics)
            {
                var localDeclaration = (LocalDeclarationStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var function = (AnonymousFunctionExpressionSyntax)diagnostic.AdditionalLocations[1].FindNode(cancellationToken);

                localDeclarationToFunction[localDeclaration] = function;

                nodesToTrack.Add(localDeclaration);
                nodesToTrack.Add(function);

                for (var i = 2; i < diagnostic.AdditionalLocations.Count; i++)
                {
                    explicitInvokeCalls.Add((MemberAccessExpressionSyntax)diagnostic.AdditionalLocations[i].FindNode(getInnermostNodeForTie: true, cancellationToken));
                }
            }

            nodesToTrack.AddRange(explicitInvokeCalls);
            var root = editor.OriginalRoot;
            var currentRoot = root.TrackNodes(nodesToTrack);

            // Process declarations in reverse order so that we see the effects of nested 
            // declarations befor processing the outer decls.
            foreach (var (originalLocalDeclaration, originalFunction) in localDeclarationToFunction.OrderByDescending(kvp => kvp.Value.SpanStart))
            {
                var delegateType = (INamedTypeSymbol)semanticModel.GetTypeInfo(originalFunction, cancellationToken).ConvertedType;
                var parameterList = GenerateParameterList(semanticModel, originalFunction, delegateType, cancellationToken);

                var currentLocalDeclaration = currentRoot.GetCurrentNode(originalLocalDeclaration);
                var currentFunction = currentRoot.GetCurrentNode(originalFunction);

                currentRoot = ReplaceAnonymousWithLocalFunction(
                    document.Project.Solution.Workspace, currentRoot,
                    currentLocalDeclaration, currentFunction,
                    delegateType, parameterList, explicitInvokeCalls.Select(node => currentRoot.GetCurrentNode(node)).ToImmutableArray(),
                    cancellationToken);
            }

            editor.ReplaceNode(root, currentRoot);
        }

        private static SyntaxNode ReplaceAnonymousWithLocalFunction(
            Workspace workspace, SyntaxNode currentRoot,
            LocalDeclarationStatementSyntax localDeclaration, AnonymousFunctionExpressionSyntax function,
            INamedTypeSymbol delegateType, ParameterListSyntax parameterList,
            ImmutableArray<MemberAccessExpressionSyntax> explicitInvokeCalls,
            CancellationToken cancellationToken)
        {
            var newLocalFunctionStatement = CreateLocalFunctionStatement(
                localDeclaration, function, delegateType, parameterList, cancellationToken);

            newLocalFunctionStatement = newLocalFunctionStatement.WithTriviaFrom(localDeclaration)
                                                                 .WithAdditionalAnnotations(Formatter.Annotation);

            var editor = new SyntaxEditor(currentRoot, workspace);
            editor.ReplaceNode(localDeclaration, newLocalFunctionStatement);

            var functionStatement = function.GetAncestor<StatementSyntax>();
            if (functionStatement != localDeclaration)
            {
                // This is the split decl+init form.  Remove the second statement as we're
                // merging into the first one.
                editor.RemoveNode(functionStatement);
            }

            foreach (var usage in explicitInvokeCalls)
            {
                editor.ReplaceNode(
                    usage.Parent,
                    (usage.Parent as InvocationExpressionSyntax).WithExpression(usage.Expression).WithTriviaFrom(usage.Parent));
            }

            return editor.GetChangedRoot();
        }

        private static LocalFunctionStatementSyntax CreateLocalFunctionStatement(
            LocalDeclarationStatementSyntax localDeclaration,
            AnonymousFunctionExpressionSyntax function,
            INamedTypeSymbol delegateType,
            ParameterListSyntax parameterList,
            CancellationToken cancellationToken)
        {
            var modifiers = function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)
                ? new SyntaxTokenList(function.AsyncKeyword)
                : default;

            var invokeMethod = delegateType.DelegateInvokeMethod;
            var returnType = invokeMethod.GenerateReturnTypeSyntax();

            var identifier = localDeclaration.Declaration.Variables[0].Identifier;
            var typeParameterList = default(TypeParameterListSyntax);

            var constraintClauses = default(SyntaxList<TypeParameterConstraintClauseSyntax>);

            var body = function.Body.IsKind(SyntaxKind.Block)
                ? (BlockSyntax)function.Body
                : null;

            var expressionBody = function.Body is ExpressionSyntax expression
                ? SyntaxFactory.ArrowExpressionClause(((LambdaExpressionSyntax)function).ArrowToken, expression)
                : null;

            var semicolonToken = function.Body is ExpressionSyntax
                ? localDeclaration.SemicolonToken
                : default;

            return SyntaxFactory.LocalFunctionStatement(
                modifiers, returnType, identifier, typeParameterList, parameterList,
                constraintClauses, body, expressionBody, semicolonToken);
        }

        private static ParameterListSyntax GenerateParameterList(
            SemanticModel semanticModel, AnonymousFunctionExpressionSyntax function, INamedTypeSymbol delegateType, CancellationToken cancellationToken)
        {
            var (parameterList, simpleParameter) = GetParameterListOrParameter(function);

            int i = 0;

            return parameterList != null
                ? parameterList.ReplaceNodes(parameterList.Parameters, (parameterNode, _) => PromoteParameter(parameterNode, i++))
                : SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>().Add(PromoteParameter(simpleParameter, 0)));

            ParameterSyntax PromoteParameter(ParameterSyntax parameterNode, int parameterIndex)
            {
                if (parameterNode.Type == null)
                {
                    var parameter = semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);
                    parameterNode = parameterNode.WithType(parameter?.Type.GenerateTypeSyntax() ?? s_objectType);
                }

                var delegateParameter = delegateType.DelegateInvokeMethod.Parameters[parameterIndex];
                if (delegateParameter.HasExplicitDefaultValue)
                {
                    parameterNode = parameterNode.WithDefault(GetDefaultValue(delegateParameter));
                }

                return parameterNode;
            }
        }

        private static (ParameterListSyntax, ParameterSyntax) GetParameterListOrParameter(AnonymousFunctionExpressionSyntax function)
        {
            switch (function)
            {
                case SimpleLambdaExpressionSyntax simpleLambda:
                    return (null, simpleLambda.Parameter);
                case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                    return (parenthesizedLambda.ParameterList, null);
                case AnonymousMethodExpressionSyntax anonymousMethod:
                    return (anonymousMethod.ParameterList, null);
                default:
                    throw ExceptionUtilities.UnexpectedValue(function);
            }
        }

        private static EqualsValueClauseSyntax GetDefaultValue(IParameterSymbol parameter)
            => SyntaxFactory.EqualsValueClause(ExpressionGenerator.GenerateExpression(parameter.Type, parameter.ExplicitDefaultValue, canUseFieldReference: true));

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Use_local_function, createChangedDocument, FeaturesResources.Use_local_function)
            {
            }
        }
    }
}
