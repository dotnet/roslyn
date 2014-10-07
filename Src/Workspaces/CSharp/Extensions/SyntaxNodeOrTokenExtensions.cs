using System.Collections.Generic;
using Roslyn.Compilers.CSharp;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Extensions
{
    internal static partial class SyntaxNodeOrTokenExtensions
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