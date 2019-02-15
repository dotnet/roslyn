// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

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
            var sinkInfosBuilder = PooledHashSet<SinkInfo>.GetInstance();

            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypes.SystemReflectionAssemblyFullName,
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
            sinkInfosBuilder.AddSinkInfo(
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
            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypes.SystemWindowsAssemblyPart,
                SinkKind.Dll,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("Load", new[] { "assemblyStream" } ),
                });

            SinkInfos = sinkInfosBuilder.ToImmutableAndFree();
        }
    }
}
