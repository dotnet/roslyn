// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class XssSanitizers
    {
        /// <summary>
        /// <see cref="SanitizerInfo"/>s for primitive type conversion tainted data sanitizers.
        /// </summary>
        public static ImmutableHashSet<SanitizerInfo> SanitizerInfos { get; }

        static XssSanitizers()
        {
            var builder = PooledHashSet<SanitizerInfo>.GetInstance();

            builder.AddSanitizerInfo(
                WellKnownTypeNames.MicrosoftSecurityApplicationAntiXss,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.MicrosoftSecurityApplicationAntiXssEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.MicrosoftSecurityApplicationEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.MicrosoftSecurityApplicationUnicodeCharacterEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemIDisposable,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "Dispose",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebHttpServerUtility,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new (MethodMatcher, (string taintedArgument, string sanitizedArgument)[])[] {
                    (
                        (methodName, arguments) => methodName == "HtmlEncode" && arguments.Length == 1,
                        new[] { ("s", TaintedTargetValue.Return) }
                    ),
                    (
                        (methodName, arguments) => methodName == "HtmlEncode" && arguments.Length == 2,
                        new[] { ("s", "output") }
                    ),
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebHttpServerUtilityBase,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebHttpServerUtilityWrapper,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebHttpUtility,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebSecurityAntiXssAntiXssEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebSecurityAntiXssUnicodeCharacterEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebUIHtmlTextWriter,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "WriteHtmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypeNames.SystemWebUtilHttpEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                });

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
