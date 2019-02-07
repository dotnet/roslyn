using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        internal bool TryGetHazardousUsageEvaluator(string trackedTypeMethodName, out HazardousUsageEvaluator hazardousUsageEvaluator)
        {
            return this.HazardousUsageEvaluators.TryGetValue((null, trackedTypeMethodName, null), out hazardousUsageEvaluator);
        }

        private ImmutableDictionary<(string InstanceTypeName, string MethodName, string ParameterName), HazardousUsageEvaluator> HazardousUsageEvaluators { get; }
    }
}
