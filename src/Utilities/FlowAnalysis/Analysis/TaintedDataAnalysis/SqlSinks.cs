// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class SqlSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data SQL sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static SqlSinks()
        {
            ImmutableHashSet<SinkInfo>.Builder sinkInfosBuilder = ImmutableHashSet.CreateBuilder<SinkInfo>();

            AddSink(
                sinkInfosBuilder,
                WellKnownTypes.SystemDataIDbCommand,
                isInterface: true,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new string[] {
                    "CommandText",
                },
                sinkMethodParameters: null);

            AddSink(
                sinkInfosBuilder,
                WellKnownTypes.SystemDataIDataAdapter,
                isInterface: true,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: null);

            AddSink(
                sinkInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsSqlDataSource,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new string[] {
                    "ConnectionString",
                    "DeleteCommand",
                    "InsertCommand",
                    "SelectCommand",
                    "UpdateCommand",
                },
                sinkMethodParameters: null);

            SinkInfos = sinkInfosBuilder.ToImmutable();
        }

        private static void AddSink(
            ImmutableHashSet<SinkInfo>.Builder builder,
            string fullTypeName,
            bool isInterface,
            bool isAnyStringParameterInConstructorASink,
            IEnumerable<string> sinkProperties,
            IDictionary<string, IEnumerable<string>> sinkMethodParameters)
        {
            SinkInfo sinkInfo = new SinkInfo(
                fullTypeName,
                SinkKind.Sql,
                isInterface,
                isAnyStringParameterInConstructorASink,
                sinkProperties:
                    sinkProperties != null
                        ? sinkProperties.ToImmutableHashSet()
                        : ImmutableHashSet<string>.Empty,
                sinkMethodParameters:
                    sinkMethodParameters != null
                        ? sinkMethodParameters
                             .Select(kvp => new KeyValuePair<string, ImmutableHashSet<string>>(kvp.Key, kvp.Value.ToImmutableHashSet()))
                             .ToImmutableDictionary()
                        : ImmutableDictionary<string, ImmutableHashSet<string>>.Empty);
            builder.Add(sinkInfo);
        }
    }
}
