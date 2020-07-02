// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T> - CacheBasedEquatable handles equality

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    internal sealed class GlobalFlowStateAnalysisValueSet : CacheBasedEquatable<GlobalFlowStateAnalysisValueSet>
    {
        public static readonly GlobalFlowStateAnalysisValueSet Unset = new GlobalFlowStateAnalysisValueSet(
            ImmutableHashSet<IAbstractAnalysisValue>.Empty, ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, 0, GlobalFlowStateAnalysisValueSetKind.Unset);
        public static readonly GlobalFlowStateAnalysisValueSet Empty = new GlobalFlowStateAnalysisValueSet(
            ImmutableHashSet<IAbstractAnalysisValue>.Empty, ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, 0, GlobalFlowStateAnalysisValueSetKind.Empty);
        public static readonly GlobalFlowStateAnalysisValueSet Unknown = new GlobalFlowStateAnalysisValueSet(
            ImmutableHashSet<IAbstractAnalysisValue>.Empty, ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, 0, GlobalFlowStateAnalysisValueSetKind.Unknown);

        public GlobalFlowStateAnalysisValueSet(
            ImmutableHashSet<IAbstractAnalysisValue> analysisValues,
            ImmutableHashSet<GlobalFlowStateAnalysisValueSet> parents,
            int height,
            GlobalFlowStateAnalysisValueSetKind kind)
        {
            Debug.Assert((!analysisValues.IsEmpty || !parents.IsEmpty) == (kind == GlobalFlowStateAnalysisValueSetKind.Known));
            Debug.Assert(analysisValues.All(value => value != default));
            Debug.Assert(parents.All(parent => parent != null));
            Debug.Assert(height >= 0);
            Debug.Assert(height == 0 || kind == GlobalFlowStateAnalysisValueSetKind.Known);
            Debug.Assert(height == 0 == parents.IsEmpty);

            AnalysisValues = analysisValues;
            Parents = parents;
            Height = height;
            Kind = kind;
        }

        public GlobalFlowStateAnalysisValueSet(IAbstractAnalysisValue analysisValue)
            : this(ImmutableHashSet.Create(analysisValue), ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, height: 0, GlobalFlowStateAnalysisValueSetKind.Known)
        {
        }

        public GlobalFlowStateAnalysisValueSet(GlobalFlowStateAnalysisValueSet parent1, GlobalFlowStateAnalysisValueSet parent2)
            : this(ImmutableHashSet<IAbstractAnalysisValue>.Empty,
                   ImmutableHashSet.Create(parent1, parent2),
                   height: Math.Max(parent1.Height, parent2.Height) + 1,
                   GlobalFlowStateAnalysisValueSetKind.Known)
        {
        }

        public ImmutableHashSet<IAbstractAnalysisValue> AnalysisValues { get; }
        public ImmutableHashSet<GlobalFlowStateAnalysisValueSet> Parents { get; }
        public int Height { get; }
        public GlobalFlowStateAnalysisValueSetKind Kind { get; }

        private GlobalFlowStateAnalysisValueSet WithRootParent(GlobalFlowStateAnalysisValueSet newRoot)
        {
            Debug.Assert(Kind == GlobalFlowStateAnalysisValueSetKind.Known);

            var newHeight = Height + newRoot.Height + 1;
            if (Parents.IsEmpty)
            {
                return new GlobalFlowStateAnalysisValueSet(AnalysisValues, ImmutableHashSet.Create(newRoot), newHeight, GlobalFlowStateAnalysisValueSetKind.Known);
            }

            using var parentsBuilder = PooledHashSet<GlobalFlowStateAnalysisValueSet>.GetInstance();
            foreach (var parent in Parents)
            {
                parentsBuilder.Add(parent.WithRootParent(newRoot));
            }

            return new GlobalFlowStateAnalysisValueSet(AnalysisValues, parentsBuilder.ToImmutable(), newHeight, GlobalFlowStateAnalysisValueSetKind.Known);
        }

        private static GlobalFlowStateAnalysisValueSet WithNegatedAnalysisValues(GlobalFlowStateAnalysisValueSet newAnalysisValueSet)
            => new GlobalFlowStateAnalysisValueSet(
                GetNegatedAnalysisValues(newAnalysisValueSet.AnalysisValues),
                newAnalysisValueSet.Parents,
                newAnalysisValueSet.Height,
                newAnalysisValueSet.Kind);

        private static ImmutableHashSet<IAbstractAnalysisValue> GetNegatedAnalysisValues(ImmutableHashSet<IAbstractAnalysisValue> values)
            => values.Select(f => f.GetNegatedValue()).ToImmutableHashSet();

        internal GlobalFlowStateAnalysisValueSet WithAdditionalAnalysisValues(GlobalFlowStateAnalysisValueSet newAnalysisValuesSet, bool negate)
        {
            return WithAdditionalAnalysisValuesCore(negate ? WithNegatedAnalysisValues(newAnalysisValuesSet) : newAnalysisValuesSet);
        }

        private GlobalFlowStateAnalysisValueSet WithAdditionalAnalysisValuesCore(GlobalFlowStateAnalysisValueSet newAnalysisValues)
        {
            Debug.Assert(Kind != GlobalFlowStateAnalysisValueSetKind.Unknown);

            if (Kind != GlobalFlowStateAnalysisValueSetKind.Known)
            {
                return newAnalysisValues;
            }

            if (newAnalysisValues.Height == 0)
            {
                return new GlobalFlowStateAnalysisValueSet(
                    AnalysisValues.AddRange(newAnalysisValues.AnalysisValues), Parents, Height, GlobalFlowStateAnalysisValueSetKind.Known);
            }

            return newAnalysisValues.WithRootParent(this);
        }

        internal GlobalFlowStateAnalysisValueSet GetNegatedValue()
        {
            Debug.Assert(Kind == GlobalFlowStateAnalysisValueSetKind.Known);

            var negatedValues = GetNegatedAnalysisValues(AnalysisValues);
            Debug.Assert(negatedValues.Count == AnalysisValues.Count);
            return new GlobalFlowStateAnalysisValueSet(negatedValues, Parents, Height, GlobalFlowStateAnalysisValueSetKind.Known);
        }

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(AnalysisValues));
            addPart(HashUtilities.Combine(Parents));
            addPart(Height.GetHashCode());
            addPart(Kind.GetHashCode());
        }

        public override string ToString()
        {
            return GetParentString() + GetAnalysisValuesString();

            string GetParentString()
            {
                if (Parents.IsEmpty)
                {
                    return string.Empty;
                }

                using var parentsBuilder = ArrayBuilder<string>.GetInstance(Parents.Count);
                foreach (var parent in Parents)
                {
                    parentsBuilder.Add(parent.ToString());
                }

                var result = string.Join(" || ", parentsBuilder.Order());
                if (parentsBuilder.Count > 1)
                {
                    result = $"({result})";
                }

                return result;
            }

            string GetAnalysisValuesString()
            {
                if (AnalysisValues.IsEmpty)
                {
                    return string.Empty;
                }

                var result = string.Join(" && ", AnalysisValues.Select(f => f.ToString()).Order());
                if (!Parents.IsEmpty)
                {
                    result = $" && {result}";
                }

                return result;
            }
        }
    }
}
