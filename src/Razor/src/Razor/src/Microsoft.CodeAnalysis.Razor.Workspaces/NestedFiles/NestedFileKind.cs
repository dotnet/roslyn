// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.NestedFiles;

/// <summary>
/// Specifies which type of nested file to create for a Razor component.
/// </summary>
internal enum NestedFileKind
{
    CSharp,
    Css,
    JavaScript,
}
