using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    internal class MetadataReferenceManager
    {
        private static readonly ConditionalWeakTable<ProjectState, WeakReference<Compilation>> compilationReferenceMap =
            new ConditionalWeakTable<ProjectState, WeakReference<Compilation>>();

        private static readonly ConditionalWeakTable<ProjectState, WeakReference<Compilation>>.CreateValueCallback createValue =
            k => new WeakReference<Compilation>(null);

        private static readonly object guard = new object();

        // Hand out the same compilation reference for everyone who asks.  Use 
        // WeakReference<Compilation> so that if no one is using the MetadataReference,
        // it can be collected.
        internal static Compilation GetCompilationForMetadataReference(ProjectState projectState, Compilation compilation)
        {
            var weakReference = compilationReferenceMap.GetValue(projectState, createValue);
            Compilation reference;
            lock (guard)
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
            WeakReference<Compilation> weakReference;
            return compilationReferenceMap.TryGetValue(projectState, out weakReference) && weakReference.TryGetTarget(out referenceCompilation);
        }
    }
}