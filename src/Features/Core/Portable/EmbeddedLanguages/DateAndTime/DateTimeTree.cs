// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.DateAndTime;

/// <summary>
/// No actual tree for DateAndTime. But we use this to fit into the common pattern of embedded languages.
/// </summary>
internal sealed class DateTimeTree
{
    public static readonly DateTimeTree Instance = new();

    private DateTimeTree()
    {
    }
}
