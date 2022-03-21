// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodPresentationOptionsStorage
    {
        private const string FeatureName = "ExtractMethodOptions";

        public static readonly PerLanguageOption2<bool> AllowBestEffort = new(FeatureName, "AllowBestEffort", defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Allow Best Effort"));
    }
}
