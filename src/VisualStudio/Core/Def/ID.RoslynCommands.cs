// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static partial class ID
    {
        /// <summary>
        /// Commands using the old Roslyn command set GUID.
        /// </summary>
        public static class RoslynCommands
        {
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
            public const int AddSuppressions = 0x011d;
            public const int AddSuppressionsInSource = 0x011f;
            public const int AddSuppressionsInSuppressionFile = 0x0120;
            public const int RemoveSuppressions = 0x0121;

            public const int GoToImplementation = 0x0200;
            public const int GoToBase = 0x0037;
        }
    }
}
