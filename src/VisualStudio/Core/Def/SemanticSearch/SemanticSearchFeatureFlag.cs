// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices;

internal static class SemanticSearchFeatureFlag
{
    public static readonly Option2<bool> Enabled = new("visual_studio_enable_semantic_search", defaultValue: false);

    /// <summary>
    /// Context id that indicates that Semantic Search feature is enabled.
    /// TODO: remove, workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1985204
    /// </summary>
    public const string UIContextId = "D5801818-6009-40BE-9204-8897C23D2856";
}
