// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal abstract partial class DocumentationDescriptor : IEquatable<DocumentationDescriptor>
{
    public static readonly DocumentationDescriptor BindTagHelper_Fallback = new SimpleDescriptor(DocumentationId.BindTagHelper_Fallback);
    public static readonly DocumentationDescriptor BindTagHelper_Fallback_Event = new SimpleDescriptor(DocumentationId.BindTagHelper_Fallback_Event);
    public static readonly DocumentationDescriptor BindTagHelper_Fallback_Format = new SimpleDescriptor(DocumentationId.BindTagHelper_Fallback_Format);
    public static readonly DocumentationDescriptor BindTagHelper_Element = new SimpleDescriptor(DocumentationId.BindTagHelper_Element);
    public static readonly DocumentationDescriptor BindTagHelper_Element_After = new SimpleDescriptor(DocumentationId.BindTagHelper_Element_After);
    public static readonly DocumentationDescriptor BindTagHelper_Element_Culture = new SimpleDescriptor(DocumentationId.BindTagHelper_Element_Culture);
    public static readonly DocumentationDescriptor BindTagHelper_Element_Event = new SimpleDescriptor(DocumentationId.BindTagHelper_Element_Event);
    public static readonly DocumentationDescriptor BindTagHelper_Element_Format = new SimpleDescriptor(DocumentationId.BindTagHelper_Element_Format);
    public static readonly DocumentationDescriptor BindTagHelper_Element_Get = new SimpleDescriptor(DocumentationId.BindTagHelper_Element_Get);
    public static readonly DocumentationDescriptor BindTagHelper_Element_Set = new SimpleDescriptor(DocumentationId.BindTagHelper_Element_Set);
    public static readonly DocumentationDescriptor BindTagHelper_Component = new SimpleDescriptor(DocumentationId.BindTagHelper_Component);
    public static readonly DocumentationDescriptor ChildContentParameterName = new SimpleDescriptor(DocumentationId.ChildContentParameterName);
    public static readonly DocumentationDescriptor ChildContentParameterName_TopLevel = new SimpleDescriptor(DocumentationId.ChildContentParameterName_TopLevel);
    public static readonly DocumentationDescriptor ComponentTypeParameter = new SimpleDescriptor(DocumentationId.ComponentTypeParameter);
    public static readonly DocumentationDescriptor EventHandlerTagHelper = new SimpleDescriptor(DocumentationId.EventHandlerTagHelper);
    public static readonly DocumentationDescriptor EventHandlerTagHelper_PreventDefault = new SimpleDescriptor(DocumentationId.EventHandlerTagHelper_PreventDefault);
    public static readonly DocumentationDescriptor EventHandlerTagHelper_StopPropagation = new SimpleDescriptor(DocumentationId.EventHandlerTagHelper_StopPropagation);
    public static readonly DocumentationDescriptor KeyTagHelper = new SimpleDescriptor(DocumentationId.KeyTagHelper);
    public static readonly DocumentationDescriptor RefTagHelper = new SimpleDescriptor(DocumentationId.RefTagHelper);
    public static readonly DocumentationDescriptor SplatTagHelper = new SimpleDescriptor(DocumentationId.SplatTagHelper);
    public static readonly DocumentationDescriptor RenderModeTagHelper = new SimpleDescriptor(DocumentationId.RenderModeTagHelper);
    public static readonly DocumentationDescriptor FormNameTagHelper = new SimpleDescriptor(DocumentationId.FormNameTagHelper);

    public static DocumentationDescriptor From(DocumentationId id, params object?[]? args)
    {
        if (id < 0 || id > DocumentationId.Last)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id), id, Resources.FormatUnknown_documentation_id_0(id));
        }

        if (args is null or { Length: 0 })
        {
            return id switch
            {
                DocumentationId.BindTagHelper_Fallback => BindTagHelper_Fallback,
                DocumentationId.BindTagHelper_Fallback_Event => BindTagHelper_Fallback_Event,
                DocumentationId.BindTagHelper_Fallback_Format => BindTagHelper_Fallback_Format,
                DocumentationId.BindTagHelper_Element => BindTagHelper_Element,
                DocumentationId.BindTagHelper_Element_After => BindTagHelper_Element_After,
                DocumentationId.BindTagHelper_Element_Culture => BindTagHelper_Element_Culture,
                DocumentationId.BindTagHelper_Element_Event => BindTagHelper_Element_Event,
                DocumentationId.BindTagHelper_Element_Format => BindTagHelper_Element_Format,
                DocumentationId.BindTagHelper_Element_Get => BindTagHelper_Element_Get,
                DocumentationId.BindTagHelper_Element_Set => BindTagHelper_Element_Set,
                DocumentationId.BindTagHelper_Component => BindTagHelper_Component,
                DocumentationId.ChildContentParameterName => ChildContentParameterName,
                DocumentationId.ChildContentParameterName_TopLevel => ChildContentParameterName_TopLevel,
                DocumentationId.ComponentTypeParameter => ComponentTypeParameter,
                DocumentationId.EventHandlerTagHelper => EventHandlerTagHelper,
                DocumentationId.EventHandlerTagHelper_PreventDefault => EventHandlerTagHelper_PreventDefault,
                DocumentationId.EventHandlerTagHelper_StopPropagation => EventHandlerTagHelper_StopPropagation,
                DocumentationId.KeyTagHelper => KeyTagHelper,
                DocumentationId.RefTagHelper => RefTagHelper,
                DocumentationId.SplatTagHelper => SplatTagHelper,
                DocumentationId.RenderModeTagHelper => RenderModeTagHelper,
                DocumentationId.FormNameTagHelper => FormNameTagHelper,

                // If this exception is thrown, there are two potential problems:
                //
                // 1. Arguments are being passed for a DocumentationId that doesn't require formatting.
                // 2. A new DocumentationId was added that needs an entry added in the switch expression above
                //    to return a DocumentationDescriptor.

                _ => throw new NotSupportedException(Resources.FormatUnknown_documentation_id_0(id))
            };
        }

        return new FormattedDescriptor(id, args);
    }

    public DocumentationId Id { get; }
    public abstract object?[] Args { get; }

    private int? _hashCode;

    private protected DocumentationDescriptor(DocumentationId id)
    {
        Id = id;
    }

    public sealed override bool Equals(object? obj)
        => obj is DocumentationDescriptor other && Equals(other);

    public abstract bool Equals(DocumentationDescriptor? other);

    public sealed override int GetHashCode()
        => _hashCode ??= ComputeHashCode();

    protected abstract int ComputeHashCode();

    public abstract string GetText();

    private string GetDocumentationText()
    {
        return Id switch
        {
            DocumentationId.BindTagHelper_Fallback => ComponentResources.BindTagHelper_Fallback_Documentation,
            DocumentationId.BindTagHelper_Fallback_Event => ComponentResources.BindTagHelper_Fallback_Event_Documentation,
            DocumentationId.BindTagHelper_Fallback_Format => ComponentResources.BindTagHelper_Fallback_Format_Documentation,
            DocumentationId.BindTagHelper_Element => ComponentResources.BindTagHelper_Element_Documentation,
            DocumentationId.BindTagHelper_Element_After => ComponentResources.BindTagHelper_Element_After_Documentation,
            DocumentationId.BindTagHelper_Element_Culture => ComponentResources.BindTagHelper_Element_Culture_Documentation,
            DocumentationId.BindTagHelper_Element_Event => ComponentResources.BindTagHelper_Element_Event_Documentation,
            DocumentationId.BindTagHelper_Element_Format => ComponentResources.BindTagHelper_Element_Format_Documentation,
            DocumentationId.BindTagHelper_Element_Get => ComponentResources.BindTagHelper_Element_Get_Documentation,
            DocumentationId.BindTagHelper_Element_Set => ComponentResources.BindTagHelper_Element_Set_Documentation,
            DocumentationId.BindTagHelper_Component => ComponentResources.BindTagHelper_Component_Documentation,
            DocumentationId.ChildContentParameterName => ComponentResources.ChildContentParameterName_Documentation,
            DocumentationId.ChildContentParameterName_TopLevel => ComponentResources.ChildContentParameterName_TopLevelDocumentation,
            DocumentationId.ComponentTypeParameter => ComponentResources.ComponentTypeParameter_Documentation,
            DocumentationId.EventHandlerTagHelper => ComponentResources.EventHandlerTagHelper_Documentation,
            DocumentationId.EventHandlerTagHelper_PreventDefault => ComponentResources.EventHandlerTagHelper_PreventDefault_Documentation,
            DocumentationId.EventHandlerTagHelper_StopPropagation => ComponentResources.EventHandlerTagHelper_StopPropagation_Documentation,
            DocumentationId.KeyTagHelper => ComponentResources.KeyTagHelper_Documentation,
            DocumentationId.RefTagHelper => ComponentResources.RefTagHelper_Documentation,
            DocumentationId.SplatTagHelper => ComponentResources.SplatTagHelper_Documentation,
            DocumentationId.RenderModeTagHelper => ComponentResources.RenderModeTagHelper_Documentation,
            DocumentationId.FormNameTagHelper => ComponentResources.FormNameTagHelper_Documentation,

            // If this exception is thrown, a new DocumentationId was added that needs an entry added in
            // the switch expression above to return a resource string.

            var id => throw new NotSupportedException(Resources.FormatUnknown_documentation_id_0(id))
        };
    }
}
