// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal sealed partial class MarkupTagHelperDirectiveAttributeSyntax
{
    public string FullName
    {
        get
        {
            var fullName = string.Concat(
                Transition.GetContent(),
                Name.GetContent(),
                Colon?.GetContent() ?? string.Empty,
                ParameterName?.GetContent() ?? string.Empty);
            return fullName;
        }
    }
}
