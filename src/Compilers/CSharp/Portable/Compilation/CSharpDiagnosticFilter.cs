// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Applies C#-specific modification and filtering of <see cref="Diagnostic"/>s.
    /// </summary>
    internal static class CSharpDiagnosticFilter
    {
        private static readonly ErrorCode[] s_alinkWarnings = { ErrorCode.WRN_ConflictingMachineAssembly,
                                                              ErrorCode.WRN_RefCultureMismatch,
                                                              ErrorCode.WRN_InvalidVersionFormat };

        /// <summary>
        /// Modifies an input <see cref="Diagnostic"/> per the given options. For example, the
        /// severity may be escalated, or the <see cref="Diagnostic"/> may be filtered out entirely
        /// (by returning null).
        /// </summary>
        /// <param name="d">The input diagnostic</param>
        /// <param name="warningLevelOption">The maximum warning level to allow. Diagnostics with a higher warning level will be filtered out.</param>
        /// <param name="generalDiagnosticOption">How warning diagnostics should be reported</param>
        /// <param name="specificDiagnosticOptions">How specific diagnostics should be reported</param>
        /// <returns>A diagnostic updated to reflect the options, or null if it has been filtered out</returns>
        public static Diagnostic Filter(
            Diagnostic d,
            int warningLevelOption,
            ReportDiagnostic generalDiagnosticOption,
            IDictionary<string, ReportDiagnostic> specificDiagnosticOptions,
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<string, ReportDiagnostic>> perTreeDiagnosticOptions)
        {
            if (d == null)
            {
                return d;
            }
            else if (d.IsNotConfigurable())
            {
                if (d.IsEnabledByDefault)
                {
                    // Enabled NotConfigurable should always be reported as it is.
                    return d;
                }
                else
                {
                    // Disabled NotConfigurable should never be reported.
                    return null;
                }
            }
            else if (d.Severity == InternalDiagnosticSeverity.Void)
            {
                return null;
            }

            //In the native compiler, all warnings originating from alink.dll were issued
            //under the id WRN_ALinkWarn - 1607. If a customer used nowarn:1607 they would get
            //none of those warnings. In Roslyn, we've given each of these warnings their
            //own number, so that they may be configured independently. To preserve compatibility
            //if a user has specifically configured 1607 and we are reporting one of the alink warnings, use
            //the configuration specified for 1607. As implemented, this could result in customers 
            //specifying warnaserror:1607 and getting a message saying "warning as error CS8012..."
            //We don't permit configuring 1607 and independently configuring the new warnings.
            ReportDiagnostic reportAction;
            bool hasPragmaSuppression;
            if (s_alinkWarnings.Contains((ErrorCode)d.Code) &&
                specificDiagnosticOptions.Keys.Contains(CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn)))
            {
                reportAction = GetDiagnosticReport(
                    ErrorFacts.GetSeverity(ErrorCode.WRN_ALinkWarn),
                    d.IsEnabledByDefault,
                    CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn),
                    ErrorFacts.GetWarningLevel(ErrorCode.WRN_ALinkWarn),
                    d.Location as Location,
                    d.Category,
                    warningLevelOption,
                    generalDiagnosticOption,
                    specificDiagnosticOptions,
                    perTreeDiagnosticOptions,
                    out hasPragmaSuppression);
            }
            else
            {
                reportAction = GetDiagnosticReport(
                    d.Severity,
                    d.IsEnabledByDefault,
                    d.Id,
                    d.WarningLevel,
                    d.Location as Location,
                    d.Category,
                    warningLevelOption,
                    generalDiagnosticOption,
                    specificDiagnosticOptions,
                    perTreeDiagnosticOptions,
                    out hasPragmaSuppression);
            }

            if (hasPragmaSuppression)
            {
                d = d.WithIsSuppressed(true);
            }

            return d.WithReportDiagnostic(reportAction);
        }

        /// <summary>
        /// Take a warning and return the final deposition of the given warning,
        /// based on both command line options and pragmas. The behavior is as follows:
        ///     1. If the diagnostic warning level is greater than the current compiler
        ///        warning level, the warning is suppressed and <paramref
        ///        name="hasPragmaSuppression"/> is false.
        ///     2. If the warning is suppressed in the specific or per-tree specific
        ///        diagnostic options, the warning is suppressed and <paramref
        ///        name="hasPragmaSuppression"/> is false.
        ///     3. If the warning is not otherwise suppressed and is suppressed by a pragma,
        ///        the warning is suppressed and <paramref name="hasPragmaSuppression"/> is true.
        ///     5. Otherwise, the <paramref name="perTreeDiagnosticOptions"/> take precedence in
        ///        deciding the warning setting, followed by
        ///        <paramref name="specificDiagnosticOptions"/>. If the warning is
        ///        <see cref="ReportDiagnostic.Default"/> after this calculation,
        ///        <paramref name="generalDiagnosticOption"/> options are applied.
        /// </summary> 
        internal static ReportDiagnostic GetDiagnosticReport(
            DiagnosticSeverity severity,
            bool isEnabledByDefault,
            string id,
            int diagnosticWarningLevel,
            Location location,
            string category,
            int warningLevelOption,
            ReportDiagnostic generalDiagnosticOption,
            IDictionary<string, ReportDiagnostic> specificDiagnosticOptions,
            ImmutableDictionary<SyntaxTree, ImmutableDictionary<string, ReportDiagnostic>> perTreeDiagnosticOptions,
            out bool hasPragmaSuppression)
        {
            hasPragmaSuppression = false;

            // honor the warning level
            if (diagnosticWarningLevel > warningLevelOption)
            {
                return ReportDiagnostic.Suppress;
            }

            // Read options (e.g., /nowarn or /warnaserror)
            ReportDiagnostic report = ReportDiagnostic.Default;
            var isSpecified = specificDiagnosticOptions.TryGetValue(id, out report);
            if (!isSpecified)
            {
                report = isEnabledByDefault ? ReportDiagnostic.Default : ReportDiagnostic.Suppress;
            }

            // If location is available, look for tree-specific options
            if (location?.SourceTree != null &&
                perTreeDiagnosticOptions.TryGetValue(location.SourceTree, out ImmutableDictionary<string, ReportDiagnostic> idToReport) &&
                idToReport.TryGetValue(id, out ReportDiagnostic perTreeSetting))
            {
                report = perTreeSetting;
            }

            // Return if the diagnostic is already suppressed, as pragmas
            // can't suppress it further and global options do not apply
            if (report == ReportDiagnostic.Suppress)
            {
                return ReportDiagnostic.Suppress;
            }

            // Check pragmas
            if (location?.SourceTree?.GetPragmaDirectiveWarningState(id, location.SourceSpan.Start) == ReportDiagnostic.Suppress)
            {
                hasPragmaSuppression = true;
            }

            // Unless specific warning options are defined (/warnaserror[+|-]:<n> or /nowarn:<n>, 
            // follow the global option (/warnaserror[+|-] or /nowarn).
            if (report == ReportDiagnostic.Default)
            {
                switch (generalDiagnosticOption)
                {
                    case ReportDiagnostic.Error:
                        // If we've been asked to do warn-as-error then don't raise severity for anything below warning (info or hidden).
                        if (severity == DiagnosticSeverity.Warning)
                        {
                            // In the case where /warnaserror+ is followed by /warnaserror-:<n> on the command line,
                            // do not promote the warning specified in <n> to an error.
                            if (!isSpecified && (report == ReportDiagnostic.Default))
                            {
                                return ReportDiagnostic.Error;
                            }
                        }
                        break;
                    case ReportDiagnostic.Suppress:
                        // When doing suppress-all-warnings, don't lower severity for anything other than warning and info.
                        // We shouldn't suppress hidden diagnostics here because then features that use hidden diagnostics to
                        // display a lightbulb would stop working if someone has suppress-all-warnings (/nowarn) specified in their project.
                        if (severity == DiagnosticSeverity.Warning || severity == DiagnosticSeverity.Info)
                        {
                            return ReportDiagnostic.Suppress;
                        }
                        break;
                    default:
                        break;
                }
            }

            return report;
        }
    }
}
