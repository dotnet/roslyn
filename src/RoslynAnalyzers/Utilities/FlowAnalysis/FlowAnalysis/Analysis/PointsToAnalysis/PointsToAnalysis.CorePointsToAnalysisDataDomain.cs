// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CorePointsToAnalysisData = DictionaryAnalysisData<AnalysisEntity, PointsToAbstractValue>;

    public partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// An abstract analysis domain implementation for <see cref="CorePointsToAnalysisData"/> tracked by <see cref="PointsToAnalysis"/>.
        /// </summary>
        private sealed class CorePointsToAnalysisDataDomain : AnalysisEntityMapAbstractDomain<PointsToAbstractValue>
        {
            private readonly Func<ITypeSymbol?, bool> _isDisposable;

            public CorePointsToAnalysisDataDomain(
                DefaultPointsToValueGenerator defaultPointsToValueGenerator,
                AbstractValueDomain<PointsToAbstractValue> valueDomain,
                Func<ITypeSymbol?, bool> isDisposable)
                : base(valueDomain, defaultPointsToValueGenerator.IsTrackedEntity, defaultPointsToValueGenerator.IsTrackedPointsToValue)
            {
                DefaultPointsToValueGenerator = defaultPointsToValueGenerator;
                this._isDisposable = isDisposable;
            }

            public DefaultPointsToValueGenerator DefaultPointsToValueGenerator { get; }

            protected override PointsToAbstractValue GetDefaultValue(AnalysisEntity analysisEntity) => DefaultPointsToValueGenerator.GetOrCreateDefaultValue(analysisEntity);

            protected override bool CanSkipNewEntry(AnalysisEntity analysisEntity, PointsToAbstractValue value)
                => value.Kind == PointsToAbstractValueKind.Unknown ||
                   value == GetDefaultValue(analysisEntity);

            protected override void OnNewMergedValue(PointsToAbstractValue value)
                => DefaultPointsToValueGenerator.AddTrackedPointsToValue(value);

            protected override void AssertValidEntryForMergedMap(AnalysisEntity analysisEntity, PointsToAbstractValue value)
            {
                PointsToAnalysisData.AssertValidPointsToAnalysisKeyValuePair(analysisEntity, value, _isDisposable);
            }

            protected override void AssertValidAnalysisData(CorePointsToAnalysisData map)
            {
                PointsToAnalysisData.AssertValidPointsToAnalysisData(map, _isDisposable);
            }

            public CorePointsToAnalysisData MergeCoreAnalysisDataForBackEdge(
                PointsToAnalysisData forwardEdgeAnalysisData,
                PointsToAnalysisData backEdgeAnalysisData,
                Func<PointsToAbstractValue, ImmutableHashSet<AnalysisEntity>> getChildAnalysisEntities,
                Action<AnalysisEntity, PointsToAnalysisData> resetAbstractValue)
            {
                // Stop tracking points to values present in both branches if their is an assignment to a may-be null value from the back edge.
                // Clone the input forwardEdgeAnalysisData to ensure we don't overwrite the input dictionary.
                forwardEdgeAnalysisData = (PointsToAnalysisData)forwardEdgeAnalysisData.Clone();
                List<AnalysisEntity> keysInMap1 = forwardEdgeAnalysisData.CoreAnalysisData.Keys.ToList();
                foreach (var key in keysInMap1)
                {
                    var forwardEdgeValue = forwardEdgeAnalysisData[key];
                    if (backEdgeAnalysisData.TryGetValue(key, out var backEdgeValue) &&
                        backEdgeValue != forwardEdgeValue)
                    {
                        switch (backEdgeValue.NullState)
                        {
                            case NullAbstractValue.MaybeNull:
                                stopTrackingAnalysisDataForKeyAndChildren();
                                break;

                            case NullAbstractValue.NotNull:
                                if (backEdgeValue.MakeMayBeNull() != forwardEdgeValue)
                                {
                                    if (forwardEdgeValue.NullState == NullAbstractValue.NotNull)
                                    {
                                        stopTrackingAnalysisDataForChildren();
                                    }
                                    else
                                    {
                                        stopTrackingAnalysisDataForKeyAndChildren();
                                    }
                                }

                                break;

                        }

                        void stopTrackingAnalysisDataForKeyAndChildren()
                        {
                            stopTrackingAnalysisDataForChildren();
                            stopTrackingAnalysisDataForEntity(key);
                        }

                        void stopTrackingAnalysisDataForChildren()
                        {
                            var childEntities = ImmutableHashSetExtensions.AddRange(
                                getChildAnalysisEntities(forwardEdgeValue),
                                getChildAnalysisEntities(backEdgeValue));
                            foreach (var childEntity in childEntities)
                            {
                                stopTrackingAnalysisDataForEntity(childEntity);
                            }
                        }

                        void stopTrackingAnalysisDataForEntity(AnalysisEntity entity)
                        {
                            if (entity.IsChildOrInstanceMember)
                            {
                                resetAbstractValue(entity, forwardEdgeAnalysisData);
                            }
                        }
                    }
                }

                var resultMap = Merge(forwardEdgeAnalysisData.CoreAnalysisData, backEdgeAnalysisData.CoreAnalysisData);
                Debug.Assert(Compare(forwardEdgeAnalysisData.CoreAnalysisData, resultMap) <= 0);
                Debug.Assert(Compare(backEdgeAnalysisData.CoreAnalysisData, resultMap) <= 0);
                return resultMap;
            }
        }
    }
}
