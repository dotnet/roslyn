// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram
{
    internal static class ConvertProgramAnalysis
    {
        public static bool CanOfferUseProgramMain(CodeStyleOption2<bool> option, bool forAnalyzer)
        {
            var userPrefersProgramMain = option.Value == false;
            var analyzerDisabled = option.Notification.Severity == ReportDiagnostic.Suppress;
            var forRefactoring = !forAnalyzer;

            // If the user likes Program.Main, then we offer to conver to Program.Main from the diagnostic analyzer.
            // If the user prefers Ttop-level-statements then we offer to use Program.Main from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersProgramMain == forAnalyzer || (forRefactoring && analyzerDisabled);
            return canOffer;
        }
    }
}
