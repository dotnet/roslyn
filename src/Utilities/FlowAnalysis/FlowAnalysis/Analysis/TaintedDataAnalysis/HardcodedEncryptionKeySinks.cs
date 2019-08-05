// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class HardcodedEncryptionKeySinks
    {
        /// <summary>
        /// <see cref="SinkInfo"/>s for tainted data process symmetric algorithm sinks.
        /// </summary>
        public static ImmutableHashSet<SinkInfo> SinkInfos { get; }

        static HardcodedEncryptionKeySinks()
        {
            var builder = PooledHashSet<SinkInfo>.GetInstance();

            builder.AddSinkInfo(
                WellKnownTypeNames.SystemSecurityCryptographySymmetricAlgorithm,
                SinkKind.HardcodedEncryptionKey,
                isInterface: false,
                isAnyStringParameterInConstructorASink: false,
                sinkProperties: new[] {
                    "Key",
                },
                sinkMethodParameters: new[] {
                    ( "CreateEncryptor", new[] { "rgbKey" }),
                    ( "CreateDecryptor", new[] { "rgbKey" }),
                });

            SinkInfos = builder.ToImmutableAndFree();
        }
    }
}
