// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class FilePathInjectionSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data file canonicalization sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static FilePathInjectionSinks()
        {
            PooledHashSet<SinkInfo> builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIODirectory,
                SinkKind.FilePathInjection,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "Exists", new[] { "path" } ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFile,
                SinkKind.FilePathInjection,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "AppendAllLines", new[] { "path" } ),
                    ( "AppendAllLinesAsync", new[] { "path" } ),
                    ( "AppendAllText", new[] { "path" } ),
                    ( "AppendAllTextAsync", new[] { "path" } ),
                    ( "AppendText", new[] { "path" } ),
                    ( "Copy", new[] { "sourceFileName", "destFileName" } ),
                    ( "Create", new[] { "path" } ),
                    ( "CreateText", new[] { "path" } ),
                    ( "Delete", new[] { "path" } ),
                    ( "Exists", new[] { "path" } ),
                    ( "Move", new[] { "sourceFileName", "destFileName" } ),
                    ( "Open", new[] { "path" } ),
                    ( "OpenRead", new[] { "path" } ),
                    ( "OpenText", new[] { "path" } ),
                    ( "OpenWrite", new[] { "path" } ),
                    ( "ReadAllBytes", new[] { "path" } ),
                    ( "ReadAllBytesAsync", new[] { "path" } ),
                    ( "ReadAllLines", new[] { "path" } ),
                    ( "ReadAllLinesAsync", new[] { "path" } ),
                    ( "ReadAllText", new[] { "path" } ),
                    ( "ReadAllTextAsync", new[] { "path" } ),
                    ( "ReadLines", new[] { "path" } ),
                    ( "WriteAllBytes", new[] { "path" } ),
                    ( "WriteAllBytesAsync", new[] { "path" } ),
                    ( "WriteAllLines", new[] { "path" } ),
                    ( "WriteAllLinesAsync", new[] { "path" } ),
                    ( "WriteAllText", new[] { "path" } ),
                    ( "WriteAllTextAsync", new[] { "path" } ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFileInfo,
                SinkKind.FilePathInjection,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "CopyTo", new[] { "destFileName" } ),
                    ( "MoveTo", new[] { "destFileName" } ),
                    ( "Replace", new[] { "destinationFileName"} ),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
