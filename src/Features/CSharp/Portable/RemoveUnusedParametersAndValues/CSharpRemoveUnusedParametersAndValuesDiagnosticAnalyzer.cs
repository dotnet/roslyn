// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer
    {
        protected override Option<CodeStyleOption<UnusedValuePreference>> UnusedValueExpressionStatementOption
            => CSharpCodeStyleOptions.UnusedValueExpressionStatement;

        protected override Option<CodeStyleOption<UnusedValuePreference>> UnusedValueAssignmentOption
            => CSharpCodeStyleOptions.UnusedValueAssignment;

        protected override bool SupportsDiscard(SyntaxTree tree)
            => ((CSharpParseOptions)tree.Options).LanguageVersion >= LanguageVersion.CSharp7;

        protected override bool MethodHasHandlesClause(IMethodSymbol method)
            => false;

        protected override bool IsIfConditionalDirective(SyntaxNode node)
            => node is IfDirectiveTriviaSyntax;

        // C# does not have an explicit "call" statement syntax for invocations with explicit value discard.
        protected override bool IsCallStatement(IExpressionStatementOperation expressionStatement)
            => false;

        protected override Location GetDefinitionLocationToFade(IOperation unusedDefinition)
        {
            switch (unusedDefinition.Syntax)
            {
                case VariableDeclaratorSyntax variableDeclartor:
                    return variableDeclartor.Identifier.GetLocation();

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
