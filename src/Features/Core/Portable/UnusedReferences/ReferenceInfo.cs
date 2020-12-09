// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal class ReferenceInfo
    {
        private ImmutableArray<string>? _allCompilationAssemblies;

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
        /// The assembly paths that this reference directly adds to the compilation.
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

        /// <summary>
        /// Gets the compilation assemblies this reference directly brings into the compilation as well as those
        /// brought in transitively.
        /// </summary>
        public ImmutableArray<string> GetAllCompilationAssemblies()
        {
            _allCompilationAssemblies ??= CompilationAssemblies
                .Concat(GetTransitiveCompilationAssemblies())
                .ToImmutableArray();

            return _allCompilationAssemblies.Value;
        }

        private IEnumerable<string> GetTransitiveCompilationAssemblies()
        {
            return Dependencies.SelectMany(dependency => dependency.GetAllCompilationAssemblies());
        }
    }
}
