// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal enum TagHelperProducerKind : ushort
{
    Default = 0,
    Bind,
    Component,
    EventHandler,
    FormName,
    Key,
    Ref,
    RenderMode,
    Splat,
    MvcViewComponent,
    Mvc1_X_ViewComponent,
    Mvc2_X_ViewComponent
}
