// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.MoveToNamespace;

internal class MoveToNamespaceOptionsResult
{
    public static readonly MoveToNamespaceOptionsResult Cancelled = new();

    public bool IsCancelled { get; }
    public string Namespace { get; }

    private MoveToNamespaceOptionsResult()
        => IsCancelled = true;

    public MoveToNamespaceOptionsResult(string @namespace)
        => Namespace = @namespace;
}
