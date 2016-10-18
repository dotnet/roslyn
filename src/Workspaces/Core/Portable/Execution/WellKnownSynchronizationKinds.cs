// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Serialization
{
    // TODO: Kind might not actually needed. see whether we can get rid of this
    internal static class WellKnownSynchronizationKinds
    {
        public const string Null = nameof(Null);

        public const string SolutionState = nameof(SolutionStateChecksums);
        public const string ProjectState = nameof(ProjectStateChecksums);
        public const string DocumentState = nameof(DocumentStateChecksums);

        public const string Projects = nameof(ProjectChecksumCollection);
        public const string Documents = nameof(DocumentChecksumCollection);
        public const string TextDocuments = nameof(TextDocumentChecksumCollection);
        public const string ProjectReferences = nameof(ProjectReferenceChecksumCollection);
        public const string MetadataReferences = nameof(MetadataReferenceChecksumCollection);
        public const string AnalyzerReferences = nameof(AnalyzerReferenceChecksumCollection);

        public const string SolutionAttributes = nameof(SolutionInfo.SolutionAttributes);
        public const string ProjectAttributes = nameof(ProjectInfo.ProjectAttributes);
        public const string DocumentAttributes = nameof(DocumentInfo.DocumentAttributes);

        public const string CompilationOptions = nameof(CompilationOptions);
        public const string ParseOptions = nameof(ParseOptions);
        public const string ProjectReference = nameof(ProjectReference);
        public const string MetadataReference = nameof(MetadataReference);
        public const string AnalyzerReference = nameof(AnalyzerReference);
        public const string SourceText = nameof(SourceText);
        public const string OptionSet = nameof(OptionSet);

        public const string RecoverableSourceText = nameof(RecoverableSourceText);
    }
}
