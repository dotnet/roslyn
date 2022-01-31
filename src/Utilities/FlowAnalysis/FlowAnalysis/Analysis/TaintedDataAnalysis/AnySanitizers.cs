// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

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
                sanitizingMethods: default(string[]),
                sanitizingInstanceMethods: new[] {
                    "Clear",
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
