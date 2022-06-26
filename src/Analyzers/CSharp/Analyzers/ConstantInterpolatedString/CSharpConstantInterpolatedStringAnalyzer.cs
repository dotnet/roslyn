// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.ConstantInterpolatedString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)] // constant interpolated strings is a C#-only feature.
    internal sealed class CSharpConstantInterpolatedStringAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpConstantInterpolatedStringAnalyzer() : base(
            IDEDiagnosticIds.UseConstantInterpolatedStringDiagnosticId,
            EnforceOnBuildValues.UseConstantInterpolatedString,
            option: null,
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_constant_interpolated_string), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (!((CSharpCompilation)context.Compilation).LanguageVersion.HasConstantInterpolatedStrings())
                {
                    return;
                }

                context.RegisterOperationAction(context =>
                {
                    var operation = (IBinaryOperation)context.Operation;
                    if (!ShouldAnalyze(operation))
                    {
                        return;
                    }

                    if (!AllOperandsAreStringLiterals(operation))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Descriptor, operation.Syntax.GetLocation()));
                    }
                }, OperationKind.Binary);
            });
        }

        private static bool AllOperandsAreStringLiterals(IBinaryOperation operation)
        {
            var stack = new Stack<IOperation>();
            stack.Push(operation);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is IBinaryOperation binaryOperation)
                {
                    stack.Push(binaryOperation.RightOperand);
                    stack.Push(binaryOperation.LeftOperand);
                }
                else if (current.Kind != OperationKind.Literal)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ShouldAnalyze(IBinaryOperation operation)
        {
            // Avoid analyzing user-defined operators.
            if (operation.OperatorMethod is not null ||
                operation.OperatorKind != BinaryOperatorKind.Add ||
                // Avoid nested diagnostics. We will report on the parent if needed.
                operation.Parent?.Kind == OperationKind.Binary ||
                operation.ConstantValue.Value is not string)
            {
                return false;
            }

            return true;
        }
    }
}
