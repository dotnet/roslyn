// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    [DataContract]
    internal sealed class SerializableReferenceInfo
    {
        [DataMember(Order = 0)]
        public ReferenceType ReferenceType { get; }

        [DataMember(Order = 1)]
        public string ItemSpecification { get; }

        [DataMember(Order = 2)]
        public bool TreatAsUsed { get; }

        [DataMember(Order = 3)]
        public ImmutableArray<string> CompilationAssemblies { get; }

        [DataMember(Order = 4)]
        public ImmutableArray<SerializableReferenceInfo> Dependencies { get; }

        public SerializableReferenceInfo(
            ReferenceType referenceType,
            string itemSpecification,
            bool treatAsUsed,
            ImmutableArray<string> compilationAssemblies,
            ImmutableArray<SerializableReferenceInfo> dependencies)
        {
            ReferenceType = referenceType;
            ItemSpecification = itemSpecification;
            TreatAsUsed = treatAsUsed;
            CompilationAssemblies = compilationAssemblies;
            Dependencies = dependencies;
        }

        public static SerializableReferenceInfo Dehydrate(ReferenceInfo referenceInfo)
        {
            var dependencies = referenceInfo.Dependencies.SelectAsArray(Dehydrate);
            return new SerializableReferenceInfo(
                referenceInfo.ReferenceType,
                referenceInfo.ItemSpecification,
                referenceInfo.TreatAsUsed,
                referenceInfo.CompilationAssemblies,
                dependencies);
        }

        public ReferenceInfo Rehydrate()
        {
            var dependencies = Dependencies.SelectAsArray(dependency => dependency.Rehydrate());
            return new ReferenceInfo(ReferenceType, ItemSpecification, TreatAsUsed, CompilationAssemblies, dependencies);
        }
    }
}
