// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class RegexSinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data process command sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static RegexSinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemTextRegularExpressionsRegex,
                SinkKind.Regex,
                isInterface: false,
                isAnyStringParameterInConstructorASink: true,
                sinkProperties: null,
                sinkMethodParameters: new[] {
                    ( "IsMatch", new[] { "pattern" }),
                    ( "Match", new[] { "pattern" }),
                    ( "Matches", new[] { "pattern" }),
                    ( "Replace", new[] { "pattern" }),
                    ( "Split", new[] { "pattern" }),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
