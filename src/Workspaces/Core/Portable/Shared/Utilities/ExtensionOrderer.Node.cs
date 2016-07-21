// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                this.Extension = extension;
            }

            public void CheckForCycles()
            {
                this.CheckForCycles(new HashSet<Node<TExtension, TMetadata>>());
            }

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
