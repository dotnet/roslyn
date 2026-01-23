// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal enum FSharpInlineRenameReplacementKind
{
    NoConflict,
    ResolvedReferenceConflict,
    ResolvedNonReferenceConflict,
    UnresolvedConflict,
    Complexified,
}
