// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Collection of <see cref="HazardousUsageEvaluator"/>s.
    /// </summary>
#pragma warning disable CA1812 // Is too instantiated.
    internal sealed class HazardousUsageEvaluatorCollection
#pragma warning restore CA1812
    {
        public HazardousUsageEvaluatorCollection(IEnumerable<HazardousUsageEvaluator> hazardousUsageEvaluators)
        {
            if (hazardousUsageEvaluators == null)
            {
                throw new ArgumentNullException(nameof(hazardousUsageEvaluators));
            }

            if (!hazardousUsageEvaluators.Any())
            {
                throw new ArgumentException("No HazardUsageEvaluators specified", nameof(hazardousUsageEvaluators));
            }

            this.HazardousUsageEvaluators =
                hazardousUsageEvaluators.ToImmutableDictionary<HazardousUsageEvaluator, (HazardousUsageEvaluatorKind Kind, string? InstanceTypeName, string? MethodName, string? ParameterName, bool derivedClasses)>(
                    h => (h.Kind, h.ContainingTypeName, h.MethodName, h.ParameterNameOfPropertySetObject, h.DerivedClass));
        }

        public HazardousUsageEvaluatorCollection(params HazardousUsageEvaluator[] hazardousUsageEvaluators)
            : this((IEnumerable<HazardousUsageEvaluator>)hazardousUsageEvaluators)
        {
        }

        private HazardousUsageEvaluatorCollection()
        {
            throw new NotSupportedException();
        }

        private ImmutableDictionary<(HazardousUsageEvaluatorKind Kind, string? InstanceTypeName, string? MethodName, string? ParameterName, bool DerivedClasses), HazardousUsageEvaluator> HazardousUsageEvaluators { get; }

        internal bool TryGetHazardousUsageEvaluator(string trackedTypeMethodName, out HazardousUsageEvaluator? hazardousUsageEvaluator, bool derivedClasses = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Invocation, null, trackedTypeMethodName, null, derivedClasses),
                out hazardousUsageEvaluator);
        }

        internal bool TryGetHazardousUsageEvaluator(
            string containingType,
            string methodName,
            string parameterName,
            [NotNullWhen(returnValue: true)] out HazardousUsageEvaluator? hazardousUsageEvaluator)
        {
            if (this.HazardousUsageEvaluators.TryGetValue(
                    (HazardousUsageEvaluatorKind.Invocation, containingType, methodName, parameterName, false),
                    out hazardousUsageEvaluator)
                || this.HazardousUsageEvaluators.TryGetValue(
                    (HazardousUsageEvaluatorKind.Invocation, containingType, methodName, parameterName, true),
                    out hazardousUsageEvaluator))
            {
                return true;
            }

            return false;
        }

        internal bool TryGetReturnHazardousUsageEvaluator(
            [NotNullWhen(returnValue: true)] out HazardousUsageEvaluator? hazardousUsageEvaluator,
            bool derivedClass = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Return, null, null, null, derivedClass),
                out hazardousUsageEvaluator);
        }

        internal bool TryGetInitializationHazardousUsageEvaluator(
            [NotNullWhen(returnValue: true)] out HazardousUsageEvaluator? hazardousUsageEvaluator,
            bool derivedClass = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Initialization, null, null, null, derivedClass),
                out hazardousUsageEvaluator);
        }

        internal bool TryGetArgumentHazardousUsageEvaluator(
            [NotNullWhen(returnValue: true)] out HazardousUsageEvaluator? hazardousUsageEvaluator,
            bool derivedClass = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Argument, null, null, null, derivedClass),
                out hazardousUsageEvaluator);
        }

        internal ImmutableDictionary<(INamedTypeSymbol, bool), string> GetTypeToNameMapping(WellKnownTypeProvider wellKnownTypeProvider)
        {
            using var _ = PooledDictionary<(INamedTypeSymbol, bool), string>.GetInstance(out var pooledDictionary);
            foreach (KeyValuePair<(HazardousUsageEvaluatorKind Kind, string? InstanceTypeName, string? MethodName, string? ParameterName, bool derivedClasses), HazardousUsageEvaluator> kvp
                    in this.HazardousUsageEvaluators)
            {
                if (kvp.Key.InstanceTypeName == null || kvp.Key.Kind != HazardousUsageEvaluatorKind.Invocation)
                {
                    continue;
                }

                if (wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(kvp.Key.InstanceTypeName, out INamedTypeSymbol? namedTypeSymbol))
                {
                    pooledDictionary[(namedTypeSymbol, kvp.Key.derivedClasses)] = kvp.Key.InstanceTypeName;
                }
            }

            return pooledDictionary.ToImmutableDictionary();
        }
    }
}
