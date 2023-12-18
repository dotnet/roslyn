// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.DocumentHighlighting;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int HighlightRelatedRegexComponentsUnderCursor
        {
            get { return GetBooleanOption(HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor); }
            set { SetBooleanOption(HighlightingOptionsStorage.HighlightRelatedRegexComponentsUnderCursor, value); }
        }

        public int HighlightRelatedJsonComponentsUnderCursor
        {
            get { return GetBooleanOption(HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor); }
            set { SetBooleanOption(HighlightingOptionsStorage.HighlightRelatedJsonComponentsUnderCursor, value); }
        }
    }
}
