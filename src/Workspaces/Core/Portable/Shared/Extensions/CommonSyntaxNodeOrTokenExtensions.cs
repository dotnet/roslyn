// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonSyntaxNodeOrTokenExtensions
    {
        public static IEnumerable<SyntaxNodeOrToken> DepthFirstTraversal(this SyntaxNodeOrToken node)
        {
            var stack = new Stack<SyntaxNodeOrToken>();
            stack.Push(node);

            while (!stack.IsEmpty())
            {
                var current = stack.Pop();

                yield return current;

                if (current.IsNode)
                {
                    foreach (var child in current.ChildNodesAndTokens().Reverse())
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }
}