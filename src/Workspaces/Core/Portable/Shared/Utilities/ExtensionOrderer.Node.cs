// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class ExtensionOrderer
    {
        private class Node<TExtension, TMetadata>
        {
            public readonly Lazy<TExtension, TMetadata> Extension;
            public readonly HashSet<Node<TExtension, TMetadata>> ExtensionsBeforeMeSet = new HashSet<Node<TExtension, TMetadata>>();

            public Node(Lazy<TExtension, TMetadata> extension)
                => this.Extension = extension;

            public void CheckForCycles()
                => this.CheckForCycles(new HashSet<Node<TExtension, TMetadata>>());

            private void CheckForCycles(
                HashSet<Node<TExtension, TMetadata>> seenNodes)
            {
                if (!seenNodes.Add(this))
                {
                    // Cycle detected in extensions
                    throw new ArgumentException(WorkspacesResources.Cycle_detected_in_extensions);
                }

                foreach (var before in this.ExtensionsBeforeMeSet)
                {
                    before.CheckForCycles(seenNodes);
                }

                seenNodes.Remove(this);
            }
        }
    }
}
