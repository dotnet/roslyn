// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation
{
    [Obsolete]
    internal static class FSharpNavigationOptions
    {
        public static Option<bool> PreferProvisionalTab { get; } = new("NavigationOptions", "PreferProvisionalTab", defaultValue: false);
    }
}
