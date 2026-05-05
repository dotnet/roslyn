// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed record TagHelperInfo(string TagName, TagMode TagMode, TagHelperBinding BindingResult);
