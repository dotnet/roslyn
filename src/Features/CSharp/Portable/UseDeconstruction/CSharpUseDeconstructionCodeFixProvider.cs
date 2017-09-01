// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseDeconstruction
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseDeconstructionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly CSharpUseDeconstructionDiagnosticAnalyzer s_analyzer = new CSharpUseDeconstructionDiagnosticAnalyzer();

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseDeconstructionDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var nodesToProcess = diagnostics.SelectAsArray(d => d.Location.FindToken(cancellationToken).Parent);

            return editor.ApplyMethodBodySemanticEditsAsync(
                document, nodesToProcess,
                (semanticModel, node) => true,
                (semanticModel, currentRoot, node) => UpdateRoot(semanticModel, currentRoot, node, cancellationToken),
                cancellationToken);
        }

        private SyntaxNode UpdateRoot(SemanticModel semanticModel, SyntaxNode root, SyntaxNode node, CancellationToken cancellationToken)
        {
            var editor = new SyntaxEditor(root, CSharpSyntaxGenerator.Instance);

            ImmutableArray<MemberAccessExpressionSyntax> memberAccessExpressions = default;
            if (node is VariableDeclaratorSyntax variableDeclarator)
            {
                var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
                if (s_analyzer.TryAnalyzeVariableDeclaration(
                        semanticModel, variableDeclaration,
                        out var tupleType, out memberAccessExpressions,
                        cancellationToken))
                {
                    editor.ReplaceNode(
                        variableDeclaration.Parent,
                        CreateDeconstructionStatement(tupleType, (LocalDeclarationStatementSyntax)variableDeclaration.Parent, variableDeclarator));
                }
            }
            else if (node is ForEachStatementSyntax forEachStatement)
            {
                if (s_analyzer.TryAnalyzeForEachStatement(
                        semanticModel, forEachStatement,
                        out var tupleType, out memberAccessExpressions,
                        cancellationToken))
                {
                    editor.ReplaceNode(
                        forEachStatement,
                        CreateForEachVariableStatement(tupleType, forEachStatement));
                }
            }

            foreach (var memberAccess in memberAccessExpressions.NullToEmpty())
            {
                editor.ReplaceNode(memberAccess, memberAccess.Name.WithTriviaFrom(memberAccess));
            }

            return editor.GetChangedRoot();
        }

        private ForEachVariableStatementSyntax CreateForEachVariableStatement(
            INamedTypeSymbol tupleType, ForEachStatementSyntax forEachStatement)
            => SyntaxFactory.ForEachVariableStatement(
                forEachStatement.ForEachKeyword,
                forEachStatement.OpenParenToken,
                CreateTupleOrDeclarationExpression(tupleType, forEachStatement.Type),
                forEachStatement.InKeyword,
                forEachStatement.Expression,
                forEachStatement.CloseParenToken,
                forEachStatement.Statement);

        private ExpressionStatementSyntax CreateDeconstructionStatement(
            INamedTypeSymbol tupleType, LocalDeclarationStatementSyntax declarationStatement, VariableDeclaratorSyntax variableDeclarator)
            => SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    CreateTupleOrDeclarationExpression(tupleType, declarationStatement.Declaration.Type),
                    variableDeclarator.Initializer.EqualsToken,
                    variableDeclarator.Initializer.Value),
                declarationStatement.SemicolonToken);

        private ExpressionSyntax CreateTupleOrDeclarationExpression(INamedTypeSymbol tupleType, TypeSyntax typeNode)
            => typeNode.IsKind(SyntaxKind.TupleType)
                ? (ExpressionSyntax)CreateTupleExpression((TupleTypeSyntax)typeNode)
                : CreateDeclarationExpression(tupleType, typeNode);

        private DeclarationExpressionSyntax CreateDeclarationExpression(INamedTypeSymbol tupleType, TypeSyntax typeNode)
            => SyntaxFactory.DeclarationExpression(
                typeNode, SyntaxFactory.ParenthesizedVariableDesignation(
                    SyntaxFactory.SeparatedList<VariableDesignationSyntax>(tupleType.TupleElements.Select(
                        e => SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(e.Name))))));

        private TupleExpressionSyntax CreateTupleExpression(TupleTypeSyntax typeNode)
            => SyntaxFactory.TupleExpression(
                typeNode.OpenParenToken,
                SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrTokenList(typeNode.Elements.GetWithSeparators().Select(ConvertTupleTypeElementComponent))),
                typeNode.CloseParenToken);

        private SyntaxNodeOrToken ConvertTupleTypeElementComponent(SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsToken)
            {
                return nodeOrToken;
            }

            var node = (TupleElementSyntax)nodeOrToken.AsNode();
            return SyntaxFactory.Argument(
                SyntaxFactory.DeclarationExpression(
                    node.Type,
                    SyntaxFactory.SingleVariableDesignation(node.Identifier)));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Deconstruct_variable_declaration, createChangedDocument, FeaturesResources.Deconstruct_variable_declaration)
            {
            }
        }
    }
}
