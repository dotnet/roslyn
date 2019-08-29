// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class PooledHashSetExtensions
    {
        // Just to make hardcoding SinkInfos more convenient.
        public static void AddSinkInfo(
            this PooledHashSet<SinkInfo> builder,
            string fullTypeName,
            SinkKind sinkKind,
            bool isInterface,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string> sinkProperties,
            IEnumerable<(string Method, string[] Parameters)> sinkMethodParameters)
        {
            builder.AddSinkInfo(
                fullTypeName,
                new[] { sinkKind },
                isInterface,
                isAnyStringParameterInConstructorASink,
                sinkProperties,
                sinkMethodParameters);
        }

        // Just to make hardcoding SinkInfos more convenient.
        public static void AddSinkInfo(
            this PooledHashSet<SinkInfo> builder,
            string fullTypeName,
            IEnumerable<SinkKind> sinkKinds,
            bool isInterface,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string> sinkProperties,
            IEnumerable<(string Method, string[] Parameters)> sinkMethodParameters)
        {
            SinkInfo sinkInfo = new SinkInfo(
                fullTypeName,
                sinkKinds.ToImmutableHashSet(),
                isInterface,
                isAnyStringParameterInConstructorASink,
                sinkProperties: sinkProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                        ?? ImmutableHashSet<string>.Empty,
                sinkMethodParameters:
                    sinkMethodParameters
                            ?.Select(o => new KeyValuePair<string, ImmutableHashSet<string>>(o.Method, o.Parameters.ToImmutableHashSet()))
                            ?.ToImmutableDictionary(StringComparer.Ordinal)
                        ?? ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
            builder.Add(sinkInfo);
        }

        // Just to make hardcoding SourceInfos more convenient.
        public static void AddSourceInfo(
            this PooledHashSet<SourceInfo> builder,
            string fullTypeName,
            bool isInterface,
            string[] taintedProperties,
            IEnumerable<string> taintedMethods)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                 taintedMethodsNeedsPointsToAnalysis:
                    taintedMethods
                        ?.Select(o => new KeyValuePair<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>>(
                            (methodName, argumets) => methodName == o,
                            ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>.Empty.Add(
                                pointsTos => { return true; },
                                new[] { TaintedTargetValue.Return }.ToImmutableHashSet())))
                        ?.ToImmutableDictionary()
                    ?? ImmutableDictionary<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    ImmutableDictionary<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>>.Empty,
                passerMethods:
                    ImmutableDictionary<MethodMapper, ImmutableHashSet<(string, string)>>.Empty,
                taintConstantArray: false);
            builder.Add(metadata);
        }

        /// <summary>
        /// Add SourceInfos which needs extra PointsToAnalysis checks or ValueContentAnalysis checks and specifies the tainted targets explicitly for each check.
        /// The tainted targets can be parameter names of the method, or the return value.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="fullTypeName"></param>
        /// <param name="taintedProperties"></param>
        /// <param name="taintedMethodsNeedsPointsToAnalysis">Specify the check functions and tainted targets for methods which only need PointsToAnalysis check.</param>
        /// <param name="taintedMethodsNeedsValueContentAnalysis">Specify the check functions and tainted targets for methods which need ValueContentAnalysis check.</param>
        /// <param name="taintConstantArray"></param>
        public static void AddSourceInfoSpecifyingTaintedTargets(
        this PooledHashSet<SourceInfo> builder,
        string fullTypeName,
        bool isInterface,
        string[] taintedProperties,
        IEnumerable<(MethodMapper methodMapper, (PointsToCheck pointsToCheck, string[] taintedTargets)[] pointsToChecksAndTargets)> taintedMethodsNeedsPointsToAnalysis,
        IEnumerable<(MethodMapper methodMapper, (ValueContentCheck valueContentCheck, string[] taintedTargets)[] valueContentChecksAndTargets)> taintedMethodsNeedsValueContentAnalysis,
        IEnumerable<(MethodMapper methodMapper, (string str, string taintedTargets)[] valueContentChecksAndTargets)> passerMethods,
        bool taintConstantArray = false)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                taintedMethodsNeedsPointsToAnalysis:
                    taintedMethodsNeedsPointsToAnalysis
                        ?.Select(o => new KeyValuePair<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>>(
                            o.methodMapper,
                            o.pointsToChecksAndTargets
                                ?.Select(s => new KeyValuePair<PointsToCheck, ImmutableHashSet<string>>(s.pointsToCheck, s.taintedTargets.ToImmutableHashSet()))
                                ?.ToImmutableDictionary()
                            ?? ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>.Empty))
                        ?.ToImmutableDictionary()
                    ?? ImmutableDictionary<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    taintedMethodsNeedsValueContentAnalysis
                        ?.Select(o => new KeyValuePair<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>>(
                            o.methodMapper,
                            o.valueContentChecksAndTargets
                                ?.Select(s => new KeyValuePair<ValueContentCheck, ImmutableHashSet<string>>(s.valueContentCheck, s.taintedTargets.ToImmutableHashSet()))
                                ?.ToImmutableDictionary()
                            ?? ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>.Empty))
                        ?.ToImmutableDictionary()
                    ?? ImmutableDictionary<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>>.Empty,
                passerMethods:
                    passerMethods
                        ?.Select(o => new KeyValuePair<MethodMapper, ImmutableHashSet<(string, string)>>(
                            o.methodMapper,
                            o.valueContentChecksAndTargets
                                ?.ToImmutableHashSet()
                            ?? ImmutableHashSet<(string, string)>.Empty))
                        ?.ToImmutableDictionary()
                    ?? ImmutableDictionary<MethodMapper, ImmutableHashSet<(string, string)>>.Empty,
                taintConstantArray: taintConstantArray);
            builder.Add(metadata);
        }

        /// <summary>
        /// Add SourceInfos which needs PointsToAnalysis checks or ValueContentAnalysis checks and each check taints return value by default.
        /// </summary>
        public static void AddSourceInfo(
        this PooledHashSet<SourceInfo> builder,
        string fullTypeName,
        bool isInterface,
        string[] taintedProperties,
        IEnumerable<(MethodMapper methodMapper, PointsToCheck[] pointsToChecks)> taintedMethodsNeedsPointsToAnalysis,
        IEnumerable<(MethodMapper methodMapper, ValueContentCheck[] valueContentChecks)> taintedMethodsNeedsValueContentAnalysis,
        bool taintConstantArray = false)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                taintedMethodsNeedsPointsToAnalysis:
                    taintedMethodsNeedsPointsToAnalysis
                        ?.Select(o => new KeyValuePair<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>>(
                            o.methodMapper,
                            o.pointsToChecks
                                ?.Select(s => new KeyValuePair<PointsToCheck, ImmutableHashSet<string>>(s, new[] { TaintedTargetValue.Return }.ToImmutableHashSet()))
                                ?.ToImmutableDictionary()
                            ?? ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>.Empty))
                        ?.ToImmutableDictionary()
                    ?? ImmutableDictionary<MethodMapper, ImmutableDictionary<PointsToCheck, ImmutableHashSet<string>>>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    taintedMethodsNeedsValueContentAnalysis
                        ?.Select(o => new KeyValuePair<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>>(
                            o.methodMapper,
                            o.valueContentChecks
                                ?.Select(s => new KeyValuePair<ValueContentCheck, ImmutableHashSet<string>>(s, new[] { TaintedTargetValue.Return }.ToImmutableHashSet()))
                                ?.ToImmutableDictionary()
                            ?? ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>.Empty))
                        ?.ToImmutableDictionary()
                    ?? ImmutableDictionary<MethodMapper, ImmutableDictionary<ValueContentCheck, ImmutableHashSet<string>>>.Empty,
                passerMethods:
                    ImmutableDictionary<MethodMapper, ImmutableHashSet<(string, string)>>.Empty,
                taintConstantArray: taintConstantArray);
            builder.Add(metadata);
        }

        // Just to make hardcoding SanitizerInfos more convenient.
        public static void AddSanitizerInfo(
            this PooledHashSet<SanitizerInfo> builder,
            string fullTypeName,
            bool isInterface,
            bool isConstructorSanitizing,
            string[] sanitizingMethods,
            string[] sanitizingInstanceMethods = null)
        {
            SanitizerInfo info = new SanitizerInfo(
                fullTypeName,
                isInterface: isInterface,
                isConstructorSanitizing: isConstructorSanitizing,
                sanitizingMethods: sanitizingMethods?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                sanitizingInstanceMethods: sanitizingInstanceMethods?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty);
            builder.Add(info);
        }
    }
}