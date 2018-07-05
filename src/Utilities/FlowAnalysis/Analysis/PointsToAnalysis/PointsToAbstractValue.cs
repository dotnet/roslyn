// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Abstract PointsTo value for an <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="PointsToAnalysis"/>.
    /// It contains the set of possible <see cref="AbstractLocation"/>s that the entity or the operation can point to and the <see cref="Kind"/> of the location(s).
    /// </summary>
    internal class PointsToAbstractValue: CacheBasedEquatable<PointsToAbstractValue>
    {
        public static PointsToAbstractValue Undefined = new PointsToAbstractValue(PointsToAbstractValueKind.Undefined, NullAbstractValue.Undefined);
        public static PointsToAbstractValue Invalid = new PointsToAbstractValue(PointsToAbstractValueKind.Invalid, NullAbstractValue.Invalid);
        public static PointsToAbstractValue Unknown = new PointsToAbstractValue(PointsToAbstractValueKind.Unknown, NullAbstractValue.MaybeNull);
        public static PointsToAbstractValue NoLocation = new PointsToAbstractValue(ImmutableHashSet.Create(AbstractLocation.NoLocation), NullAbstractValue.NotNull);
        public static PointsToAbstractValue NullLocation = new PointsToAbstractValue(ImmutableHashSet.Create(AbstractLocation.Null), NullAbstractValue.Null);

        private PointsToAbstractValue(ImmutableHashSet<AbstractLocation> locations, NullAbstractValue nullState)
        {
            Debug.Assert(!locations.IsEmpty);
            Debug.Assert(locations.All(location => !location.IsNull) || nullState != NullAbstractValue.NotNull);
            Debug.Assert(nullState != NullAbstractValue.Undefined);
            Debug.Assert(nullState != NullAbstractValue.Invalid);

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
            Debug.Assert(nullState != NullAbstractValue.Null);

            Locations = ImmutableHashSet<AbstractLocation>.Empty;
            LValueCapturedOperations = ImmutableHashSet<IOperation>.Empty;
            Kind = kind;
            NullState = nullState;
        }

        public static PointsToAbstractValue Create(AbstractLocation location, bool mayBeNull)
        {
            Debug.Assert(!location.IsNull, "Use 'PointsToAbstractValue.NullLocation' singleton");
            Debug.Assert(!location.IsNoLocation, "Use 'PointsToAbstractValue.NoLocation' singleton");

            return new PointsToAbstractValue(ImmutableHashSet.Create(location), mayBeNull ? NullAbstractValue.MaybeNull : NullAbstractValue.NotNull);
        }

        public static PointsToAbstractValue Create(IOperation lValueCapturedOperation)
        {
            Debug.Assert(lValueCapturedOperation != null);
            return new PointsToAbstractValue(ImmutableHashSet.Create(lValueCapturedOperation));
        }

        public static PointsToAbstractValue Create(ImmutableHashSet<AbstractLocation> locations, NullAbstractValue nullState)
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

            return new PointsToAbstractValue(locations, nullState);
        }

        public static PointsToAbstractValue Create(ImmutableHashSet<IOperation> lValueCapturedOperations)
        {
            Debug.Assert(!lValueCapturedOperations.IsEmpty);
            return new PointsToAbstractValue(lValueCapturedOperations);
        }

        public PointsToAbstractValue MakeNonNull(IOperation operation)
        {
            Debug.Assert(Kind != PointsToAbstractValueKind.KnownLValueCaptures);

            if (NullState == NullAbstractValue.NotNull)
            {
                return this;
            }

            if (Kind != PointsToAbstractValueKind.KnownLocations)
            {
                return Create(AbstractLocation.CreateAllocationLocation(operation, operation.Type), mayBeNull: false);
            }

            var locations = Locations.Where(location => !location.IsNull).ToImmutableHashSet();
            if (locations.Count == Locations.Count)
            {
                locations = Locations;
            }

            return new PointsToAbstractValue(locations, NullAbstractValue.NotNull);
        }

        public PointsToAbstractValue MakeNull()
        {
            Debug.Assert(Kind != PointsToAbstractValueKind.KnownLValueCaptures);

            if (NullState == NullAbstractValue.Null)
            {
                return this;
            }

            if (Kind != PointsToAbstractValueKind.KnownLocations)
            {
                return NullLocation;
            }

            return new PointsToAbstractValue(Locations, NullAbstractValue.Null);
        }

        public PointsToAbstractValue MakeMayBeNull()
        {
            Debug.Assert(Kind != PointsToAbstractValueKind.KnownLValueCaptures);
            Debug.Assert(NullState != NullAbstractValue.Null);

            if (NullState == NullAbstractValue.MaybeNull || ReferenceEquals(this, Unknown))
            {
                return this;
            }
            else if (Locations.IsEmpty)
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

        protected override int ComputeHashCode()
        {
            int hashCode = HashUtilities.Combine(Kind.GetHashCode(), NullState.GetHashCode());

            hashCode = HashUtilities.Combine(Locations.Count.GetHashCode(), hashCode);
            foreach (var location in Locations)
            {
                hashCode = HashUtilities.Combine(location.GetHashCode(), hashCode);
            }

            hashCode = HashUtilities.Combine(LValueCapturedOperations.Count.GetHashCode(), hashCode);
            foreach (var lValueCapturedOperation in LValueCapturedOperations)
            {
                hashCode = HashUtilities.Combine(lValueCapturedOperation.GetHashCode(), hashCode);
            }

            return hashCode;
        }
    }
}
