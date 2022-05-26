// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Serialization
{
    // TODO: Kind might not actually needed. see whether we can get rid of this
    internal enum WellKnownSynchronizationKind
    {
        Null,

        SolutionState,
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
        OptionSet,

        SerializableSourceText,

        //

        SyntaxTreeIndex,
        SymbolTreeInfo,

        ProjectReferenceChecksumCollection,
        MetadataReferenceChecksumCollection,
        AnalyzerReferenceChecksumCollection,
        TextDocumentChecksumCollection,
        DocumentChecksumCollection,
        AnalyzerConfigDocumentChecksumCollection,
        ProjectChecksumCollection,
        SolutionStateChecksums,
        ProjectStateChecksums,
        DocumentStateChecksums,
    }
}
