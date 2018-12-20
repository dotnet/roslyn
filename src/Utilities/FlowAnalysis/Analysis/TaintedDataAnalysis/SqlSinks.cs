// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

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
            var sinkInfosBuilder = PooledHashSet<SinkInfo>.GetInstance();

            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypes.SystemDataIDbCommand,
                SinkKind.Sql,
                isInterface: true,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new string[] {
                    "CommandText",
                },
                sinkMethodParameters: null);

            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypes.SystemDataIDataAdapter,
                SinkKind.Sql,
                isInterface: true,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: null);

            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypes.SystemWebUIWebControlsSqlDataSource,
                SinkKind.Sql,
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

            SinkInfos = sinkInfosBuilder.ToImmutableAndFree();
        }
    }
}
