// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

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
                    ( "AppendAllLinesAsync", ["path"] ),
                    ( "AppendAllText", ["path"] ),
                    ( "AppendAllTextAsync", ["path"] ),
                    ( "AppendText", ["path"] ),
                    ( "Copy", ["sourceFileName", "destFileName"] ),
                    ( "Create", ["path"] ),
                    ( "CreateText", ["path"] ),
                    ( "Delete", ["path"] ),
                    ( "Exists", ["path"] ),
                    ( "Move", ["sourceFileName", "destFileName"] ),
                    ( "Open", ["path"] ),
                    ( "OpenRead", ["path"] ),
                    ( "OpenText", ["path"] ),
                    ( "OpenWrite", ["path"] ),
                    ( "ReadAllBytes", ["path"] ),
                    ( "ReadAllBytesAsync", ["path"] ),
                    ( "ReadAllLines", ["path"] ),
                    ( "ReadAllLinesAsync", ["path"] ),
                    ( "ReadAllText", ["path"] ),
                    ( "ReadAllTextAsync", ["path"] ),
                    ( "ReadLines", ["path"] ),
                    ( "WriteAllBytes", ["path"] ),
                    ( "WriteAllBytesAsync", ["path"] ),
                    ( "WriteAllLines", ["path"] ),
                    ( "WriteAllLinesAsync", ["path"] ),
                    ( "WriteAllText", ["path"] ),
                    ( "WriteAllTextAsync", ["path"] ),
                });
            builder.AddSinkInfo(
                WellKnownTypeNames.SystemIOFileInfo,
                SinkKind.FilePathInjection,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "CopyTo", new[] { "destFileName" } ),
                    ( "MoveTo", ["destFileName"] ),
                    ( "Replace", ["destinationFileName"] ),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
