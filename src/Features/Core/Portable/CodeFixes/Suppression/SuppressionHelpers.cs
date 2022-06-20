// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal static class SuppressionHelpers
    {
        private const string SynthesizedExternalSourceDiagnosticTag = "SynthesizedExternalSourceDiagnostic";
        public static readonly ImmutableArray<string> SynthesizedExternalSourceDiagnosticCustomTags = ImmutableArray.Create(SynthesizedExternalSourceDiagnosticTag);

        public static bool CanBeSuppressed(Diagnostic diagnostic)
            => CanBeSuppressedOrUnsuppressed(diagnostic, checkCanBeSuppressed: true);

        public static bool CanBeSuppressedWithAttribute(Diagnostic diagnostic)
        {
            if (IsCompilerDiagnostic(diagnostic))
            {
                return false;
            }

            // IDE0055 cannot be suppressed with an attribute because the formatter implementation only adheres to
            // pragma-based source suppressions.
            return diagnostic.Id != IDEDiagnosticIds.FormattingDiagnosticId;
        }

        public static bool CanBeUnsuppressed(Diagnostic diagnostic)
            => CanBeSuppressedOrUnsuppressed(diagnostic, checkCanBeSuppressed: false);

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
                    throw ExceptionUtilities.UnexpectedValue(diagnostic.Severity);
            }
        }

        public static bool IsNotConfigurableDiagnostic(DiagnosticData diagnostic)
            => HasCustomTag(diagnostic.CustomTags, WellKnownDiagnosticTags.NotConfigurable);

        public static bool IsNotConfigurableDiagnostic(Diagnostic diagnostic)
            => HasCustomTag(diagnostic.Descriptor.ImmutableCustomTags(), WellKnownDiagnosticTags.NotConfigurable);

        public static bool IsCompilerDiagnostic(DiagnosticData diagnostic)
            => HasCustomTag(diagnostic.CustomTags, WellKnownDiagnosticTags.Compiler);

        public static bool IsCompilerDiagnostic(Diagnostic diagnostic)
            => HasCustomTag(diagnostic.Descriptor.ImmutableCustomTags(), WellKnownDiagnosticTags.Compiler);

        public static bool IsSynthesizedExternalSourceDiagnostic(DiagnosticData diagnostic)
            => HasCustomTag(diagnostic.CustomTags, SynthesizedExternalSourceDiagnosticTag);

        public static bool IsSynthesizedExternalSourceDiagnostic(Diagnostic diagnostic)
            => HasCustomTag(diagnostic.Descriptor.ImmutableCustomTags(), SynthesizedExternalSourceDiagnosticTag);

        public static bool HasCustomTag(ImmutableArray<string> customTags, string tagToFind)
            => customTags.Any(static (c, tagToFind) => CultureInfo.InvariantCulture.CompareInfo.Compare(c, tagToFind) == 0, tagToFind);
    }
}
