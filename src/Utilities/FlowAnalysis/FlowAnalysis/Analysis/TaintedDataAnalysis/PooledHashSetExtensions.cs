// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

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
                taintedMethodsNeedPointsToAnalysis:
                    taintedMethods
                    ?.Select(o => new KeyValuePair<string, IsInvocationTaintedWithPointsToAnalysis>(o, (IEnumerable<PointsToAbstractValue> argumentValueContents) => { return true; }))
                    ?.ToImmutableDictionary(StringComparer.Ordinal)
                    ?? ImmutableDictionary<string, IsInvocationTaintedWithPointsToAnalysis>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    ImmutableDictionary<string, IsInvocationTaintedWithValueContentAnalysis>.Empty,
                taintConstantArray: false);
            builder.Add(metadata);
        }

        // Just to make hardcoding SourceInfos more convenient.
        public static void AddSourceInfo(
        this PooledHashSet<SourceInfo> builder,
        string fullTypeName,
        bool isInterface,
        string[] taintedProperties,
        IEnumerable<(string Method, IsInvocationTaintedWithPointsToAnalysis pointsToCheck)> taintedMethodsNeedPointsToAnalysis,
        IEnumerable<(string Method, IsInvocationTaintedWithValueContentAnalysis valueContentCheck)> taintedMethodsNeedsValueContentAnalysis,
        bool taintConstantArray = false)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties?.ToImmutableHashSet(StringComparer.Ordinal)
                    ?? ImmutableHashSet<string>.Empty,
                taintedMethodsNeedPointsToAnalysis:
                    taintedMethodsNeedPointsToAnalysis
                        ?.Select(o => new KeyValuePair<string, IsInvocationTaintedWithPointsToAnalysis>(o.Method, o.pointsToCheck))
                        ?.ToImmutableDictionary(StringComparer.Ordinal)
                    ?? ImmutableDictionary<string, IsInvocationTaintedWithPointsToAnalysis>.Empty,
                taintedMethodsNeedsValueContentAnalysis:
                    taintedMethodsNeedsValueContentAnalysis
                        ?.Select(o => new KeyValuePair<string, IsInvocationTaintedWithValueContentAnalysis>(o.Method, o.valueContentCheck))
                        ?.ToImmutableDictionary(StringComparer.Ordinal)
                    ?? ImmutableDictionary<string, IsInvocationTaintedWithValueContentAnalysis>.Empty,
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