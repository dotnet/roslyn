// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Testing.TestAnalyzers
{
    /// <summary>
    /// Reports a diagnostic on any integer literal with a value less than five.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class LiteralUnderFiveAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Descriptor =
            new DiagnosticDescriptor("LiteralUnderFive", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(HandleLiteralOperation, OperationKind.Literal);
        }

        private void HandleLiteralOperation(OperationAnalysisContext context)
        {
            var operation = (ILiteralOperation)context.Operation;
            if (operation.ConstantValue.HasValue
                && operation.ConstantValue.Value is int value
                && value < 5)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, operation.Syntax.GetLocation()));
            }
        }
    }
}
