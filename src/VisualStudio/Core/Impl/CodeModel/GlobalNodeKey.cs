// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal readonly struct GlobalNodeKey
{
    public readonly SyntaxNodeKey NodeKey;
    public readonly SyntaxPath Path;

    public GlobalNodeKey(SyntaxNodeKey nodeKey, SyntaxPath path)
    {
        this.NodeKey = nodeKey;
        this.Path = path;
    }
}
