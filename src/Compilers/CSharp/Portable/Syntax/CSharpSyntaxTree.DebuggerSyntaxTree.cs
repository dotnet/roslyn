// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class CSharpSyntaxTree
    {
        private class DebuggerSyntaxTree : ParsedSyntaxTree
        {
            public DebuggerSyntaxTree(CSharpSyntaxNode root, SourceText text)
                : base(
                    text,
                    text.Encoding,
                    text.ChecksumAlgorithm,
                    path: "",
                    options: CSharpParseOptions.Default,
                    root: root,
                    directives: Syntax.InternalSyntax.DirectiveStack.Empty)
            {
            }

            internal override bool SupportsLocations
            {
                get { return true; }
            }
        }
    }
}
