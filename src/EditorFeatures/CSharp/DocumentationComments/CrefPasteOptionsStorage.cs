// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;

internal static class CrefPasteOptionsStorage
{
    public static readonly PerLanguageOption2<bool> FixCrefOnPaste = new("csharp_fix_cref_on_paste", defaultValue: true);
}
