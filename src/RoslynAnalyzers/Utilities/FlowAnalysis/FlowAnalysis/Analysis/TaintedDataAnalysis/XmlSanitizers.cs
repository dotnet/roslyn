// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class XmlSanitizers
    {
        /// <summary>
        /// <see cref="SanitizerInfo"/>s for XML injection sanitizers.
        /// </summary>
        public static ImmutableHashSet<SanitizerInfo> SanitizerInfos { get; }

        static XmlSanitizers()
        {
            var builder = PooledHashSet<SanitizerInfo>.GetInstance();

            builder.AddSanitizerInfo(
                WellKnownTypeNames.MicrosoftSecurityApplicationAntiXss,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "XmlAttributeEncode",
                    "XmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.MicrosoftSecurityApplicationEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "XmlAttributeEncode",
                    "XmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebSecurityAntiXssAntiXssEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "XmlAttributeEncode",
                    "XmlEncode",
                });

            // Consider SecurityElement.Escape().

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
