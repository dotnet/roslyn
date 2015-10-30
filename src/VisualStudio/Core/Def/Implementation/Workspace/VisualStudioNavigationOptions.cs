// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal static class VisualStudioNavigationOptions
    {
        public const string FeatureName = "VisualStudioNavigation";

        public static readonly PerLanguageOption<bool> NavigateToObjectBrowser = new PerLanguageOption<bool>(FeatureName, "NavigateToObjectBrowser", defaultValue: false);
    }
}
