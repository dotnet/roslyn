using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.Utilities
{
    /// <summary>
    /// an iterator that will return every single node contained by the span in the tree
    /// 
    /// use iterator rather than visitor so that we can easily visit nodes in parallel and also
    /// cache nodes so that we can avoid using expensive tree navigation again.
    /// </summary>
    internal class NodeIterator : IEnumerable<CommonSyntaxNode>
    {
        private readonly CommonSyntaxNode root;
        private readonly TextSpan fullSpan;
        private readonly CancellationToken cancellationToken;

        public NodeIterator(
            CommonSyntaxNode root,
            TextSpan fullSpan,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(root);

            this.root = root;
            this.fullSpan = fullSpan;
            this.cancellationToken = cancellationToken;
        }

        public IEnumerator<CommonSyntaxNode> GetEnumerator()
        {
            var stack = new Stack<CommonSyntaxNode>();
            stack.Push(this.root);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var node = stack.Pop();

                var fullSpan = node.FullSpan;

                if (!this.fullSpan.IntersectsWith(fullSpan))
                {
                    continue;
                }

                yield return node;

                int currentPosition = fullSpan.Start;
                foreach (var child in node.ChildNodesAndTokens())
                {
                    Debug.Assert(currentPosition == child.FullSpan.Start);
                    if (!child.IsNode)
                    {
                        currentPosition += child.FullSpan.Length;
                        continue;
                    }

                    currentPosition += child.FullSpan.Length;
                    if (currentPosition < this.fullSpan.Start)
                    {
                        continue;
                    }

                    stack.Push(child.AsNode());

                    Debug.Assert(currentPosition == child.FullSpan.End);
                    if (this.fullSpan.End < currentPosition)
                    {
                        break;
                    }
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
