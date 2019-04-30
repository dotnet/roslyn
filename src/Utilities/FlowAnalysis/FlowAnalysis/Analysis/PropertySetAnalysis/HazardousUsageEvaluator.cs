// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Determines if usage of a <see cref="PropertySetAbstractValue"/> is hazardous or not.
    /// </summary>
#pragma warning disable CA1812 // Is too instantiated.
    internal sealed class HazardousUsageEvaluator
#pragma warning restore CA1812
    {
        /// <summary>
        /// Evaluates if the method invocation with a given <see cref="PropertySetAbstractValue"/> is hazardous or not.
        /// </summary>
        /// <param name="methodSymbol">Invoked method.</param>
        /// <param name="propertySetAbstractValue">Abstract value of the type being tracked by PropertySetAnalysis.</param>
        /// <returns>Evaluation result of whether the usage is hazardous.</returns>
        public delegate HazardousUsageEvaluationResult EvaluationCallback(IMethodSymbol methodSymbol, PropertySetAbstractValue propertySetAbstractValue);

        /// <summary>
        /// Initializes a <see cref="HazardousUsageEvaluator"/> that evaluates a method invocation on the type being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="trackedTypeMethodName">Name of the method within the tracked type.</param>
        /// <param name="evaluator">Evaluation callback.</param>
        public HazardousUsageEvaluator(string trackedTypeMethodName, EvaluationCallback evaluator)
        {
            MethodName = trackedTypeMethodName ?? throw new ArgumentNullException(nameof(trackedTypeMethodName));
            Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        /// <summary>
        /// Initializes a <see cref="HazardousUsageEvaluator"/> that evaluates a method invocation with an argument of the type being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="instanceTypeName">Name of the instance that the method is invoked on.</param>
        /// <param name="methodName">Name of the method within <paramref name="instanceTypeName"/>.</param>
        /// <param name="parameterNameOfPropertySetObject">Name of the method parameter containing the type being tracked by PropertySetAnalysis.</param>
        /// <param name="evaluator">Evaluation callback.</param>
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

        /// <summary>
        /// Name of the type containing the method, or null if method is part of the type being tracked by PropertySetAnalysis.
        /// </summary>
        public string InstanceTypeName { get; }

        /// <summary>
        /// Name of the method being invoked.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Name of the parameter containing the object containing the type being tracked by PropertySetAnalysis, or null if the method is part of the type being tracked by PropertySetAnalysis.
        /// </summary>
        public string ParameterNameOfPropertySetObject { get; }

        /// <summary>
        /// Evaluates if the method invocation with a given <see cref="PropertySetAbstractValue"/> is hazardous or not.
        /// </summary>
        public EvaluationCallback Evaluator { get; }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(
                this.InstanceTypeName.GetHashCodeOrDefault(),
                HashUtilities.Combine(this.MethodName.GetHashCodeOrDefault(),
                HashUtilities.Combine(this.ParameterNameOfPropertySetObject.GetHashCodeOrDefault(),
                this.Evaluator.GetHashCodeOrDefault())));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as HazardousUsageEvaluator);
        }

        public bool Equals(HazardousUsageEvaluator other)
        {
            return other != null
                && this.InstanceTypeName == other.InstanceTypeName
                && this.MethodName == other.MethodName
                && this.ParameterNameOfPropertySetObject == other.ParameterNameOfPropertySetObject
                && this.Evaluator == other.Evaluator;
        }
    }
}
