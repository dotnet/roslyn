// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Serialization
{
    // TODO: Kind might not actually needed. see whether we can get rid of this
    internal enum WellKnownSynchronizationKind
    {
        Null,

        SolutionState,
        ProjectState,
        DocumentState,

        Projects,
        Documents,
        TextDocuments,
        AnalyzerConfigDocuments,
        ProjectReferences,
        MetadataReferences,
        AnalyzerReferences,

        SolutionAttributes,
        ProjectAttributes,
        DocumentAttributes,

        CompilationOptions,
        ParseOptions,
        ProjectReference,
        MetadataReference,
        AnalyzerReference,
        SourceText,
        OptionSet,

        RecoverableSourceText,

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
