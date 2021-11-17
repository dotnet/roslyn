﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    [DataContract]
    internal class ReferenceInfo
    {
        /// <summary>
        /// Indicates the type of reference.
        /// </summary>
        [DataMember(Order = 0)]
        public ReferenceType ReferenceType { get; }

        /// <summary>
        /// Uniquely identifies the reference.
        /// </summary>
        /// <remarks>
        /// Should match the Include or Name attribute used in the project file.
        /// </remarks>
        [DataMember(Order = 1)]
        public string ItemSpecification { get; }

        /// <summary>
        /// Indicates that this reference should be treated as if it were used.
        /// </summary>
        [DataMember(Order = 2)]
        public bool TreatAsUsed { get; }

        /// <summary>
        /// The full assembly paths that this reference directly adds to the compilation.
        /// </summary>
        [DataMember(Order = 3)]
        public ImmutableArray<string> CompilationAssemblies { get; }

        /// <summary>
        /// The dependencies that this reference transitively brings in to the compilation.
        /// </summary>
        [DataMember(Order = 4)]
        public ImmutableArray<ReferenceInfo> Dependencies { get; }

        public ReferenceInfo(ReferenceType referenceType, string itemSpecification, bool treatAsUsed, ImmutableArray<string> compilationAssemblies, ImmutableArray<ReferenceInfo> dependencies)
        {
            ReferenceType = referenceType;
            ItemSpecification = itemSpecification;
            TreatAsUsed = treatAsUsed;
            CompilationAssemblies = compilationAssemblies;
            Dependencies = dependencies;
        }

        public ReferenceInfo WithItemSpecification(string itemSpecification)
            => new(ReferenceType, itemSpecification, TreatAsUsed, CompilationAssemblies, Dependencies);

        public ReferenceInfo WithDependencies(IEnumerable<ReferenceInfo>? dependencies)
            => new(ReferenceType, ItemSpecification, TreatAsUsed, CompilationAssemblies, dependencies.AsImmutableOrEmpty());
    }
}
