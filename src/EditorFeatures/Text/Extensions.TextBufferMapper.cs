// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
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
            private static readonly ConditionalWeakTable<ITextImage, WeakReference<ITextSnapshot>> s_roslynToEditorSnapshotMap = new ConditionalWeakTable<ITextImage, WeakReference<ITextSnapshot>>();

            public static ITextImage RecordTextSnapshotAndGetImage(ITextSnapshot editorSnapshot)
            {
                Contract.ThrowIfNull(editorSnapshot);

                var textImage = ((ITextSnapshot2)editorSnapshot).TextImage;
                Contract.ThrowIfNull(textImage);

                // If we're already in the map, there's nothing to update.  Do a quick check
                // to avoid two allocations per call to RecordTextSnapshotAndGetImage.
                if (!s_roslynToEditorSnapshotMap.TryGetValue(textImage, out var weakReference) ||
                    weakReference.GetTarget() != editorSnapshot)
                {
                    // put reverse entry that won't hold onto anything
                    s_roslynToEditorSnapshotMap.GetValue(
                        textImage, _ => new WeakReference<ITextSnapshot>(editorSnapshot));
                }

                return textImage;
            }

            public static ITextSnapshot TryFindEditorSnapshot(ITextImage textImage)
            {
                Contract.ThrowIfNull(textImage);
                if (!s_roslynToEditorSnapshotMap.TryGetValue(textImage, out var weakReference) ||
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
