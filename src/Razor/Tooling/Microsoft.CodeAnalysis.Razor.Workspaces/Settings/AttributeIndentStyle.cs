// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Settings;

internal enum AttributeIndentStyle
{
    /// <summary>
    /// Matches the behaviour of the Html formatter in VS and VS Code, and makes attributes on subsequent lines
    /// align with the first attribute on the first line of a tag
    /// </summary>
    AlignWithFirst,
    /// <summary>
    /// Indents attributes on subsequent lines by one more level than the indentation level of the line the tag starts on
    /// </summary>
    IndentByOne,
    /// <summary>
    /// Indents attributes on subsequent lines by two more levels than the indentation level of the line the tag starts on.
    /// This differentiates attributes from child elements, which are indented by one level.
    /// </summary>
    IndentByTwo
}
