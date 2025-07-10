// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

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
                WellKnownTypeNames.SystemReflectionAssembly,
                SinkKind.Dll,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("LoadFrom", new[] { "assemblyFile" } ),
                    ("Load", ["assemblyString", "rawAssembly"] ),
                    ("LoadFile", ["partialName"] ),
                    ("LoadModule", ["moduleName"] ),
                    ("UnsafeLoadFrom", ["assemblyFile"] ),
                });
            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypeNames.SystemAppDomain,
                SinkKind.Dll,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("ExecuteAssembly", new[] { "assemblyFile" } ),
                    ("ExecuteAssemblyByName", ["assemblyName"] ),
                    ("Load", ["rawAssembly", "assemblyRef", "assemblyString",] ),
                });
            sinkInfosBuilder.AddSinkInfo(
                WellKnownTypeNames.SystemWindowsAssemblyPart,
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
