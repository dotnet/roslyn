using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression
{
    [ExportSuppressionFixProvider(PredefinedCodeFixProviderNames.ConfigureSeverity, LanguageNames.CSharp), Shared]
    internal class CSharpConfigureSeverityLevel : ConfigureSeverityLevelCodeFixProvider
    {
        public CSharpConfigureSeverityLevel() : base(diagnosticToOptionCSharp, LanguageNames.CSharp, expressionOptionsCSharp)
        {
        }

        private static readonly Dictionary<string, Option<CodeStyleOption<bool>>> diagnosticToOptionCSharp = new Dictionary<string, Option<CodeStyleOption<bool>>>()
        {
            { IDEDiagnosticIds.AddBracesDiagnosticId,  CSharpCodeStyleOptions.PreferBraces },
            { IDEDiagnosticIds.InlineAsTypeCheckId, CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck },
            { IDEDiagnosticIds.InlineIsTypeCheckId, CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck },
            { IDEDiagnosticIds.UseDefaultLiteralDiagnosticId, CSharpCodeStyleOptions.PreferSimpleDefaultExpression },
            { IDEDiagnosticIds.UseLocalFunctionDiagnosticId, CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction },
        };

        private static readonly Dictionary<string, Option<CodeStyleOption<ExpressionBodyPreference>>> expressionOptionsCSharp = new Dictionary<string, Option<CodeStyleOption<ExpressionBodyPreference>>>()
        {
            { IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedConstructors },
            { IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedMethods },
            { IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedOperators },
            { IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedOperators },
            { IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedProperties },
            { IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedIndexers },
            { IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId, CSharpCodeStyleOptions.PreferExpressionBodiedAccessors }
        };
    }
}
