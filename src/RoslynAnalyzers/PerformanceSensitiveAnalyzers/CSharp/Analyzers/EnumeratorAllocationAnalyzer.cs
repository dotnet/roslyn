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
    internal sealed class EnumeratorAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ReferenceTypeEnumeratorRuleId = "HAA0401";

        internal static readonly DiagnosticDescriptor ReferenceTypeEnumeratorRule = new(
            ReferenceTypeEnumeratorRuleId,
            CreateLocalizableResourceString(nameof(ReferenceTypeEnumeratorRuleTitle)),
            CreateLocalizableResourceString(nameof(ReferenceTypeEnumeratorRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ReferenceTypeEnumeratorRule);

        protected override ImmutableArray<SyntaxKind> Expressions { get; } = ImmutableArray.Create(SyntaxKind.ForEachStatement, SyntaxKind.InvocationExpression);

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
                if ((enumerator == null || enumerator.IsEmpty) && typeInfo.ConvertedType != null)
                {
                    // 1st we try and fallback to using the ConvertedType
                    enumerator = typeInfo.ConvertedType.GetMembers("GetEnumerator");
                }

                if ((enumerator == null || enumerator.IsEmpty) && typeInfo.Type.Interfaces != null)
                {
                    // 2nd fallback, now we try and find the IEnumerable Interface explicitly
                    var iEnumerable = typeInfo.Type.Interfaces.WhereAsArray(i => i.Name == "IEnumerable");
                    if (iEnumerable != null && !iEnumerable.IsEmpty)
                    {
                        enumerator = iEnumerable[0].GetMembers("GetEnumerator");
                    }
                }

                if (enumerator != null && !enumerator.IsEmpty)
                {
                    // probably should do something better here, hack.
                    if (enumerator[0] is IMethodSymbol methodSymbol)
                    {
                        if (methodSymbol.ReturnType.IsReferenceType && methodSymbol.ReturnType.SpecialType != SpecialType.System_Collections_IEnumerator)
                        {
                            reportDiagnostic(foreachExpression.InKeyword.CreateDiagnostic(ReferenceTypeEnumeratorRule, EmptyMessageArgs));
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
                            if (@interface.SpecialType is SpecialType.System_Collections_Generic_IEnumerator_T or SpecialType.System_Collections_IEnumerator)
                            {
                                reportDiagnostic(invocationExpression.CreateDiagnostic(ReferenceTypeEnumeratorRule, EmptyMessageArgs));
                            }
                        }
                    }
                }
            }
        }
    }
}
