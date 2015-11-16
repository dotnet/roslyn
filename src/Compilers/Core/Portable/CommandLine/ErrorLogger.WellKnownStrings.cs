// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal partial class ErrorLogger
    {
        /// <summary>
        /// Contains well known property strings for error log file.
        /// </summary>
        private static class WellKnownStrings
        {
            public const string OutputFormatVersion = "version";
            public const string ToolFileVersion = "fileVersion";
            public const string ToolAssemblyVersion = "version";
            public const string ToolInfo = "toolInfo";
            public const string ToolName = "name";
            public const string RunLogs = "runLogs";
            public const string Issues = "issues";
            public const string DiagnosticId = "ruleId";
            public const string Locations = "locations";
            public const string ShortMessage = "shortMessage";
            public const string FullMessage = "fullMessage";
            public const string IsSuppressedInSource = "isSuppressedInSource";
            public const string Properties = "properties";
            public const string Location = "analysisTarget";
            public const string LocationSyntaxTreeUri = "uri";
            public const string LocationSpanInfo = "region";
            public const string LocationSpanStartLine = "startLine";
            public const string LocationSpanStartColumn = "startColumn";
            public const string LocationSpanEndLine = "endLine";
            public const string LocationSpanEndColumn = "endColumn";

            // Diagnostic/DiagnosticDescriptor properties which are not defined in our log format.
            public const string Category = "category";
            public const string Title = "title";
            public const string HelpLink = "helpLink";
            public const string CustomTags = "customTags";
            public const string IsEnabledByDefault = "isEnabledByDefault";
            public const string DefaultSeverity = "defaultSeverity";
            public const string Severity = "severity";
            public const string WarningLevel = "warningLevel";
            public const string CustomProperties = "customProperties";

            public const string None = "<None>";
        }
    }
}
