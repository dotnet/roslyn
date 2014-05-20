// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpSyntaxTreeFactoryServiceFactory
    {
        internal partial class CSharpSyntaxTreeFactoryService
        {
            /// <summary>
            /// Represents a syntax reference that was passed a null
            /// reference to a node. In this case, we just hold onto the
            /// weak tree reference and throw if any invalid properties
            /// are accessed.
            /// </summary>
            private class NullSyntaxReference : SyntaxReference
            {
                private readonly SyntaxTree tree;

                public NullSyntaxReference(SyntaxTree tree)
                {
                    this.tree = tree;
                }

                public override SyntaxTree SyntaxTree
                {
                    get
                    {
                        return tree;
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
                        throw new NotSupportedException(CSharpWorkspaceResources.CannotRetrieveTheSpanOfA);
                    }
                }
            }
        }
    }
}
