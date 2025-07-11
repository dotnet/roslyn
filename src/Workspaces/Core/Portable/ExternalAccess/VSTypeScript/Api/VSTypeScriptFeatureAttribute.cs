// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal static class VSTypeScriptFeatureAttribute
{
    public const string RenameTracking = FeatureAttribute.RenameTracking;
    public const string Snippets = FeatureAttribute.Snippets;
    public const string Workspace = FeatureAttribute.Workspace;
    public const string SolutionCrawlerLegacy = FeatureAttribute.SolutionCrawlerLegacy;
}
