// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Wrapping.SeparatedSyntaxList;

internal enum WrappingStyle
{
    /// <summary>
    /// Wraps first item.  Subsequent items, if wrapped, will be aligned with that first item:
    ///      MethodName(
    ///          int a, int b, int c, int d, int e,
    ///          int f, int g, int h, int i, int j)
    /// </summary>
    WrapFirst_IndentRest,

    /// <summary>
    /// Unwraps first item.  Subsequent items, if wrapped, will be aligned with that first item:
    ///      MethodName(int a, int b, int c, int d, int e,
    ///                 int f, int g, int h, int i, int j)
    /// </summary>
    UnwrapFirst_AlignRest,

    /// <summary>
    /// Unwraps first item.  Subsequent items, if wrapped, will be indented:
    ///      MethodName(int a, int b, int c, int d, int e,
    ///          int f, int g, int h, int i, int j)
    /// </summary>
    UnwrapFirst_IndentRest,
}
