// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BuildValidator
{
    public record SyntaxTreeInfo(
        string FilePath,
        SourceText SourceText)
    {
        public static SyntaxTreeInfo Create(SyntaxTree syntaxTree, CancellationToken cancellationToken = default) =>
            new SyntaxTreeInfo(syntaxTree.FilePath, syntaxTree.GetText(cancellationToken));
    }
}
