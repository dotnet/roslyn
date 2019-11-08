// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>Analyzer that looks boxing operations.</summary>
    public class BoxingOperationAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>Diagnostic category "Performance".</summary>
        private const string PerformanceCategory = "Performance";

        private readonly static LocalizableString s_localizableTitle = "Boxing";
        private readonly static LocalizableString s_localizableMessage = "Boxing is expensive";

        /// <summary>The diagnostic descriptor used when boxing is detected.</summary>
        public static readonly DiagnosticDescriptor BoxingDescriptor = new DiagnosticDescriptor(
            "BoxingRule",
            s_localizableTitle,
            s_localizableMessage,
            PerformanceCategory,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>Gets the set of supported diagnostic descriptors from this analyzer.</summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(BoxingDescriptor); }
        }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(
                 (operationContext) =>
                 {
                     IOperation operation = operationContext.Operation;

                     if (operation.Kind == OperationKind.Conversion)
                     {
                         IConversionOperation conversion = (IConversionOperation)operation;
                         if (conversion is {
                             Operand:
                             {
                                 Type: { IsReferenceType: true, IsValueType: true }
                             }, OperatorMethod: null
                         })
                         {
                             Report(operationContext, conversion.Syntax);
                         }
                     }

                     // Calls to instance methods of value types don’t have conversions.
                     if (operation.Kind == OperationKind.Invocation)
                     {
                         IInvocationOperation invocation = (IInvocationOperation)operation;

                         if (invocation.Instance != null &&
                             invocation.Instance.Type.IsValueType &&
                             invocation.TargetMethod.ContainingType.IsReferenceType)
                         {
                             Report(operationContext, invocation.Instance.Syntax);
                         }
                     }
                 },
                 OperationKind.Conversion,
                 OperationKind.Invocation);
        }

        /// <summary>Reports a diagnostic warning for a boxing operation.</summary>
        /// <param name="context">The context.</param>
        /// <param name="boxingExpression">The expression that produces the boxing.</param>
        internal void Report(OperationAnalysisContext context, SyntaxNode boxingExpression)
        {
            context.ReportDiagnostic(Diagnostic.Create(BoxingDescriptor, boxingExpression.GetLocation()));
        }
    }
}
