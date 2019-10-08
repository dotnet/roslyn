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
        public delegate HazardousUsageEvaluationResult InvocationEvaluationCallback(IMethodSymbol methodSymbol, PropertySetAbstractValue propertySetAbstractValue);

        /// <summary>
        /// Evaluates if a given <see cref="PropertySetAbstractValue"/> is hazardous or not.
        /// </summary>
        /// <param name="propertySetAbstractValue">Abstract value of the type being tracked by PropertySetAnalysis.</param>
        /// <returns>Evaluation result of whether the usage is hazardous.</returns>
        public delegate HazardousUsageEvaluationResult EvaluationCallback(PropertySetAbstractValue propertySetAbstractValue);

        /// <summary>
        /// Initializes a <see cref="HazardousUsageEvaluator"/> that evaluates a method invocation on the type being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="trackedTypeMethodName">Name of the method within the tracked type.</param>
        /// <param name="evaluator">Evaluation callback.</param>
        /// <param name="derivedClass">Whether to consider derived class.</param>
        public HazardousUsageEvaluator(string trackedTypeMethodName, InvocationEvaluationCallback evaluator, bool derivedClass = false)
        {
            MethodName = trackedTypeMethodName ?? throw new ArgumentNullException(nameof(trackedTypeMethodName));
            InvocationEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            Kind = HazardousUsageEvaluatorKind.Invocation;
            DerivedClass = derivedClass;
        }

        /// <summary>
        /// Initializes a <see cref="HazardousUsageEvaluator"/> that evaluates a method invocation with an argument of the type being tracked by PropertySetAnalysis.
        /// </summary>
        /// <param name="containingType">Name of the instance that the method is invoked on.</param>
        /// <param name="methodName">Name of the method within <paramref name="containingType"/>.</param>
        /// <param name="parameterNameOfPropertySetObject">Name of the method parameter containing the type being tracked by PropertySetAnalysis.</param>
        /// <param name="evaluator">Evaluation callback.</param>
        /// <param name="derivedClass">Whether to consider derived class.</param>
        public HazardousUsageEvaluator(string containingType, string methodName, string parameterNameOfPropertySetObject, InvocationEvaluationCallback evaluator, bool derivedClass = false)
        {
            ContainingTypeName = containingType ?? throw new ArgumentNullException(nameof(containingType));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            ParameterNameOfPropertySetObject = parameterNameOfPropertySetObject ?? throw new ArgumentNullException(nameof(parameterNameOfPropertySetObject));
            InvocationEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            DerivedClass = derivedClass;
            Kind = HazardousUsageEvaluatorKind.Invocation;
        }

        /// <summary>
        /// Initializes a <see cref="HazardousUsageEvaluator"/> that evaluates a return statement with a return value of the tracked type.
        /// </summary>
        /// <param name="evaluator">Evaluation callback.</param>
        /// <param name="derivedClass">Whether to consider derived class.</param>
        public HazardousUsageEvaluator(HazardousUsageEvaluatorKind kind, EvaluationCallback evaluator, bool derivedClass = false)
        {
            if (kind != HazardousUsageEvaluatorKind.Return && kind != HazardousUsageEvaluatorKind.Initialization && kind != HazardousUsageEvaluatorKind.Argument)
            {
                throw new ArgumentException(
                    "kind must be Return or Initialization or Argument.  Use other constructors for Invocation.",
                    nameof(kind));
            }

            Kind = kind;
            DerivedClass = derivedClass;
            ValueEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        private HazardousUsageEvaluator()
        {
        }

        public HazardousUsageEvaluatorKind Kind { get; }

        /// <summary>
        /// Name of the type containing the method, or null if method is part of the type being tracked by PropertySetAnalysis or this is for a return statement.
        /// </summary>
        public string ContainingTypeName { get; }

        /// <summary>
        /// Name of the method being invoked, or null if this is for a return statement.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Name of the parameter containing the object containing the type being tracked by PropertySetAnalysis, or null if the method is part of the type being tracked by PropertySetAnalysis.
        /// </summary>
        public string ParameterNameOfPropertySetObject { get; }

        /// <summary>
        /// Evaluates if the method invocation with a given <see cref="PropertySetAbstractValue"/> is hazardous or not.
        /// </summary>
        public InvocationEvaluationCallback InvocationEvaluator { get; }

        /// <summary>
        /// Determines whether consider the derived classes of the containing type.
        /// </summary>
        public bool DerivedClass { get; }

        /// <summary>
        /// Evaluates if the return statement or initialization value with a given <see cref="PropertySetAbstractValue"/> is hazardous or not.
        /// </summary>
        public EvaluationCallback ValueEvaluator { get; }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(
                this.ContainingTypeName.GetHashCodeOrDefault(),
                HashUtilities.Combine(this.MethodName.GetHashCodeOrDefault(),
                HashUtilities.Combine(this.ParameterNameOfPropertySetObject.GetHashCodeOrDefault(),
                 HashUtilities.Combine(this.DerivedClass.GetHashCodeOrDefault(),
                this.InvocationEvaluator.GetHashCodeOrDefault()))));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as HazardousUsageEvaluator);
        }

        public bool Equals(HazardousUsageEvaluator other)
        {
            return other != null
                && this.ContainingTypeName == other.ContainingTypeName
                && this.MethodName == other.MethodName
                && this.ParameterNameOfPropertySetObject == other.ParameterNameOfPropertySetObject
                && this.DerivedClass == other.DerivedClass
                && this.InvocationEvaluator == other.InvocationEvaluator;
        }
    }
}
