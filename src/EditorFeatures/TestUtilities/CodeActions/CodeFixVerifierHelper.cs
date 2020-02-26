// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    internal static class CodeFixVerifierHelper
    {
        public static void VerifyStandardProperties(DiagnosticAnalyzer analyzer, bool verifyHelpLink = false)
        {
            VerifyMessageTitle(analyzer);
            VerifyMessageDescription(analyzer);

            if (verifyHelpLink)
            {
                VerifyMessageHelpLinkUri(analyzer);
            }
        }

        private static void VerifyMessageTitle(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    // The title only displayed for rule configuration
                    continue;
                }

                Assert.NotEqual("", descriptor.Title?.ToString() ?? "");
            }
        }

        private static void VerifyMessageDescription(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                if (ShouldSkipMessageDescriptionVerification(descriptor))
                {
                    continue;
                }

                Assert.NotEqual("", descriptor.MessageFormat?.ToString() ?? "");
            }

            return;

            // Local function
            static bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
            {
                if (descriptor.CustomTags.Contains(WellKnownDiagnosticTags.NotConfigurable))
                {
                    if (!descriptor.IsEnabledByDefault || descriptor.DefaultSeverity == DiagnosticSeverity.Hidden)
                    {
                        // The message only displayed if either enabled and not hidden, or configurable
                        return true;
                    }
                }

                return false;
            }
        }

        private static void VerifyMessageHelpLinkUri(DiagnosticAnalyzer analyzer)
        {
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                Assert.NotEqual("", descriptor.HelpLinkUri ?? "");
            }
        }
    }
}
