using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Applies C#-specific modification and filtering of <see cref="Diagnostic"/>s.
    /// </summary>
    public static class CSharpDiagnosticFilter
    {
        private static readonly ErrorCode[] AlinkWarnings = { ErrorCode.WRN_ConflictingMachineAssembly,
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
        public static Diagnostic Filter(Diagnostic d, int warningLevelOption, ReportDiagnostic generalDiagnosticOption, IDictionary<string, ReportDiagnostic> specificDiagnosticOptions)
        {
            if (d == null) return d;
            switch (d.Severity)
            {
                case DiagnosticSeverity.Error:
                    return d;
                case InternalDiagnosticSeverity.Void:
                    return null;
                default:
                    break;
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
            if (AlinkWarnings.Contains((ErrorCode)d.Code) &&
                specificDiagnosticOptions.Keys.Contains(CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn)))
            {
                reportAction = GetDiagnosticReport(ErrorFacts.GetSeverity(ErrorCode.WRN_ALinkWarn),
                    d.IsEnabledByDefault,
                    CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn),
                    ErrorFacts.GetWarningLevel(ErrorCode.WRN_ALinkWarn),
                    d.Location as Location,
                    d.Category,
                    warningLevelOption,
                    generalDiagnosticOption,
                    specificDiagnosticOptions);
            }
            else
            {
                reportAction = GetDiagnosticReport(d.Severity, d.IsEnabledByDefault, d.Id, d.WarningLevel, d.Location as Location, d.Category, warningLevelOption, generalDiagnosticOption, specificDiagnosticOptions);
            }

            return d.WithReportDiagnostic(reportAction);
        }

        // Take a warning and return the final deposition of the given warning,
        // based on both command line options and pragmas
        private static ReportDiagnostic GetDiagnosticReport(DiagnosticSeverity severity, bool isEnabledByDefault, string id, int diagnosticWarningLevel, Location location, string category, int warningLevelOption, ReportDiagnostic generalDiagnosticOption, IDictionary<string, ReportDiagnostic> specificDiagnosticOptions)
        {
            switch (severity)
            {
                case InternalDiagnosticSeverity.Void:
                    // If this is a deleted diagnostic, suppress it.
                    return ReportDiagnostic.Suppress;
                case DiagnosticSeverity.Hidden:
                    // Compiler diagnostics cannot have severity Hidden, but user generated diagnostics can.
                    Debug.Assert(category != Diagnostic.CompilerDiagnosticCategory);
                    break;
                case DiagnosticSeverity.Info:
                    if (category == Diagnostic.CompilerDiagnosticCategory)
                    {
                        // Don't modify compiler generated Info diagnostics.
                        return ReportDiagnostic.Default;
                    }
                    break;
                case DiagnosticSeverity.Warning:
                    // Process warnings below.
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(severity);
            }

            // Read options (e.g., /nowarn or /warnaserror)
            ReportDiagnostic report = ReportDiagnostic.Default;
            if (!specificDiagnosticOptions.TryGetValue(id, out report))
            {
                report = isEnabledByDefault ? ReportDiagnostic.Default : ReportDiagnostic.Suppress;
            }

            // Compute if the reporting should be suppressed.
            if (diagnosticWarningLevel > warningLevelOption  // honor the warning level
                || report == ReportDiagnostic.Suppress)                // check options (/nowarn)
            {
                return ReportDiagnostic.Suppress;
            }

            // If location is available, check out pragmas
            if (location != null &&
                location.SourceTree != null &&
                ((SyntaxTree)location.SourceTree).GetPragmaDirectiveWarningState(id, location.SourceSpan.Start) == ReportDiagnostic.Suppress)
            {
                return ReportDiagnostic.Suppress;
            }

            // Unless specific warning options are defined (/warnaserror[+|-]:<n> or /nowarn:<n>, 
            // follow the global option (/warnaserror[+|-] or /nowarn).
            if (report == ReportDiagnostic.Default)
            {
                // If we've been asked to do warn-as-error then don't raise severity for anything below warning (info or hidden).
                // When doing suppress-all-warnings, don't lower severity for anything other than warning and info
                switch (generalDiagnosticOption)
                {
                    case ReportDiagnostic.Error:
                        if (severity == DiagnosticSeverity.Warning)
                        {
                            return ReportDiagnostic.Error;
                        }
                        break;
                    case ReportDiagnostic.Suppress:
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
