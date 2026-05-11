// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal sealed record class RazorCodeActionContext(
    VSCodeActionParams Request,
    IDocumentSnapshot DocumentSnapshot,
    RazorCodeDocument CodeDocument,
    Uri? DelegatedDocumentUri,
    int StartAbsoluteIndex,
    int EndAbsoluteIndex,
    Protocol.RazorLanguageKind LanguageKind,
    SourceText SourceText,
    bool SupportsFileCreation,
    bool SupportsCodeActionResolve)
{
    public bool HasSelection => StartAbsoluteIndex != EndAbsoluteIndex;

    public bool ContainsDiagnostic(string code)
    {
        if (Request.Context.Diagnostics is null)
        {
            return false;
        }

        foreach (var diagnostic in Request.Context.Diagnostics)
        {
            if (diagnostic.Code is { } codeSumType &&
                codeSumType.TryGetSecond(out var codeString) &&
                codeString == code)
            {
                return true;
            }
        }

        return false;
    }
}
