// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices;

internal static class SemanticSearchFeatureFlag
{
    public static readonly Option2<bool> PromptEnabled = new("visual_studio_enable_semantic_search_prompt", defaultValue: false);
}
