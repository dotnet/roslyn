// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#nullable disable warnings

#if HAS_IOPERATION

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Operations;

#if LEGACY_CODE_METRICS_MODE
using Analyzer.Utilities.Extensions;
#endif

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    internal static class MetricsHelper
    {
        internal static int GetAverageRoundedMetricValue(int total, int childrenCount)
        {
            Debug.Assert(childrenCount != 0);
            return RoundMetricValue(total / childrenCount);
        }

        private static int RoundMetricValue(double value) => (int)Math.Round(value, 0);

        internal static int NormalizeAndRoundMaintainabilityIndex(double maintIndex)
        {
            maintIndex = Math.Max(0.0, maintIndex);
            return RoundMetricValue(maintIndex / 171.0 * 100.0);
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, WellKnownTypeProvider wellKnownTypeProvider,
            IEnumerable<ITypeSymbol> coupledTypes)
        {
            foreach (var coupledType in coupledTypes)
            {
                AddCoupledNamedTypesCore(builder, coupledType, wellKnownTypeProvider);
            }
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, WellKnownTypeProvider wellKnownTypeProvider,
            params ITypeSymbol[] coupledTypes)
        {
            foreach (var coupledType in coupledTypes)
            {
                AddCoupledNamedTypesCore(builder, coupledType, wellKnownTypeProvider);
            }
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableArray<IParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                AddCoupledNamedTypesCore(builder, parameter.Type, wellKnownTypeProvider);
            }
        }

        internal static async Task<long> GetLinesOfCodeAsync(ImmutableArray<SyntaxReference> declarations, ISymbol symbol, CodeMetricsAnalysisContext context)
        {
            long linesOfCode = 0;
            foreach (var decl in declarations)
            {
                SyntaxNode declSyntax = await GetTopmostSyntaxNodeForDeclarationAsync(decl, symbol, context).ConfigureAwait(false);

                // For namespace symbols, don't count lines of code for declarations of child namespaces.
                // For example, "namespace N1.N2 { }" is a declaration reference for N1, but the actual declaration is for N2.
                if (symbol.Kind == SymbolKind.Namespace)
                {
                    var model = context.GetSemanticModel(declSyntax);
                    if (!Equals(model.GetDeclaredSymbol(declSyntax, context.CancellationToken), symbol))
                    {
                        continue;
                    }
                }

                FileLinePositionSpan linePosition = declSyntax.SyntaxTree.GetLineSpan(declSyntax.FullSpan, context.CancellationToken);
                long delta = linePosition.EndLinePosition.Line - linePosition.StartLinePosition.Line;
                if (delta == 0)
                {
                    // Declaration on a single line, we count it as a separate line.
                    delta = 1;
                }
                else
                {
                    // Ensure that we do not count the leading and trailing empty new lines.
                    var additionalNewLines = Math.Max(0, GetNewlineCount(declSyntax.GetLeadingTrivia(), leading: true) + GetNewlineCount(declSyntax.GetTrailingTrivia(), leading: false) - 1);
                    delta -= additionalNewLines;
                }

                linesOfCode += delta;
            }

            return linesOfCode;

            static int GetNewlineCount(SyntaxTriviaList trivialList, bool leading)
            {
                var triviaParts = trivialList.ToFullString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToImmutableArray();
                return GetNewlineCount(triviaParts, leading);

                static int GetNewlineCount(ImmutableArray<string> triviaParts, bool leading)
                {
                    var index = leading ? 0 : triviaParts.Length - 1;
                    var loopCondition = leading ? LoopConditionForLeading : (Func<int, int, bool>)LoopConditionForTrailing;
                    var incrementOrDecrement = leading ? 1 : -1;
                    var count = 0;
                    while (loopCondition(index, triviaParts.Length) && string.IsNullOrWhiteSpace(triviaParts[index]))
                    {
                        index += incrementOrDecrement;
                        count++;
                    }

                    return count;

                    static bool LoopConditionForLeading(int index, int length) => index < length - 1;
                    static bool LoopConditionForTrailing(int index, int _) => index > 0;
                }
            }
        }

        internal static async Task<SyntaxNode> GetTopmostSyntaxNodeForDeclarationAsync(SyntaxReference declaration, ISymbol declaredSymbol, CodeMetricsAnalysisContext context)
        {
            var declSyntax = await declaration.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false);
            if (declSyntax.Language == LanguageNames.VisualBasic)
            {
                SemanticModel model = context.GetSemanticModel(declSyntax);
                while (declSyntax.Parent != null && Equals(model.GetDeclaredSymbol(declSyntax.Parent, context.CancellationToken), declaredSymbol))
                {
                    declSyntax = declSyntax.Parent;
                }
            }

            return declSyntax;
        }

        internal static async Task<(int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics)> ComputeCoupledTypesAndComplexityExcludingMemberDeclsAsync(
            ImmutableArray<SyntaxReference> declarations,
            ISymbol symbol,
            ImmutableHashSet<INamedTypeSymbol>.Builder builder,
            CodeMetricsAnalysisContext context)
        {
            int cyclomaticComplexity = 0;
            ComputationalComplexityMetrics computationalComplexityMetrics = ComputationalComplexityMetrics.Default;

            var nodesToProcess = new Queue<SyntaxNode>();
            using var applicableAttributeNodes = PooledHashSet<SyntaxNode>.GetInstance();

            var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(context.Compilation);

            foreach (var declaration in declarations)
            {
                SyntaxNode syntax = await GetTopmostSyntaxNodeForDeclarationAsync(declaration, symbol, context).ConfigureAwait(false);
                nodesToProcess.Enqueue(syntax);

                // Ensure we process parameter initializers and attributes.
                var parameters = GetParameters(symbol);
                foreach (var parameter in parameters)
                {
                    var parameterSyntaxRef = parameter.DeclaringSyntaxReferences.FirstOrDefault();
                    if (parameterSyntaxRef != null)
                    {
                        var parameterSyntax = await parameterSyntaxRef.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false);
                        nodesToProcess.Enqueue(parameterSyntax);
                    }
                }

                var attributes = symbol.GetAttributes();
                if (symbol is IMethodSymbol methodSymbol)
                {
                    attributes = attributes.AddRange(methodSymbol.GetReturnTypeAttributes());
                }

                foreach (var attribute in attributes)
                {
                    if (attribute.ApplicationSyntaxReference != null &&
                        attribute.ApplicationSyntaxReference.SyntaxTree == declaration.SyntaxTree)
                    {
                        var attributeSyntax = await attribute.ApplicationSyntaxReference.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false);
                        if (applicableAttributeNodes.Add(attributeSyntax))
                        {
                            nodesToProcess.Enqueue(attributeSyntax);
                        }
                    }
                }

                do
                {
                    var node = nodesToProcess.Dequeue();
                    var model = context.GetSemanticModel(node);

                    if (!ReferenceEquals(node, syntax))
                    {
                        var declaredSymbol = model.GetDeclaredSymbol(node, context.CancellationToken);
                        if (declaredSymbol != null && !Equals(symbol, declaredSymbol) && declaredSymbol.Kind != SymbolKind.Parameter)
                        {
                            // Skip member declarations.
                            continue;
                        }
                    }

                    var typeInfo = model.GetTypeInfo(node, context.CancellationToken);
                    AddCoupledNamedTypesCore(builder, typeInfo.Type, wellKnownTypeProvider);

                    var operationBlock = model.GetOperation(node, context.CancellationToken);
                    if (operationBlock != null && operationBlock.Parent == null)
                    {
                        switch (operationBlock.Kind)
                        {
                            case OperationKind.Block:
                            case OperationKind.MethodBodyOperation:
                            case OperationKind.ConstructorBodyOperation:
                                cyclomaticComplexity += 1;
                                break;

                            case OperationKind.None:
                                // Skip non-applicable attributes.
                                if (!applicableAttributeNodes.Contains(node))
                                {
                                    continue;
                                }

                                break;
                        }

                        computationalComplexityMetrics = computationalComplexityMetrics.Union(ComputationalComplexityMetrics.Compute(operationBlock));

                        // Add used types within executable code in the operation tree.
                        foreach (var operation in operationBlock.DescendantsAndSelf())
                        {
#if LEGACY_CODE_METRICS_MODE
                            // Legacy mode does not account for code within lambdas/local functions for code metrics.
                            if (operation.IsWithinLambdaOrLocalFunction(out _))
                            {
                                continue;
                            }
#endif

                            if (!operation.IsImplicit && hasConditionalLogic(operation))
                            {
                                cyclomaticComplexity += 1;
                            }

                            AddCoupledNamedTypesCore(builder, operation.Type, wellKnownTypeProvider);

                            // Handle static member accesses specially as there is no operation for static type off which the member is accessed.
                            if (operation is IMemberReferenceOperation memberReference &&
                                memberReference.Member.IsStatic)
                            {
                                AddCoupledNamedTypesCore(builder, memberReference.Member.ContainingType, wellKnownTypeProvider);
                            }
                            else if (operation is IInvocationOperation invocation &&
                                (invocation.TargetMethod.IsStatic || invocation.TargetMethod.IsExtensionMethod))
                            {
                                AddCoupledNamedTypesCore(builder, invocation.TargetMethod.ContainingType, wellKnownTypeProvider);
                            }
                        }
                    }
                    else
                    {
                        // Enqueue child nodes for further processing.
                        foreach (var child in node.ChildNodes())
                        {
                            nodesToProcess.Enqueue(child);
                        }
                    }
                } while (nodesToProcess.Count != 0);
            }

            return (cyclomaticComplexity, computationalComplexityMetrics);
            static bool hasConditionalLogic(IOperation operation)
            {
                switch (operation.Kind)
                {
                    case OperationKind.CaseClause:
                    case OperationKind.Coalesce:
                    case OperationKind.Conditional:
                    case OperationKind.ConditionalAccess:
                    case OperationKind.Loop:
                        return true;

                    case OperationKind.BinaryOperator:
                        var binaryOperation = (IBinaryOperation)operation;
                        return binaryOperation.OperatorKind == BinaryOperatorKind.ConditionalAnd ||
                            binaryOperation.OperatorKind == BinaryOperatorKind.ConditionalOr ||
                            (binaryOperation.Type.SpecialType == SpecialType.System_Boolean &&
                             (binaryOperation.OperatorKind == BinaryOperatorKind.Or || binaryOperation.OperatorKind == BinaryOperatorKind.And));

                    default:
                        return false;
                }
            }
        }

        private static void AddCoupledNamedTypesCore(ImmutableHashSet<INamedTypeSymbol>.Builder builder, ITypeSymbol typeOpt,
            WellKnownTypeProvider wellKnownTypeProvider)
        {
            if (typeOpt is INamedTypeSymbol usedType &&
                !isIgnoreableType(usedType, wellKnownTypeProvider))
            {
                // Save the OriginalDefinition of the type as IEnumerable<int> and IEnumerable<float>
                // should register only one IEnumerable...
                builder.Add(usedType.OriginalDefinition);

                // ... but always parse the generic type arguments as IEnumerable<int> and IEnumerable<float>
                // should register int and float.
                if (usedType.IsGenericType)
                {
                    foreach (var type in usedType.TypeArguments)
                    {
                        AddCoupledNamedTypesCore(builder, type, wellKnownTypeProvider);
                    }
                }
            }

            static bool isIgnoreableType(INamedTypeSymbol namedType, WellKnownTypeProvider wellKnownTypeProvider)
            {
                return namedType.SpecialType switch
                {
                    SpecialType.System_Boolean
                    or SpecialType.System_Byte
                    or SpecialType.System_Char
                    or SpecialType.System_Double
                    or SpecialType.System_Int16
                    or SpecialType.System_Int32
                    or SpecialType.System_Int64
                    or SpecialType.System_UInt16
                    or SpecialType.System_UInt32
                    or SpecialType.System_UInt64
                    or SpecialType.System_IntPtr
                    or SpecialType.System_UIntPtr
                    or SpecialType.System_SByte
                    or SpecialType.System_Single
                    or SpecialType.System_String
                    or SpecialType.System_Object
                    or SpecialType.System_ValueType
                    or SpecialType.System_Void => true,
                    _ => namedType.IsAnonymousType
                        || namedType.GetAttributes().Any(a =>
                            a.AttributeClass.Equals(wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) ||
                            a.AttributeClass.Equals(wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCodeDomCompilerGeneratedCodeAttribute))),
                };
            }
        }

        internal static void RemoveContainingTypes(ISymbol symbol, ImmutableHashSet<INamedTypeSymbol>.Builder coupledTypesBuilder)
        {
            var namedType = symbol as INamedTypeSymbol ?? symbol.ContainingType;
            while (namedType != null)
            {
                coupledTypesBuilder.Remove(namedType);
                namedType = namedType.ContainingType;
            }
        }

        internal static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol member)
        {
            return member.Kind switch
            {
                SymbolKind.Method => ((IMethodSymbol)member).Parameters,
                SymbolKind.Property => ((IPropertySymbol)member).Parameters,
                _ => ImmutableArray<IParameterSymbol>.Empty,
            };
        }
    }
}

#endif
