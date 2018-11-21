// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Generates and stores the default <see cref="PointsToAbstractValue"/> for <see cref="AnalysisEntity"/> instances generated for member and element reference operations.
    /// </summary>
    internal sealed class DefaultPointsToValueGenerator
    {
        private readonly ControlFlowGraph _controlFlowGraph;
        private readonly ImmutableDictionary<AnalysisEntity, PointsToAbstractValue>.Builder _defaultPointsToValueMapBuilder;
        private ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> _lazyDefaultPointsToValueMap;

        public DefaultPointsToValueGenerator(ControlFlowGraph controlFlowGraph)
        {
            _controlFlowGraph = controlFlowGraph;
            _defaultPointsToValueMapBuilder = ImmutableDictionary.CreateBuilder<AnalysisEntity, PointsToAbstractValue>();
        }

        public PointsToAbstractValue GetOrCreateDefaultValue(AnalysisEntity analysisEntity)
        {
            // Must be one of the following:
            //  1. A reference type OR
            //  2. A nullable value type OR
            //  3. ThisOrMeInstance of a non-nullable value type OR
            //  4. An lvalue capture of a non-nullable value type
            Debug.Assert(analysisEntity.Type.IsReferenceTypeOrNullableValueType() ||
                         analysisEntity.IsThisOrMeInstance ||
                         (analysisEntity.CaptureIdOpt != null &&
                          LValueFlowCapturesProvider.GetOrCreateLValueFlowCaptures(_controlFlowGraph).Contains(analysisEntity.CaptureIdOpt.Value)));
            Debug.Assert(_lazyDefaultPointsToValueMap == null);

            if (!_defaultPointsToValueMapBuilder.TryGetValue(analysisEntity, out PointsToAbstractValue value))
            {
                if (analysisEntity.SymbolOpt?.Kind == SymbolKind.Local ||
                    analysisEntity.SymbolOpt is IParameterSymbol parameter && parameter.RefKind == RefKind.Out ||
                    analysisEntity.CaptureIdOpt != null)
                {
                    return PointsToAbstractValue.Undefined;
                }
                else if (analysisEntity.IsThisOrMeInstance &&
                    !analysisEntity.Type.IsReferenceTypeOrNullableValueType())
                {
                    return PointsToAbstractValue.NoLocation;
                }

                value = PointsToAbstractValue.Create(AbstractLocation.CreateAnalysisEntityDefaultLocation(analysisEntity), mayBeNull: true);
                _defaultPointsToValueMapBuilder.Add(analysisEntity, value);
            }

            return value;
        }

        public void AddTrackedEntities(ImmutableArray<AnalysisEntity>.Builder builder) => builder.AddRange(_defaultPointsToValueMapBuilder.Keys);
        public bool IsTrackedEntity(AnalysisEntity analysisEntity) => _defaultPointsToValueMapBuilder.ContainsKey(analysisEntity);

        public ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> GetDefaultPointsToValueMap()
        {
            _lazyDefaultPointsToValueMap = _lazyDefaultPointsToValueMap ?? _defaultPointsToValueMapBuilder.ToImmutableDictionary();
            return _lazyDefaultPointsToValueMap;
        }
    }
}
