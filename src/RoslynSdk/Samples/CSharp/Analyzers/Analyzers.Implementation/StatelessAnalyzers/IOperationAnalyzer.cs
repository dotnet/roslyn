// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Sample.Analyzers.StatelessAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class IOperationAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Reduce allocations and use Array.Empty";
        private const string MessageFormat = "Replace empty array allocation with Array.Empty.";
        private const string Description = "Reduce allocations and use Array.Empty.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.IOperationAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Stateless,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.ArrayCreation);
        }

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            IArrayCreationOperation creationExpression = (IArrayCreationOperation)context.Operation;

            if (creationExpression.DimensionSizes.Length == 1 && creationExpression.DimensionSizes[0].ConstantValue.HasValue)
            {
                object arrayDimension = creationExpression.DimensionSizes[0].ConstantValue.Value;
                if (arrayDimension is int && (int)arrayDimension == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation()));
                }
            }
        }
    }
}
