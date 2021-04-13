// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal abstract class ScriptCompiler
    {
        public abstract Compilation CreateSubmission(Script script);
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }
        public abstract StringComparer IdentifierComparer { get; }

        public abstract SyntaxTree ParseSubmission(SourceText text, ParseOptions parseOptions, CancellationToken cancellationToken);
        public abstract bool IsCompleteSubmission(SyntaxTree tree);
    }
}
