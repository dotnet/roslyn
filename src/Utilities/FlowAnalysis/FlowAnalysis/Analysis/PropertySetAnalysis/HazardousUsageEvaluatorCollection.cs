// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

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
                hazardousUsageEvaluators.ToImmutableDictionary<HazardousUsageEvaluator, (HazardousUsageEvaluatorKind Kind, string InstanceTypeName, string MethodName, string ParameterName, bool derivedClasses)>(
                    h => (h.Kind, h.ContainingTypeName, h.MethodName, h.ParameterNameOfPropertySetObject, h.DerivedClass));
        }

        public HazardousUsageEvaluatorCollection(params HazardousUsageEvaluator[] hazardousUsageEvaluators)
            : this((IEnumerable<HazardousUsageEvaluator>)hazardousUsageEvaluators)
        {
        }

        private HazardousUsageEvaluatorCollection()
        {
        }

        private ImmutableDictionary<(HazardousUsageEvaluatorKind Kind, string InstanceTypeName, string MethodName, string ParameterName, bool DerivedClasses), HazardousUsageEvaluator> HazardousUsageEvaluators { get; }

        internal bool TryGetHazardousUsageEvaluator(string trackedTypeMethodName, out HazardousUsageEvaluator hazardousUsageEvaluator, bool derivedClasses = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Invocation, null, trackedTypeMethodName, null, derivedClasses),
                out hazardousUsageEvaluator);
        }

        internal bool TryGetHazardousUsageEvaluator(string containingType, string methodName, string parameterName, out HazardousUsageEvaluator hazardousUsageEvaluator)
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

        internal bool TryGetReturnHazardousUsageEvaluator(out HazardousUsageEvaluator hazardousUsageEvaluator, bool derivedClass = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Return, null, null, null, derivedClass),
                out hazardousUsageEvaluator);
        }

        internal bool TryGetInitializationHazardousUsageEvaluator(out HazardousUsageEvaluator hazardousUsageEvaluator, bool derivedClass = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Initialization, null, null, null, derivedClass),
                out hazardousUsageEvaluator);
        }

        internal bool TryGetArgumentHazardousUsageEvaluator(out HazardousUsageEvaluator hazardousUsageEvaluator, bool derivedClass = false)
        {
            return this.HazardousUsageEvaluators.TryGetValue(
                (HazardousUsageEvaluatorKind.Argument, null, null, null, derivedClass),
                out hazardousUsageEvaluator);
        }

        internal ImmutableDictionary<(INamedTypeSymbol, bool), string> GetTypeToNameMapping(WellKnownTypeProvider wellKnownTypeProvider)
        {
            PooledDictionary<(INamedTypeSymbol, bool), string> pooledDictionary = PooledDictionary<(INamedTypeSymbol, bool), string>.GetInstance();
            try
            {
                foreach (KeyValuePair<(HazardousUsageEvaluatorKind Kind, string InstanceTypeName, string MethodName, string ParameterName, bool derivedClasses), HazardousUsageEvaluator> kvp
                    in this.HazardousUsageEvaluators)
                {
                    if (kvp.Key.InstanceTypeName == null || kvp.Key.Kind != HazardousUsageEvaluatorKind.Invocation)
                    {
                        continue;
                    }

                    if (wellKnownTypeProvider.TryGetTypeByMetadataName(kvp.Key.InstanceTypeName, out INamedTypeSymbol namedTypeSymbol))
                    {
                        pooledDictionary[(namedTypeSymbol, kvp.Key.derivedClasses)] = kvp.Key.InstanceTypeName;
                    }
                }

                return pooledDictionary.ToImmutableDictionaryAndFree();
            }
            catch (Exception)
            {
                pooledDictionary.Free();
                throw;
            }
        }
    }
}
