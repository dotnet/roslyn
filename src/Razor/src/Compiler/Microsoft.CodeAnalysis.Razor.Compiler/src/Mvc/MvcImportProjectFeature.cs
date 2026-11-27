// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal sealed class MvcImportProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
{
    internal const string ImportsFileName = "_ViewImports.cshtml";

    private static readonly DefaultImportProjectItem s_defaultImport = new($"Default MVC 3.0 imports ({ImportsFileName})", @"
@using global::System
@using global::System.Collections.Generic
@using global::System.Linq
@using global::System.Threading.Tasks
@using global::Microsoft.AspNetCore.Mvc
@using global::Microsoft.AspNetCore.Mvc.Rendering
@using global::Microsoft.AspNetCore.Mvc.ViewFeatures
@inject global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TModel> Html
@inject global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json
@inject global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component
@inject global::Microsoft.AspNetCore.Mvc.IUrlHelper Url
@inject global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider
@addTagHelper global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper, Microsoft.AspNetCore.Mvc.Razor
@addTagHelper global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.HeadTagHelper, Microsoft.AspNetCore.Mvc.Razor
@addTagHelper global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.BodyTagHelper, Microsoft.AspNetCore.Mvc.Razor
");

    public void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports)
    {
        ArgHelper.ThrowIfNull(projectItem);

        // Don't add MVC imports for a component
        if (projectItem.FileKind.IsComponent())
        {
            return;
        }

        AddDefaultDirectivesImport(ref imports);

        // We add hierarchical imports second so any default directive imports can be overridden.
        AddHierarchicalImports(projectItem, ref imports);
    }

    // Internal for testing
    internal static void AddDefaultDirectivesImport(ref PooledArrayBuilder<RazorProjectItem> imports)
    {
        imports.Add(s_defaultImport);
    }

    // Internal for testing
    internal void AddHierarchicalImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports)
    {
        // We want items in descending order. FindHierarchicalItems returns items in ascending order.
        var importProjectItems = ProjectEngine.FileSystem.FindHierarchicalItems(projectItem.FilePath, ImportsFileName);
        imports.AddRange(importProjectItems);
    }
}
