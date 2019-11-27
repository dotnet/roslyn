// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

#if LEGACY_CODE_METRICS_MODE
using Analyzer.Utilities.Extensions;
#endif

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    /// <summary>
    /// Calculates computational complexity metrics based on the number 
    /// of operators and operands found in the code.
    /// </summary>
    /// <remarks>This metric is based off of the Halstead metric.</remarks>
    internal sealed class ComputationalComplexityMetrics
    {
        internal static readonly ComputationalComplexityMetrics Default = new ComputationalComplexityMetrics(0, 0, 0, 0, 0, ImmutableHashSet<OperationKind>.Empty,
            ImmutableHashSet<BinaryOperatorKind>.Empty, ImmutableHashSet<UnaryOperatorKind>.Empty, ImmutableHashSet<CaseKind>.Empty, ImmutableHashSet<ISymbol>.Empty, ImmutableHashSet<object>.Empty);
        private static readonly object s_nullConstantPlaceholder = new object();

        private readonly long _operatorUsageCounts;
        private readonly long _symbolUsageCounts;
        private readonly long _constantUsageCounts;
        private readonly ImmutableHashSet<OperationKind> _distinctOperatorKinds;
        private readonly ImmutableHashSet<BinaryOperatorKind> _distinctBinaryOperatorKinds;
        private readonly ImmutableHashSet<UnaryOperatorKind> _distinctUnaryOperatorKinds;
        private readonly ImmutableHashSet<CaseKind> _distinctCaseKinds;
        private readonly ImmutableHashSet<ISymbol> _distinctReferencedSymbols;
        private readonly ImmutableHashSet<object> _distinctReferencedConstants;

        private ComputationalComplexityMetrics(
            long executableLinesOfCode,
            long effectiveLinesOfMaintainableCode,
            long operatorUsageCounts,
            long symbolUsageCounts,
            long constantUsageCounts,
            ImmutableHashSet<OperationKind> distinctOperatorKinds,
            ImmutableHashSet<BinaryOperatorKind> distinctBinaryOperatorKinds,
            ImmutableHashSet<UnaryOperatorKind> distinctUnaryOperatorKinds,
            ImmutableHashSet<CaseKind> distinctCaseKinds,
            ImmutableHashSet<ISymbol> distinctReferencedSymbols,
            ImmutableHashSet<object> distinctReferencedConstants)
        {
            ExecutableLines = executableLinesOfCode;
            EffectiveLinesOfCode = effectiveLinesOfMaintainableCode;
            _operatorUsageCounts = operatorUsageCounts;
            _symbolUsageCounts = symbolUsageCounts;
            _constantUsageCounts = constantUsageCounts;
            _distinctOperatorKinds = distinctOperatorKinds;
            _distinctBinaryOperatorKinds = distinctBinaryOperatorKinds;
            _distinctUnaryOperatorKinds = distinctUnaryOperatorKinds;
            _distinctCaseKinds = distinctCaseKinds;
            _distinctReferencedSymbols = distinctReferencedSymbols;
            _distinctReferencedConstants = distinctReferencedConstants;
        }

        private static ComputationalComplexityMetrics Create(
            long executableLinesOfCode,
            long operatorUsageCounts,
            long symbolUsageCounts,
            long constantUsageCounts,
            bool hasSymbolInitializer,
            ImmutableHashSet<OperationKind> distinctOperatorKinds,
            ImmutableHashSet<BinaryOperatorKind> distinctBinaryOperatorKinds,
            ImmutableHashSet<UnaryOperatorKind> distinctUnaryOperatorKinds,
            ImmutableHashSet<CaseKind> distinctCaseKinds,
            ImmutableHashSet<ISymbol> distinctReferencedSymbols,
            ImmutableHashSet<object> distinctReferencedConstants)
        {
            if (executableLinesOfCode == 0 && operatorUsageCounts == 0 && symbolUsageCounts == 0 && constantUsageCounts == 0 && !hasSymbolInitializer)
            {
                return Default;
            }

            // Use incremented count for maintainable code lines for symbol initializers. 
            var effectiveLinesOfMaintainableCode = hasSymbolInitializer ? executableLinesOfCode + 1 : executableLinesOfCode;

            return new ComputationalComplexityMetrics(executableLinesOfCode, effectiveLinesOfMaintainableCode, operatorUsageCounts, symbolUsageCounts, constantUsageCounts,
                distinctOperatorKinds, distinctBinaryOperatorKinds, distinctUnaryOperatorKinds, distinctCaseKinds, distinctReferencedSymbols, distinctReferencedConstants);
        }

        public static ComputationalComplexityMetrics Compute(IOperation operationBlock)
        {
            bool hasSymbolInitializer = false;
            long executableLinesOfCode = 0;
            long operatorUsageCounts = 0;
            long symbolUsageCounts = 0;
            long constantUsageCounts = 0;
            ImmutableHashSet<OperationKind>.Builder? distinctOperatorKindsBuilder = null;
            ImmutableHashSet<BinaryOperatorKind>.Builder? distinctBinaryOperatorKindsBuilder = null;
            ImmutableHashSet<UnaryOperatorKind>.Builder? distinctUnaryOperatorKindsBuilder = null;
            ImmutableHashSet<CaseKind>.Builder? distinctCaseKindsBuilder = null;
            ImmutableHashSet<ISymbol>.Builder? distinctReferencedSymbolsBuilder = null;
            ImmutableHashSet<object>.Builder? distinctReferencedConstantsBuilder = null;

            // Explicit user applied attribute.
            if (operationBlock.Kind == OperationKind.None &&
                hasAnyExplicitExpression(operationBlock))
            {
                executableLinesOfCode += 1;
            }

            foreach (var operation in operationBlock.Descendants())
            {
                executableLinesOfCode += getExecutableLinesOfCode(operation, ref hasSymbolInitializer);

                if (operation.IsImplicit)
                {
                    continue;
                }

#if LEGACY_CODE_METRICS_MODE
                // Legacy mode does not account for code within lambdas/local functions for code metrics.
                if (operation.IsWithinLambdaOrLocalFunction())
                {
                    continue;
                }
#endif

                if (operation.ConstantValue.HasValue)
                {
                    constantUsageCounts++;
                    distinctReferencedConstantsBuilder ??= ImmutableHashSet.CreateBuilder<object>();
                    distinctReferencedConstantsBuilder.Add(operation.ConstantValue.Value ?? s_nullConstantPlaceholder);
                    continue;
                }

                switch (operation.Kind)
                {
                    // Symbol references.
                    case OperationKind.LocalReference:
                        countOperand(((ILocalReferenceOperation)operation).Local);
                        continue;
                    case OperationKind.ParameterReference:
                        countOperand(((IParameterReferenceOperation)operation).Parameter);
                        continue;
                    case OperationKind.FieldReference:
                    case OperationKind.MethodReference:
                    case OperationKind.PropertyReference:
                    case OperationKind.EventReference:
                        countOperator(operation);
                        countOperand(((IMemberReferenceOperation)operation).Member);
                        continue;

                    // Symbol initializers.
                    case OperationKind.FieldInitializer:
                        foreach (var field in ((IFieldInitializerOperation)operation).InitializedFields)
                        {
                            countOperator(operation);
                            countOperand(field);
                        }
                        continue;
                    case OperationKind.PropertyInitializer:
                        foreach (var property in ((IPropertyInitializerOperation)operation).InitializedProperties)
                        {
                            countOperator(operation);
                            countOperand(property);
                        }
                        continue;
                    case OperationKind.ParameterInitializer:
                        countOperator(operation);
                        countOperand(((IParameterInitializerOperation)operation).Parameter);
                        continue;
                    case OperationKind.VariableInitializer:
                        countOperator(operation);
                        // We count the operand in the variable declarator.
                        continue;
                    case OperationKind.VariableDeclarator:
                        var variableDeclarator = (IVariableDeclaratorOperation)operation;
                        if (variableDeclarator.GetVariableInitializer() != null)
                        {
                            countOperand(variableDeclarator.Symbol);
                        }
                        continue;

                    // Invocations and Object creations.
                    case OperationKind.Invocation:
                        countOperator(operation);
                        var invocation = (IInvocationOperation)operation;
                        if (!invocation.TargetMethod.ReturnsVoid)
                        {
                            countOperand(invocation.TargetMethod);
                        }
                        continue;
                    case OperationKind.ObjectCreation:
                        countOperator(operation);
                        countOperand(((IObjectCreationOperation)operation).Constructor);
                        continue;
                    case OperationKind.DelegateCreation:
                    case OperationKind.AnonymousObjectCreation:
                    case OperationKind.TypeParameterObjectCreation:
                    case OperationKind.DynamicObjectCreation:
                    case OperationKind.DynamicInvocation:
                        countOperator(operation);
                        continue;

                    // Operators with special operator kinds.
                    case OperationKind.BinaryOperator:
                        countBinaryOperator(operation, ((IBinaryOperation)operation).OperatorKind);
                        continue;
                    case OperationKind.CompoundAssignment:
                        countBinaryOperator(operation, ((ICompoundAssignmentOperation)operation).OperatorKind);
                        continue;
                    case OperationKind.TupleBinaryOperator:
                        countBinaryOperator(operation, ((ITupleBinaryOperation)operation).OperatorKind);
                        continue;
                    case OperationKind.UnaryOperator:
                        countUnaryOperator(operation, ((IUnaryOperation)operation).OperatorKind);
                        continue;
                    case OperationKind.CaseClause:
                        var caseClauseOperation = (ICaseClauseOperation)operation;
                        distinctCaseKindsBuilder ??= ImmutableHashSet.CreateBuilder<CaseKind>();
                        distinctCaseKindsBuilder.Add(caseClauseOperation.CaseKind);
                        if (caseClauseOperation.CaseKind == CaseKind.Relational)
                        {
                            countBinaryOperator(operation, ((IRelationalCaseClauseOperation)operation).Relation);
                        }
                        else
                        {
                            countOperator(operation);
                        }
                        continue;

                    // Other common operators.
                    case OperationKind.Increment:
                    case OperationKind.Decrement:
                    case OperationKind.SimpleAssignment:
                    case OperationKind.DeconstructionAssignment:
                    case OperationKind.EventAssignment:
                    case OperationKind.Coalesce:
                    case OperationKind.ConditionalAccess:
                    case OperationKind.Conversion:
                    case OperationKind.ArrayElementReference:
                    case OperationKind.Await:
                    case OperationKind.NameOf:
                    case OperationKind.SizeOf:
                    case OperationKind.TypeOf:
                    case OperationKind.AddressOf:
                    case OperationKind.MemberInitializer:
                    case OperationKind.IsType:
                    case OperationKind.IsPattern:
                    case OperationKind.Parenthesized:
                        countOperator(operation);
                        continue;

                    // Following are considered operators for now, but we may want to revisit.
                    case OperationKind.ArrayCreation:
                    case OperationKind.ArrayInitializer:
                    case OperationKind.DynamicMemberReference:
                    case OperationKind.DynamicIndexerAccess:
                    case OperationKind.Tuple:
                    case OperationKind.Lock:
                    case OperationKind.Using:
                    case OperationKind.Throw:
                    case OperationKind.RaiseEvent:
                    case OperationKind.InterpolatedString:
                        countOperator(operation);
                        continue;

                    // Return value.
                    case OperationKind.Return:
                    case OperationKind.YieldBreak:
                    case OperationKind.YieldReturn:
                        if (((IReturnOperation)operation).ReturnedValue != null)
                        {
                            countOperator(operation);
                        }
                        continue;
                }
            }

            return Create(
                executableLinesOfCode,
                operatorUsageCounts,
                symbolUsageCounts,
                constantUsageCounts,
                hasSymbolInitializer,
                distinctOperatorKindsBuilder != null ? distinctOperatorKindsBuilder.ToImmutable() : ImmutableHashSet<OperationKind>.Empty,
                distinctBinaryOperatorKindsBuilder != null ? distinctBinaryOperatorKindsBuilder.ToImmutable() : ImmutableHashSet<BinaryOperatorKind>.Empty,
                distinctUnaryOperatorKindsBuilder != null ? distinctUnaryOperatorKindsBuilder.ToImmutable() : ImmutableHashSet<UnaryOperatorKind>.Empty,
                distinctCaseKindsBuilder != null ? distinctCaseKindsBuilder.ToImmutable() : ImmutableHashSet<CaseKind>.Empty,
                distinctReferencedSymbolsBuilder != null ? distinctReferencedSymbolsBuilder.ToImmutable() : ImmutableHashSet<ISymbol>.Empty,
                distinctReferencedConstantsBuilder != null ? distinctReferencedConstantsBuilder.ToImmutable() : ImmutableHashSet<object>.Empty);

            static int getExecutableLinesOfCode(IOperation operation, ref bool hasSymbolInitializer)
            {
                if (operation.Parent != null)
                {
                    switch (operation.Parent.Kind)
                    {
                        case OperationKind.Block:
                            return hasAnyExplicitExpression(operation) ? 1 : 0;

                        case OperationKind.FieldInitializer:
                        case OperationKind.PropertyInitializer:
                        case OperationKind.ParameterInitializer:
                            if (hasAnyExplicitExpression(operation))
                            {
                                hasSymbolInitializer = true;
                                return 1;
                            }

                            break;

                        case OperationKind.Conditional:
                            // Nested conditional
                            return operation.Kind == OperationKind.Conditional && hasAnyExplicitExpression(operation) ? 1 : 0;
                    }
                }

                return 0;
            }

            static bool hasAnyExplicitExpression(IOperation operation)
            {
                // Check if all descendants are either implicit or are explicit non-branch operations with no constant value or type, indicating it is not user written code.
                return !operation.DescendantsAndSelf().All(o => o.IsImplicit || (!o.ConstantValue.HasValue && o.Type == null && o.Kind != OperationKind.Branch));
            }

            void countOperator(IOperation operation)
            {
                operatorUsageCounts++;
                distinctOperatorKindsBuilder ??= ImmutableHashSet.CreateBuilder<OperationKind>();
                distinctOperatorKindsBuilder.Add(operation.Kind);
            }

            void countOperand(ISymbol symbol)
            {
                symbolUsageCounts++;
                distinctReferencedSymbolsBuilder ??= ImmutableHashSet.CreateBuilder<ISymbol>();
                distinctReferencedSymbolsBuilder.Add(symbol);
            }

            void countBinaryOperator(IOperation operation, BinaryOperatorKind operatorKind)
            {
                countOperator(operation);
                distinctBinaryOperatorKindsBuilder ??= ImmutableHashSet.CreateBuilder<BinaryOperatorKind>();
                distinctBinaryOperatorKindsBuilder.Add(operatorKind);
            }

            void countUnaryOperator(IOperation operation, UnaryOperatorKind operatorKind)
            {
                countOperator(operation);
                distinctUnaryOperatorKindsBuilder ??= ImmutableHashSet.CreateBuilder<UnaryOperatorKind>();
                distinctUnaryOperatorKindsBuilder.Add(operatorKind);
            }
        }

        public ComputationalComplexityMetrics Union(ComputationalComplexityMetrics other)
        {
            if (ReferenceEquals(this, Default))
            {
                return other;
            }
            else if (ReferenceEquals(other, Default))
            {
                return this;
            }

            return new ComputationalComplexityMetrics(
                executableLinesOfCode: ExecutableLines + other.ExecutableLines,
                effectiveLinesOfMaintainableCode: EffectiveLinesOfCode + other.EffectiveLinesOfCode,
                operatorUsageCounts: _operatorUsageCounts + other._operatorUsageCounts,
                symbolUsageCounts: _symbolUsageCounts + other._symbolUsageCounts,
                constantUsageCounts: _constantUsageCounts + other._constantUsageCounts,
                distinctOperatorKinds: _distinctOperatorKinds.Union(other._distinctOperatorKinds),
                distinctBinaryOperatorKinds: _distinctBinaryOperatorKinds.Union(other._distinctBinaryOperatorKinds),
                distinctUnaryOperatorKinds: _distinctUnaryOperatorKinds.Union(other._distinctUnaryOperatorKinds),
                distinctCaseKinds: _distinctCaseKinds.Union(other._distinctCaseKinds),
                distinctReferencedSymbols: _distinctReferencedSymbols.Union(other._distinctReferencedSymbols),
                distinctReferencedConstants: _distinctReferencedConstants.Union(other._distinctReferencedConstants));
        }

        public bool IsDefault => ReferenceEquals(this, Default);

        /// <summary>The number of unique operators found.</summary>
        public long DistinctOperators        //n1
        {
            get
            {
                var count = _distinctBinaryOperatorKinds.Count;
                if (_distinctBinaryOperatorKinds.Count > 1)
                {
                    count += _distinctBinaryOperatorKinds.Count - 1;
                }
                if (_distinctUnaryOperatorKinds.Count > 1)
                {
                    count += _distinctUnaryOperatorKinds.Count - 1;
                }
                if (_distinctCaseKinds.Count > 1)
                {
                    count += _distinctCaseKinds.Count - 1;
                }

                return count;
            }
        }

        /// <summary>The number of unique operands found.</summary>
        public long DistinctOperands         //n2
        {
            get
            {
                return _distinctReferencedSymbols.Count + _distinctReferencedConstants.Count;
            }
        }

        /// <summary>The total number of operator usages found.</summary>
        public long TotalOperators           //N1
        {
            get { return _operatorUsageCounts; }
        }

        /// <summary>The total number of operand usages found.</summary>
        public long TotalOperands            //N2
        {
            get
            {
                return _symbolUsageCounts + _constantUsageCounts;
            }
        }

        public long Vocabulary               //n
        {
            // n = n1 + n2
            get { return DistinctOperators + DistinctOperands; }
        }

        public long Length                   //N
        {
            // N = N1 + N2
            get { return TotalOperators + TotalOperands; }
        }

        public double Volume                //V
        {
            // V = N * Log2(n)
            get { return (Length * Math.Max(0.0, Math.Log(Vocabulary, 2))); }
        }

        /// <summary>
        /// Count of executable lines of code, i.e. basically IOperations parented by IBlockOperation.
        /// </summary>
        public long ExecutableLines { get; }

        /// <summary>
        /// Count of effective lines of code for computation of maintainability index.
        /// </summary>
        public long EffectiveLinesOfCode { get; }
    }
}

#endif
