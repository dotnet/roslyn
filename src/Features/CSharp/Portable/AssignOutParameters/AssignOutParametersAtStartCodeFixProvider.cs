//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System;
//using System.Collections.Immutable;
//using System.Composition;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
//using Microsoft.CodeAnalysis.CSharp.Extensions;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Editing;
//using Microsoft.CodeAnalysis.Formatting;
//using Microsoft.CodeAnalysis.LanguageServices;
//using Microsoft.CodeAnalysis.PooledObjects;
//using Microsoft.CodeAnalysis.Shared.Extensions;
//using Microsoft.CodeAnalysis.Text;
//using Microsoft.CodeAnalysis.Wrapping;

//namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters
//{
//    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
//    internal class AssignOutParametersAtStartCodeFixProvider : AbstractAssignOutParametersCodeFixProvider
//    {
//        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
//        {
//            var cancellationToken = context.CancellationToken;
//            var document = context.Document;
//            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

//            var (container, location) = GetContainer(root, context.Span);
//            if (container != null)
//            {
//                context.RegisterCodeFix(new MyCodeAction(
//                    CSharpFeaturesResources.Assign_out_parameters,
//                    c => FixAsync(document, context.Diagnostics[0], c)), context.Diagnostics);
//            }
//        }

//        protected override void TryRegisterFix(CodeFixContext context, Document document, SyntaxNode container, SyntaxNode location)
//        {
//            // Don't offer if we're already the starting statement of the container. This case will
//            // be handled by the AssignOutParametersAboveReturnCodeFixProvider class.
//            if (location is ExpressionSyntax)
//            {
//                return;
//            }

//            if (location is StatementSyntax statement &&
//                statement.Parent is BlockSyntax block &&
//                block.Statements[0] == statement &&
//                block.Parent == container)
//            {
//                return;
//            }

//            context.RegisterCodeFix(new MyCodeAction(
//               CSharpFeaturesResources.Assign_out_parameters_at_start,
//               c => FixAsync(document, context.Diagnostics[0], c)), context.Diagnostics);
//        }

//        protected override void AddAssignmentStatements(
//            SyntaxEditor editor, SyntaxNode exprOrStatement, ArrayBuilder<SyntaxNode> statements)
//        {
//            throw new NotImplementedException();
//        }

//        private static (SyntaxNode container, SyntaxNode exprOrStatement) GetContainer(SyntaxNode root, TextSpan span)
//        {
//            var location = root.FindNode(span);
//            if (IsValidLocation(location))
//            {
//                var container = GetContainer(location);
//                if (container != null)
//                {
//                    return (container, location);
//                }
//            }

//            return default;
//        }

//        private static bool IsValidLocation(SyntaxNode location)
//        {
//            if (location is StatementSyntax)
//            {
//                return location.Parent is BlockSyntax
//                    || location.Parent is SwitchSectionSyntax
//                    || location.Parent.IsEmbeddedStatementOwner();
//            }

//            if (location is ExpressionSyntax)
//            {
//                return location.Parent is ArrowExpressionClauseSyntax || location.Parent is LambdaExpressionSyntax;
//            }

//            return false;
//        }

//        private static SyntaxNode GetContainer(SyntaxNode node)
//        {
//            for (var current = node; current != null; current = current.Parent)
//            {
//                var parameterList = CSharpSyntaxGenerator.GetParameterList(current);
//                if (parameterList != null)
//                {
//                    return current;
//                }
//            }

//            return null;
//        }

//        protected override async Task FixAllAsync(
//            Document document, ImmutableArray<Diagnostic> diagnostics,
//            SyntaxEditor editor, CancellationToken cancellationToken)
//        {
//            var root = editor.OriginalRoot;
//            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

//            var containersAndLocations =
//                diagnostics.SelectAsArray(d => GetContainer(root, d.Location.SourceSpan))
//                           .WhereAsArray(t => t.container != null);

//            foreach (var group in containersAndLocations.GroupBy(t => t.container))
//            {
//                var container = group.Key;

//                var parameterList = CSharpSyntaxGenerator.GetParameterList(container);
//                var outParameters =
//                    parameterList.Parameters.Select(p => semanticModel.GetDeclaredSymbol(p, cancellationToken))
//                                            .Where(p => p?.RefKind == RefKind.Out)
//                                            .ToImmutableArray();

//                var distinctExprsOrStatements = group.Select(t => t.exprOrStatement).Distinct();
//                foreach (var exprOrStatement in distinctExprsOrStatements)
//                {
//                    var dataFlow = semanticModel.AnalyzeDataFlow(exprOrStatement);
//                    var definitelAssignedOnExit = dataFlow.DefinitelyAssignedOnEntry.AddRange(dataFlow.AlwaysAssigned);

//                    var unassignedParameters = outParameters.WhereAsArray(p => !definitelAssignedOnExit.Contains(p));
//                    if (unassignedParameters.Length > 0)
//                    {
//                        AssignOutParameters(editor, exprOrStatement, unassignedParameters);
//                    }
//                }
//            }
//        }

//        private void AssignOutParameters(
//            SyntaxEditor editor, SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol> unassignedParameters)
//        {
//            var generator = editor.Generator;
//            var statements = ArrayBuilder<SyntaxNode>.GetInstance();

//            foreach (var parameter in unassignedParameters)
//            {
//                statements.Add(generator.ExpressionStatement(generator.AssignmentStatement(
//                    generator.IdentifierName(parameter.Name),
//                    ExpressionGenerator.GenerateExpression(parameter.Type, value: null, canUseFieldReference: false))));
//            }

//            var parent = exprOrStatement.Parent;
//            if (parent.IsEmbeddedStatementOwner())
//            {
//                statements.Add(exprOrStatement);
//                ReplaceWithBlock(editor, exprOrStatement, statements);
//            }
//            else if (parent is BlockSyntax || parent is SwitchSectionSyntax)
//            {
//                editor.InsertBefore(exprOrStatement, statements);
//            }
//            else if (parent is ArrowExpressionClauseSyntax)
//            {
//                statements.Add(generator.ReturnStatement(exprOrStatement));
//                editor.ReplaceNode(
//                    parent.Parent,
//                    generator.WithStatements(parent.Parent, statements));
//            }
//            else
//            {
//                Debug.Assert(parent is LambdaExpressionSyntax);
//                statements.Add(generator.ReturnStatement(exprOrStatement));
//                ReplaceWithBlock(editor, exprOrStatement, statements);
//            }

//            statements.Free();
//        }

//        private static void ReplaceWithBlock(
//            SyntaxEditor editor, SyntaxNode exprOrStatement, ArrayBuilder<SyntaxNode> statements)
//        {
//            editor.ReplaceNode(
//                exprOrStatement,
//                editor.Generator.ScopeBlock(statements));
//            editor.ReplaceNode(
//                exprOrStatement.Parent,
//                (c, _) => c.WithAdditionalAnnotations(Formatter.Annotation));
//        }

//        private class MyCodeAction : CodeAction.DocumentChangeAction
//        {
//            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
//                : base(title, createChangedDocument, title)
//            {
//            }
//        }
//    }
//}
