using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal sealed class HazardousUsageEvaluator
    {
        public delegate bool EvaluationCallback(ImmutableArray<PropertySetAbstractValueKind> propertyAbstractValues);

        public HazardousUsageEvaluator(string instanceTypeName, string methodName, EvaluationCallback evaluator)
        {
            InstanceTypeName = instanceTypeName ?? throw new ArgumentNullException(nameof(instanceTypeName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public HazardousUsageEvaluator(string instanceTypeName, string methodName, string parameterNameOfPropertySetObject, EvaluationCallback evaluator)
        {
            InstanceTypeName = instanceTypeName ?? throw new ArgumentNullException(nameof(instanceTypeName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            ParameterNameOfPropertySetObject = parameterNameOfPropertySetObject ?? throw new ArgumentNullException(nameof(parameterNameOfPropertySetObject));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        private HazardousUsageEvaluator()
        {
        }

        public string InstanceTypeName { get; }
        public string MethodName { get; }

        /// <summary>
        /// Name of the parameter containing the object containing the properties, or null if "this".
        /// </summary>
        public string ParameterNameOfPropertySetObject { get; }

        public EvaluationCallback Evaluator { get; }
    }
}
