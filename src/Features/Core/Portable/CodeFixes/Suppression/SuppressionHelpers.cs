// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal static class SuppressionHelpers
    {
        private const string SynthesizedExternalSourceDiagnosticTag = "SynthesizedExternalSourceDiagnostic";
        public static readonly string[] SynthesizedExternalSourceDiagnosticCustomTags = new string[] { SynthesizedExternalSourceDiagnosticTag };

        public static bool CanBeSuppressed(Diagnostic diagnostic)
        {
            return CanBeSuppressedOrUnsuppressed(diagnostic, checkCanBeSuppressed: true);
        }

        public static bool CanBeUnsuppressed(Diagnostic diagnostic)
        {
            return CanBeSuppressedOrUnsuppressed(diagnostic, checkCanBeSuppressed: false);
        }

        private static bool CanBeSuppressedOrUnsuppressed(Diagnostic diagnostic, bool checkCanBeSuppressed)
        {
            if (diagnostic.IsSuppressed == checkCanBeSuppressed ||
                IsNotConfigurableDiagnostic(diagnostic))
            {
                // Don't offer suppression fixes for:
                //   1. Diagnostics with a source suppression.
                //   2. Non-configurable diagnostics (includes compiler errors).
                return false;
            }

            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    // Hidden diagnostics should never show up.
                    return false;

                case DiagnosticSeverity.Error:
                case DiagnosticSeverity.Warning:
                case DiagnosticSeverity.Info:
                    // We allow suppressions for all the remaining configurable, non-hidden diagnostics.
                    // Note that compiler errors are not configurable by default, so only analyzer errors are suppressable.
                    return true;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public static bool IsNotConfigurableDiagnostic(DiagnosticData diagnostic)
        {
            return HasCustomTag(diagnostic.CustomTags, WellKnownDiagnosticTags.NotConfigurable);
        }

        public static bool IsNotConfigurableDiagnostic(Diagnostic diagnostic)
        {
            return HasCustomTag(diagnostic.Descriptor.CustomTags, WellKnownDiagnosticTags.NotConfigurable);
        }

        public static bool IsCompilerDiagnostic(DiagnosticData diagnostic)
        {
            return HasCustomTag(diagnostic.CustomTags, WellKnownDiagnosticTags.Compiler);
        }

        public static bool IsCompilerDiagnostic(Diagnostic diagnostic)
        {
            return HasCustomTag(diagnostic.Descriptor.CustomTags, WellKnownDiagnosticTags.Compiler);
        }

        public static bool IsSynthesizedExternalSourceDiagnostic(DiagnosticData diagnostic)
        {
            return HasCustomTag(diagnostic.CustomTags, SynthesizedExternalSourceDiagnosticTag);
        }

        public static bool IsSynthesizedExternalSourceDiagnostic(Diagnostic diagnostic)
        {
            return HasCustomTag(diagnostic.Descriptor.CustomTags, SynthesizedExternalSourceDiagnosticTag);
        }

        public static bool HasCustomTag(IEnumerable<string> customTags, string tagToFind)
        {
            return customTags != null && customTags.Any(c => CultureInfo.InvariantCulture.CompareInfo.Compare(c, tagToFind) == 0);
        }
    }
}
