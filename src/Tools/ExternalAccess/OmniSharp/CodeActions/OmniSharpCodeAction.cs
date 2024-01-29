// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions;

internal static class OmniSharpCodeAction
{
    [Obsolete("CodeActions.NestedAction is now public.  Use that instead.  Once done, remove this method", error: false)]
    public static ImmutableArray<CodeAction> GetNestedCodeActions(this CodeAction codeAction)
        => codeAction.NestedActions;
}
