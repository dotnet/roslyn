// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpSyntaxTree
    {
        /// <summary>
        /// Use by Expression Evaluator.
        /// </summary>
        private sealed class DebuggerSyntaxTree : ParsedSyntaxTree
        {
            public DebuggerSyntaxTree(CSharpSyntaxNode root, SourceText text, CSharpParseOptions options)
                : base(
                    text,
                    text.Encoding,
                    text.ChecksumAlgorithm,
                    path: "",
                    options: options,
                    root: root,
                    directives: Syntax.InternalSyntax.DirectiveStack.Empty,
                    diagnosticOptions: null,
                    cloneRoot: true)
            {
            }

            internal override bool SupportsLocations
            {
                get { return true; }
            }
        }
    }
}
