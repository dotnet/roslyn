// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.EditorConfig;

internal static class LanguageConstants
{
    public const string DefaultCSharpPath = "/" + DefaultCSharpExtension;
    public const string DefaultCSharpSplat = "*" + DefaultCSharpExtension;
    public const string DefaultCSharpExtension = "." + DefaultCSharpExtensionWithoutDot;
    public const string DefaultCSharpExtensionWithoutDot = "cs";
    public const string DefaultVisualBasicPath = "/" + DefaultVisualBasicExtension;
    public const string DefaultVisualBasicSplat = "*" + DefaultVisualBasicExtension;
    public const string DefaultVisualBasicExtension = "." + DefaultVisualBasicExtensionWithoutDot;
    public const string DefaultVisualBasicExtensionWithoutDot = "vb";
}
