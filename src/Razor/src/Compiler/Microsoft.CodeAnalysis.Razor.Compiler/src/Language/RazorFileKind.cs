// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  The type of Razor file.
/// </summary>
public enum RazorFileKind : byte
{
    None = 0,

    /// <summary>
    ///  A file containing a Razor component, i.e. has a '.razor' file extension.
    /// </summary>
    Component = 1,

    /// <summary>
    ///  A file containing a Razor component import, i.e. file name is '_Imports.razor'.
    /// </summary>
    ComponentImport = 2,

    /// <summary>
    ///  A file containing legacy Razor code, i.e. has a '.cshtml' file extension.
    /// </summary>
    Legacy = 3
}
