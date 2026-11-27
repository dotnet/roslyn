// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

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
            return RoundMetricValue((double)total / childrenCount);
        }

        private static int RoundMetricValue(double value) => (int)Math.Round(value, 0);

        internal static int NormalizeAndRoundMaintainabilityIndex(double maintIndex)
        {
            maintIndex = Math.Max(0.0, maintIndex);
            return RoundMetricValue(maintIndex / 171.0 * 100.0);
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableHashSet<INamedTypeSymbol> coupledTypes)
        {
            foreach (var coupledType in coupledTypes)
            {
                AddCoupledNamedTypesCore(builder, coupledType, wellKnownTypeProvider);
            }
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, WellKnownTypeProvider wellKnownTypeProvider,
            ITypeSymbol coupledType)
        {
            AddCoupledNamedTypesCore(builder, coupledType, wellKnownTypeProvider);
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, WellKnownTypeProvider wellKnownTypeProvider,
            ImmutableArray<IParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                AddCoupledNamedTypesCore(builder, parameter.Type, wellKnownTypeProvider);
            }
        }

        internal static long GetLinesOfCode(ImmutableArray<SyntaxReference> declarations, ISymbol symbol, CodeMetricsAnalysisContext context)
        {
            long linesOfCode = 0;
            foreach (var decl in declarations)
            {
                SyntaxNode declSyntax = GetTopmostSyntaxNodeForDeclaration(decl, symbol, context);

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
                var fullTrivia = trivialList.ToFullString();
                ReadOnlySpan<char> remainingTrivia = fullTrivia.AsSpan();

                return GetNewlineCount(remainingTrivia, leading);

                static bool TryTakeNextLine(ref ReadOnlySpan<char> remaining, out ReadOnlySpan<char> next, bool leading)
                {
                    if (remaining.IsEmpty)
                    {
                        next = ReadOnlySpan<char>.Empty;
                        return false;
                    }

                    if (leading)
                    {
                        var index = remaining.IndexOfAny('\r', '\n');
                        if (index < 0)
                        {
                            next = remaining;
                            remaining = ReadOnlySpan<char>.Empty;
                            return false;
                        }

                        next = remaining[..index];
                        if (remaining[index] == '\r' && remaining.Length > index + 1 && remaining[index + 1] == '\n')
                        {
                            remaining = remaining[(index + 2)..];
                        }
                        else
                        {
                            remaining = remaining[(index + 1)..];
                        }

                        return true;
                    }
                    else
                    {
                        var index = remaining.LastIndexOfAny('\r', '\n');
                        if (index < 0)
                        {
                            next = remaining;
                            remaining = ReadOnlySpan<char>.Empty;
                            return false;
                        }

                        next = remaining[(index + 1)..];
                        if (remaining[index] == '\n' && index > 0 && remaining[index - 1] == '\r')
                        {
                            remaining = remaining[..(index - 1)];
                        }
                        else
                        {
                            remaining = remaining[..index];
                        }

                        return true;
                    }
                }

                static int GetNewlineCount(ReadOnlySpan<char> trivia, bool leading)
                {
                    var count = 0;
                    while (TryTakeNextLine(ref trivia, out var next, leading))
                    {
                        if (!next.IsWhiteSpace())
                            break;

                        count++;
                    }

                    return count;
                }
            }
        }

        internal static SyntaxNode GetTopmostSyntaxNodeForDeclaration(SyntaxReference declaration, ISymbol declaredSymbol, CodeMetricsAnalysisContext context)
        {
            var declSyntax = declaration.GetSyntax(context.CancellationToken);
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

        internal static (int cyclomaticComplexity, ComputationalComplexityMetrics computationalComplexityMetrics) ComputeCoupledTypesAndComplexityExcludingMemberDecls(
            ImmutableArray<SyntaxReference> declarations,
            ISymbol symbol,
            ImmutableHashSet<INamedTypeSymbol>.Builder builder,
            CodeMetricsAnalysisContext context)
        {
            int cyclomaticComplexity = 0;
            ComputationalComplexityMetrics computationalComplexityMetrics = ComputationalComplexityMetrics.Default;

            var nodesToProcess = new Queue<SyntaxNode>();
            using var _1 = PooledHashSet<SyntaxNode>.GetInstance(out var applicableAttributeNodes);

            foreach (var declaration in declarations)
            {
                SyntaxNode syntax = GetTopmostSyntaxNodeForDeclaration(declaration, symbol, context);
                nodesToProcess.Enqueue(syntax);

                // Ensure we process parameter initializers and attributes.
                var parameters = GetParameters(symbol);
                foreach (var parameter in parameters)
                {
                    var parameterSyntaxRef = parameter.DeclaringSyntaxReferences.FirstOrDefault();
                    if (parameterSyntaxRef != null)
                    {
                        var parameterSyntax = parameterSyntaxRef.GetSyntax(context.CancellationToken);
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
                        var attributeSyntax = attribute.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
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
                    AddCoupledNamedTypesCore(builder, typeInfo.Type, context.WellKnownTypeProvider);

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

                            case OperationKind.Attribute:
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

                            AddCoupledNamedTypesCore(builder, operation.Type, context.WellKnownTypeProvider);

                            // Handle static member accesses specially as there is no operation for static type off which the member is accessed.
                            if (operation is IMemberReferenceOperation memberReference &&
                                memberReference.Member.IsStatic)
                            {
                                AddCoupledNamedTypesCore(builder, memberReference.Member.ContainingType, context.WellKnownTypeProvider);
                            }
                            else if (operation is IInvocationOperation invocation &&
                                (invocation.TargetMethod.IsStatic || invocation.TargetMethod.IsExtensionMethod))
                            {
                                AddCoupledNamedTypesCore(builder, invocation.TargetMethod.ContainingType, context.WellKnownTypeProvider);
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
                        || namedType.GetAttributes().Any((a) =>
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
