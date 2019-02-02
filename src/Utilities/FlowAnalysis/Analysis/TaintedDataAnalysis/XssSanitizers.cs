// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

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
                WellKnownTypes.MicrosoftSecurityApplicationAntiXss,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.MicrosoftSecurityApplicationAntiXssEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.MicrosoftSecurityApplicationEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.MicrosoftSecurityApplicationUnicodeCharacterEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemIDisposable,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "Dispose",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebHttpServerUtility,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebHttpServerUtilityBase,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebHttpServerUtilityWrapper,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebHttpUtility,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebSecurityAntiXssAntiXssEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebSecurityAntiXssUnicodeCharacterEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                    "XmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebUIHtmlTextWriter,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "WriteHtmlAttributeEncode",
                });
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebUIWebControlsBoundField,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: new[] {
                    "HtmlEncode",
                    "SupportsHtmlEncode",
                },
                sanitizingMethods: null);
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebUIWebControlsCheckBoxField,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: new[] {
                    "HtmlEncode",
                    "SupportsHtmlEncode",
                },
                sanitizingMethods: null);
            builder.AddSanitizerInfo(
                WellKnownTypes.SystemWebUtilHttpEncoder,
                isInterface: false,
                isConstructorSanitizing: false,
                sanitizingProperties: null,
                sanitizingMethods: new[] {
                    "HtmlAttributeEncode",
                    "HtmlEncode",
                });

            builder.AddRange(PrimitiveTypeConverterSanitizers.SanitizerInfos);

            SanitizerInfos = builder.ToImmutableAndFree();
        }
    }
}
