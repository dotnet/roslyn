// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

internal sealed class ViewComponentTagHelperTargetExtension : ViewComponentTagHelperTargetExtensionBase
{
    protected override string TagHelperContentVariableName => "content";
    protected override string TagHelperContextVariableName => "context";
    protected override string TagHelperOutputVariableName => "output";
    protected override string ViewComponentHelperVariableName => "_helper";

    protected override ImmutableArray<string> GetInvokeArguments(TagHelperDescriptor tagHelper)
    {
        var propertyNames = tagHelper.BoundAttributes.Select(attribute => attribute.PropertyName);
        var joinedPropertyNames = string.Join(", ", propertyNames);
        var parametersString = $"new {{ {joinedPropertyNames} }}";
        var viewComponentName = tagHelper.ViewComponentName;

        return [$"\"{viewComponentName}\"", parametersString];
    }
}
