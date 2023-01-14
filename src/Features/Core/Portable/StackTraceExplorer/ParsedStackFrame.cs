// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    /// <summary>
    /// A line from <see cref="StackTraceAnalyzer.Parse(string, CancellationToken)"/> that
    /// was parsed by <see cref="StackFrameParser"/>
    /// </summary>
    internal sealed class ParsedStackFrame : ParsedFrame
    {
        public readonly StackFrameTree Tree;

        public ParsedStackFrame(
            StackFrameTree tree)
        {
            Tree = tree;
        }

        public StackFrameCompilationUnit Root => Tree.Root;

        public override string ToString()
        {
            return Tree.Text.CreateString();
        }
    }
}
