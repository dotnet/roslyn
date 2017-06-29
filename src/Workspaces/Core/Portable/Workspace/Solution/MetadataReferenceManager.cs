// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    internal class MetadataReferenceManager
    {
        private static readonly ConditionalWeakTable<ProjectState, WeakReference<Compilation>> s_compilationReferenceMap =
            new ConditionalWeakTable<ProjectState, WeakReference<Compilation>>();

        private static readonly ConditionalWeakTable<ProjectState, WeakReference<Compilation>>.CreateValueCallback s_createValue =
            k => new WeakReference<Compilation>(null);

        private static readonly object s_guard = new object();

        // Hand out the same compilation reference for everyone who asks.  Use 
        // WeakReference<Compilation> so that if no-one is using the MetadataReference,
        // it can be collected.
        internal static Compilation GetCompilationForMetadataReference(ProjectState projectState, Compilation compilation)
        {
            var weakReference = s_compilationReferenceMap.GetValue(projectState, s_createValue);
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
