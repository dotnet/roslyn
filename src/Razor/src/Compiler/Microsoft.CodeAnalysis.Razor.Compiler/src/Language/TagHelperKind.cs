// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public enum TagHelperKind : byte
{
    ITagHelper,
    ViewComponent,

    // Component kinds.
    Component,
    ChildContent,
    EventHandler,
    Bind,
    Key,
    Ref,
    Splat,
    FormName,
    RenderMode // Last component kind. If another is added, update TagHelperKindExtensions.IsAnyComponentKind.
}
