// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal class ReferenceInfo
    {
        /// <summary>
        /// Indicates the type of reference.
        /// </summary>
        public ReferenceType ReferenceType { get; }

        /// <summary>
        /// Uniquely identifies the reference.
        /// </summary>
        /// <remarks>
        /// Should match the Include or Name attribute used in the project file.
        /// </remarks>
        public string ItemSpecification { get; }

        /// <summary>
        /// Indicates that this reference should be treated as if it were used.
        /// </summary>
        public bool TreatAsUsed { get; }

        /// <summary>
        /// The full assembly paths that this reference directly adds to the compilation.
        /// </summary>
        public ImmutableArray<string> CompilationAssemblies { get; }

        /// <summary>
        /// The dependencies that this reference transitively brings in to the compilation.
        /// </summary>
        public ImmutableArray<ReferenceInfo> Dependencies { get; }

        public ReferenceInfo(ReferenceType referenceType, string itemSpecification, bool treatAsUsed, ImmutableArray<string> compilationAssemblies, ImmutableArray<ReferenceInfo> dependencies)
        {
            ReferenceType = referenceType;
            ItemSpecification = itemSpecification;
            TreatAsUsed = treatAsUsed;
            CompilationAssemblies = compilationAssemblies;
            Dependencies = dependencies;
        }
    }
}
