// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentHelpers
{
    public const string ImportsFileName = "_Imports.razor";

    private const string MangledClassNamePrefix = "__generated__";

    public static string MangleClassName(string className)
    {
        if (className.IsNullOrEmpty())
        {
            return string.Empty;
        }

        return MangledClassNamePrefix + className;
    }

    public static bool IsMangledClass(string? className)
        => className?.StartsWith(MangledClassNamePrefix, StringComparison.Ordinal) == true;

    public static class ChildContent
    {
        /// <summary>
        /// The name of the synthesized attribute used to set a child content parameter.
        /// </summary>
        public const string ParameterAttributeName = "Context";

        /// <summary>
        /// The default name of the child content parameter (unless set by a Context attribute).
        /// </summary>
        public const string DefaultParameterName = "context";
    }
}
