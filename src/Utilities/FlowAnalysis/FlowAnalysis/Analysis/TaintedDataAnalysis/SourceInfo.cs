// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal delegate bool PointsToCheck(ImmutableArray<PointsToAbstractValue> pointsTos);
    internal delegate bool ValueContentCheck(ImmutableArray<PointsToAbstractValue> pointsTos, ImmutableArray<ValueContentAbstractValue> valueContents);
    internal delegate bool MethodMatcher(string methodName, ImmutableArray<IArgumentOperation> arguments);

    /// <summary>
    /// Info for tainted data sources, which generate tainted data.
    /// </summary>
    internal class SourceInfo : ITaintedDataInfo, IEquatable<SourceInfo>
    {
        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="fullTypeName">Full type name of the...type (namespace + type).</param>
        /// <param name="taintedProperties">Properties that generate tainted data.</param>
        /// <param name="taintedMethods">Methods that generate tainted data and whose arguments don't need extra analysis.</param>
        /// <param name="taintedMethodsNeedsPointsToAnalysis">Methods that generate tainted data and whose arguments don't need extra value content analysis.</param>
        /// <param name="taintedMethodsNeedsValueContentAnalysis">Methods that generate tainted data and whose arguments need extra value content analysis and points to analysis.</param>
        /// <param name="taintConstantArray"></param>
        public SourceInfo(
            string fullTypeName,
            bool isInterface,
            ImmutableHashSet<string> taintedProperties,
            ImmutableHashSet<(MethodMatcher, ImmutableHashSet<string>)> taintedMethods,
            ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(PointsToCheck, string)>)> taintedMethodsNeedsPointsToAnalysis,
            ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(ValueContentCheck, string)>)> taintedMethodsNeedsValueContentAnalysis,
            ImmutableHashSet<(MethodMatcher, ImmutableHashSet<(string, string)>)> transferMethods,
            bool taintConstantArray)
        {
            FullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
            IsInterface = isInterface;
            TaintedProperties = taintedProperties ?? throw new ArgumentNullException(nameof(taintedProperties));
            TaintedMethods = taintedMethods ?? throw new ArgumentNullException(nameof(taintedMethods));
            TaintedMethodsNeedsPointsToAnalysis = taintedMethodsNeedsPointsToAnalysis ?? throw new ArgumentNullException(nameof(taintedMethodsNeedsPointsToAnalysis));
            TaintedMethodsNeedsValueContentAnalysis = taintedMethodsNeedsValueContentAnalysis ?? throw new ArgumentNullException(nameof(taintedMethodsNeedsValueContentAnalysis));
            TransferMethods = transferMethods ?? throw new ArgumentNullException(nameof(transferMethods));
            TaintConstantArray = taintConstantArray;
        }

        /// <summary>
        /// Full type name of the...type (namespace + type).
        /// </summary>
        public string FullTypeName { get; }

        /// <summary>
        /// Indicates this type is an interface.
        /// </summary>
        public bool IsInterface { get; }

        /// <summary>
        /// Properties that generate tainted data.
        /// </summary>
        public ImmutableHashSet<string> TaintedProperties { get; }

        /// <summary>
        /// Methods that generate tainted data.
        /// TaintedTarget is the tainted target (arguments / return value).
        /// </summary>
        public ImmutableHashSet<(MethodMatcher MethodMatcher, ImmutableHashSet<string> TaintedTargets)> TaintedMethods { get; }

        /// <summary>
        /// Methods that generate tainted data and whose arguments don't need extra value content analysis.
        /// </summary>
        /// <remarks>
        /// MethodMatcher determines if the outermost tuple applies, based on the method names and arguments.
        /// PointsToCheck determines if the inner tuple applies, based on the method invocation's arguments PointsToAbstractValues.
        /// TaintedTarget is the tainted target (arguments / return value).
        ///
        /// Example:
        /// (
        ///   (methodName, argumentOperations) => methodName == "Bar",  // MethodMatcher
        ///   {
        ///      (
        ///         (pointsTos) => true,  // PointsToCheck
        ///         TaintedTargetValue.Return  // TaintedTarget
        ///      )
        ///   }
        /// )
        ///
        /// will treat any invocation of the "Bar" method's return value as tainted.
        /// </remarks>
        public ImmutableHashSet<(MethodMatcher MethodMatcher, ImmutableHashSet<(PointsToCheck PointsToCheck, string TaintedTarget)>)> TaintedMethodsNeedsPointsToAnalysis { get; }

        /// <summary>
        /// Methods that generate tainted data and whose arguments need extra value content analysis and points to analysis.
        /// </summary>
        /// <remarks>
        /// MethodMatcher determines if the outermost tuple applies, based on the method names and arguments.
        /// ValueContentCheck determines if the inner tuple applies, based on the method invocation's arguments PointsToAbstractValues and ValueContentAbstractValues.
        /// TaintedTarget is the tainted target (arguments / return value).
        ///
        /// Example:
        /// (
        ///   (methodName, argumentOperations) => methodName == "Bar",  // MethodMatcher
        ///   {
        ///      (
        ///         (pointsTos, valueContents) => true,  // ValueContentCheck
        ///         TaintedTargetValue.Return  // TaintedTarget
        ///      )
        ///   }
        /// )
        ///
        /// will treat any invocation of the "Bar" method's return value as tainted.
        /// </remarks>
        public ImmutableHashSet<(MethodMatcher MethodMatcher, ImmutableHashSet<(ValueContentCheck ValueContentCheck, string TaintedTarget)>)> TaintedMethodsNeedsValueContentAnalysis { get; }

        /// <summary>
        /// Methods that could taint another argument when one of its argument is tainted.
        /// </summary>
        /// <remarks>
        /// MethodMatcher determines if the outermost tuple applies, based on the method names and arguments.
        /// (IfTaintedParameter, ThenTaintedTarget) determines if the ThenTaintedTarget is tainted, based on if the IfTaintedParameter is tainted.
        ///
        /// Example:
        /// (
        ///   (methodName, argumentOperations) => methodName == "Bar",  // MethodMatcher
        ///   {
        ///      ("a", "b")
        ///   }
        /// )
        ///
        /// will treat the parameter "b" as tainted when parameter "a" is tainted of the "Bar" method.
        /// </remarks>
        public ImmutableHashSet<(MethodMatcher MethodMatcher, ImmutableHashSet<(string IfTaintedParameter, string ThenTaintedTarget)>)> TransferMethods { get; }

        /// <summary>
        /// Indicates arrays initialized with constant values of this type generates tainted data.
        /// </summary>
        public bool TaintConstantArray { get; }

        /// <summary>
        /// Indicates that this <see cref="SourceInfo"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        public bool RequiresValueContentAnalysis => this.TaintedMethodsNeedsValueContentAnalysis.Any();

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.TaintConstantArray.GetHashCode(),
                HashUtilities.Combine(this.TaintedProperties,
                HashUtilities.Combine(this.TaintedMethods,
                HashUtilities.Combine(this.TaintedMethodsNeedsPointsToAnalysis,
                HashUtilities.Combine(this.TaintedMethodsNeedsValueContentAnalysis,
                HashUtilities.Combine(this.TransferMethods,
                HashUtilities.Combine(this.IsInterface.GetHashCode(),
                    StringComparer.Ordinal.GetHashCode(this.FullTypeName))))))));
        }

        public override bool Equals(object obj)
        {
            return obj is SourceInfo other ? this.Equals(other) : false;
        }

        public bool Equals(SourceInfo other)
        {
            return other != null
                && this.FullTypeName == other.FullTypeName
                && this.IsInterface == other.IsInterface
                && this.TaintedProperties == other.TaintedProperties
                && this.TaintedMethods == other.TaintedMethods
                && this.TaintedMethodsNeedsPointsToAnalysis == other.TaintedMethodsNeedsPointsToAnalysis
                && this.TaintedMethodsNeedsValueContentAnalysis == other.TaintedMethodsNeedsValueContentAnalysis
                && this.TransferMethods == other.TransferMethods
                && this.TaintConstantArray == other.TaintConstantArray;
        }
    }
}
