// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{
    public readonly struct SyntaxTreeTransformation
    {
        public SyntaxTreeTransformation(SyntaxTree newTree, SyntaxTree? oldTree)
        {
            OldTree = oldTree;
            NewTree = newTree;
        }

        public SyntaxTree? OldTree { get;  }
        public SyntaxTree NewTree { get;  }
    }
}
