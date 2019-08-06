// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            return RoundMetricValue((maintIndex / 171.0) * 100.0);
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, IEnumerable<ITypeSymbol> coupledTypes)
        {
            foreach (var coupledType in coupledTypes)
            {
                AddCoupledNamedTypesCore(builder, coupledType);
            }
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, params ITypeSymbol[] coupledTypes)
        {
            foreach (var coupledType in coupledTypes)
            {
                AddCoupledNamedTypesCore(builder, coupledType);
            }
        }

        internal static void AddCoupledNamedTypes(ImmutableHashSet<INamedTypeSymbol>.Builder builder, ImmutableArray<IParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                AddCoupledNamedTypesCore(builder, parameter.Type);
            }
        }

        internal static async Task<long> GetLinesOfCodeAsync(ImmutableArray<SyntaxReference> declarations, ISymbol symbol, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
        {
            long linesOfCode = 0;
            foreach (var decl in declarations)
            {
                SyntaxNode declSyntax = await GetTopmostSyntaxNodeForDeclarationAsync(decl, symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                // For namespace symbols, don't count lines of code for declarations of child namespaces.
                // For example, "namespace N1.N2 { }" is a declaration reference for N1, but the actual declaration is for N2.
                if (symbol.Kind == SymbolKind.Namespace)
                {
                    var model = semanticModelProvider.GetSemanticModel(declSyntax);
                    if (model.GetDeclaredSymbol(declSyntax, cancellationToken) != (object)symbol)
                    {
                        continue;
                    }
                }

                FileLinePositionSpan linePosition = declSyntax.SyntaxTree.GetLineSpan(declSyntax.FullSpan, cancellationToken);
                long delta = linePosition.EndLinePosition.Line - linePosition.StartLinePosition.Line;
                if (delta == 0)
                {
                    // Declaration on a single line, we count it as a separate line.
                    delta = 1;
                }

                linesOfCode += delta;
            }

            return linesOfCode;
        }

        internal static async Task<SyntaxNode> GetTopmostSyntaxNodeForDeclarationAsync(SyntaxReference declaration, ISymbol declaredSymbol, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
        {
            var declSyntax = await declaration.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            if (declSyntax.Language == LanguageNames.VisualBasic)
            {
                SemanticModel model = semanticModelProvider.GetSemanticModel(declSyntax);
                while (declSyntax.Parent != null && Equals(model.GetDeclaredSymbol(declSyntax.Parent, cancellationToken), declaredSymbol))
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
            SemanticModelProvider semanticModelProvider,
            CancellationToken cancellationToken)
        {
            int cyclomaticComplexity = 0;
            ComputationalComplexityMetrics computationalComplexityMetrics = ComputationalComplexityMetrics.Default;

            var nodesToProcess = new Queue<SyntaxNode>();

            foreach (var declaration in declarations)
            {
                SyntaxNode syntax = await GetTopmostSyntaxNodeForDeclarationAsync(declaration, symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);
                nodesToProcess.Enqueue(syntax);

                // Ensure we process parameter initializers and attributes.
                var parameters = GetParameters(symbol);
                foreach (var parameter in parameters)
                {
                    var parameterSyntaxRef = parameter.DeclaringSyntaxReferences.FirstOrDefault();
                    if (parameterSyntaxRef != null)
                    {
                        var parameterSyntax = await parameterSyntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
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
                    if (attribute.ApplicationSyntaxReference != null)
                    {
                        var attributeSyntax = await attribute.ApplicationSyntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                        nodesToProcess.Enqueue(attributeSyntax);
                    }
                }

                do
                {
                    var node = nodesToProcess.Dequeue();
                    var model = semanticModelProvider.GetSemanticModel(node);

                    if (!ReferenceEquals(node, syntax))
                    {
                        var declaredSymbol = model.GetDeclaredSymbol(node, cancellationToken);
                        if (declaredSymbol != null && !Equals(symbol, declaredSymbol) && declaredSymbol.Kind != SymbolKind.Parameter)
                        {
                            // Skip member declarations.
                            continue;
                        }
                    }

                    var typeInfo = model.GetTypeInfo(node, cancellationToken);
                    AddCoupledNamedTypesCore(builder, typeInfo.Type);

                    var operationBlock = model.GetOperation(node, cancellationToken);
                    if (operationBlock != null && operationBlock.Parent == null)
                    {
                        switch (operationBlock.Kind)
                        {
                            case OperationKind.Block:
                            case OperationKind.MethodBodyOperation:
                            case OperationKind.ConstructorBodyOperation:
                                cyclomaticComplexity += 1;
                                break;
                        }

                        computationalComplexityMetrics = computationalComplexityMetrics.Union(ComputationalComplexityMetrics.Compute(operationBlock));

                        // Add used types within executable code in the operation tree.
                        foreach (var operation in operationBlock.DescendantsAndSelf())
                        {
#if LEGACY_CODE_METRICS_MODE
                            // Legacy mode does not account for code within lambdas/local functions for code metrics.
                            if (operation.IsWithinLambdaOrLocalFunction())
                            {
                                continue;
                            }
#endif

                            if (!operation.IsImplicit && hasConditionalLogic(operation))
                            {
                                cyclomaticComplexity += 1;
                            }

                            AddCoupledNamedTypesCore(builder, operation.Type);

                            // Handle static member accesses specially as there is no operation for static type off which the member is accessed.
                            if (operation is IMemberReferenceOperation memberReference &&
                                memberReference.Member.IsStatic)
                            {
                                AddCoupledNamedTypesCore(builder, memberReference.Member.ContainingType);
                            }
                            else if (operation is IInvocationOperation invocation &&
                                (invocation.TargetMethod.IsStatic || invocation.TargetMethod.IsExtensionMethod))
                            {
                                AddCoupledNamedTypesCore(builder, invocation.TargetMethod.ContainingType);
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

        private static void AddCoupledNamedTypesCore(ImmutableHashSet<INamedTypeSymbol>.Builder builder, ITypeSymbol typeOpt)
        {
            if (typeOpt is INamedTypeSymbol usedType &&
                !isIgnoreableType(usedType) &&
                builder.Add(usedType))
            {
                if (usedType.IsGenericType)
                {
                    foreach (var type in usedType.TypeArguments)
                    {
                        AddCoupledNamedTypesCore(builder, type);
                    }
                }
            }

            // Compat
            static bool isIgnoreableType(INamedTypeSymbol namedType)
            {
                switch (namedType.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Char:
                    case SpecialType.System_Double:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Single:
                    case SpecialType.System_String:
                    case SpecialType.System_Object:
                    case SpecialType.System_ValueType:
                    case SpecialType.System_Void:
                        return true;

                    default:
                        return false;
                }
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
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((IMethodSymbol)member).Parameters;
                case SymbolKind.Property:
                    return ((IPropertySymbol)member).Parameters;
                default:
                    return ImmutableArray<IParameterSymbol>.Empty;
            }
        }
    }
}

#endif
