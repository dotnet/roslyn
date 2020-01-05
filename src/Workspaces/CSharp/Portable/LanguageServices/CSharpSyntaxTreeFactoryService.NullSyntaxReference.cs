// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpSyntaxTreeFactoryServiceFactory
    {
        private partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax reference that was passed a null
            /// reference to a node. In this case, we just hold onto the
            /// weak tree reference and throw if any invalid properties
            /// are accessed.
            /// </summary>
            private class NullSyntaxReference : SyntaxReference
            {
                private readonly SyntaxTree _tree;

                public NullSyntaxReference(SyntaxTree tree)
                {
                    _tree = tree;
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return _tree;
                    }
                }

                public override SyntaxNode GetSyntax(CancellationToken cancellationToken)
                {
                    return null;
                }

                public override TextSpan Span
                {
                    get
                    {
                        throw new NotSupportedException(CSharpWorkspaceResources.Cannot_retrieve_the_Span_of_a_null_syntax_reference);
                    }
                }
            }
        }
    }
}
