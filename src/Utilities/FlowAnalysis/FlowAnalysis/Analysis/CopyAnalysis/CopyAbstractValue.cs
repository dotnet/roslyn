// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Abstract copy value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="CopyAnalysis"/>.
    /// </summary>
    public class CopyAbstractValue : CacheBasedEquatable<CopyAbstractValue>
    {
        public static CopyAbstractValue NotApplicable { get; } = new CopyAbstractValue(CopyAbstractValueKind.NotApplicable);
        public static CopyAbstractValue Invalid { get; } = new CopyAbstractValue(CopyAbstractValueKind.Invalid);
        public static CopyAbstractValue Unknown { get; } = new CopyAbstractValue(CopyAbstractValueKind.Unknown);

        internal CopyAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities, CopyAbstractValueKind kind)
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

        internal CopyAbstractValue(AnalysisEntity analysisEntity)
            : this(ImmutableHashSet.Create(analysisEntity),
                   kind: analysisEntity.Type.IsReferenceType ? CopyAbstractValueKind.KnownReferenceCopy : CopyAbstractValueKind.KnownValueCopy)
        {
        }

        internal CopyAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities, bool isReferenceCopy)
            : this(analysisEntities,
                   kind: isReferenceCopy ? CopyAbstractValueKind.KnownReferenceCopy : CopyAbstractValueKind.KnownValueCopy)
        {
            Debug.Assert(!analysisEntities.IsEmpty);
        }

        internal CopyAbstractValue WithEntityRemoved(AnalysisEntity entityToRemove)
        {
            Debug.Assert(AnalysisEntities.Contains(entityToRemove));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind.IsKnown());

            return new CopyAbstractValue(AnalysisEntities.Remove(entityToRemove), Kind);
        }

        internal CopyAbstractValue WithEntitiesRemoved(IEnumerable<AnalysisEntity> entitiesToRemove)
        {
            Debug.Assert(entitiesToRemove.All(entityToRemove => AnalysisEntities.Contains(entityToRemove)));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind.IsKnown());

            return new CopyAbstractValue(AnalysisEntities.Except(entitiesToRemove), Kind);
        }

        public ImmutableHashSet<AnalysisEntity> AnalysisEntities { get; }
        public CopyAbstractValueKind Kind { get; }

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(AnalysisEntities));
            addPart(Kind.GetHashCode());
        }
    }
}
