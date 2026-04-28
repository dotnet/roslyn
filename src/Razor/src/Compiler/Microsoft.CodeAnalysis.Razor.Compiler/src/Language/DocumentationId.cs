// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal enum DocumentationId
{
    // New values must always be placed at the end of the enum
    // and Last must be updated to point to the final value.
    // Do NOT insert items or change the order below.
    BindTagHelper_Fallback,
    BindTagHelper_Fallback_Event,
    BindTagHelper_Fallback_Format,
    BindTagHelper_Element,
    BindTagHelper_Element_After,
    BindTagHelper_Element_Culture,
    BindTagHelper_Element_Event,
    BindTagHelper_Element_Format,
    BindTagHelper_Element_Get,
    BindTagHelper_Element_Set,
    BindTagHelper_Component,
    ChildContentParameterName,
    ChildContentParameterName_TopLevel,
    ComponentTypeParameter,
    EventHandlerTagHelper,
    EventHandlerTagHelper_PreventDefault,
    EventHandlerTagHelper_StopPropagation,
    KeyTagHelper,
    RefTagHelper,
    SplatTagHelper,
    RenderModeTagHelper,
    FormNameTagHelper,

    Last = FormNameTagHelper
}
