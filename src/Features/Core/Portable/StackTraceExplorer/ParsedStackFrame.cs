// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

/// <summary>
/// A line from <see cref="StackTraceAnalyzer.Parse(string, CancellationToken)"/> that
/// was parsed by <see cref="StackFrameParser"/>
/// </summary>
internal sealed class ParsedStackFrame(
    StackFrameTree tree) : ParsedFrame
{
    public readonly StackFrameTree Tree = tree;

    public StackFrameCompilationUnit Root => Tree.Root;

    public override string ToString()
    {
        return Tree.Text.CreateString();
    }
}
