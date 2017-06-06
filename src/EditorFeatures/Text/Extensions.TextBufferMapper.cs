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

            public static ITextImage ToReverseMappedTextImage(ITextSnapshot editorSnapshot)
            {
                Contract.ThrowIfNull(editorSnapshot);

                var textImage = ((ITextSnapshot2)editorSnapshot).TextImage;
                Contract.ThrowIfNull(textImage);

                // put reverse entry that won't hold onto anything
                var weakEditorSnapshot = new WeakReference<ITextSnapshot>(editorSnapshot);
                s_roslynToEditorSnapshotMap.GetValue(textImage, _ => weakEditorSnapshot);

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