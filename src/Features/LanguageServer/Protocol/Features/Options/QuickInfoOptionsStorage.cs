// Licensed to the .NET Foundation under one or more agreements.
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
            "dotnet_quick_info_options_show_remarks_in_quick_info", QuickInfoOptions.Default.ShowRemarksInQuickInfo);

        public static readonly Option2<bool> IncludeNavigationHintsInQuickInfo = new(
            "dotnet_quick_info_options_include_navigation_hints_in_quick_info", QuickInfoOptions.Default.IncludeNavigationHintsInQuickInfo);
    }
}
