// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Abstract PointsTo value for an <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="PointsToAnalysis"/>.
    /// It contains the set of possible <see cref="AbstractLocation"/>s that the entity or the operation can point to and the <see cref="Kind"/> of the location(s).
    /// </summary>
    public class PointsToAbstractValue : CacheBasedEquatable<PointsToAbstractValue>
    {
        // An upper bound on number of underlying locations contained in the tracked PointsTo value.
        // This is required to prevent infinite analysis from interprocedural calls within a loop
        // The chosen constant value is just an approximate heuristic, which can be fine tuned in future.
        private const int LocationThreshold = 20;

        public static PointsToAbstractValue Undefined { get; } = new PointsToAbstractValue(PointsToAbstractValueKind.Undefined, NullAbstractValue.Undefined);
        public static PointsToAbstractValue Invalid { get; } = new PointsToAbstractValue(PointsToAbstractValueKind.Invalid, NullAbstractValue.Invalid);
        public static PointsToAbstractValue Unknown { get; } = new PointsToAbstractValue(PointsToAbstractValueKind.Unknown, NullAbstractValue.MaybeNull);
        public static PointsToAbstractValue UnknownNull { get; } = new PointsToAbstractValue(PointsToAbstractValueKind.UnknownNull, NullAbstractValue.Null);
        public static PointsToAbstractValue UnknownNotNull { get; } = new PointsToAbstractValue(PointsToAbstractValueKind.UnknownNotNull, NullAbstractValue.NotNull);
        public static PointsToAbstractValue NoLocation { get; } = new PointsToAbstractValue(ImmutableHashSet.Create(AbstractLocation.NoLocation), NullAbstractValue.NotNull);
        public static PointsToAbstractValue NullLocation { get; } = new PointsToAbstractValue(ImmutableHashSet.Create(AbstractLocation.Null), NullAbstractValue.Null);

        private PointsToAbstractValue(ImmutableHashSet<AbstractLocation> locations, NullAbstractValue nullState)
        {
            Debug.Assert(!locations.IsEmpty);
            Debug.Assert(locations.All(location => !location.IsNull) || nullState != NullAbstractValue.NotNull);
            Debug.Assert(nullState != NullAbstractValue.Undefined);
            Debug.Assert(nullState != NullAbstractValue.Invalid);
            Debug.Assert(!locations.Any(l => l.IsAnalysisEntityDefaultLocation && l.AnalysisEntityOpt.HasUnknownInstanceLocation));
            Debug.Assert(locations.Count <= LocationThreshold);

            Locations = locations;
            LValueCapturedOperations = ImmutableHashSet<IOperation>.Empty;
            Kind = PointsToAbstractValueKind.KnownLocations;
            NullState = nullState;
        }

        private PointsToAbstractValue(ImmutableHashSet<IOperation> lValueCapturedOperations)
        {
            Debug.Assert(!lValueCapturedOperations.IsEmpty);

            LValueCapturedOperations = lValueCapturedOperations;
            Locations = ImmutableHashSet<AbstractLocation>.Empty;
            Kind = PointsToAbstractValueKind.KnownLValueCaptures;
            NullState = NullAbstractValue.NotNull;
        }

        private PointsToAbstractValue(PointsToAbstractValueKind kind, NullAbstractValue nullState)
        {
            Debug.Assert(kind != PointsToAbstractValueKind.KnownLocations);
            Debug.Assert(kind != PointsToAbstractValueKind.KnownLValueCaptures);

            Locations = ImmutableHashSet<AbstractLocation>.Empty;
            LValueCapturedOperations = ImmutableHashSet<IOperation>.Empty;
            Kind = kind;
            NullState = nullState;
        }

        internal static PointsToAbstractValue Create(AbstractLocation location, bool mayBeNull)
        {
            Debug.Assert(!location.IsNull, "Use 'PointsToAbstractValue.NullLocation' singleton");
            Debug.Assert(!location.IsNoLocation, "Use 'PointsToAbstractValue.NoLocation' singleton");

            return new PointsToAbstractValue(ImmutableHashSet.Create(location), mayBeNull ? NullAbstractValue.MaybeNull : NullAbstractValue.NotNull);
        }

        internal static PointsToAbstractValue Create(IOperation lValueCapturedOperation)
        {
            Debug.Assert(lValueCapturedOperation != null);
            return new PointsToAbstractValue(ImmutableHashSet.Create(lValueCapturedOperation));
        }

        internal static PointsToAbstractValue Create(ImmutableHashSet<AbstractLocation> locations, NullAbstractValue nullState)
        {
            Debug.Assert(!locations.IsEmpty);

            if (locations.Count == 1)
            {
                var location = locations.Single();
                if (location.IsNull)
                {
                    return NullLocation;
                }
                if (location.IsNoLocation)
                {
                    return NoLocation;
                }
            }
            else if (locations.Count > LocationThreshold)
            {
                switch (nullState)
                {
                    case NullAbstractValue.Null:
                        return UnknownNull;

                    case NullAbstractValue.NotNull:
                        return UnknownNotNull;

                    default:
                        return Unknown;
                }
            }

            return new PointsToAbstractValue(locations, nullState);
        }

        internal static PointsToAbstractValue Create(ImmutableHashSet<IOperation> lValueCapturedOperations)
        {
            Debug.Assert(!lValueCapturedOperations.IsEmpty);
            return new PointsToAbstractValue(lValueCapturedOperations);
        }

        internal PointsToAbstractValue MakeNonNull()
        {
            Debug.Assert(Kind != PointsToAbstractValueKind.KnownLValueCaptures);

            if (NullState == NullAbstractValue.NotNull)
            {
                return this;
            }

            if (Locations.IsEmpty)
            {
                return UnknownNotNull;
            }

            var locations = Locations.Where(location => !location.IsNull).ToImmutableHashSet();
            if (locations.IsEmpty)
            {
                return UnknownNotNull;
            }
            else if (locations.Count == Locations.Count)
            {
                locations = Locations;
            }

            return new PointsToAbstractValue(locations, NullAbstractValue.NotNull);
        }

        internal PointsToAbstractValue MakeNull()
        {
            Debug.Assert(Kind != PointsToAbstractValueKind.KnownLValueCaptures);

            if (NullState == NullAbstractValue.Null)
            {
                return this;
            }

            if (Locations.IsEmpty)
            {
                return UnknownNull;
            }

            return new PointsToAbstractValue(Locations, NullAbstractValue.Null);
        }

        internal PointsToAbstractValue MakeMayBeNull()
        {
            Debug.Assert(Kind != PointsToAbstractValueKind.KnownLValueCaptures);
            Debug.Assert(NullState != NullAbstractValue.Null);

            if (NullState == NullAbstractValue.MaybeNull)
            {
                return this;
            }

            if (Locations.IsEmpty)
            {
                return Unknown;
            }

            Debug.Assert(Locations.All(location => !location.IsNull));
            return new PointsToAbstractValue(Locations, NullAbstractValue.MaybeNull);
        }

        public ImmutableHashSet<AbstractLocation> Locations { get; }
        public ImmutableHashSet<IOperation> LValueCapturedOperations { get; }
        public PointsToAbstractValueKind Kind { get; }
        public NullAbstractValue NullState { get; }

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(Locations));
            addPart(HashUtilities.Combine(LValueCapturedOperations));
            addPart(Kind.GetHashCode());
            addPart(NullState.GetHashCode());
        }
    }
}
