// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class ProcessCommandSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data process command sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static ProcessCommandSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemDiagnosticsProcess,
                SinkKind.ProcessCommand,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "Start", new[] { "fileName", "arguments", } ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemDiagnosticsProcessStartInfo,
                SinkKind.ProcessCommand,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new[] {
                    "ArgumentList",
                    "Arguments",
                    "FileName",
                },
                sinkMethodParameters: null);

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
