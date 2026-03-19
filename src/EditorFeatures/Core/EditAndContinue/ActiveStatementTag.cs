// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class ActiveStatementTag : TextMarkerTag
{
    internal const string TagId = "RoslynActiveStatementTag";

    public static readonly ActiveStatementTag Instance = new();

    private ActiveStatementTag()
        : base(TagId)
    {
    }
}
