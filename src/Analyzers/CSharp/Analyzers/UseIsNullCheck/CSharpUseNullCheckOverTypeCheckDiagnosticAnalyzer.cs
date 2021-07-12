﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseNullCheckOverTypeCheckDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseNullCheckOverTypeCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId,
                   EnforceOnBuildValues.UseNullCheckOverTypeCheck,
                   CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck,
                   CSharpAnalyzersResources.Prefer_null_check_over_type_check,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Null_check_can_be_clarified), AnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (((CSharpCompilation)context.Compilation).LanguageVersion < LanguageVersion.CSharp9)
                {
                    return;
                }

                context.RegisterOperationAction(c => AnalyzeIsTypeOperation(c), OperationKind.IsType);
                context.RegisterOperationAction(c => AnalyzeNegatedPatternOperation(c), OperationKind.NegatedPattern);
            });
        }

        private static bool ShouldAnalyze(OperationAnalysisContext context, out ReportDiagnostic severity)
        {
            var option = context.Options.GetOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, context.Operation.Syntax.SyntaxTree, context.CancellationToken);
            if (!option.Value)
            {
                severity = ReportDiagnostic.Default;
                return false;
            }

            severity = option.Notification.Severity;
            return true;
        }

        private void AnalyzeNegatedPatternOperation(OperationAnalysisContext context)
        {
            if (!ShouldAnalyze(context, out var severity) ||
                context.Operation.Syntax is not UnaryPatternSyntax)
            {
                return;
            }

            var negatedPattern = (INegatedPatternOperation)context.Operation;
            // Matches 'x is not MyType'
            // InputType is the type of 'x'
            // MatchedType is 'MyType'
            // We check InheritsFromOrEquals so that we report a diagnostic on the following:
            // 1. x is not object (which is also equivalent to 'is null' check)
            // 2. derivedObj is parentObj (which is the same as the previous point).
            // 3. str is string (where str is a string, this is also equivalent to 'is null' check).
            // This doesn't match `x is not MyType y` because in such case, negatedPattern.Pattern will
            // be `DeclarationPattern`, not `TypePattern`.
            if (negatedPattern.Pattern is ITypePatternOperation typePatternOperation &&
                typePatternOperation.InputType.InheritsFromOrEquals(typePatternOperation.MatchedType))
            {
                context.ReportDiagnostic(
                    DiagnosticHelper.Create(
                        Descriptor, context.Operation.Syntax.GetLocation(), severity, additionalLocations: null, properties: null));
            }
        }

        private void AnalyzeIsTypeOperation(OperationAnalysisContext context)
        {
            if (!ShouldAnalyze(context, out var severity) ||
                context.Operation.Syntax is not BinaryExpressionSyntax)
            {
                return;
            }

            var isTypeOperation = (IIsTypeOperation)context.Operation;
            // Matches 'x is MyType'
            // isTypeOperation.TypeOperand is 'MyType'
            // isTypeOperation.ValueOperand.Type is the type of 'x'.
            // We check InheritsFromOrEquals for the same reason as stated in AnalyzeNegatedPatternOperation.
            // This doesn't match `x is MyType y` because in such case, we have an IsPattern instead of IsType operation.
            if (isTypeOperation.ValueOperand.Type is not null &&
                isTypeOperation.ValueOperand.Type.InheritsFromOrEquals(isTypeOperation.TypeOperand))
            {
                context.ReportDiagnostic(
                    DiagnosticHelper.Create(
                        Descriptor, context.Operation.Syntax.GetLocation(), severity, additionalLocations: null, properties: null));
            }
        }
    }
}
