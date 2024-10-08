// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Snippets;

internal static class SnippetsOptionsStorage
{
    public static readonly Option2<bool> Snippets = new("dotnet_enable_snippets", defaultValue: true);
}
