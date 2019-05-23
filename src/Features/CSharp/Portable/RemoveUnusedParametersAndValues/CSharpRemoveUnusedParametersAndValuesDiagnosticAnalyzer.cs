// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer
    {
        public CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer()
            : base(unusedValueExpressionStatementOption: CSharpCodeStyleOptions.UnusedValueExpressionStatement,
                   unusedValueAssignmentOption: CSharpCodeStyleOptions.UnusedValueAssignment,
                   LanguageNames.CSharp)
        {
        }

        protected override bool SupportsDiscard(SyntaxTree tree)
            => ((CSharpParseOptions)tree.Options).LanguageVersion >= LanguageVersion.CSharp7;

        protected override bool MethodHasHandlesClause(IMethodSymbol method)
            => false;

        protected override bool IsIfConditionalDirective(SyntaxNode node)
            => node is IfDirectiveTriviaSyntax;

        protected override bool ShouldBailOutFromRemovableAssignmentAnalysis(IOperation unusedSymbolWriteOperation)
        {
            // We don't want to recommend removing the write operation if it is within a statement
            // that is not parented by an explicit block with curly braces.
            // For example, assignment to 'x' in 'if (...) x = 1;'
            // Replacing 'x = 1' with an empty statement ';' is not useful, and user is most likely
            // going to remove the entire if statement in this case. However, we don't
            // want to suggest removing the entire if statement as that might lead to change of semantics.
            // So, we conservatively bail out from removable assignment analysis for such cases.

            var statementAncestor = unusedSymbolWriteOperation.Syntax.FirstAncestorOrSelf<StatementSyntax>()?.Parent;
            switch (statementAncestor)
            {
                case BlockSyntax _:
                case SwitchSectionSyntax _:
                    return false;

                default:
                    return true;
            }
        }

        // C# does not have an explicit "call" statement syntax for invocations with explicit value discard.
        protected override bool IsCallStatement(IExpressionStatementOperation expressionStatement)
            => false;

        protected override bool IsExpressionOfExpressionBody(IExpressionStatementOperation expressionStatementOperation)
            => expressionStatementOperation.Parent is IBlockOperation blockOperation &&
               !blockOperation.Syntax.IsKind(SyntaxKind.Block);

        protected override Location GetDefinitionLocationToFade(IOperation unusedDefinition)
        {
            switch (unusedDefinition.Syntax)
            {
                case VariableDeclaratorSyntax variableDeclarator:
                    return variableDeclarator.Identifier.GetLocation();

                case DeclarationPatternSyntax declarationPattern:
                    return declarationPattern.Designation.GetLocation();

                default:
                    // C# syntax node for foreach statement has no syntax node for the loop control variable declaration,
                    // so the operation tree has an IVariableDeclaratorOperation with the syntax mapped to the type node syntax instead of variable declarator syntax.
                    // Check if the unused definition syntax is the foreach statement's type node.
                    if (unusedDefinition.Syntax.Parent is ForEachStatementSyntax forEachStatement &&
                        forEachStatement.Type == unusedDefinition.Syntax)
                    {
                        return forEachStatement.Identifier.GetLocation();
                    }

                    return unusedDefinition.Syntax.GetLocation();
            }
        }
    }
}
