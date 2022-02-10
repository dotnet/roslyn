// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{
    internal static class ColumnDefinitions
    {
        private const string Prefix = "editorconfig.";

        internal static class Analyzer
        {
            private const string AnalyzerPrefix = "analyzer.";
            public const string Category = Prefix + AnalyzerPrefix + "categoryname";
            public const string Enabled = Prefix + AnalyzerPrefix + "enabledname";
            public const string Description = Prefix + AnalyzerPrefix + "descriptionname";
            public const string Id = Prefix + AnalyzerPrefix + "idname";
            public const string Severity = Prefix + AnalyzerPrefix + "severityname";
            public const string Title = Prefix + AnalyzerPrefix + "titlename";
            public const string Location = Prefix + AnalyzerPrefix + "location";

        }

        internal static class CodeStyle
        {
            private const string CodeStylePrefix = "codestyle.";
            public const string Category = Prefix + CodeStylePrefix + "categoryname";
            public const string Description = Prefix + CodeStylePrefix + "descriptionname";
            public const string Value = Prefix + CodeStylePrefix + "valuename";
            public const string Severity = Prefix + CodeStylePrefix + "severityname";
            public const string Location = Prefix + CodeStylePrefix + "location";
        }

        internal static class NamingStyle
        {
            private const string NamingStylePrefix = "namingstyle.";
            public const string Type = Prefix + NamingStylePrefix + "type";
            public const string Style = Prefix + NamingStylePrefix + "style";
            public const string Severity = Prefix + NamingStylePrefix + "severityname";
            public const string Location = Prefix + NamingStylePrefix + "location";
        }

        internal static class Whitespace
        {
            private const string FormattingPrefix = "whitespace.";
            public const string Category = Prefix + FormattingPrefix + "categoryname";
            public const string Description = Prefix + FormattingPrefix + "descriptionname";
            public const string Value = Prefix + FormattingPrefix + "valuename";
            public const string Location = Prefix + FormattingPrefix + "location";

        }
    }
}
