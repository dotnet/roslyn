// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [ExportLanguageService(typeof(IDiagnosticIdToEditorConfigOptionMappingService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpDiagnosticIdToEditorConfigOptionMappingService : AbstractDiagnosticIdToEditorConfigOptionMappingService
    {
        protected override (IOption optionOpt, bool handled) TryGetLanguageSpecificOption(string diagnosticId)
        {
            IOption option;
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.AddBracesDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferBraces;
                    break;
                case IDEDiagnosticIds.InlineAsTypeCheckId:
                    option = CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck;
                    break;
                case IDEDiagnosticIds.InlineIsTypeCheckId:
                    option = CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedConstructors;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedMethods;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedOperators;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedOperators;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedProperties;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedIndexers;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedAccessors;
                    break;
                case IDEDiagnosticIds.UseDefaultLiteralDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferSimpleDefaultExpression;
                    break;
                case IDEDiagnosticIds.UseLocalFunctionDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction;
                    break;
                case IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId:
                    option = CSharpCodeStyleOptions.PreferConditionalDelegateCall;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedLambdas;
                    break;
                case IDEDiagnosticIds.UseIndexOperatorDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferIndexOperator;
                    break;
                case IDEDiagnosticIds.UseRangeOperatorDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferRangeOperator;
                    break;
                case IDEDiagnosticIds.ExpressionValueIsUnusedDiagnosticId:
                    option = CSharpCodeStyleOptions.UnusedValueExpressionStatement;
                    break;
                case IDEDiagnosticIds.ValueAssignedIsUnusedDiagnosticId:
                    option = CSharpCodeStyleOptions.UnusedValueAssignment;
                    break;
                case IDEDiagnosticIds.UseExpressionBodyForLocalFunctionsDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions;
                    break;
                case IDEDiagnosticIds.MakeLocalFunctionStaticDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferStaticLocalFunction;
                    break;
                case IDEDiagnosticIds.UseSimpleUsingStatementDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferSimpleUsingStatement;
                    break;
                case IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId:
                    option = CSharpCodeStyleOptions.PreferredUsingDirectivePlacement;
                    break;

                default:
                    return (optionOpt: null, handled: false);
            }

            return (optionOpt: option, handled: true);
        }
    }
}
