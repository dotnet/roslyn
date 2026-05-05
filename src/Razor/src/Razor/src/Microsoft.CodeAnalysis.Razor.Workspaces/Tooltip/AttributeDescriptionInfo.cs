// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

/// <summary>
/// Provides description information for HTML attributes that are not bound to tag helpers.
/// </summary>
/// <param name="Name">The name of the attribute.</param>
/// <param name="Documentation">The documentation text describing the attribute.</param>
internal sealed record AttributeDescriptionInfo(string Name, string Documentation);
