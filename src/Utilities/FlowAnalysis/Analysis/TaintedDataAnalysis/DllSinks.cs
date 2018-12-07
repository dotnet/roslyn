// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class DllSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data Dll sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static DllSinks()
        {
            ImmutableHashSet<SinkInfo>.Builder sinkInfosBuilder = ImmutableHashSet.CreateBuilder<SinkInfo>();

            sinkInfosBuilder.AddSink(
                WellKnownTypes.SystemReflectionAssembly,
                SinkKind.Dll,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("LoadFrom", new[] { "assemblyFile" } ),
                    ("Load", new[] { "assemblyString", "rawAssembly" } ),
                    ("LoadFile", new[] { "partialName" } ),
                    ("LoadModule", new[] { "moduleName" } ),
                    ("UnsafeLoadFrom", new[] { "assemblyFile" } ),
                });
            sinkInfosBuilder.AddSink(
                WellKnownTypes.SystemAppDomain,
                SinkKind.Dll,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("ExecuteAssembly", new[] { "assemblyFile" } ),
                    ("ExecuteAssemblyByName", new[] { "assemblyName" } ),
                    ("Load", new[] { "rawAssembly", "assemblyRef", "assemblyString", } ),
                });
            sinkInfosBuilder.AddSink(
                WellKnownTypes.SystemWindowsAssemblyPart,
                SinkKind.Dll,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("Load", new[] { "assemblyStream" } ),
                });

            SinkInfos = sinkInfosBuilder.ToImmutable();
        }
    }
}
