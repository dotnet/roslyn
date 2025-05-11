// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal partial class AbstractCodeModelService
{
    protected abstract AbstractCodeModelEventCollector CreateCodeModelEventCollector();

    protected abstract class AbstractCodeModelEventCollector
    {
        private const int MaxChildDelta = 5;

        protected readonly AbstractCodeModelService CodeModelService;

        protected AbstractCodeModelEventCollector(AbstractCodeModelService codeModelService)
            => this.CodeModelService = codeModelService;

        protected abstract void CollectCore(SyntaxNode oldRoot, SyntaxNode newRoot, CodeModelEventQueue eventQueue);

        protected abstract void EnqueueAddEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventQueue eventQueue);
        protected abstract void EnqueueRemoveEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventQueue eventQueue);
        protected abstract void EnqueueChangeEvent(SyntaxNode node, SyntaxNode parent, CodeModelEventType eventType, CodeModelEventQueue eventQueue);

        public Queue<CodeModelEvent> Collect(SyntaxTree oldTree, SyntaxTree newTree)
        {
            var queue = new Queue<CodeModelEvent>();
            var eventQueue = new CodeModelEventQueue(queue);
            CollectCore(oldTree.GetRoot(CancellationToken.None), newTree.GetRoot(CancellationToken.None), eventQueue);
            return queue;
        }

        protected delegate bool NodeComparison<TNode, TParent>(
            TNode oldNode, TNode newNode,
            TParent newNodeParent,
            CodeModelEventQueue eventQueue)
            where TNode : SyntaxNode
            where TParent : SyntaxNode;

        protected enum DeclarationChange { WholeDeclaration, NameOnly }

        protected bool CompareChildren<TNode, TParent>(
            NodeComparison<TNode, TParent> compare,
            IReadOnlyList<TNode> oldChildren,
            IReadOnlyList<TNode> newChildren,
            TParent newNodeParent,
            CodeModelEventType eventType,
            CodeModelEventQueue eventQueue)
            where TNode : SyntaxNode
            where TParent : SyntaxNode
        {
            var oldCount = oldChildren.Count;
            var newCount = newChildren.Count;

            if (oldCount == newCount)
            {
                return FindDifferentChild(compare, oldChildren, newChildren, newNodeParent, eventQueue);
            }
            else if (Math.Abs(oldCount - newCount) > MaxChildDelta)
            {
                // We got two discrepancies, enqueue element changed node for containing node
                EnqueueChangeEvent(newNodeParent, null, eventType, eventQueue);
            }
            else
            {
                if (oldCount > newCount)
                {
                    FindRemovedChild(compare, oldChildren, newChildren, newNodeParent, oldCount - newCount, eventQueue);
                }
                else
                {
                    FindAddedChild(compare, oldChildren, newChildren, newNodeParent, newCount - oldCount, eventQueue);
                }
            }

            return false;
        }

        protected DeclarationChange CompareRenamedDeclarations<TNode, TParent>(
            NodeComparison<TNode, TParent> compare,
            IReadOnlyList<TNode> oldChildren,
            IReadOnlyList<TNode> newChildren,
            SyntaxNode oldNode,
            SyntaxNode newNode,
            TParent newNodeParent,
            CodeModelEventQueue eventQueue)
            where TNode : SyntaxNode
            where TParent : SyntaxNode
        {
            var oldCount = oldChildren.Count;
            var newCount = newChildren.Count;

            if (oldCount == newCount)
            {
                // We now check the children of the old and new types against each other. If any of them have changed,
                // it means that the old type has essentially been removed and a new one added.
                for (var i = 0; i < oldCount; i++)
                {
                    if (!compare(oldChildren[i], newChildren[i], newNodeParent, null))
                    {
                        EnqueueRemoveEvent(oldNode, newNodeParent, eventQueue);
                        EnqueueAddEvent(newNode, newNodeParent, eventQueue);

                        // Report that the whole declaration has changed
                        return DeclarationChange.WholeDeclaration;
                    }
                }

                // The children are all the same, so only the name has changed.
                return DeclarationChange.NameOnly;
            }
            else
            {
                // Since the number of members is different, essentially the old type has been removed, and a new one added.
                EnqueueRemoveEvent(oldNode, newNodeParent, eventQueue);
                EnqueueAddEvent(newNode, newNodeParent, eventQueue);

                // Report that the whole declaration has changed
                return DeclarationChange.WholeDeclaration;
            }
        }

        // Finds the child node which is different OR enqueues a unknown on containing node.
        private bool FindDifferentChild<TNode, TParent>(
            NodeComparison<TNode, TParent> compare,
            IReadOnlyList<TNode> oldChildren,
            IReadOnlyList<TNode> newChildren,
            TParent newNodeParent,
            CodeModelEventQueue eventQueue)
            where TNode : SyntaxNode
            where TParent : SyntaxNode
        {
            Debug.Assert(oldChildren.Count == newChildren.Count);

            var eventCount = eventQueue != null
                ? eventQueue.Count
                : 0;

            var hasChanges = false;

            // Find first child that is different.
            int i;
            for (i = 0; i < oldChildren.Count; i++)
            {
                if (!compare(oldChildren[i], newChildren[i], newNodeParent, eventQueue))
                {
                    hasChanges = true;
                    i++;
                    break;
                }
            }

            // Look for a second different child. If there is one, we'll throw away any events from
            // the first different child and enqueue an unknown event on the containing node.
            for (; i < oldChildren.Count; i++)
            {
                if (!compare(oldChildren[i], newChildren[i], newNodeParent, null))
                {
                    // rollback any events added by the first difference
                    if (eventQueue != null)
                    {
                        while (eventQueue.Count > eventCount)
                        {
                            eventQueue.Discard();
                        }
                    }

                    EnqueueChangeEvent(newNodeParent, null, CodeModelEventType.Unknown, eventQueue);
                    return false;
                }
            }

            return !hasChanges;
        }

        private void FindAddedChild<TNode, TParent>(
            NodeComparison<TNode, TParent> compare,
            IReadOnlyList<TNode> oldChildren,
            IReadOnlyList<TNode> newChildren,
            TParent newNodeParent,
            int delta,
            CodeModelEventQueue eventQueue)
            where TNode : SyntaxNode
            where TParent : SyntaxNode
        {
            Debug.Assert(oldChildren.Count + delta == newChildren.Count);

            // The strategy is to assume that all of the added children are contiguous.
            // If that turns out not to be the case, an unknown change event is raised
            // for the containing node.

            var firstAdded = -1;

            // Look for the first different child. If there is one, track that index as
            // the first added node.
            int oldIndex, newIndex;
            for (oldIndex = 0, newIndex = 0; newIndex < newChildren.Count; oldIndex++, newIndex++)
            {
                if (oldIndex >= oldChildren.Count || !compare(oldChildren[oldIndex], newChildren[newIndex], newNodeParent, null))
                {
                    firstAdded = newIndex;
                    newIndex += delta;
                    break;
                }
            }

            // Look for a second different child. If there is one, we'll throw away any events from
            // the first different child and enqueue an unknown event on the containing node.
            for (; newIndex < newChildren.Count; oldIndex++, newIndex++)
            {
                if (!compare(oldChildren[oldIndex], newChildren[newIndex], newNodeParent, null))
                {
                    EnqueueChangeEvent(newNodeParent, null, CodeModelEventType.Unknown, eventQueue);
                    return;
                }
            }

            if (firstAdded >= 0)
            {
                for (var i = 0; i < delta; i++)
                {
                    EnqueueAddEvent(newChildren[firstAdded + i], newNodeParent, eventQueue);
                }
            }
        }

        private void FindRemovedChild<TNode, TParent>(
            NodeComparison<TNode, TParent> compare,
            IReadOnlyList<TNode> oldChildren,
            IReadOnlyList<TNode> newChildren,
            TParent newNodeParent,
            int delta,
            CodeModelEventQueue eventQueue)
            where TNode : SyntaxNode
            where TParent : SyntaxNode
        {
            Debug.Assert(oldChildren.Count - delta == newChildren.Count);

            // The strategy is to assume that all of the removed children are contiguous.
            // If that turns out not to be the case, an unknown change event is raised
            // for the containing node.

            var firstRemoved = -1;

            // Look for the first different child. If there is one, track that index as
            // the first added node.
            int oldIndex, newIndex;
            for (oldIndex = 0, newIndex = 0; oldIndex < oldChildren.Count; oldIndex++, newIndex++)
            {
                if (newIndex >= newChildren.Count || !compare(oldChildren[oldIndex], newChildren[newIndex], newNodeParent, null))
                {
                    firstRemoved = oldIndex;
                    oldIndex += delta;
                    break;
                }
            }

            // Look for a second different child. If there is one, we'll throw away any events from
            // the first different child and enqueue an unknown event on the containing node.
            for (; oldIndex < oldChildren.Count; oldIndex++, newIndex++)
            {
                if (!compare(oldChildren[oldIndex], newChildren[newIndex], newNodeParent, null))
                {
                    EnqueueChangeEvent(newNodeParent, null, CodeModelEventType.Unknown, eventQueue);
                    return;
                }
            }

            if (firstRemoved >= 0)
            {
                for (var i = 0; i < delta; i++)
                {
                    EnqueueRemoveEvent(oldChildren[firstRemoved + i], newNodeParent, eventQueue);
                }
            }
        }
    }
}
