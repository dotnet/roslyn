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
    internal sealed class EnumeratorAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ReferenceTypeEnumeratorRuleId = "HAA0401";

        private static readonly LocalizableString s_localizableReferenceTypeEnumeratorRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ReferenceTypeEnumeratorRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableReferenceTypeEnumeratorRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ReferenceTypeEnumeratorRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        internal static DiagnosticDescriptor ReferenceTypeEnumeratorRule = new DiagnosticDescriptor(
            ReferenceTypeEnumeratorRuleId,
            s_localizableReferenceTypeEnumeratorRuleTitle,
            s_localizableReferenceTypeEnumeratorRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ReferenceTypeEnumeratorRule);

        protected override ImmutableArray<SyntaxKind> Expressions => ImmutableArray.Create(SyntaxKind.ForEachStatement, SyntaxKind.InvocationExpression);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;
            if (node is ForEachStatementSyntax foreachExpression)
            {
                var typeInfo = semanticModel.GetTypeInfo(foreachExpression.Expression, cancellationToken);
                if (typeInfo.Type == null)
                    return;

                if (typeInfo.Type.Name == "String" && typeInfo.Type.ContainingNamespace.Name == "System")
                {
                    // Special case for System.String which is optmizined by
                    // the compiler and does not result in an allocation.
                    return;
                }

                // Regular way of getting the enumerator
                ImmutableArray<ISymbol> enumerator = typeInfo.Type.GetMembers("GetEnumerator");
                if ((enumerator == null || enumerator.Length == 0) && typeInfo.ConvertedType != null)
                {
                    // 1st we try and fallback to using the ConvertedType
                    enumerator = typeInfo.ConvertedType.GetMembers("GetEnumerator");
                }
                if ((enumerator == null || enumerator.Length == 0) && typeInfo.Type.Interfaces != null)
                {
                    // 2nd fallback, now we try and find the IEnumerable Interface explicitly
                    var iEnumerable = typeInfo.Type.Interfaces.Where(i => i.Name == "IEnumerable").ToImmutableArray();
                    if (iEnumerable != null && iEnumerable.Length > 0)
                    {
                        enumerator = iEnumerable[0].GetMembers("GetEnumerator");
                    }
                }

                if (enumerator != null && enumerator.Length > 0)
                {
                    // probably should do something better here, hack.
                    if (enumerator[0] is IMethodSymbol methodSymbol)
                    {
                        if (methodSymbol.ReturnType.IsReferenceType && methodSymbol.ReturnType.SpecialType != SpecialType.System_Collections_IEnumerator)
                        {
                            reportDiagnostic(Diagnostic.Create(ReferenceTypeEnumeratorRule, foreachExpression.InKeyword.GetLocation(), EmptyMessageArgs));
                        }
                    }
                }

                return;
            }

            if (node is InvocationExpressionSyntax invocationExpression)
            {
                var methodInfo = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol as IMethodSymbol;
                if (methodInfo?.ReturnType != null && methodInfo.ReturnType.IsReferenceType)
                {
                    if (methodInfo.ReturnType.AllInterfaces != null)
                    {
                        foreach (var @interface in methodInfo.ReturnType.AllInterfaces)
                        {
                            if (@interface.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T || @interface.SpecialType == SpecialType.System_Collections_IEnumerator)
                            {
                                reportDiagnostic(Diagnostic.Create(ReferenceTypeEnumeratorRule, invocationExpression.GetLocation(), EmptyMessageArgs));
                            }
                        }
                    }
                }
            }
        }
    }
}
