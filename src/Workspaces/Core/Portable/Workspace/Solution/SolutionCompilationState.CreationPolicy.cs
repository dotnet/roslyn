// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal sealed partial class SolutionCompilationState
{
    /// <summary>
    /// Flags controlling if generator documents should be created or not.
    /// </summary>
    private enum GeneratedDocumentCreationPolicy
    {
        /// <summary>
        /// Source generators should be run and should produce up to date results.
        /// </summary>
        Create,

        /// <summary>
        /// Source generators that are considered required should be run and produce results. Previously
        /// computed results should be reused for other generators.
        /// </summary>
        /// <remarks>
        /// Today the only required generator is Razor.
        /// </remarks>
        CreateOnlyRequired
    }

    /// <summary>
    /// Flags controlling if skeleton references should be created or not.
    /// </summary>
    private enum SkeletonReferenceCreationPolicy
    {
        /// <summary>
        /// Skeleton references should be created, and should be up to date with the project they are created for.
        /// </summary>
        Create,

        /// <summary>
        /// Skeleton references should only be created for a compilation if no existing skeleton exists for their
        /// project from some point in the past.
        /// </summary>
        CreateIfAbsent,

        /// <summary>
        /// Skeleton references should not be created at all.
        /// </summary>
        DoNotCreate,
    }

    private readonly record struct CreationPolicy(
        GeneratedDocumentCreationPolicy GeneratedDocumentCreationPolicy,
        SkeletonReferenceCreationPolicy SkeletonReferenceCreationPolicy)
    {
        /// <summary>
        /// Create up to date source generator docs and create up to date skeleton references when needed.
        /// </summary>
        public static readonly CreationPolicy Create = new(GeneratedDocumentCreationPolicy.Create, SkeletonReferenceCreationPolicy.Create);

        /// <summary>
        /// Create only the source generator documents considered to be required and do not create
        /// up to date skeleton references for P2P references. For generators combine the newly generated 
        /// required docs with whatever else was most recently generated. For skeleton references use 
        /// whatever has been generated most recently.
        /// </summary>
        public static readonly CreationPolicy CreateOnlyRequiredGeneratorDocs_DoNotCreateSkeletonReferences = new(GeneratedDocumentCreationPolicy.CreateOnlyRequired, SkeletonReferenceCreationPolicy.DoNotCreate);
    }
}
