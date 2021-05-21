﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions
{
    internal static class OmniSharpCodeAction
    {
        public static ImmutableArray<CodeAction> GetNestedCodeActions(this CodeAction codeAction)
            => codeAction.NestedCodeActions;
    }
}
