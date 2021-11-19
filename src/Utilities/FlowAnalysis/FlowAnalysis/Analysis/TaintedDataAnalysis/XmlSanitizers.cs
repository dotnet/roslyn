// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;

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
