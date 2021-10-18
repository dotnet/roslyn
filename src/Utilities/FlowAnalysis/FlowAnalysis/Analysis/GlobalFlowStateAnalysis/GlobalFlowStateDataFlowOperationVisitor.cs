// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    /// <summary>
    /// Operation visitor to flow the GlobalFlowState values across a given statement in a basic block.
    /// </summary>
    internal abstract class GlobalFlowStateDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        : AnalysisEntityDataFlowOperationVisitor<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TAbstractAnalysisValue : IEquatable<TAbstractAnalysisValue>
    {
        // This is the global entity storing CFG wide state, which gets updated for every visited operation in the visitor.
        protected AnalysisEntity GlobalEntity { get; }
        protected bool HasPredicatedGlobalState { get; }

        private readonly ImmutableDictionary<IOperation, TAbstractAnalysisValue>.Builder _globalValuesMapBuilder;

        protected GlobalFlowStateDataFlowOperationVisitor(TAnalysisContext analysisContext, bool hasPredicatedGlobalState)
            : base(analysisContext)
        {
            GlobalEntity = GetGlobalEntity(analysisContext);
            HasPredicatedGlobalState = hasPredicatedGlobalState;
            _globalValuesMapBuilder = ImmutableDictionary.CreateBuilder<IOperation, TAbstractAnalysisValue>();
        }

        internal ImmutableDictionary<IOperation, TAbstractAnalysisValue> GetGlobalValuesMap()
            => _globalValuesMapBuilder.ToImmutable();

        private static AnalysisEntity GetGlobalEntity(TAnalysisContext analysisContext)
        {
            ISymbol owningSymbol;
            if (analysisContext.InterproceduralAnalysisData == null)
            {
                owningSymbol = analysisContext.OwningSymbol;
            }
            else
            {
                owningSymbol = analysisContext.InterproceduralAnalysisData.MethodsBeingAnalyzed
                    .Single(m => m.InterproceduralAnalysisData == null)
                    .OwningSymbol;
            }

            return AnalysisEntity.Create(
                owningSymbol,
                ImmutableArray<AbstractIndex>.Empty,
                owningSymbol.GetMemberOrLocalOrParameterType()!,
                instanceLocation: PointsToAbstractValue.Unknown,
                parent: null);
        }

        protected TAbstractAnalysisValue GlobalState
        {
            get => GetAbstractValue(GlobalEntity);
            set => SetAbstractValue(GlobalEntity, value);
        }

        protected sealed override void ResetAbstractValue(AnalysisEntity analysisEntity)
            => SetAbstractValue(analysisEntity, ValueDomain.UnknownOrMayBeValue);

        #region Visitor methods

        public override TAbstractAnalysisValue Visit(IOperation operation, object? argument)
        {
            var value = base.Visit(operation, argument);

            if (operation != null)
            {
                // Store the current global value in a separate global values builder.
                // These values need to be saved into the base operation value builder in the final analysis result.
                // This will be done as a post-step after the analysis is complete.
                _globalValuesMapBuilder[operation] = GlobalState;
            }

            return value;
        }

        #endregion
    }
}