// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T> - CacheBasedEquatable handles equality

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    internal sealed class FlightEnabledAbstractValue : CacheBasedEquatable<FlightEnabledAbstractValue>
    {
        public static readonly FlightEnabledAbstractValue Unset = new FlightEnabledAbstractValue(
            ImmutableHashSet<IGlobalAbstractValue>.Empty, ImmutableHashSet<FlightEnabledAbstractValue>.Empty, 0, FlightEnabledAbstractValueKind.Unset);
        public static readonly FlightEnabledAbstractValue Empty = new FlightEnabledAbstractValue(
            ImmutableHashSet<IGlobalAbstractValue>.Empty, ImmutableHashSet<FlightEnabledAbstractValue>.Empty, 0, FlightEnabledAbstractValueKind.Empty);
        public static readonly FlightEnabledAbstractValue Unknown = new FlightEnabledAbstractValue(
            ImmutableHashSet<IGlobalAbstractValue>.Empty, ImmutableHashSet<FlightEnabledAbstractValue>.Empty, 0, FlightEnabledAbstractValueKind.Unknown);

        public FlightEnabledAbstractValue(
            ImmutableHashSet<IGlobalAbstractValue> enabledFlights,
            ImmutableHashSet<FlightEnabledAbstractValue> parents,
            int height,
            FlightEnabledAbstractValueKind kind)
        {
            Debug.Assert((!enabledFlights.IsEmpty || !parents.IsEmpty) == (kind == FlightEnabledAbstractValueKind.Known));
            Debug.Assert(enabledFlights.All(enabledFlightSet => enabledFlightSet != default));
            Debug.Assert(parents.All(parent => parent != null));
            Debug.Assert(height >= 0);
            Debug.Assert(height == 0 || kind == FlightEnabledAbstractValueKind.Known);
            Debug.Assert(height == 0 == parents.IsEmpty);

            EnabledFlights = enabledFlights;
            Parents = parents;
            Height = height;
            Kind = kind;
        }

        public FlightEnabledAbstractValue(IGlobalAbstractValue enabledFlight)
            : this(ImmutableHashSet.Create(enabledFlight), ImmutableHashSet<FlightEnabledAbstractValue>.Empty, height: 0, FlightEnabledAbstractValueKind.Known)
        {
        }

        public FlightEnabledAbstractValue(FlightEnabledAbstractValue parent1, FlightEnabledAbstractValue parent2)
            : this(ImmutableHashSet<IGlobalAbstractValue>.Empty,
                   ImmutableHashSet.Create(parent1, parent2),
                   height: Math.Max(parent1.Height, parent2.Height) + 1,
                   FlightEnabledAbstractValueKind.Known)
        {
        }

        public ImmutableHashSet<IGlobalAbstractValue> EnabledFlights { get; }
        public ImmutableHashSet<FlightEnabledAbstractValue> Parents { get; }
        public int Height { get; }
        public FlightEnabledAbstractValueKind Kind { get; }

        private FlightEnabledAbstractValue WithRootParent(FlightEnabledAbstractValue newRoot)
        {
            Debug.Assert(Kind == FlightEnabledAbstractValueKind.Known);

            var newHeight = Height + newRoot.Height + 1;
            if (Parents.IsEmpty)
            {
                return new FlightEnabledAbstractValue(EnabledFlights, ImmutableHashSet.Create(newRoot), newHeight, FlightEnabledAbstractValueKind.Known);
            }

            using var parentsBuilder = PooledHashSet<FlightEnabledAbstractValue>.GetInstance();
            foreach (var parent in Parents)
            {
                parentsBuilder.Add(parent.WithRootParent(newRoot));
            }

            return new FlightEnabledAbstractValue(EnabledFlights, parentsBuilder.ToImmutable(), newHeight, FlightEnabledAbstractValueKind.Known);
        }

        private static FlightEnabledAbstractValue WithNegatedFlights(FlightEnabledAbstractValue newEnabledFlights)
            => new FlightEnabledAbstractValue(
                GetNegatedFlightValues(newEnabledFlights.EnabledFlights),
                newEnabledFlights.Parents,
                newEnabledFlights.Height,
                newEnabledFlights.Kind);

        private static ImmutableHashSet<IGlobalAbstractValue> GetNegatedFlightValues(ImmutableHashSet<IGlobalAbstractValue> flights)
            => flights.Select(f => f.GetNegatedValue()).ToImmutableHashSet();

        internal FlightEnabledAbstractValue WithAdditionalEnabledFlights(FlightEnabledAbstractValue newEnabledFlights, bool negate)
        {
            return WithAdditionalEnabledFlightsCore(negate ? WithNegatedFlights(newEnabledFlights) : newEnabledFlights);
        }

        private FlightEnabledAbstractValue WithAdditionalEnabledFlightsCore(FlightEnabledAbstractValue newEnabledFlights)
        {
            Debug.Assert(Kind != FlightEnabledAbstractValueKind.Unknown);

            if (Kind != FlightEnabledAbstractValueKind.Known)
            {
                return newEnabledFlights;
            }

            if (newEnabledFlights.Height == 0)
            {
                return new FlightEnabledAbstractValue(
                    EnabledFlights.AddRange(newEnabledFlights.EnabledFlights), Parents, Height, FlightEnabledAbstractValueKind.Known);
            }

            return newEnabledFlights.WithRootParent(this);
        }

        internal FlightEnabledAbstractValue GetNegatedValue()
        {
            Debug.Assert(Kind == FlightEnabledAbstractValueKind.Known);

            var negatedFlights = GetNegatedFlightValues(EnabledFlights);
            Debug.Assert(negatedFlights.Count == EnabledFlights.Count);
            return new FlightEnabledAbstractValue(negatedFlights, Parents, Height, FlightEnabledAbstractValueKind.Known);
        }

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(EnabledFlights));
            addPart(HashUtilities.Combine(Parents));
            addPart(Height.GetHashCode());
            addPart(Kind.GetHashCode());
        }

        public override string ToString()
        {
            return GetParentString() + GetFlightsString();

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

            string GetFlightsString()
            {
                if (EnabledFlights.IsEmpty)
                {
                    return string.Empty;
                }

                var result = string.Join(" && ", EnabledFlights.Select(f => f.ToString()).Order());
                if (Parents.Count > 0)
                {
                    result = $" && {result}";
                }

                return result;
            }
        }
    }
}
