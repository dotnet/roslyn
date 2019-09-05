// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnusedVariable;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedVariable), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal partial class CSharpRemoveUnusedVariableCodeFixProvider : AbstractRemoveUnusedVariableCodeFixProvider<LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, VariableDeclarationSyntax>
    {
        public const string CS0168 = nameof(CS0168);
        public const string CS0219 = nameof(CS0219);

        [ImportingConstructor]
        public CSharpRemoveUnusedVariableCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS0168, CS0219);

        protected override bool IsCatchDeclarationIdentifier(SyntaxToken token)
            => token.Parent is CatchDeclarationSyntax catchDeclaration && catchDeclaration.Identifier == token;

        protected override SyntaxNode GetNodeToRemoveOrReplace(SyntaxNode node)
        {
            node = node.Parent;
            if (node.Kind() == SyntaxKind.SimpleAssignmentExpression)
            {
                var parent = node.Parent;
                if (parent.Kind() == SyntaxKind.ExpressionStatement)
                {
                    return parent;
                }
                else
                {
                    return node;
                }
            }

            return null;
        }

        protected override void RemoveOrReplaceNode(SyntaxEditor editor, SyntaxNode node, ISyntaxFactsService syntaxFacts)
        {
            switch (node.Kind())
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    editor.ReplaceNode(node, ((AssignmentExpressionSyntax)node).Right);
                    return;
                default:
                    RemoveNode(editor, node, syntaxFacts);
                    return;
            }
        }

        protected override SeparatedSyntaxList<SyntaxNode> GetVariables(LocalDeclarationStatementSyntax localDeclarationStatement)
            => localDeclarationStatement.Declaration.Variables;
    }
}
