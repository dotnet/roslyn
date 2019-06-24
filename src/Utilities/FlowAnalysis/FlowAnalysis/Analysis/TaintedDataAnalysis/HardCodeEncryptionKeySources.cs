// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardCodeEncryptionKeySources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for hardcoded key tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static HardCodeEncryptionKeySources()
        {
            var builder = PooledHashSet<SourceInfo>.GetInstance();

            builder.AddSourceInfo(
                WellKnownTypeNames.SystemConvert,
                isInterface: false,
                taintedProperties: null,
                taintedMethods: new[] {
                    "FromBase64String",
                });
            builder.AddSourceInfo(
                WellKnownTypeNames.SystemByte,
                isInterface: false,
                taintedProperties: null,
                taintedMethods: null,
                taintArray: true);

            SourceInfos = builder.ToImmutableAndFree();
        }
    }
}
