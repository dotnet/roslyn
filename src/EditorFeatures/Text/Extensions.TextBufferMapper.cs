// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    public static partial class Extensions
    {
        /// <summary>
        /// mapper between editor text buffer to our text buffer
        /// </summary>
        private static class TextBufferMapper
        {
            private static readonly ConditionalWeakTable<ITextSnapshot, ITextSnapshot> s_editorToRoslynSnapshotMap = new ConditionalWeakTable<ITextSnapshot, ITextSnapshot>();
            private static readonly ConditionalWeakTable<ITextSnapshot, WeakReference<ITextSnapshot>> s_roslynToEditorSnapshotMap = new ConditionalWeakTable<ITextSnapshot, WeakReference<ITextSnapshot>>();
            private static readonly ConditionalWeakTable<ITextSnapshot, ITextSnapshot>.CreateValueCallback s_createSnapshotCallback = CreateSnapshot;

            public static ITextSnapshot ToRoslyn(ITextSnapshot editorSnapshot)
            {
                Contract.ThrowIfNull(editorSnapshot);

                var roslynSnapshot = s_editorToRoslynSnapshotMap.GetValue(editorSnapshot, s_createSnapshotCallback);
                if (roslynSnapshot == null)
                {
                    return editorSnapshot;
                }

                return roslynSnapshot;
            }

            private static ITextSnapshot CreateSnapshot(ITextSnapshot editorSnapshot)
            {
                var factory = TextBufferFactory;

                // We might not have a factory if there is no primary workspace (for example, under the unit test harness,
                // or in CodeSense where they are just using the parsers by themselves). In that case, just use the editor
                // snapshot as-is.
                //
                // Creating a buffer off a given snapshot should be cheap, so it should be okay to create a dummy buffer here
                // just to host the snapshot we want.
                var roslynSnapshot = factory != null
                                        ? factory.Clone(editorSnapshot.GetFullSpan()).CurrentSnapshot
                                        : editorSnapshot;

                // put reverse entry that won't hold onto anything
                var weakEditorSnapshot = new WeakReference<ITextSnapshot>(editorSnapshot);
                s_roslynToEditorSnapshotMap.GetValue(roslynSnapshot, _ => weakEditorSnapshot);

                return roslynSnapshot;
            }

            public static ITextSnapshot ToEditor(ITextSnapshot roslynSnapshot)
            {
                Contract.ThrowIfNull(roslynSnapshot);
                if (!s_roslynToEditorSnapshotMap.TryGetValue(roslynSnapshot, out var weakReference) ||
                    !weakReference.TryGetTarget(out var editorSnapshot))
                {
                    return null;
                }

                return editorSnapshot;
            }

            private static ITextBufferCloneService TextBufferFactory
            {
                get
                {
                    // simplest way to get text factory
                    var ws = PrimaryWorkspace.Workspace;
                    if (ws != null)
                    {
                        return ws.Services.GetService<ITextBufferCloneService>();
                    }

                    return null;
                }
            }
        }
    }
}
