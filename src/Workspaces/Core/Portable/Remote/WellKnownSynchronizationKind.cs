// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Serialization;

internal enum WellKnownSynchronizationKind
{
    // Start at a different value from 0 so that if we ever get 0 we know it's a bug.

    SolutionState = 1,
    ProjectState,
    DocumentState,

    ChecksumCollection,

    SolutionAttributes,
    ProjectAttributes,
    DocumentAttributes,
    SourceGeneratedDocumentIdentity,

    CompilationOptions,
    ParseOptions,
    ProjectReference,
    MetadataReference,
    AnalyzerReference,
    SourceText,

    SerializableSourceText,
}
