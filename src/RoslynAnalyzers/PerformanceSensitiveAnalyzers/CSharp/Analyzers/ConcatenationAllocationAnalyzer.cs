// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{
    using static AnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConcatenationAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string StringConcatenationAllocationRuleId = "HAA0201";
        public const string ValueTypeToReferenceTypeInAStringConcatenationRuleId = "HAA0202";

        internal static readonly DiagnosticDescriptor StringConcatenationAllocationRule = new(
            StringConcatenationAllocationRuleId,
            CreateLocalizableResourceString(nameof(StringConcatenationAllocationRuleTitle)),
            CreateLocalizableResourceString(nameof(StringConcatenationAllocationRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/2839d5h5(v=vs.110).aspx");

        internal static readonly DiagnosticDescriptor ValueTypeToReferenceTypeInAStringConcatenationRule = new(
            ValueTypeToReferenceTypeInAStringConcatenationRuleId,
            CreateLocalizableResourceString(nameof(ValueTypeToReferenceTypeInAStringConcatenationRuleTitle)),
            CreateLocalizableResourceString(nameof(ValueTypeToReferenceTypeInAStringConcatenationRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/yz2be5wk.aspx");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(StringConcatenationAllocationRule, ValueTypeToReferenceTypeInAStringConcatenationRule);

        protected override ImmutableArray<SyntaxKind> Expressions { get; } = ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;
            var binaryExpressions = node.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>().Reverse(); // need inner most expressions

            int stringConcatenationCount = 0;
            foreach (var binaryExpression in binaryExpressions)
            {
                if (binaryExpression.Left == null || binaryExpression.Right == null)
                {
                    continue;
                }

                bool isConstant = semanticModel.GetConstantValue(binaryExpression, cancellationToken).HasValue;
                if (isConstant)
                {
                    continue;
                }

                var left = semanticModel.GetTypeInfo(binaryExpression.Left, cancellationToken);
                var leftConversion = semanticModel.GetConversion(binaryExpression.Left, cancellationToken);
                CheckTypeConversion(left, leftConversion, reportDiagnostic, binaryExpression.Left);

                var right = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);
                var rightConversion = semanticModel.GetConversion(binaryExpression.Right, cancellationToken);
                CheckTypeConversion(right, rightConversion, reportDiagnostic, binaryExpression.Right);

                // regular string allocation
                if (left.Type?.SpecialType == SpecialType.System_String || right.Type?.SpecialType == SpecialType.System_String)
                {
                    stringConcatenationCount++;
                }
            }

            if (stringConcatenationCount > 3)
            {
                reportDiagnostic(node.CreateDiagnostic(StringConcatenationAllocationRule, EmptyMessageArgs));
            }
        }

        private static void CheckTypeConversion(TypeInfo typeInfo, Conversion conversionInfo, Action<Diagnostic> reportDiagnostic, ExpressionSyntax expression)
        {
            if (conversionInfo.IsBoxing && typeInfo.Type != null && !IsOptimizedValueType(typeInfo.Type))
            {
                reportDiagnostic(expression.CreateDiagnostic(ValueTypeToReferenceTypeInAStringConcatenationRule, typeInfo.Type.ToDisplayString()));
            }

            return;

            static bool IsOptimizedValueType(ITypeSymbol type)
            {
                return type.SpecialType is SpecialType.System_Boolean or
                       SpecialType.System_Char or
                       SpecialType.System_IntPtr or
                       SpecialType.System_UIntPtr;
            }
        }
    }
}
