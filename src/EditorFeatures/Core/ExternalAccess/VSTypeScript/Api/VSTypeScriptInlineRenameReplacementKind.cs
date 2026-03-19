// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal enum VSTypeScriptInlineRenameReplacementKind
{
    NoConflict,
    ResolvedReferenceConflict,
    ResolvedNonReferenceConflict,
    UnresolvedConflict,
    Complexified,
}
