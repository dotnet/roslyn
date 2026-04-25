// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

public class TestTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
{
    private TagHelperCollection? _tagHelpers;

    public void SetTagHelpers(TagHelperCollection tagHelpers)
    {
        _tagHelpers = tagHelpers;
    }

    public TagHelperCollection GetTagHelpers(CancellationToken cancellationToken = default)
        => _tagHelpers ?? [];
}
