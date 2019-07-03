// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

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
                WellKnownTypeNames.SystemIOFileFullName,
                SinkKind.ZipSlip,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ("Open", new[] { "path" } ),
                    ("OpenWrite", new[] { "path" } ),
                    ("OpenCreate", new[] { "path" } ),
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
