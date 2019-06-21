// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class ZipSlipSanitizers
    {
        /// <summary>
        /// <see cref="SanitizerInfo"/>s for zip slip tainted data sanitizers.
        /// </summary>
        public static ImmutableHashSet<SanitizerInfo> SanitizerInfos { get; }

        static ZipSlipSanitizers()
        {
            var builder = PooledHashSet<SanitizerInfo>.GetInstance();

            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemIOPath,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "GetFileName",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemString,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "Substring",
                },
                sanitizingInstanceMethods: new[] {
                    "StartsWith",
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
