// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T> - CacheBasedEquatable handles equality

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Abstract copy value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="CopyAnalysis"/>.
    /// </summary>
    internal class CopyAbstractValue : CacheBasedEquatable<CopyAbstractValue>
    {
        public static CopyAbstractValue NotApplicable = new CopyAbstractValue(CopyAbstractValueKind.NotApplicable);
        public static CopyAbstractValue Invalid = new CopyAbstractValue(CopyAbstractValueKind.Invalid);
        public static CopyAbstractValue Unknown = new CopyAbstractValue(CopyAbstractValueKind.Unknown);

        public CopyAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities, CopyAbstractValueKind kind)
        {
            Debug.Assert(analysisEntities.IsEmpty != kind.IsKnown());
            Debug.Assert(kind != CopyAbstractValueKind.KnownReferenceCopy || analysisEntities.All(a => !a.Type.IsValueType));

            if (kind == CopyAbstractValueKind.KnownValueCopy &&
                analysisEntities.Count == 1 &&
                !analysisEntities.First().Type.IsValueType)
            {
                kind = CopyAbstractValueKind.KnownReferenceCopy;
            }

            AnalysisEntities = analysisEntities;
            Kind = kind;
        }

        private CopyAbstractValue(CopyAbstractValueKind kind)
            : this(ImmutableHashSet<AnalysisEntity>.Empty, kind)
        {
            Debug.Assert(!kind.IsKnown());
        }

        public CopyAbstractValue(AnalysisEntity analysisEntity)
            : this(ImmutableHashSet.Create(analysisEntity),
                   kind: analysisEntity.Type.IsReferenceType ? CopyAbstractValueKind.KnownReferenceCopy : CopyAbstractValueKind.KnownValueCopy)
        {
        }

        public CopyAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities, bool isReferenceCopy)
            : this(analysisEntities,
                   kind: isReferenceCopy ? CopyAbstractValueKind.KnownReferenceCopy : CopyAbstractValueKind.KnownValueCopy)
        {
            Debug.Assert(!analysisEntities.IsEmpty);
        }

        public CopyAbstractValue WithEntityRemoved(AnalysisEntity entityToRemove)
        {
            Debug.Assert(AnalysisEntities.Contains(entityToRemove));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind.IsKnown());

            return new CopyAbstractValue(AnalysisEntities.Remove(entityToRemove), Kind);
        }

        public CopyAbstractValue WithEntitiesRemoved(IEnumerable<AnalysisEntity> entitiesToRemove)
        {
            Debug.Assert(entitiesToRemove.All(entityToRemove => AnalysisEntities.Contains(entityToRemove)));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind.IsKnown());

            return new CopyAbstractValue(AnalysisEntities.Except(entitiesToRemove), Kind);
        }

        public ImmutableHashSet<AnalysisEntity> AnalysisEntities { get; }
        public CopyAbstractValueKind Kind { get; }

        protected override void ComputeHashCodeParts(ArrayBuilder<int> builder)
        {
            builder.Add(HashUtilities.Combine(AnalysisEntities));
            builder.Add(Kind.GetHashCode());
        }
    }
}
