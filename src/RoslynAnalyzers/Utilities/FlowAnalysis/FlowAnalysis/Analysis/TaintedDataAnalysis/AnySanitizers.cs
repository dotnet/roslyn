// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class AnySanitizers
    {
        /// <summary>
        /// <see cref="SanitizerInfo"/>s for any tainted data sanitizers.
        /// </summary>
        public static ImmutableHashSet<SanitizerInfo> SanitizerInfos { get; }

        static AnySanitizers()
        {
            var builder = PooledHashSet<SanitizerInfo>.GetInstance();

            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemTextStringBuilder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: (string[]?)null,
                sanitizingInstanceMethods: new[] {
                    "Clear",
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
