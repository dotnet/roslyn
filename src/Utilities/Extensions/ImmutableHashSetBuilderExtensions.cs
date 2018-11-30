// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class ImmutableHashSetBuilderExtensions
    {
        // Just to make hardcoding SinkInfos more convenient.
        public static void AddSink(
            this ImmutableHashSet<SinkInfo>.Builder builder,
            string fullTypeName,
            SinkKind sinkKind,
            bool isInterface,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string> sinkProperties,
            IEnumerable<(string Method, string[] Parameters)> sinkMethodParameters)
        {
            SinkInfo sinkInfo = new SinkInfo(
                fullTypeName,
                sinkKind,
                isInterface,
                isAnyStringParameterInConstructorASink,
                sinkProperties:
                    sinkProperties != null
                        ? sinkProperties.ToImmutableHashSet()
                        : ImmutableHashSet<string>.Empty,
                sinkMethodParameters:
                    sinkMethodParameters != null
                        ? sinkMethodParameters
                             .Select(o => new KeyValuePair<string, ImmutableHashSet<string>>(o.Method, o.Parameters.ToImmutableHashSet()))
                             .ToImmutableDictionary()
                        : ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
            builder.Add(sinkInfo);
        }
    }
}
