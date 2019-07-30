// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConcatenationAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string StringConcatenationAllocationRuleId = "HAA0201";
        public const string ValueTypeToReferenceTypeInAStringConcatenationRuleId = "HAA0202";

        private static readonly LocalizableString s_localizableStringConcatenationAllocationRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.StringConcatenationAllocationRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableStringConcatenationAllocationRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.StringConcatenationAllocationRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableValueTypeToReferenceTypeInAStringConcatenationRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ValueTypeToReferenceTypeInAStringConcatenationRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableValueTypeToReferenceTypeInAStringConcatenationRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ValueTypeToReferenceTypeInAStringConcatenationRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        internal static DiagnosticDescriptor StringConcatenationAllocationRule = new DiagnosticDescriptor(
            StringConcatenationAllocationRuleId,
            s_localizableStringConcatenationAllocationRuleTitle,
            s_localizableStringConcatenationAllocationRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/2839d5h5(v=vs.110).aspx");

        internal static DiagnosticDescriptor ValueTypeToReferenceTypeInAStringConcatenationRule = new DiagnosticDescriptor(
            ValueTypeToReferenceTypeInAStringConcatenationRuleId,
            s_localizableValueTypeToReferenceTypeInAStringConcatenationRuleTitle,
            s_localizableValueTypeToReferenceTypeInAStringConcatenationRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "http://msdn.microsoft.com/en-us/library/yz2be5wk.aspx");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(StringConcatenationAllocationRule, ValueTypeToReferenceTypeInAStringConcatenationRule);

        protected override ImmutableArray<SyntaxKind> Expressions => ImmutableArray.Create(SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression);

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
                CheckTypeConversion(left, leftConversion, reportDiagnostic, binaryExpression.Left.GetLocation());

                var right = semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken);
                var rightConversion = semanticModel.GetConversion(binaryExpression.Right, cancellationToken);
                CheckTypeConversion(right, rightConversion, reportDiagnostic, binaryExpression.Right.GetLocation());

                // regular string allocation
                if (left.Type?.SpecialType == SpecialType.System_String || right.Type?.SpecialType == SpecialType.System_String)
                {
                    stringConcatenationCount++;
                }
            }

            if (stringConcatenationCount > 3)
            {
                reportDiagnostic(Diagnostic.Create(StringConcatenationAllocationRule, node.GetLocation(), EmptyMessageArgs));
            }
        }

        private static void CheckTypeConversion(TypeInfo typeInfo, Conversion conversionInfo, Action<Diagnostic> reportDiagnostic, Location location)
        {
            if (conversionInfo.IsBoxing && !IsOptimizedValueType(typeInfo.Type))
            {
                reportDiagnostic(Diagnostic.Create(ValueTypeToReferenceTypeInAStringConcatenationRule, location, new[] { typeInfo.Type.ToDisplayString() }));
            }

            return;

            static bool IsOptimizedValueType(ITypeSymbol type)
            {
                return type.SpecialType == SpecialType.System_Boolean ||
                       type.SpecialType == SpecialType.System_Char ||
                       type.SpecialType == SpecialType.System_IntPtr ||
                       type.SpecialType == SpecialType.System_UIntPtr;
            }
        }
    }
}