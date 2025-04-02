// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal sealed class PreviewWarningTag : TextMarkerTag
{
    public const string TagId = "RoslynPreviewWarningTag";

    public static readonly PreviewWarningTag Instance = new();

    private PreviewWarningTag()
        : base(TagId)
    {
    }
}
