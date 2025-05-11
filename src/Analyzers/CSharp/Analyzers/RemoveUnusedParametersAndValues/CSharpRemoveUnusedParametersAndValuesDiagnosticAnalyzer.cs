// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer : AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer
{
    public CSharpRemoveUnusedParametersAndValuesDiagnosticAnalyzer()
        : base(unusedValueExpressionStatementOption: CSharpCodeStyleOptions.UnusedValueExpressionStatement,
               unusedValueAssignmentOption: CSharpCodeStyleOptions.UnusedValueAssignment)
    {
    }

    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    protected override bool SupportsDiscard(SyntaxTree tree)
        => tree.Options.LanguageVersion() >= LanguageVersion.CSharp7;

    protected override bool MethodHasHandlesClause(IMethodSymbol method)
        => false;

    protected override bool IsIfConditionalDirective(SyntaxNode node)
        => node is IfDirectiveTriviaSyntax;

    protected override bool ReturnsThrow(SyntaxNode? node)
    {
        if (node is not BaseMethodDeclarationSyntax methodSyntax)
        {
            return false;
        }

        if (methodSyntax.ExpressionBody is not null)
        {
            return methodSyntax.ExpressionBody.Expression is ThrowExpressionSyntax;
        }

        return methodSyntax.Body is { Statements: [ThrowStatementSyntax] };
    }

    protected override CodeStyleOption2<UnusedValuePreference> GetUnusedValueExpressionStatementOption(AnalyzerOptionsProvider provider)
        => ((CSharpAnalyzerOptionsProvider)provider).UnusedValueExpressionStatement;

    protected override CodeStyleOption2<UnusedValuePreference> GetUnusedValueAssignmentOption(AnalyzerOptionsProvider provider)
        => ((CSharpAnalyzerOptionsProvider)provider).UnusedValueAssignment;

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
        return statementAncestor is not (BlockSyntax or SwitchSectionSyntax);
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

            case RecursivePatternSyntax recursivePattern:
                Debug.Assert(recursivePattern.Designation is not null, "If we got to this point variable designation cannot be null");
                return recursivePattern.Designation!.GetLocation();

            case ListPatternSyntax listPattern:
                Debug.Assert(listPattern.Designation is not null, "If we got to this point variable designation cannot be null");
                return listPattern.Designation!.GetLocation();

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
