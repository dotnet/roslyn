// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Generates and stores the default <see cref="CopyAbstractValue"/> for <see cref="AnalysisEntity"/> instances generated for member and element reference operations.
    /// </summary>
    internal sealed class AddressSharedEntitiesProvider<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisContext : AbstractDataFlowAnalysisContext<TAnalysisData, TAnalysisContext, TAnalysisResult, TAbstractAnalysisValue>
        where TAnalysisResult : IDataFlowAnalysisResult<TAbstractAnalysisValue>
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
            SetAddressSharedEntities(analysisContext.InterproceduralAnalysisDataOpt?.AddressSharedEntities);
        }

        public void SetAddressSharedEntities(ImmutableDictionary<AnalysisEntity, CopyAbstractValue> addressSharedEntitiesOpt)
        {
            _addressSharedEntitiesBuilder.Clear();
            if (addressSharedEntitiesOpt != null)
            {
                _addressSharedEntitiesBuilder.AddRange(addressSharedEntitiesOpt);
            }
        }

        public void UpdateAddressSharedEntitiesForParameter(IParameterSymbol parameter, AnalysisEntity analysisEntity, ArgumentInfo<TAbstractAnalysisValue> assignedValueOpt)
        {
            if (parameter.RefKind != RefKind.None &&
                assignedValueOpt?.AnalysisEntityOpt != null)
            {
                var copyValue = new CopyAbstractValue(ComputeAddressSharedEntities());
                foreach (var entity in copyValue.AnalysisEntities)
                {
                    _addressSharedEntitiesBuilder[entity] = copyValue;
                }
            }

            ImmutableHashSet<AnalysisEntity> ComputeAddressSharedEntities()
            {
                var addressSharedEntitiesBuilder = PooledHashSet<AnalysisEntity>.GetInstance();
                addressSharedEntitiesBuilder.Add(analysisEntity);
                addressSharedEntitiesBuilder.Add(assignedValueOpt.AnalysisEntityOpt);

                // We need to handle multiple ref/out parameters passed the same location.
                // For example, "M(ref a, ref a);"
                if (_addressSharedEntitiesBuilder.TryGetValue(assignedValueOpt.AnalysisEntityOpt, out var existingValue))
                {
                    foreach (var entity in existingValue.AnalysisEntities)
                    {
                        addressSharedEntitiesBuilder.Add(entity);
                    }
                }

                // Also handle case where the passed in argument is also a ref/out parameter and has address shared entities.
                if (_addressSharedEntitiesBuilder.TryGetValue(analysisEntity, out existingValue))
                {
                    foreach (var entity in existingValue.AnalysisEntities)
                    {
                        addressSharedEntitiesBuilder.Add(entity);
                    }
                }

                return addressSharedEntitiesBuilder.ToImmutableAndFree();
            }
        }

        public CopyAbstractValue GetDefaultCopyValue(AnalysisEntity analysisEntity)
            => TryGetAddressSharedCopyValue(analysisEntity) ?? new CopyAbstractValue(analysisEntity);

        public CopyAbstractValue TryGetAddressSharedCopyValue(AnalysisEntity analysisEntity)
            => _addressSharedEntitiesBuilder.TryGetValue(analysisEntity, out var addressSharedEntities) ?
            addressSharedEntities :
            null;

        public ImmutableDictionary<AnalysisEntity, CopyAbstractValue> GetAddressedSharedEntityMap()
            => _addressSharedEntitiesBuilder.ToImmutable();
    }
}
