// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal class ConflictTag : TextMarkerTag
{
    public const string TagId = "RoslynConflictTag";

    public static readonly ConflictTag Instance = new();

    private ConflictTag()
        : base(TagId)
    {
    }
}
