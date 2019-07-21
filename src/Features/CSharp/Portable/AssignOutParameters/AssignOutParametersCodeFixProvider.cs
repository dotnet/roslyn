// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class AssignOutParametersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS0177 = nameof(CS0177); // The out parameter 'x' must be assigned to before control leaves the current method

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0177);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var (container, location) = GetContainer(root, context.Span);
            if (container != null)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    c => FixAsync(document, context.Diagnostics[0], c)), context.Diagnostics);
            }
        }

        private static (SyntaxNode container, SyntaxNode exprOrStatement) GetContainer(SyntaxNode root, TextSpan span)
        {
            var location = root.FindNode(span);
            if (IsValidLocation(location))
            {
                var container = GetContainer(location);
                if (container != null)
                {
                    return (container, location);
                }
            }

            return default;
        }

        private static bool IsValidLocation(SyntaxNode location)
        {
            if (location is StatementSyntax)
            {
                return location.Parent is BlockSyntax
                    || location.Parent is SwitchSectionSyntax
                    || location.Parent.IsEmbeddedStatementOwner();
            }

            if (location is ExpressionSyntax)
            {
                return location.Parent is ArrowExpressionClauseSyntax || location.Parent is LambdaExpressionSyntax;
            }

            return false;
        }

        private static SyntaxNode GetContainer(SyntaxNode node)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                var parameterList = CSharpSyntaxGenerator.GetParameterList(current);
                if (parameterList != null)
                {
                    return current;
                }
            }

            return null;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var containersAndLocations =
                diagnostics.SelectAsArray(d => GetContainer(root, d.Location.SourceSpan))
                           .WhereAsArray(t => t.container != null);

            foreach (var group in containersAndLocations.GroupBy(t => t.container))
            {
                var container = group.Key;

                var parameterList = CSharpSyntaxGenerator.GetParameterList(container);
                var outParameters =
                    parameterList.Parameters.Select(p => semanticModel.GetDeclaredSymbol(p, cancellationToken))
                                            .Where(p => p?.RefKind == RefKind.Out)
                                            .ToImmutableArray();

                foreach (var (_, exprOrStatement) in group)
                {
                    var dataFlow = semanticModel.AnalyzeDataFlow(exprOrStatement);
                    var definitelAssignedOnExit = dataFlow.DefinitelyAssignedOnEntry.AddRange(dataFlow.AlwaysAssigned);

                    var unassignedParameters = outParameters.WhereAsArray(p => !definitelAssignedOnExit.Contains(p));
                    if (unassignedParameters.Length > 0)
                    {
                        AssignOutParameters(editor, exprOrStatement, unassignedParameters);
                    }
                }
            }
        }

        private void AssignOutParameters(
            SyntaxEditor editor, SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol> unassignedParameters)
        {
            var generator = editor.Generator;
            var statements = ArrayBuilder<StatementSyntax>.GetInstance();

            foreach (var parameter in unassignedParameters)
            {
                statements.Add((StatementSyntax)generator.ExpressionStatement(generator.AssignmentStatement(
                    generator.IdentifierName(parameter.Name),
                    ExpressionGenerator.GenerateExpression(parameter.Type, value: null, canUseFieldReference: false))));
            }

            if (exprOrStatement is StatementSyntax statement)
            {
                if (exprOrStatement.Parent.IsEmbeddedStatementOwner())
                {
                    statements.Add(statement);
                    editor.ReplaceNode(
                        statement,
                        SyntaxFactory.Block(statements));
                    editor.ReplaceNode(
                        statement.Parent,
                        (c, _) => c.WithAdditionalAnnotations(Formatter.Annotation));
                }
                else
                {
                    editor.InsertBefore(exprOrStatement, statements);
                }
            }

            statements.Free();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Assign_out_parameters, createChangedDocument, CSharpFeaturesResources.Assign_out_parameters)
            {
            }
        }
    }
}
