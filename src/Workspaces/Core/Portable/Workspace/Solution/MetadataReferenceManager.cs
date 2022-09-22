// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    internal class MetadataReferenceManager
    {
        private static readonly ConditionalWeakTable<ProjectState, WeakReference<Compilation>> s_compilationReferenceMap = new();

        private static readonly object s_guard = new();

        // Hand out the same compilation reference for everyone who asks.  Use 
        // WeakReference<Compilation> so that if no-one is using the MetadataReference,
        // it can be collected.
        internal static Compilation GetCompilationForMetadataReference(ProjectState projectState, Compilation compilation)
        {
            var weakReference = s_compilationReferenceMap.GetValue(projectState, static _ => new WeakReference<Compilation>(null));
            Compilation reference;
            lock (s_guard)
            {
                if (!weakReference.TryGetTarget(out reference))
                {
                    reference = compilation.Clone(); // drop all existing symbols
                    weakReference.SetTarget(reference);
                }
            }

            return reference;
        }

        internal static bool TryGetCompilationForMetadataReference(ProjectState projectState, out Compilation referenceCompilation)
        {
            referenceCompilation = null;
            return s_compilationReferenceMap.TryGetValue(projectState, out var weakReference) && weakReference.TryGetTarget(out referenceCompilation);
        }
    }
}
