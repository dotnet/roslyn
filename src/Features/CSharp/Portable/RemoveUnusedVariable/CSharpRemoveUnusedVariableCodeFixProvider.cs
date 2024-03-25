// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.RemoveUnusedVariable;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedVariable), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
internal partial class CSharpRemoveUnusedVariableCodeFixProvider : AbstractRemoveUnusedVariableCodeFixProvider<LocalDeclarationStatementSyntax, VariableDeclaratorSyntax, VariableDeclarationSyntax>
{
    public const string CS0168 = nameof(CS0168);
    public const string CS0219 = nameof(CS0219);

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpRemoveUnusedVariableCodeFixProvider()
    {
    }

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [CS0168, CS0219];

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

    protected override void RemoveOrReplaceNode(SyntaxEditor editor, SyntaxNode node, IBlockFactsService blockFacts)
    {
        switch (node.Kind())
        {
            case SyntaxKind.SimpleAssignmentExpression:
                editor.ReplaceNode(node, ((AssignmentExpressionSyntax)node).Right);
                return;
            default:
                RemoveNode(editor, node.IsParentKind(SyntaxKind.GlobalStatement) ? node.Parent : node, blockFacts);
                return;
        }
    }

    protected override SeparatedSyntaxList<SyntaxNode> GetVariables(LocalDeclarationStatementSyntax localDeclarationStatement)
        => localDeclarationStatement.Declaration.Variables;

    protected override bool ShouldOfferFixForLocalDeclaration(IBlockFactsService blockFacts, SyntaxNode node)
    {
        // If the fix location is not for a local declaration then we can allow it (eg, when inside a for
        // or catch).
        if (node.Parent?.Parent is not LocalDeclarationStatementSyntax localDeclaration)
            return true;

        // Local declarations must be parented by an executable block, or global statement, otherwise
        // removing them would be invalid (and more than likely crash the fixer)
        return localDeclaration.Parent is GlobalStatementSyntax ||
            blockFacts.IsExecutableBlock(localDeclaration.Parent);
    }
}
