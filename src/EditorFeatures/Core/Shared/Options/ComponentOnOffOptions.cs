// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options;

/// <summary>
/// options to indicate whether a certain component in Roslyn is enabled or not
/// </summary>
internal sealed class EditorComponentOnOffOptions
{
    public static readonly Option2<bool> Adornment = new("dotnet_enable_editor_adornment", defaultValue: true);
    public static readonly Option2<bool> Tagger = new("dotnet_enable_editor_tagger", defaultValue: true);
    public static readonly Option2<bool> CodeRefactorings = new("dotnet_enable_code_refactorings", defaultValue: true);
}
