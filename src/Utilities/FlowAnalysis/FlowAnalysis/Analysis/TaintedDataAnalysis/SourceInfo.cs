// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal delegate bool PointsToCheck(ImmutableArray<PointsToAbstractValue> pointsTos);
    internal delegate bool ValueContentCheck(ImmutableArray<PointsToAbstractValue> pointsTos, ImmutableArray<ValueContentAbstractValue> valueContents);
    internal delegate bool MethodMapper(string methodName, ImmutableArray<IArgumentOperation> arguments);

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
        /// <param name="taintedMethodsNeedsPointsToAnalysis">Methods that generate tainted data and whose arguments don't need extra value content analysis.</param>
        /// <param name="taintedMethodsNeedsValueContentAnalysis">Methods that generate tainted data and whose arguments need extra value content analysis and points to analysis.</param>
        /// <param name="taintConstantArray"></param>
        public SourceInfo(
            string fullTypeName,
            bool isInterface,
            ImmutableHashSet<string> taintedProperties,
            ImmutableDictionary<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>> taintedMethodsNeedsPointsToAnalysis,
            ImmutableDictionary<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>> taintedMethodsNeedsValueContentAnalysis,
            ImmutableDictionary<MethodMapper, ImmutableHashSet<(string, string)>> passerMethods,
            bool taintConstantArray)
        {
            FullTypeName = fullTypeName ?? throw new ArgumentNullException(nameof(fullTypeName));
            IsInterface = isInterface;
            TaintedProperties = taintedProperties ?? throw new ArgumentNullException(nameof(taintedProperties));
            TaintedMethodsNeedsPointsToAnalysis = taintedMethodsNeedsPointsToAnalysis ?? throw new ArgumentNullException(nameof(taintedMethodsNeedsPointsToAnalysis));
            TaintedMethodsNeedsValueContentAnalysis = taintedMethodsNeedsValueContentAnalysis ?? throw new ArgumentNullException(nameof(taintedMethodsNeedsValueContentAnalysis));
            PasserMethods = passerMethods ?? throw new ArgumentNullException(nameof(passerMethods));
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
        /// Methods that generate tainted data and whose arguments don't need extra value content analysis.
        /// </summary>
        public ImmutableDictionary<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>> TaintedMethodsNeedsPointsToAnalysis { get; }

        /// <summary>
        /// Methods that generate tainted data and whose arguments need extra value content analysis and points to analysis.
        /// </summary>
        public ImmutableDictionary<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>> TaintedMethodsNeedsValueContentAnalysis { get; }

        /// <summary>
        /// Methods that could taint another argument when one of its argument is tainted.
        /// </summary>
        public ImmutableDictionary<MethodMapper, ImmutableHashSet<(string, string)>> PasserMethods { get; }

        /// <summary>
        /// Indicates arrays initialized with constant values of this type generates tainted data.
        /// </summary>
        public bool TaintConstantArray { get; }

        /// <summary>
        /// Indicates that this <see cref="SourceInfo"/> uses <see cref="ValueContentAbstractValue"/>s.
        /// </summary>
        public bool RequiresValueContentAnalysis => this.TaintedMethodsNeedsValueContentAnalysis != null;

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.TaintConstantArray.GetHashCode(),
                HashUtilities.Combine(this.TaintedProperties,
                HashUtilities.Combine(this.TaintedMethodsNeedsPointsToAnalysis,
                HashUtilities.Combine(this.TaintedMethodsNeedsValueContentAnalysis,
                HashUtilities.Combine(this.PasserMethods,
                HashUtilities.Combine(this.IsInterface.GetHashCode(),
                    StringComparer.Ordinal.GetHashCode(this.FullTypeName)))))));
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
                && this.TaintedMethodsNeedsPointsToAnalysis == other.TaintedMethodsNeedsPointsToAnalysis
                && this.TaintedMethodsNeedsValueContentAnalysis == other.TaintedMethodsNeedsValueContentAnalysis
                && this.PasserMethods == other.PasserMethods
                && this.TaintConstantArray == other.TaintConstantArray;
        }
    }
}
