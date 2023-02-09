﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal static class QuickInfoOptionsStorage
    {
        public static QuickInfoOptions GetQuickInfoOptions(this IGlobalOptionService globalOptions, string language)
          => new()
          {
              ShowRemarksInQuickInfo = globalOptions.GetOption(ShowRemarksInQuickInfo, language),
              IncludeNavigationHintsInQuickInfo = globalOptions.GetOption(IncludeNavigationHintsInQuickInfo),
          };

        public static readonly PerLanguageOption2<bool> ShowRemarksInQuickInfo = new(
            "QuickInfoOptions_ShowRemarksInQuickInfo", QuickInfoOptions.Default.ShowRemarksInQuickInfo);

        public static readonly Option2<bool> IncludeNavigationHintsInQuickInfo = new(
            "QuickInfoOptions_IncludeNavigationHintsInQuickInfo", QuickInfoOptions.Default.IncludeNavigationHintsInQuickInfo);
    }
}
