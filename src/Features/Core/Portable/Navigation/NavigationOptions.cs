// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal static class NavigationOptions
    {
        public const string FeatureName = "Navigation";

        public static readonly Option<bool> UsePreviewTab = new Option<bool>(FeatureName, "UsePreviewTab", defaultValue: false);
    }
}
