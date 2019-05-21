// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class NullableWalker
    {
        /// <summary>
        /// Represents the result of visiting an expression.
        /// Contains a result type which tells us whether the expression may be null,
        /// and an l-value type which tells us whether we can assign null to the expression.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        private readonly struct VisitResult
        {
            public readonly TypeWithState RValueType;
            public readonly TypeWithAnnotations LValueType;

            public VisitResult(TypeWithState rValueType, TypeWithAnnotations lValueType)
            {
                RValueType = rValueType;
                LValueType = lValueType;
                // https://github.com/dotnet/roslyn/issues/34993: Doesn't hold true for Tuple_Assignment_10. See if we can make it hold true
                //Debug.Assert((RValueType.Type is null && LValueType.TypeSymbol is null) ||
                //             RValueType.Type.Equals(LValueType.TypeSymbol, TypeCompareKind.ConsiderEverything | TypeCompareKind.AllIgnoreOptions));
            }

            public VisitResult(TypeSymbol type, NullableAnnotation annotation, NullableFlowState state)
            {
                RValueType = TypeWithState.Create(type, state);
                LValueType = TypeWithAnnotations.Create(type, annotation);
                Debug.Assert(RValueType.Type.Equals(LValueType.Type, TypeCompareKind.ConsiderEverything));
            }

            internal string GetDebuggerDisplay() => $"{{LValue: {LValueType.GetDebuggerDisplay()}, RValue: {RValueType.GetDebuggerDisplay()}}}";
        }

        /// <summary>
        /// Represents the result of visiting an argument expression.
        /// In addition to storing the <see cref="VisitResult"/>, also stores the <see cref="LocalState"/>
        /// for reanalyzing a lambda.
        /// </summary>
        [DebuggerDisplay("{VisitResult.GetDebuggerDisplay(), nq}")]
        private readonly struct VisitArgumentResult
        {
            public readonly VisitResult VisitResult;
            public readonly Optional<LocalState> StateForLambda;
            public TypeWithState RValueType => VisitResult.RValueType;
            public TypeWithAnnotations LValueType => VisitResult.LValueType;

            public VisitArgumentResult(VisitResult visitResult, Optional<LocalState> stateForLambda)
            {
                VisitResult = visitResult;
                StateForLambda = stateForLambda;
            }
        }

        /// <summary>
        /// Contains a checkpoint for the NullableWalker at any given point of execution, used for restoring the walker to
        /// a specific point for speculatively analyzing a piece of code that does not appear in the original tree.
        /// </summary>
        internal readonly struct Checkpoint
        {
            private readonly VariableState _variableState;
            private readonly bool _useMethodSignatureParameterTypes;
            private readonly Symbol _symbol;
            private readonly ImmutableArray<(BoundReturnStatement, TypeWithAnnotations)> _returnTypesOpt;
            private readonly ImmutableDictionary<BoundExpression, TypeWithState> _methodGroupReceiverMapOpt;
            private readonly VisitResult _visitResult;
            private readonly VisitResult _currentConditionalReceiverVisitResult;
            private readonly ImmutableDictionary<object, PlaceholderLocal> _placeholderLocalsOpt;
            private readonly int _lastConditionalAccessSlot;

            private Checkpoint(
                VariableState variableState,
                bool useMethodSignatureParameterTypes,
                Symbol symbol,
                ImmutableArray<(BoundReturnStatement, TypeWithAnnotations)> returnTypesOpt,
                ImmutableDictionary<BoundExpression, TypeWithState> methodGroupReceiverMapOpt,
                VisitResult visitResult,
                VisitResult currentConditionalReceiverVisitResult,
                ImmutableDictionary<object, PlaceholderLocal> placeholderLocalsOpt,
                int lastConditionalAccessSlot)
            {
                _variableState = variableState;
                _useMethodSignatureParameterTypes = useMethodSignatureParameterTypes;
                _symbol = symbol;
                _returnTypesOpt = returnTypesOpt;
                _methodGroupReceiverMapOpt = methodGroupReceiverMapOpt;
                _visitResult = visitResult;
                _currentConditionalReceiverVisitResult = currentConditionalReceiverVisitResult;
                _placeholderLocalsOpt = placeholderLocalsOpt;
                _lastConditionalAccessSlot = lastConditionalAccessSlot;
            }

            internal static Checkpoint CheckpointWalker(NullableWalker walker) =>
                new Checkpoint(
                    walker.GetVariableState(walker.State),
                    walker._useMethodSignatureParameterTypes,
                    walker._symbol,
                    walker._returnTypesOpt?.ToImmutable() ?? default,
                    walker._methodGroupReceiverMapOpt?.ToImmutableDictionary(),
                    walker._visitResult,
                    walker._currentConditionalReceiverVisitResult,
                    walker._placeholderLocalsOpt?.ToImmutableDictionary(),
                    walker._lastConditionalAccessSlot);

            internal NullableWalker RestoreFromCheckpoint(CSharpCompilation compilation,
                                                     BoundNode nodeToAnalyze,
                                                     Binder binder,
                                                     Dictionary<BoundExpression, (NullabilityInfo, TypeSymbol)> analyzedNullabilityMap)
            {
                ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)> returnTypesOpt = null;
                if (!_returnTypesOpt.IsDefault)
                {
                    returnTypesOpt = ArrayBuilder<(BoundReturnStatement, TypeWithAnnotations)>.GetInstance(_returnTypesOpt.Length);
                    returnTypesOpt.AddRange(_returnTypesOpt);
                }

                return new NullableWalker(
                    compilation,
                    this._symbol,
                    this._useMethodSignatureParameterTypes,
                    this._symbol as MethodSymbol,
                    nodeToAnalyze,
                    binder,
                    binder.Conversions,
                    returnTypesOpt,
                    this._variableState,
                    analyzedNullabilityMap,
                    checkpointMapOpt: null)
                {
                    _methodGroupReceiverMapOpt = createFromImmutable(this._methodGroupReceiverMapOpt),
                    _visitResult = this._visitResult,
                    _currentConditionalReceiverVisitResult = this._currentConditionalReceiverVisitResult,
                    _placeholderLocalsOpt = createFromImmutable(this._placeholderLocalsOpt),
                    _lastConditionalAccessSlot = this._lastConditionalAccessSlot
                };

                static PooledDictionary<K, V> createFromImmutable<K, V>(ImmutableDictionary<K, V> dictOpt)
                {
                    if (dictOpt is null)
                    {
                        return null;
                    }

                    var pooledDict = PooledDictionary<K, V>.GetInstance();
                    foreach (var (key, value) in dictOpt)
                    {
                        pooledDict[key] = value;
                    }

                    return pooledDict;
                }
            }
        }
    }
}
