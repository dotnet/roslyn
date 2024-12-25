// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

internal partial class ContainedLanguage
{
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
    private readonly Guid _languageServiceGuid;

    protected readonly Workspace Workspace;
    protected readonly IComponentModel ComponentModel;

    public ProjectSystemProject? Project { get; }

    protected readonly ContainedDocument ContainedDocument;

    public IVsTextBufferCoordinator BufferCoordinator { get; protected set; }

    /// <summary>
    /// The subject (secondary) buffer that contains the C# or VB code.
    /// </summary>
    public ITextBuffer SubjectBuffer { get; }

    /// <summary>
    /// The underlying buffer that contains C# or VB code. NOTE: This is NOT the "document" buffer
    /// that is saved to disk.  Instead it is the view that the user sees.  The normal buffer graph
    /// in Venus includes 4 buffers:
    /// <code>
    ///            SurfaceBuffer/Databuffer (projection)
    ///             /                               |
    /// Subject Buffer (C#/VB projection)           |
    ///             |                               |
    /// Inert (generated) C#/VB Buffer         Document (aspx) buffer
    /// </code>
    /// In normal circumstance, the Subject and Inert C# buffer are identical in content, and the
    /// Surface and Document are also identical.  The Subject Buffer is the one that is part of the
    /// workspace, that most language operations deal with.  The surface buffer is the one that the
    /// view is created over, and the Document buffer is the one that is saved to disk.
    /// </summary>
    public ITextBuffer DataBuffer { get; }

    // Set when a TextViewFilter is set.  We hold onto this to keep our TagSource objects alive even if Venus
    // disconnects the subject buffer from the view temporarily (which they do frequently).  Otherwise, we have to
    // re-compute all of the tag data when they re-connect it, and this causes issues like classification
    // flickering.
    private readonly ITagAggregator<ITag> _bufferTagAggregator;

    internal ContainedLanguage(
        IVsTextBufferCoordinator bufferCoordinator,
        IComponentModel componentModel,
        Workspace workspace,
        ProjectId projectId,
        ProjectSystemProject? project,
        Guid languageServiceGuid,
        AbstractFormattingRule? vbHelperFormattingRule = null)
    {
        BufferCoordinator = bufferCoordinator;
        ComponentModel = componentModel;
        Project = project;
        _languageServiceGuid = languageServiceGuid;

        Workspace = workspace;

        _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
        _diagnosticAnalyzerService = componentModel.GetService<IDiagnosticAnalyzerService>();

        // Get the ITextBuffer for the secondary buffer
        Marshal.ThrowExceptionForHR(bufferCoordinator.GetSecondaryBuffer(out var secondaryTextLines));
        SubjectBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(secondaryTextLines)!;

        // Get the ITextBuffer for the primary buffer
        Marshal.ThrowExceptionForHR(bufferCoordinator.GetPrimaryBuffer(out var primaryTextLines));
        DataBuffer = _editorAdaptersFactoryService.GetDataBuffer(primaryTextLines)!;

        // Create our tagger
        var bufferTagAggregatorFactory = ComponentModel.GetService<IBufferTagAggregatorFactoryService>();
        _bufferTagAggregator = bufferTagAggregatorFactory.CreateTagAggregator<ITag>(SubjectBuffer);

        var filePath = GetFilePathFromBuffers();
        DocumentId documentId;

        if (this.Project != null)
        {
            documentId = this.Project.AddSourceTextContainer(
                SubjectBuffer.AsTextContainer(),
                filePath,
                sourceCodeKind: SourceCodeKind.Regular,
                folders: default,
                designTimeOnly: true,
                documentServiceProvider: new ContainedDocument.DocumentServiceProvider(DataBuffer));
        }
        else
        {
            documentId = DocumentId.CreateNewId(projectId, $"{nameof(ContainedDocument)}: {filePath}");

            // We must jam a document into an existing workspace, which we'll assume is safe to do with OnDocumentAdded
            Workspace.OnDocumentAdded(DocumentInfo.Create(
                documentId,
                name: filePath,
                loader: null,
                filePath: filePath));

            Workspace.OnDocumentOpened(documentId, SubjectBuffer.AsTextContainer());
        }

        ContainedDocument = new ContainedDocument(
            documentId,
            subjectBuffer: SubjectBuffer,
            dataBuffer: DataBuffer,
            BufferCoordinator,
            Workspace,
            Project,
            ComponentModel,
            vbHelperFormattingRule);

        // link subject buffer back to the ContainedDocument, so that we can find it given just the buffer:
        SubjectBuffer.Properties.AddProperty(typeof(IContainedDocument), ContainedDocument);

        // TODO: Can contained documents be linked or shared?
        this.DataBuffer.Changed += OnDataBufferChanged;
    }

    public IGlobalOptionService GlobalOptions => _diagnosticAnalyzerService.GlobalOptions;

    private void OnDisconnect()
    {
        this.DataBuffer.Changed -= OnDataBufferChanged;

        if (this.Project != null)
        {
            this.Project.RemoveSourceTextContainer(SubjectBuffer.AsTextContainer());
        }
        else
        {
            // It's possible the host of the workspace might have already removed the entire project
            if (Workspace.CurrentSolution.ContainsDocument(ContainedDocument.Id))
            {
                Workspace.OnDocumentRemoved(ContainedDocument.Id);
            }
        }

        this.ContainedDocument.Dispose();

        _bufferTagAggregator?.Dispose();
    }

    private void OnDataBufferChanged(object sender, TextContentChangedEventArgs e)
    {
        // we don't actually care what has changed in primary buffer. we just want to re-analyze secondary buffer
        // when primary buffer has changed to update diagnostic positions.
        _diagnosticAnalyzerService.RequestDiagnosticRefresh();
    }

    public string GetFilePathFromBuffers()
    {
        var textDocumentFactoryService = ComponentModel.GetService<ITextDocumentFactoryService>();

        // Try to get the file path from the secondary buffer
        if (!textDocumentFactoryService.TryGetTextDocument(SubjectBuffer, out var document))
        {
            // Fallback to the primary buffer
            textDocumentFactoryService.TryGetTextDocument(DataBuffer, out document);
        }

        if (document == null)
        {
            FatalError.ReportAndPropagate(new InvalidOperationException("Failed to get an ITextDocument for a contained document"));
        }

        return document?.FilePath ?? string.Empty;
    }
}
