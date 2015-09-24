// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal static class SuppressionHelpers
    {
        public static bool CanBeSuppressed(Diagnostic diagnostic)
        {
            if (diagnostic.Location.Kind != LocationKind.SourceFile || diagnostic.IsSuppressed || IsNotConfigurableDiagnostic(diagnostic))
            {
                // Don't offer suppression fixes for:
                //   1. Diagnostics without a source location.
                //   2. Diagnostics with a source suppression.
                //   3. Non-configurable diagnostics (includes compiler errors).
                return false;
            }

            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Hidden:
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

        public static bool HasCustomTag(IEnumerable<string> customTags, string tagToFind)
        {
            return customTags.Any(c => CultureInfo.InvariantCulture.CompareInfo.Compare(c, tagToFind) == 0);
        }
    }
}
