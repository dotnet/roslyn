// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // TODO: Find a better name for this
    internal interface IVisualStudioWorkspaceHost
    {
        void OnSolutionAdded(SolutionInfo solutionInfo);
        void OnSolutionRemoved();
        void OnProjectAdded(ProjectInfo projectInfo);
        void OnProjectRemoved(ProjectId projectId);
        void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference);
        void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference);
        void OnMetadataReferenceAdded(ProjectId projectId, PortableExecutableReference metadataReference);
        void OnMetadataReferenceRemoved(ProjectId projectId, PortableExecutableReference metadataReference);
        void OnDocumentAdded(DocumentInfo documentInfo);
        void OnDocumentRemoved(DocumentId documentId);
        void OnDocumentOpened(DocumentId documentId, ITextBuffer textBuffer, bool isCurrentContext);
        void OnDocumentClosed(DocumentId documentId, ITextBuffer textBuffer, TextLoader loader, bool updateActiveContext);
        void ClearSolution();
        void OnDocumentTextUpdatedOnDisk(DocumentId id);
        void OnAssemblyNameChanged(ProjectId id, string assemblyName);
        void OnOutputFilePathChanged(ProjectId id, string outputFilePath);
        void OnOptionsChanged(ProjectId projectId, CompilationOptions compilationOptions, ParseOptions parseOptions);
        void OnProjectNameChanged(ProjectId projectId, string name, string filePath);
        void OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference);
        void OnAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference);
        void OnAdditionalDocumentAdded(DocumentInfo documentInfo);
        void OnAdditionalDocumentRemoved(DocumentId documentInfo);
        void OnAdditionalDocumentOpened(DocumentId documentId, ITextBuffer textBuffer, bool isCurrentContext);
        void OnAdditionalDocumentClosed(DocumentId documentId, ITextBuffer textBuffer, TextLoader loader);
        void OnAdditionalDocumentTextUpdatedOnDisk(DocumentId id);
    }
}
