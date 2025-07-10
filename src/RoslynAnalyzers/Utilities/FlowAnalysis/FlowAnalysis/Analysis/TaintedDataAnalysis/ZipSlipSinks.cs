// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class ZipSlipSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data zip slip sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static ZipSlipSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOCompressionZipFileExtensions,
                SinkKind.ZipSlip,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("ExtractToFile", new[] { "destinationFileName" } ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFile,
                SinkKind.ZipSlip,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("Open", new[] { "path" } ),
                    ("OpenWrite", ["path"] ),
                    ("OpenCreate", ["path"] ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemDirectoryDirectoryEntry,
                SinkKind.ZipSlip,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    (".ctor", new[] { "path", "adsObject" } ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFileStream,
                SinkKind.ZipSlip,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new[] {
                    "path",
                },
                sinkMethodParameters: null);
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFileInfo,
                SinkKind.ZipSlip,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: new[] {
                    "fileName",
                },
                sinkMethodParameters: null);

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
