// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class InformationDisclosureSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for information disclosure tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static InformationDisclosureSources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemException,
                isInterface: false,
                taintedProperties: new[] {
                    "Message",
                    "StackTrace",
                },
                taintedMethods: new[] {
                    "ToString",
                });

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
