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
            if (node is VariableDeclaratorSyntax variableDeclarator)
            {
                var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
                if (s_analyzer.TryAnalyzeVariableDeclaration(
                        semanticModel, variableDeclaration,
                        out var tupleType, out var memberAccessExpressions,
                        cancellationToken))
                {
                    var editor = new SyntaxEditor(root, CSharpSyntaxGenerator.Instance);

                    foreach (var memberAccess in memberAccessExpressions)
                    {
                        editor.ReplaceNode(memberAccess, memberAccess.Name.WithTriviaFrom(memberAccess));
                    }

                    editor.ReplaceNode(
                        variableDeclaration.Parent,
                        CreateDeconstructionStatement(tupleType, (LocalDeclarationStatementSyntax)variableDeclaration.Parent, variableDeclaration));

                    return editor.GetChangedRoot();
                }
            }

            return root;
        }

        private ExpressionStatementSyntax CreateDeconstructionStatement(
            INamedTypeSymbol tupleType, LocalDeclarationStatementSyntax declarationStatement, VariableDeclarationSyntax variableDeclaration)
        {
            var variableDeclarator = variableDeclaration.Variables[0];
            var left = CreateTupleOrDeclarationExpression(tupleType, variableDeclaration.Type);
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    left,
                    variableDeclarator.Initializer.EqualsToken,
                    variableDeclarator.Initializer.Value),
                declarationStatement.SemicolonToken);
        }

        private ExpressionSyntax CreateTupleOrDeclarationExpression(INamedTypeSymbol tupleType, TypeSyntax typeNode)
        {
            if (typeNode.IsKind(SyntaxKind.TupleType))
            {
                return CreateTupleExpression((TupleTypeSyntax)typeNode);
            }
            else
            {
                Debug.Assert(typeNode.IsVar);
                return CreateDeclarationExpression(tupleType, typeNode);
            }
        }

        private DeclarationExpressionSyntax CreateDeclarationExpression(INamedTypeSymbol tupleType, TypeSyntax typeNode)
        {
            return SyntaxFactory.DeclarationExpression(
                typeNode, SyntaxFactory.ParenthesizedVariableDesignation(
                    CreateVariableDesignations(tupleType)));
        }

        private SeparatedSyntaxList<VariableDesignationSyntax> CreateVariableDesignations(INamedTypeSymbol tupleType)
        {
            return SyntaxFactory.SeparatedList<VariableDesignationSyntax>(tupleType.TupleElements.Select(
                e => SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(e.Name))));
        }

        private TupleExpressionSyntax CreateTupleExpression(TupleTypeSyntax typeNode)
        {
            return SyntaxFactory.TupleExpression(
                typeNode.OpenParenToken,
                SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrTokenList(typeNode.Elements.GetWithSeparators().Select(ConvertTupleTypeElementComponent))),
                typeNode.CloseParenToken);
        }

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
