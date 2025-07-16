// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Generates and stores the default <see cref="CopyAbstractValue"/> for <see cref="AnalysisEntity"/> instances generated for member and element reference operations.
    /// </summary>
    internal sealed class AddressSharedEntitiesProvider<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : class, IDataFlowAnalysisResult<TAbstractAnalysisValue>
        where TAnalysisData : AbstractAnalysisData
    {
        /// <summary>
        /// Map builder from entity to set of entities that share the same instance location.
        /// Primarily used for ref arguments for context sensitive interprocedural analysis
        /// to ensure that PointsTo value updates to any of the mapped entities is reflected in the others in the set.
        /// </summary>
        private readonly ImmutableDictionary<AnalysisEntity, CopyAbstractValue>.Builder _addressSharedEntitiesBuilder;

        public AddressSharedEntitiesProvider(TAnalysisContext analysisContext)
        {
            _addressSharedEntitiesBuilder = ImmutableDictionary.CreateBuilder<AnalysisEntity, CopyAbstractValue>();
            SetAddressSharedEntities(analysisContext.InterproceduralAnalysisData?.AddressSharedEntities);
        }

        public void SetAddressSharedEntities(ImmutableDictionary<AnalysisEntity, CopyAbstractValue>? addressSharedEntities)
        {
            _addressSharedEntitiesBuilder.Clear();
            if (addressSharedEntities != null)
            {
                _addressSharedEntitiesBuilder.AddRange(addressSharedEntities);
            }
        }

        public void UpdateAddressSharedEntitiesForParameter(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<TAbstractAnalysisValue>? assignedValue)
        {
            if (parameter.RefKind != RefKind.None &&
                assignedValue?.AnalysisEntity != null)
            {
                var addressSharedEntities = ComputeAddressSharedEntities();
                var isReferenceCopy = !addressSharedEntities.Any(a => a.Type.IsValueType);
                var copyValue = new CopyAbstractValue(addressSharedEntities, isReferenceCopy);
                foreach (var entity in copyValue.AnalysisEntities)
                {
                    _addressSharedEntitiesBuilder[entity] = copyValue;
                }
            }

            ImmutableHashSet<AnalysisEntity> ComputeAddressSharedEntities()
            {
                RoslynDebug.Assert(assignedValue?.AnalysisEntity != null);

                var builder = PooledHashSet<AnalysisEntity>.GetInstance();
                AddIfHasKnownInstanceLocation(analysisEntity, builder);
                AddIfHasKnownInstanceLocation(assignedValue.AnalysisEntity, builder);

                // We need to handle multiple ref/out parameters passed the same location.
                // For example, "M(ref a, ref a);"
                if (_addressSharedEntitiesBuilder.TryGetValue(assignedValue.AnalysisEntity, out var existingValue))
                {
                    foreach (var entity in existingValue.AnalysisEntities)
                    {
                        AddIfHasKnownInstanceLocation(entity, builder);
                    }
                }

                // Also handle case where the passed in argument is also a ref/out parameter and has address shared entities.
                if (_addressSharedEntitiesBuilder.TryGetValue(analysisEntity, out existingValue))
                {
                    foreach (var entity in existingValue.AnalysisEntities)
                    {
                        AddIfHasKnownInstanceLocation(entity, builder);
                    }
                }

                Debug.Assert(builder.All(e => !e.HasUnknownInstanceLocation));
                return builder.ToImmutableAndFree();
            }

            static void AddIfHasKnownInstanceLocation(AnalysisEntity entity, PooledHashSet<AnalysisEntity> builder)
            {
                // Only add entity to address shared entities if they have known instance location.
                if (!entity.HasUnknownInstanceLocation)
                {
                    builder.Add(entity);
                }
            }
        }

        public CopyAbstractValue GetDefaultCopyValue(AnalysisEntity analysisEntity)
            => TryGetAddressSharedCopyValue(analysisEntity) ?? new CopyAbstractValue(analysisEntity);

        public CopyAbstractValue? TryGetAddressSharedCopyValue(AnalysisEntity analysisEntity)
            => _addressSharedEntitiesBuilder.TryGetValue(analysisEntity, out var addressSharedEntities) ?
            addressSharedEntities :
            null;

        public ImmutableDictionary<AnalysisEntity, CopyAbstractValue> GetAddressedSharedEntityMap()
            => _addressSharedEntitiesBuilder.ToImmutable();
    }
}
