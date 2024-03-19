// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A class that represents both a tree and its top level signature version
/// </summary>
internal sealed class TreeAndVersion(SyntaxTree tree, VersionStamp version)
{
    /// <summary>
    /// The syntax tree
    /// </summary>
    public SyntaxTree Tree { get; } = tree;

    /// <summary>
    /// The version of the top level signature of the tree
    /// </summary>
    public VersionStamp Version { get; } = version;
}
