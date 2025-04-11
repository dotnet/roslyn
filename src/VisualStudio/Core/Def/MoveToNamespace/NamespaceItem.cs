// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace;

internal sealed class NamespaceItem
{
    public string Namespace { get; }
    public bool IsFromHistory { get; }

    public NamespaceItem(bool isFromHistory, string @namespace)
    {
        IsFromHistory = isFromHistory;
        Namespace = @namespace;
    }

    public override string ToString() => Namespace;
}
