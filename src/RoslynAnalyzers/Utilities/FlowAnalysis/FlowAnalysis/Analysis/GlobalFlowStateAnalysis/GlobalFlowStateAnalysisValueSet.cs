// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T> - CacheBasedEquatable handles equality

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    internal sealed class GlobalFlowStateAnalysisValueSet : CacheBasedEquatable<GlobalFlowStateAnalysisValueSet>
    {
        public static readonly GlobalFlowStateAnalysisValueSet Unset = new(
            ImmutableHashSet<IAbstractAnalysisValue>.Empty, ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, 0, GlobalFlowStateAnalysisValueSetKind.Unset);
        public static readonly GlobalFlowStateAnalysisValueSet Empty = new(
            ImmutableHashSet<IAbstractAnalysisValue>.Empty, ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, 0, GlobalFlowStateAnalysisValueSetKind.Empty);
        public static readonly GlobalFlowStateAnalysisValueSet Unknown = new(
            ImmutableHashSet<IAbstractAnalysisValue>.Empty, ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, 0, GlobalFlowStateAnalysisValueSetKind.Unknown);

        private GlobalFlowStateAnalysisValueSet(
            ImmutableHashSet<IAbstractAnalysisValue> analysisValues,
            ImmutableHashSet<GlobalFlowStateAnalysisValueSet> parents,
            int height,
            GlobalFlowStateAnalysisValueSetKind kind)
        {
            Debug.Assert((!analysisValues.IsEmpty || !parents.IsEmpty) == (kind == GlobalFlowStateAnalysisValueSetKind.Known));
            Debug.Assert(analysisValues.All(value => value != null));
            Debug.Assert(parents.All(parent => parent != null));
            Debug.Assert(height >= 0);
            Debug.Assert(height == 0 || kind == GlobalFlowStateAnalysisValueSetKind.Known);
            Debug.Assert(height == 0 == parents.IsEmpty);

            AnalysisValues = analysisValues;
            Parents = parents;
            Height = height;
            Kind = kind;
        }

        public static GlobalFlowStateAnalysisValueSet Create(
            ImmutableHashSet<IAbstractAnalysisValue> analysisValues,
            ImmutableHashSet<GlobalFlowStateAnalysisValueSet> parents,
            int height)
        {
            Debug.Assert(!analysisValues.IsEmpty || !parents.IsEmpty);
            return new(analysisValues, parents, height, GlobalFlowStateAnalysisValueSetKind.Known);
        }

        public static GlobalFlowStateAnalysisValueSet Create(IAbstractAnalysisValue analysisValue)
            => new(ImmutableHashSet.Create(analysisValue), ImmutableHashSet<GlobalFlowStateAnalysisValueSet>.Empty, height: 0, GlobalFlowStateAnalysisValueSetKind.Known);

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
                return GlobalFlowStateAnalysisValueSet.Create(AnalysisValues, ImmutableHashSet.Create(newRoot), newHeight);
            }

            using var _ = PooledHashSet<GlobalFlowStateAnalysisValueSet>.GetInstance(out var parentsBuilder);
            foreach (var parent in Parents)
            {
                parentsBuilder.Add(parent.WithRootParent(newRoot));
            }

            return GlobalFlowStateAnalysisValueSet.Create(AnalysisValues, parentsBuilder.ToImmutable(), newHeight);
        }

        internal GlobalFlowStateAnalysisValueSet WithAdditionalAnalysisValues(GlobalFlowStateAnalysisValueSet newAnalysisValuesSet, bool negate)
        {
            return WithAdditionalAnalysisValuesCore(negate ? newAnalysisValuesSet.GetNegatedValue() : newAnalysisValuesSet);
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
                return GlobalFlowStateAnalysisValueSet.Create(
                    ImmutableHashSetExtensions.AddRange(AnalysisValues, newAnalysisValues.AnalysisValues), Parents, Height);
            }

            return newAnalysisValues.WithRootParent(this);
        }

        internal GlobalFlowStateAnalysisValueSet GetNegatedValue()
        {
            Debug.Assert(Kind == GlobalFlowStateAnalysisValueSetKind.Known);

            if (Height == 0 && AnalysisValues.Count == 1)
            {
                var negatedAnalysisValues = ImmutableHashSet.Create(AnalysisValues.Single().GetNegatedValue());
                return GlobalFlowStateAnalysisValueSet.Create(negatedAnalysisValues, Parents, Height);
            }
            else if (Height > 0 && AnalysisValues.Count == 0)
            {
                return GetNegateValueFromParents(Parents);
            }
            else
            {
                var parentsBuilder = ImmutableHashSet.CreateBuilder<GlobalFlowStateAnalysisValueSet>();
                foreach (var analysisValue in AnalysisValues)
                {
                    parentsBuilder.Add(GlobalFlowStateAnalysisValueSet.Create(analysisValue.GetNegatedValue()));
                }

                int height;
                if (Height > 0)
                {
                    var negatedValueFromParents = GetNegateValueFromParents(Parents);
                    parentsBuilder.Add(negatedValueFromParents);
                    height = negatedValueFromParents.Height + 1;
                }
                else
                {
                    Debug.Assert(AnalysisValues.Count > 1);
                    Debug.Assert(parentsBuilder.Count > 1);
                    height = 1;
                }

                return GlobalFlowStateAnalysisValueSet.Create(ImmutableHashSet<IAbstractAnalysisValue>.Empty, parentsBuilder.ToImmutable(), height);
            }

            static GlobalFlowStateAnalysisValueSet GetNegateValueFromParents(ImmutableHashSet<GlobalFlowStateAnalysisValueSet> parents)
            {
                Debug.Assert(parents.Count > 0);
                var analysisValuesBuilder = ImmutableHashSet.CreateBuilder<IAbstractAnalysisValue>();
                var parentsBuilder = ImmutableHashSet.CreateBuilder<GlobalFlowStateAnalysisValueSet>();

                var height = 0;
                foreach (var parent in parents)
                {
                    if (parent.AnalysisValues.Count == 1 && parent.Height == 0)
                    {
                        analysisValuesBuilder.Add(parent.AnalysisValues.Single().GetNegatedValue());
                    }
                    else
                    {
                        var negatedParent = parent.GetNegatedValue();
                        parentsBuilder.Add(negatedParent);
                        height = Math.Max(height, negatedParent.Height + 1);
                    }
                }

                return GlobalFlowStateAnalysisValueSet.Create(analysisValuesBuilder.ToImmutable(), parentsBuilder.ToImmutable(), height);
            }
        }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(AnalysisValues));
            hashCode.Add(HashUtilities.Combine(Parents));
            hashCode.Add(Height.GetHashCode());
            hashCode.Add(((int)Kind).GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<GlobalFlowStateAnalysisValueSet> obj)
        {
            var other = (GlobalFlowStateAnalysisValueSet)obj;
            return HashUtilities.Combine(AnalysisValues) == HashUtilities.Combine(other.AnalysisValues)
                && HashUtilities.Combine(Parents) == HashUtilities.Combine(other.Parents)
                && Height.GetHashCode() == other.Height.GetHashCode()
                && ((int)Kind).GetHashCode() == ((int)other.Kind).GetHashCode();
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

                using var _ = ArrayBuilder<string>.GetInstance(Parents.Count, out var parentsBuilder);
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
