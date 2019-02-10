using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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

            this.HazardousUsageEvaluators =
                hazardousUsageEvaluators.ToImmutableDictionary<HazardousUsageEvaluator, (string InstanceTypeName, string MethodName, string ParameterName)>(
                    h => (h.InstanceTypeName, h.MethodName, h.ParameterNameOfPropertySetObject));
        }

        public HazardousUsageEvaluatorCollection(params HazardousUsageEvaluator[] hazardousUsageEvaluators)
            : this((IEnumerable<HazardousUsageEvaluator>)hazardousUsageEvaluators)
        {
        }

        private HazardousUsageEvaluatorCollection()
        {
        }

        private ImmutableDictionary<(string InstanceTypeName, string MethodName, string ParameterName), HazardousUsageEvaluator> HazardousUsageEvaluators { get; }

        internal bool TryGetHazardousUsageEvaluator(string trackedTypeMethodName, out HazardousUsageEvaluator hazardousUsageEvaluator)
        {
            return this.HazardousUsageEvaluators.TryGetValue((null, trackedTypeMethodName, null), out hazardousUsageEvaluator);
        }

        internal ImmutableDictionary<INamedTypeSymbol, string> GetTypeToNameMapping(WellKnownTypeProvider wellKnownTypeProvider)
        {
            PooledDictionary<INamedTypeSymbol, string> pooledDictionary = PooledDictionary<INamedTypeSymbol, string>.GetInstance();
            try
            {
                foreach (KeyValuePair<(string InstanceTypeName, string MethodName, string ParameterName), HazardousUsageEvaluator> kvp
                    in this.HazardousUsageEvaluators)
                {
                    if (kvp.Key.InstanceTypeName == null)
                    {
                        continue;
                    }

                    if (wellKnownTypeProvider.TryGetTypeByMetadataName(kvp.Key.InstanceTypeName, out INamedTypeSymbol namedTypeSymbol))
                    {
                        pooledDictionary.Add(namedTypeSymbol, kvp.Key.InstanceTypeName);
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
