// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        /// <param name="nullableOption">Whether Nullable Reference Types feature is enabled globally</param>
        /// <param name="specificDiagnosticOptions">How specific diagnostics should be reported</param>
        /// <returns>A diagnostic updated to reflect the options, or null if it has been filtered out</returns>
        internal static Diagnostic? Filter(
            Diagnostic d,
            int warningLevelOption,
            NullableContextOptions nullableOption,
            ReportDiagnostic generalDiagnosticOption,
            IDictionary<string, ReportDiagnostic> specificDiagnosticOptions,
            SyntaxTreeOptionsProvider? syntaxTreeOptions,
            CancellationToken cancellationToken)
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
                reportAction = GetDiagnosticReport(ErrorFacts.GetSeverity(ErrorCode.WRN_ALinkWarn),
                    d.IsEnabledByDefault,
                    d.Code,
                    CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn),
                    ErrorFacts.GetWarningLevel(ErrorCode.WRN_ALinkWarn),
                    d.Location,
                    warningLevelOption,
                    nullableOption,
                    generalDiagnosticOption,
                    specificDiagnosticOptions,
                    syntaxTreeOptions,
                    cancellationToken,
                    out hasPragmaSuppression);
            }
            else
            {
                reportAction = GetDiagnosticReport(d.Severity,
                    d.IsEnabledByDefault,
                    d.Code,
                    d.Id,
                    d.WarningLevel,
                    d.Location,
                    warningLevelOption,
                    nullableOption,
                    generalDiagnosticOption,
                    specificDiagnosticOptions,
                    syntaxTreeOptions,
                    cancellationToken,
                    out hasPragmaSuppression);
            }

            if (hasPragmaSuppression)
            {
                d = d.WithIsSuppressed(true);
            }

            return d.WithReportDiagnostic(reportAction);
        }

        /// <summary>
        /// Take a warning and return the final disposition of the given warning,
        /// based on both command line options and pragmas. The diagnostic options
        /// have precedence in the following order:
        ///     1. Warning level
        ///     2. Command line options (/nowarn, /warnaserror)
        ///     3. Editor config options (syntax tree level)
        ///     4. Global analyzer config options (compilation level)
        ///     5. Global warning level
        ///
        /// Pragmas are considered separately. If a diagnostic would not otherwise
        /// be suppressed, but is suppressed by a pragma, <paramref name="hasPragmaSuppression"/>
        /// is true but the diagnostic is not reported as suppressed.
        /// </summary> 
        internal static ReportDiagnostic GetDiagnosticReport(
            DiagnosticSeverity severity,
            bool isEnabledByDefault,
            int errorCode,
            string id,
            int diagnosticWarningLevel,
            Location location,
            int warningLevelOption,
            NullableContextOptions nullableOption,
            ReportDiagnostic generalDiagnosticOption,
            IDictionary<string, ReportDiagnostic> specificDiagnosticOptions,
            SyntaxTreeOptionsProvider? syntaxTreeOptions,
            CancellationToken cancellationToken,
            out bool hasPragmaSuppression)
        {
            hasPragmaSuppression = false;

            Debug.Assert(location.SourceTree is null || location.SourceTree is CSharpSyntaxTree);
            var tree = location.SourceTree as CSharpSyntaxTree;
            var position = location.SourceSpan.Start;

            bool isNullableFlowAnalysisWarning = ErrorFacts.NullableWarnings.Contains(id);
            if (isNullableFlowAnalysisWarning)
            {
                Syntax.NullableContextState.State? warningsState = tree?.GetNullableContextState(position).WarningsState;
                var nullableWarningsEnabled = warningsState switch
                {
                    Syntax.NullableContextState.State.Enabled => true,
                    Syntax.NullableContextState.State.Disabled => false,
                    Syntax.NullableContextState.State.ExplicitlyRestored => nullableOption.WarningsEnabled(),
                    Syntax.NullableContextState.State.Unknown =>
                        // IsGeneratedCode may be slow, check the option first:
                        nullableOption.WarningsEnabled() && tree?.IsGeneratedCode(syntaxTreeOptions, cancellationToken) != true,
                    null => nullableOption.WarningsEnabled(),
                    _ => throw ExceptionUtilities.UnexpectedValue(warningsState)
                };

                if (!nullableWarningsEnabled)
                {
                    return ReportDiagnostic.Suppress;
                }
            }

            // 1. Warning level
            if (diagnosticWarningLevel > warningLevelOption)  // honor the warning level
            {
                return ReportDiagnostic.Suppress;
            }

            ReportDiagnostic report;
            bool isSpecified = false;
            bool specifiedWarnAsErrorMinus = false;

            if (specificDiagnosticOptions.TryGetValue(id, out report))
            {
                // 2. Command line options (/nowarn, /warnaserror)
                isSpecified = true;

                // 'ReportDiagnostic.Default' is added to SpecificDiagnosticOptions for "/warnaserror-:DiagnosticId",
                if (report == ReportDiagnostic.Default)
                {
                    specifiedWarnAsErrorMinus = true;
                }
            }

            // Apply syntax tree options, if applicable.
            if (syntaxTreeOptions != null &&
                (!isSpecified || specifiedWarnAsErrorMinus))
            {
                // 3. Editor config options (syntax tree level)
                // 4. Global analyzer config options (compilation level)
                // Do not apply config options if it is bumping a warning to an error and "/warnaserror-:DiagnosticId" was specified on the command line.
                if ((tree != null && syntaxTreeOptions.TryGetDiagnosticValue(tree, id, cancellationToken, out var reportFromSyntaxTreeOptions) ||
                    syntaxTreeOptions.TryGetGlobalDiagnosticValue(id, cancellationToken, out reportFromSyntaxTreeOptions)) &&
                    !(specifiedWarnAsErrorMinus && severity == DiagnosticSeverity.Warning && reportFromSyntaxTreeOptions == ReportDiagnostic.Error))
                {
                    isSpecified = true;
                    report = reportFromSyntaxTreeOptions;

                    // '/warnaserror' should promote warnings configured in analyzer config to error.
                    if (!specifiedWarnAsErrorMinus && report == ReportDiagnostic.Warn && generalDiagnosticOption == ReportDiagnostic.Error)
                    {
                        report = ReportDiagnostic.Error;
                    }
                }
            }

            if (!isSpecified)
            {
                report = isEnabledByDefault ? ReportDiagnostic.Default : ReportDiagnostic.Suppress;
            }

            if (report == ReportDiagnostic.Suppress)
            {
                return ReportDiagnostic.Suppress;
            }

            // If location.SourceTree is available, check out pragmas
            var pragmaWarningState = tree?.GetPragmaDirectiveWarningState(id, position) ?? Syntax.PragmaWarningState.Default;
            if (pragmaWarningState == Syntax.PragmaWarningState.Disabled)
            {
                hasPragmaSuppression = true;
            }

            // NOTE: this may be removed as part of https://github.com/dotnet/roslyn/issues/36550
            if (pragmaWarningState == Syntax.PragmaWarningState.Enabled)
            {
                switch (report)
                {
                    case ReportDiagnostic.Error:
                    case ReportDiagnostic.Hidden:
                    case ReportDiagnostic.Info:
                    case ReportDiagnostic.Warn:
                        // No need to adjust the current report state, it already means "enabled"
                        return report;

                    case ReportDiagnostic.Suppress:
                        // Enable the warning
                        return ReportDiagnostic.Default;

                    case ReportDiagnostic.Default:
                        if (generalDiagnosticOption == ReportDiagnostic.Error && promoteToAnError())
                        {
                            return ReportDiagnostic.Error;
                        }

                        return ReportDiagnostic.Default;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(report);
                }
            }
            else if (report == ReportDiagnostic.Suppress) // check options (/nowarn)
            {
                return ReportDiagnostic.Suppress;
            }

            // 5. Global options
            // Unless specific warning options are defined (/warnaserror[+|-]:<n> or /nowarn:<n>, 
            // follow the global option (/warnaserror[+|-] or /nowarn).
            if (report == ReportDiagnostic.Default)
            {
                switch (generalDiagnosticOption)
                {
                    case ReportDiagnostic.Error:
                        if (promoteToAnError())
                        {
                            return ReportDiagnostic.Error;
                        }
                        break;
                    case ReportDiagnostic.Suppress:
                        // When doing suppress-all-warnings, don't lower severity for anything other than warning and info.
                        // We shouldn't suppress hidden diagnostics here because then features that use hidden diagnostics to
                        // display a lightbulb would stop working if someone has suppress-all-warnings (/nowarn) specified in their project.
                        if (severity == DiagnosticSeverity.Warning || severity == DiagnosticSeverity.Info)
                        {
                            report = ReportDiagnostic.Suppress;
                            isSpecified = true;
                        }
                        break;
                }
            }

            if (!isSpecified && errorCode == (int)ErrorCode.WRN_Experimental)
            {
                // Special handling for [Experimental] warning (treat as error severity by default)
                Debug.Assert(isEnabledByDefault);
                Debug.Assert(!specifiedWarnAsErrorMinus);
                report = ReportDiagnostic.Error;
            }

            return report;

            bool promoteToAnError()
            {
                Debug.Assert(report == ReportDiagnostic.Default);
                Debug.Assert(generalDiagnosticOption == ReportDiagnostic.Error);

                // If we've been asked to do warn-as-error then don't raise severity for anything below warning (info or hidden).
                return severity == DiagnosticSeverity.Warning &&
                       // In the case where /warnaserror+ is followed by /warnaserror-:<n> on the command line,
                       // do not promote the warning specified in <n> to an error.
                       !isSpecified;

            }
        }
    }
}
