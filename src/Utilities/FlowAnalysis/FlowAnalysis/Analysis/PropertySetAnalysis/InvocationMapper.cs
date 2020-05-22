// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Maps from a method invocation on a tracked type (or derived) to a <see cref="PropertySetAbstractValue"/>.
    /// </summary>
    internal class InvocationMapper
    {
        /// <summary>
        /// Predicate for matching methods.
        /// </summary>
        /// <param name="method">Method to match.</param>
        /// <returns>True if matching, false otherwise.</returns>
        public delegate bool MethodSignatureMatcher(IMethodSymbol method);

        /// <summary>
        /// Mapping from a method invocation to a <see cref="PropertySetAbstractValue"/>, by examing the arguments'
        /// <see cref="ValueContentAbstractValue"/>s and <see cref="PointsToAbstractValue"/>s.
        /// </summary>
        /// <param name="previousAbstractValue">Previous abstract value of all properties being tracked.</param>
        /// <param name="methodSymbol">The method being invoked.</param>
        /// <param name="valueContentAbstractValues">Invoked method's arguments' <see cref="ValueContentAbstractValue"/>s.</param>
        /// <param name="pointsToAbstractValues">Invoked method's arguments' <see cref="PointsToAbstractValue"/>s.</param>
        /// <returns>Updated <see cref="PropertySetAbstractValue"/> after the method invocation.</returns>
        public delegate PropertySetAbstractValue InvocationValueContentAbstractValueCallback(
            PropertySetAbstractValue previousAbstractValue,
            IMethodSymbol methodSymbol,
            IReadOnlyList<ValueContentAbstractValue> valueContentAbstractValues,
            IReadOnlyList<PointsToAbstractValue> pointsToAbstractValues);

        /// <summary>
        /// Mapping from a method invocation to a <see cref="PropertySetAbstractValue"/>, by examing the arguments'
        /// <see cref="PointsToAbstractValue"/>s.
        /// </summary>
        /// <param name="previousAbstractValue">Previous abstract value of all properties being tracked.</param>
        /// <param name="methodSymbol">The method being invoked.</param>
        /// <param name="pointsToAbstractValues">Invoked method's arguments' <see cref="PointsToAbstractValue"/>s.</param>
        /// <returns>Updated <see cref="PropertySetAbstractValue"/> after the method invocation.</returns>
        public delegate PropertySetAbstractValue InvocationPointsToAbstractValueCallback(
            PropertySetAbstractValue previousAbstractValue,
            IMethodSymbol methodSymbol,
            IReadOnlyList<PointsToAbstractValue> pointsToAbstractValues);

        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="methodMetadataName">Method being invoked.</param>
        /// <param name="methodSignatureMatcher">Predicate for matching method overloads by their arguments.</param>
        /// <param name="mapFromArgumentValueContentAbstractValues">Callback for mapping the invoked method's arguments to a 
        /// <see cref="PropertySetAbstractValue"/>.</param>
        public InvocationMapper(
            string methodMetadataName,
            MethodSignatureMatcher methodSignatureMatcher,
            InvocationValueContentAbstractValueCallback mapFromArgumentValueContentAbstractValues)
        {
            this.MethodMetadataName = methodMetadataName ?? throw new ArgumentNullException(nameof(methodMetadataName));
            this.SignatureMatcher = methodSignatureMatcher ?? throw new ArgumentNullException(nameof(methodSignatureMatcher));
            this.MapFromArgumentValueContentAbstractValues = mapFromArgumentValueContentAbstractValues ?? throw new ArgumentNullException(nameof(mapFromArgumentValueContentAbstractValues));
        }

        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="methodMetadataName">Method being invoked.</param>
        /// <param name="methodSignatureMatcher">Predicate for matching method overloads by their arguments.</param>
        /// <param name="mapFromArgumentPointsToAbstractValues">Callback for mapping the invoked method's arguments to a 
        /// <see cref="PropertySetAbstractValue"/>.</param>
        public InvocationMapper(
            string methodMetadataName,
            MethodSignatureMatcher methodSignatureMatcher,
            InvocationPointsToAbstractValueCallback mapFromArgumentPointsToAbstractValues)
        {
            this.MethodMetadataName = methodMetadataName ?? throw new ArgumentNullException(nameof(methodMetadataName));
            this.SignatureMatcher = methodSignatureMatcher ?? throw new ArgumentNullException(nameof(methodSignatureMatcher));
            this.MapFromArgumentPointsToAbstractValues = mapFromArgumentPointsToAbstractValues ?? throw new ArgumentNullException(nameof(mapFromArgumentPointsToAbstractValues));
        }

        /// <summary>
        /// Method's metadata name.
        /// </summary>
        internal string MethodMetadataName { get; }

        /// <summary>
        /// Predicate for matching by method's parameters.
        /// </summary>
        internal MethodSignatureMatcher SignatureMatcher { get; }

        /// <summary>
        /// Callback for mapping from invoked method arguments.
        /// </summary>
        internal InvocationValueContentAbstractValueCallback? MapFromArgumentValueContentAbstractValues { get; }

        /// <summary>
        /// Callback for mapping from invoked method arguments.
        /// </summary>
        internal InvocationPointsToAbstractValueCallback? MapFromArgumentPointsToAbstractValues { get; }

        /// <summary>
        /// Indicates that ValueContentAnalysis is required.
        /// </summary>
        internal bool RequiresValueContentAnalysis { get { return this.MapFromArgumentValueContentAbstractValues != null; } }
    }
}
