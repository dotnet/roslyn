// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Debug.Assert(entitiesToRemove.All(AnalysisEntities.Contains));
            Debug.Assert(AnalysisEntities.Count > 1);
            Debug.Assert(Kind.IsKnown());

            return new CopyAbstractValue(AnalysisEntities.Except(entitiesToRemove), Kind);
        }

        public ImmutableHashSet<AnalysisEntity> AnalysisEntities { get; }
        public CopyAbstractValueKind Kind { get; }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(AnalysisEntities));
            hashCode.Add(((int)Kind).GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<CopyAbstractValue> obj)
        {
            var other = (CopyAbstractValue)obj;
            return HashUtilities.Combine(AnalysisEntities) == HashUtilities.Combine(other.AnalysisEntities)
                && ((int)Kind).GetHashCode() == ((int)other.Kind).GetHashCode();
        }
    }
}
