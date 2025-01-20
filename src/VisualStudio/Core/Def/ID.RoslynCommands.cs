// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices;

internal static partial class ID
{
    /// <summary>
    /// Commands using the old Roslyn command set GUID.
    /// </summary>
    public static class RoslynCommands
    {
        // Analyzer node context menu command IDs
        public const int AnalyzerContextMenu = 0x0103;
        public const int AddAnalyzer = 0x0106;
        public const int AnalyzerFolderContextMenu = 0x0107;
        public const int RemoveAnalyzer = 0x0109;
        public const int OpenRuleSet = 0x010a;
        public const int ProjectAddAnalyzer = 0x010b;
        public const int ProjectContextAddAnalyzer = 0x010c;
        public const int ReferencesContextAddAnalyzer = 0x010d;
        public const int DiagnosticContextMenu = 0x010e;
        public const int DiagnosticSeverityGroup = 0x010f;
        public const int SetSeverityError = 0x0110;
        public const int SetSeverityWarning = 0x0111;
        public const int SetSeverityInfo = 0x0112;
        public const int SetSeverityHidden = 0x0113;
        public const int SetSeverityNone = 0x0114;
        public const int OpenDiagnosticHelpLink = 0x0116;
        public const int SetActiveRuleSet = 0x0118;
        public const int SetSeverityDefault = 0x011b;

        // Analyze and Code Cleanup menu IDs
        public const int AnalysisScopeDefault = 0x0131;
        public const int AnalysisScopeCurrentDocument = 0x0132;
        public const int AnalysisScopeOpenDocuments = 0x0133;
        public const int AnalysisScopeEntireSolution = 0x0134;
        public const int AnalysisScopeNone = 0x0137;

        public const int GoToImplementation = 0x0200;

        public const int RunCodeAnalysisForProject = 0x0201;
        public const int RemoveUnusedReferences = 0x0202;
        public const int GoToValueTrackingWindow = 0x0203;
        public const int SyncNamespaces = 0x0204;

        // Document Outline
        public const int DocumentOutlineToolbar = 0x300;
        public const int DocumentOutlineExpandAll = 0x311;
        public const int DocumentOutlineCollapseAll = 0x312;
        public const int DocumentOutlineSortByName = 0x313;
        public const int DocumentOutlineSortByOrder = 0x314;
        public const int DocumentOutlineSortByType = 0x315;
        public const int DocumentOutlineToolbarGroup = 0x350;
    }
}
