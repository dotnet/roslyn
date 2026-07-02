// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static void Deconstruct(this LspLocation position, out DocumentUri uri, out LspRange range)
        => (uri, range) = (position.DocumentUri, position.Range);
}
