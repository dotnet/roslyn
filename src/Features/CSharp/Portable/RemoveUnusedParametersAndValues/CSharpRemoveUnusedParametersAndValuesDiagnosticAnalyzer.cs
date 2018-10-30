// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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

        protected override Location GetDefinitionLocationToFade(IOperation unusedDefinition)
        {
            switch (unusedDefinition.Syntax)
            {
                case VariableDeclaratorSyntax variableDeclartor:
                    return variableDeclartor.Identifier.GetLocation();

                case DeclarationPatternSyntax declarationPattern:
                    return declarationPattern.Designation.GetLocation();

                default:
                    if (unusedDefinition.Syntax?.Parent is ForEachStatementSyntax forEachStatement &&
                        forEachStatement.Type == unusedDefinition.Syntax)
                    {
                        return forEachStatement.Identifier.GetLocation();
                    }

                    return unusedDefinition.Syntax.GetLocation();
            }
        }
    }
}
