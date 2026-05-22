// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Records which tag helpers were contributed by a directive.
/// </summary>
/// <remarks>
/// In components, this deals with `@using` directives, and for legacy it deals with `@addTagHelper` directives.
/// </remarks>
internal readonly record struct DirectiveTagHelperContribution(
    int DirectiveSpanStart,
    TagHelperCollection ContributedTagHelpers);
