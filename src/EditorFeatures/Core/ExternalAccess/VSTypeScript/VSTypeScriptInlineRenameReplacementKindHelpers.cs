// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

internal static class VSTypeScriptInlineRenameReplacementKindHelpers
{
    public static VSTypeScriptInlineRenameReplacementKind ConvertFrom(InlineRenameReplacementKind kind)
    {
        return kind switch
        {
            InlineRenameReplacementKind.NoConflict => VSTypeScriptInlineRenameReplacementKind.NoConflict,
            InlineRenameReplacementKind.ResolvedReferenceConflict => VSTypeScriptInlineRenameReplacementKind.ResolvedReferenceConflict,
            InlineRenameReplacementKind.ResolvedNonReferenceConflict => VSTypeScriptInlineRenameReplacementKind.ResolvedNonReferenceConflict,
            InlineRenameReplacementKind.UnresolvedConflict => VSTypeScriptInlineRenameReplacementKind.UnresolvedConflict,
            InlineRenameReplacementKind.Complexified => VSTypeScriptInlineRenameReplacementKind.Complexified,
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    public static InlineRenameReplacementKind ConvertTo(VSTypeScriptInlineRenameReplacementKind kind)
    {
        return kind switch
        {
            VSTypeScriptInlineRenameReplacementKind.NoConflict => InlineRenameReplacementKind.NoConflict,
            VSTypeScriptInlineRenameReplacementKind.ResolvedReferenceConflict => InlineRenameReplacementKind.ResolvedReferenceConflict,
            VSTypeScriptInlineRenameReplacementKind.ResolvedNonReferenceConflict => InlineRenameReplacementKind.ResolvedNonReferenceConflict,
            VSTypeScriptInlineRenameReplacementKind.UnresolvedConflict => InlineRenameReplacementKind.UnresolvedConflict,
            VSTypeScriptInlineRenameReplacementKind.Complexified => InlineRenameReplacementKind.Complexified,
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }
}
