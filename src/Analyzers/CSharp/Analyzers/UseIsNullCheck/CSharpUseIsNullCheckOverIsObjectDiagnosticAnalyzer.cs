// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseIsNullCheckOverIsObjectDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseIsNullCheckOverIsObjectDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseIsNullOverIsObjectDiagnosticId,
                   EnforceOnBuildValues.UseIsNullCheck,
                   CSharpCodeStyleOptions.PreferIsNullCheckOverIsObject,
                   CSharpAnalyzersResources.Use_is_null_check,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Null_check_can_be_clarified), AnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                var objectType = context.Compilation.ObjectType;
                if (objectType.TypeKind == TypeKind.Error)
                {
                    return;
                }

                // All trees should have the same language version. Bail-out early in compilation start instead of checking every tree.
                var tree = context.Compilation.SyntaxTrees.FirstOrDefault();
                if (tree is null || ((CSharpParseOptions)tree.Options).LanguageVersion < LanguageVersion.CSharp9)
                {
                    return;
                }

                context.RegisterOperationAction(c => AnalyzeOperation(c, objectType), OperationKind.IsType, OperationKind.NegatedPattern);
            });
        }

        private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol objectType)
        {
            var option = context.Options.GetOption(CSharpCodeStyleOptions.PreferIsNullCheckOverIsObject, context.Operation.Syntax.SyntaxTree, context.CancellationToken);
            if (!option.Value)
            {
                return;
            }

            if (ShouldReportDiagnostic(context.Operation, objectType))
            {
                var severity = option.Notification.Severity;
                context.ReportDiagnostic(
                    DiagnosticHelper.Create(
                        Descriptor, context.Operation.Syntax.GetLocation(), severity, additionalLocations: null, properties: null));
            }
        }

        private static bool ShouldReportDiagnostic(IOperation operation, INamedTypeSymbol objectType)
        {
            if (operation is IIsTypeOperation isTypeOperation)
            {
                return objectType.Equals(isTypeOperation.TypeOperand, SymbolEqualityComparer.Default);
            }
            else if (operation is INegatedPatternOperation negatedPattern)
            {
                return negatedPattern.Pattern is ITypePatternOperation typePatternOperation &&
                    objectType.Equals(typePatternOperation.MatchedType, SymbolEqualityComparer.Default);
            }

            // Only OperationKind.IsType and OperationKind.NegatedPattern are registered.
            throw ExceptionUtilities.Unreachable;
        }
    }
}
