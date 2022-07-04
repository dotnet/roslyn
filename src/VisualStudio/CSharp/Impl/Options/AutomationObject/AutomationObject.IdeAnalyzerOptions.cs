// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int ReportInvalidPlaceholdersInStringDotFormatCalls
        {
            get { return GetBooleanOption(IdeAnalyzerOptionsStorage.ReportInvalidPlaceholdersInStringDotFormatCalls); }
            set { SetBooleanOption(IdeAnalyzerOptionsStorage.ReportInvalidPlaceholdersInStringDotFormatCalls, value); }
        }

        public int ReportInvalidRegexPatterns
        {
            get { return GetBooleanOption(IdeAnalyzerOptionsStorage.ReportInvalidRegexPatterns); }
            set { SetBooleanOption(IdeAnalyzerOptionsStorage.ReportInvalidRegexPatterns, value); }
        }

        public int ReportInvalidJsonPatterns
        {
            get { return GetBooleanOption(IdeAnalyzerOptionsStorage.ReportInvalidJsonPatterns); }
            set { SetBooleanOption(IdeAnalyzerOptionsStorage.ReportInvalidJsonPatterns, value); }
        }
    }
}
