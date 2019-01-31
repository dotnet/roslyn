using System;
using System.Collections.Generic;
using System.Text;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal class HazardousUsageEvaluator
    {
        public HazardousUsageEvaluator(string instanceTypeName, string methodName, Func<PropertySetAbstractValue, bool> evaluator)
        {
            InstanceTypeName = instanceTypeName ?? throw new ArgumentNullException(nameof(instanceTypeName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public HazardousUsageEvaluator(string instanceTypeName, string methodName, string parameterNameOfPropertySetObject, Func<PropertySetAbstractValue, bool> evaluator)
        {
            InstanceTypeName = instanceTypeName ?? throw new ArgumentNullException(nameof(instanceTypeName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            ParameterNameOfPropertySetObject = parameterNameOfPropertySetObject ?? throw new ArgumentNullException(nameof(parameterNameOfPropertySetObject));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public string InstanceTypeName { get; }
        public string MethodName { get; }

        /// <summary>
        /// Name of the parameter containing the object containing the properties, or null if "this".
        /// </summary>
        public string ParameterNameOfPropertySetObject { get; }

        public Func<PropertySetAbstractValue, bool> Evaluator { get; }
    }
}
