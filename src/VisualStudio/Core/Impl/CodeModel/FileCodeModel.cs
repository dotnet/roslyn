// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

/// <summary>
/// Implementations of EnvDTE.FileCodeModel for both languages.
/// </summary>
public sealed partial class FileCodeModel : AbstractCodeModelObject, EnvDTE.FileCodeModel, EnvDTE80.FileCodeModel2, ICodeElementContainer<AbstractCodeElement>, IVBFileCodeModelEvents, ICSCodeModelRefactoring
{
    internal static ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> Create(
        CodeModelState state,
        object? parent,
        DocumentId documentId,
        bool isSourceGeneratorOutput,
        ITextManagerAdapter textManagerAdapter)
    {
        // Keep track that code model was accessed.  We want to get a sense of how widespread usage of it still is.
        Logger.Log(FunctionId.CodeModel_FileCodeModel_Create);
        return new FileCodeModel(state, parent, documentId, isSourceGeneratorOutput, textManagerAdapter).GetComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>();
    }

    private readonly ComHandle<object?, object?> _parentHandle;

    /// <summary>
    /// Don't use directly. Instead, call <see cref="GetDocumentId()"/>.
    /// </summary>
    private DocumentId? _documentId;
    private readonly bool _isSourceGeneratedOutput;

    // Note: these are only valid when the underlying file is being renamed. Do not use.
    private ProjectId? _incomingProjectId;
    private string? _incomingFilePath;
    private Document? _previousDocument;

    private readonly CleanableWeakComHandleTable<SyntaxNodeKey, EnvDTE.CodeElement> _codeElementTable;

    // These are used during batching.
    private bool _batchMode;
    private List<AbstractKeyedCodeElement>? _batchElements;
    private Document? _batchDocument;

    // track state to make sure we open editor only once
    private int _editCount;
    private IInvisibleEditor? _invisibleEditor;

    private SyntaxTree _lastSyntaxTree;

    private FileCodeModel(
        CodeModelState state,
        object? parent,
        DocumentId documentId,
        bool isSourceGeneratedOutput,
        ITextManagerAdapter textManagerAdapter)
        : base(state)
    {
        RoslynDebug.AssertNotNull(documentId);
        RoslynDebug.AssertNotNull(textManagerAdapter);

        _parentHandle = new ComHandle<object?, object?>(parent);
        _documentId = documentId;
        _isSourceGeneratedOutput = isSourceGeneratedOutput;
        TextManagerAdapter = textManagerAdapter;

        _codeElementTable = new CleanableWeakComHandleTable<SyntaxNodeKey, EnvDTE.CodeElement>(state.ThreadingContext);

        _batchMode = false;
        _batchDocument = null;
        _lastSyntaxTree = GetSyntaxTree();
    }

    internal ITextManagerAdapter TextManagerAdapter
    {
        get; set;
    }

    /// <summary>
    /// Internally, we store the DocumentId for the document that the FileCodeModel represents. If the underlying file
    /// is renamed, the DocumentId will become invalid because the Roslyn VS workspace treats file renames as a remove/add pair.
    /// To work around this, the FileCodeModel is notified when a file rename is about to occur. At that point, the
    /// <see cref="_documentId"/> field is null'd out and <see cref="_incomingFilePath"/> is set to the name of the new file.
    /// The next time that a FileCodeModel operation occurs that requires the DocumentId, it will be retrieved from the workspace
    /// using the <see cref="_incomingFilePath"/>.
    /// </summary>
    internal void OnRename(string newFilePath)
    {
        Debug.Assert(_editCount == 0, "FileCodeModel have an open edit and the underlying file is being renamed. This is a bug.");

        RoslynDebug.AssertNotNull(_documentId);

        _previousDocument = Workspace.CurrentSolution.GetDocument(_documentId);
        _incomingFilePath = newFilePath;
        _incomingProjectId = _documentId.ProjectId;

        _documentId = null;
    }

    internal override void Shutdown()
    {
        if (_invisibleEditor != null)
        {
            // we are shutting down, so do not worry about editCount. We will detach our format tracking from the text
            // buffer now; if anybody else had an invisible editor open to this file, we wouldn't want our format tracking
            // to trigger. We can safely do that on a background thread since it's just disconnecting a few event handlers.
            // We have to defer the shutdown of the invisible editor though as that requires talking to the UI thread.
            // We don't want to block up file removal on the UI thread since we want that path to stay asynchronous.
            CodeModelService.DetachFormatTrackingToBuffer(_invisibleEditor.TextBuffer);

            State.ProjectCodeModelFactory.ScheduleDeferredCleanupTask(
                cancellationToken =>
                {
                    // Ignore cancellationToken: we always need to call Dispose since it triggers the file save.
                    _ = cancellationToken;

                    _invisibleEditor.Dispose();
                });
        }

        base.Shutdown();
    }

    private bool TryGetDocumentId([NotNullWhen(true)] out DocumentId? documentId)
    {
        if (_documentId != null)
        {
            documentId = _documentId;
            return true;
        }

        documentId = null;

        // We don't have DocumentId, so try to retrieve it from the workspace.
        if (_incomingProjectId == null || _incomingFilePath == null)
        {
            return false;
        }

        var project = this.State.Workspace.CurrentSolution.GetProject(_incomingProjectId);
        if (project == null)
        {
            return false;
        }

        documentId = project.Solution.GetDocumentIdsWithFilePath(_incomingFilePath).FirstOrDefault(d => d.ProjectId == project.Id);
        if (documentId == null)
        {
            return false;
        }

        _documentId = documentId;
        _incomingProjectId = null;
        _incomingFilePath = null;
        _previousDocument = null;

        return true;
    }

    internal DocumentId GetDocumentId()
    {
        if (_documentId != null)
        {
            return _documentId;
        }

        if (TryGetDocumentId(out var documentId))
        {
            return documentId;
        }

        throw Exceptions.ThrowEUnexpected();
    }

    internal void UpdateCodeElementNodeKey(AbstractKeyedCodeElement keyedElement, SyntaxNodeKey oldNodeKey, SyntaxNodeKey newNodeKey)
    {
        if (!_codeElementTable.TryGetValue(oldNodeKey, out var codeElement))
        {
            throw new InvalidOperationException($"Could not find {oldNodeKey} in Code Model element table.");
        }

        _codeElementTable.Remove(oldNodeKey);

        var managedElement = ComAggregate.GetManagedObject<AbstractKeyedCodeElement>(codeElement);
        if (!object.Equals(managedElement, keyedElement))
        {
            throw new InvalidOperationException($"Unexpected failure in Code Model while updating node keys {oldNodeKey} -> {newNodeKey}");
        }

        // If we're updating this element with the same node key as an element that's already in the table,
        // just remove the old element. The old element will continue to function (through its node key), but
        // the new element will replace it in the cache.
        if (_codeElementTable.ContainsKey(newNodeKey))
        {
            _codeElementTable.Remove(newNodeKey);
        }

        _codeElementTable.Add(newNodeKey, codeElement);
    }

    internal void OnCodeElementCreated(SyntaxNodeKey nodeKey, EnvDTE.CodeElement element)
    {
        // If we're updating this element with the same node key as an element that's already in the table,
        // just remove the old element. The old element will continue to function (through its node key), but
        // the new element will replace it in the cache.
        if (_codeElementTable.ContainsKey(nodeKey))
        {
            _codeElementTable.Remove(nodeKey);
        }

        _codeElementTable.Add(nodeKey, element);
    }

    internal void OnCodeElementDeleted(SyntaxNodeKey nodeKey)
        => _codeElementTable.Remove(nodeKey);

    internal T GetOrCreateCodeElement<T>(SyntaxNode node)
    {
        var nodeKey = CodeModelService.TryGetNodeKey(node);

        if (!nodeKey.IsEmpty)
        {
            // Since the node already has a key, check to see if a code element already
            // exists for it. If so, return that element it it's still valid; otherwise,
            // remove it from the table.
            if (_codeElementTable.TryGetValue(nodeKey, out var codeElement))
            {
                if (codeElement != null)
                {
                    var element = ComAggregate.GetManagedObject<AbstractCodeElement>(codeElement);
                    if (element.IsValidNode())
                    {
                        if (codeElement is T tcodeElement)
                        {
                            return tcodeElement;
                        }

                        throw new InvalidOperationException($"Found a valid code element for {nodeKey}, but it is not of type, {typeof(T).ToString()}");
                    }
                }
            }

            // Go ahead and remove the nodeKey from the table. At this point, we'll be creating a new one.
            _codeElementTable.Remove(nodeKey);
        }

        return (T)CodeModelService.CreateInternalCodeElement(this.State, this, node);
    }

    private void InitializeEditor()
    {
        // If this is a source generated file, we can't edit it, so just block this at the very start.
        // E_FAIL is probably as a good as anything else, is and is also what we use files that go missing
        // so it's consistent for "this file isn't something you can use."
        if (_isSourceGeneratedOutput)
        {
            throw Exceptions.ThrowEFail();
        }

        _editCount++;

        if (_editCount == 1)
        {
            Debug.Assert(_invisibleEditor == null);

            _invisibleEditor = Workspace.OpenInvisibleEditor(GetDocumentId());
            CodeModelService.AttachFormatTrackingToBuffer(_invisibleEditor.TextBuffer);
        }
    }

    private void ReleaseEditor()
    {
        Debug.Assert(_editCount >= 1);

        _editCount--;
        if (_editCount == 0)
        {
            RoslynDebug.AssertNotNull(_invisibleEditor);
            CodeModelService.DetachFormatTrackingToBuffer(_invisibleEditor.TextBuffer);

            _invisibleEditor.Dispose();
            _invisibleEditor = null;
        }
    }

    internal void EnsureEditor(Action action)
    {
        InitializeEditor();
        try
        {
            action();
        }
        finally
        {
            ReleaseEditor();
        }
    }

    internal T EnsureEditor<T>(Func<T> action)
    {
        InitializeEditor();
        try
        {
            return action();
        }
        finally
        {
            ReleaseEditor();
        }
    }

    internal void PerformEdit(Func<Document, Document> action)
    {
        EnsureEditor(() =>
        {
            Debug.Assert(_invisibleEditor != null);

            var document = GetDocument();
            var workspace = document.Project.Solution.Workspace;

            var result = action(document);

            var formatted = State.ThreadingContext.JoinableTaskFactory.Run(async () =>
            {
                var formatted = await Formatter.FormatAsync(result, Formatter.Annotation, CancellationToken.None).ConfigureAwait(true);
                formatted = await Formatter.FormatAsync(formatted, SyntaxAnnotation.ElasticAnnotation, CancellationToken.None).ConfigureAwait(true);

                return formatted;
            });

            ApplyChanges(workspace, formatted);
        });
    }

    internal T PerformEdit<T>(Func<Document, Tuple<T, Document>> action) where T : SyntaxNode
    {
        return EnsureEditor(() =>
        {
            Debug.Assert(_invisibleEditor != null);

            var document = GetDocument();
            var workspace = document.Project.Solution.Workspace;

            var result = action(document);

            ApplyChanges(workspace, result.Item2);

            return result.Item1;
        });
    }

    private void ApplyChanges(Workspace workspace, Document document)
    {
        if (IsBatchOpen)
        {
            _batchDocument = document;
        }
        else
        {
            var applied = workspace.TryApplyChanges(document.Project.Solution);
            if (!applied)
            {
                FatalError.ReportAndPropagate(new Exception("Failed to apply the workspace changes."), ErrorSeverity.Critical);
            }
        }
    }

    internal Document GetDocument()
    {
        if (!TryGetDocument(out var document))
        {
            throw Exceptions.ThrowEFail();
        }

        return document;
    }

    internal bool TryGetDocument([NotNullWhen(true)] out Document? document)
    {
        if (IsBatchOpen && _batchDocument != null)
        {
            document = _batchDocument;
            return true;
        }

        if (!TryGetDocumentId(out _) && _previousDocument != null)
        {
            document = _previousDocument;
        }
        else if (_isSourceGeneratedOutput)
        {
            document = State.ThreadingContext.JoinableTaskFactory.Run(
                () => Workspace.CurrentSolution.GetSourceGeneratedDocumentAsync(GetDocumentId(), CancellationToken.None).AsTask());
        }
        else
        {
            document = Workspace.CurrentSolution.GetDocument(GetDocumentId());
        }

        return document != null;
    }

    internal SyntaxTree GetSyntaxTree()
    {
        return GetDocument().GetRequiredSyntaxTreeSynchronously(CancellationToken.None);
    }

    internal SyntaxNode GetSyntaxRoot()
    {
        return GetDocument().GetRequiredSyntaxRootSynchronously(CancellationToken.None);
    }

    internal SemanticModel GetSemanticModel()
        => State.ThreadingContext.JoinableTaskFactory.Run(() =>
        {
            return GetDocument()
                .GetRequiredSemanticModelAsync(CancellationToken.None).AsTask();
        });

    internal CodeGenerationOptions GetDocumentOptions()
        => State.ThreadingContext.JoinableTaskFactory.Run(() =>
        {
            return GetDocument()
                .GetCodeGenerationOptionsAsync(CancellationToken.None).AsTask();
        });

    internal Compilation GetCompilation()
        => State.ThreadingContext.JoinableTaskFactory.Run(() =>
        {
            return GetDocument().Project
                .GetRequiredCompilationAsync(CancellationToken.None);
        });

    internal ProjectId GetProjectId()
        => GetDocumentId().ProjectId;

    internal SyntaxNode LookupNode(SyntaxNodeKey nodeKey)
        => CodeModelService.LookupNode(nodeKey, GetSyntaxTree());

    public EnvDTE.CodeAttribute AddAttribute(string name, string value, object position)
    {
        return EnsureEditor(() =>
        {
            return AddAttribute(GetSyntaxRoot(), name, value, position, target: CodeModelService.AssemblyAttributeString);
        });
    }

    public EnvDTE.CodeClass AddClass(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
    {
        return EnsureEditor(() =>
        {
            return AddClass(GetSyntaxRoot(), name, position, bases, implementedInterfaces, access);
        });
    }

    public EnvDTE.CodeDelegate AddDelegate(string name, object type, object position, EnvDTE.vsCMAccess access)
    {
        return EnsureEditor(() =>
        {
            return AddDelegate(GetSyntaxRoot(), name, type, position, access);
        });
    }

    public EnvDTE.CodeEnum AddEnum(string name, object position, object bases, EnvDTE.vsCMAccess access)
    {
        return EnsureEditor(() =>
        {
            return AddEnum(GetSyntaxRoot(), name, position, bases, access);
        });
    }

    public EnvDTE.CodeFunction AddFunction(string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public EnvDTE80.CodeImport AddImport(string name, object position, string alias)
    {
        return EnsureEditor(() =>
        {
            return AddImport(GetSyntaxRoot(), name, position, alias);
        });
    }

    public EnvDTE.CodeInterface AddInterface(string name, object position, object bases, EnvDTE.vsCMAccess access)
    {
        return EnsureEditor(() =>
        {
            return AddInterface(GetSyntaxRoot(), name, position, bases, access);
        });
    }

    public EnvDTE.CodeNamespace AddNamespace(string name, object position)
    {
        return EnsureEditor(() =>
        {
            return AddNamespace(GetSyntaxRoot(), name, position);
        });
    }

    public EnvDTE.CodeStruct AddStruct(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
    {
        return EnsureEditor(() =>
        {
            return AddStruct(GetSyntaxRoot(), name, position, bases, implementedInterfaces, access);
        });
    }

    public EnvDTE.CodeVariable AddVariable(string name, object type, object position, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeElement CodeElementFromPoint(EnvDTE.TextPoint point, EnvDTE.vsCMElement scope)
    {
        // Can't use point.AbsoluteCharOffset because it's calculated by the native
        // implementation in GetAbsoluteOffset (in env\msenv\textmgr\autoutil.cpp)
        // to only count each newline as a single character. We need to ask for line and 
        // column and calculate the right offset ourselves. See DevDiv2 530496 for details.
        var position = GetPositionFromTextPoint(point);

        var result = CodeElementFromPosition(position, scope);
        if (result == null)
        {
            throw Exceptions.ThrowEFail();
        }

        return result;
    }

    private int GetPositionFromTextPoint(EnvDTE.TextPoint point)
    {
        var lineNumber = point.Line - 1;
        var column = point.LineCharOffset - 1;
        var line = GetDocument().GetTextSynchronously(CancellationToken.None).Lines[lineNumber];
        var position = line.Start + column;

        return position;
    }

    internal EnvDTE.CodeElement? CodeElementFromPosition(int position, EnvDTE.vsCMElement scope)
    {
        var root = GetSyntaxRoot();
        var leftToken = root.FindTokenOnLeftOfPosition(position);
        var rightToken = root.FindTokenOnRightOfPosition(position);

        // We apply a set of heuristics to determine which member we pick to start searching.
        var token = leftToken;
        if (leftToken != rightToken)
        {
            if (leftToken.Span.End == position && rightToken.SpanStart == position)
            {
                // If both tokens are touching, we prefer identifiers and keywords to
                // separators. Note that the language doesn't allow both tokens to be a
                // keyword or identifier.
                if (SyntaxFactsService.IsReservedOrContextualKeyword(rightToken) ||
                    SyntaxFactsService.IsIdentifier(rightToken))
                {
                    token = rightToken;
                }
            }
            else if (leftToken.Span.End < position && rightToken.SpanStart <= position)
            {
                // If only the right token is touching, we have to use it.
                token = rightToken;
            }
        }

        // If we ended up using the left token but the position is after that token,
        // walk up to the first node who's last token is not the leftToken. By doing this, we
        // ensure that we don't find members when the position is actually between them.
        // In that case, we should find the enclosing type or namespace.
        var parent = token.Parent;
        if (token == leftToken && position > token.Span.End)
        {
            while (parent != null)
            {
                if (parent.GetLastToken() == token)
                {
                    parent = parent.Parent;
                }
                else
                {
                    break;
                }
            }
        }

        var node = parent?.AncestorsAndSelf().FirstOrDefault(n => CodeModelService.MatchesScope(n, scope));

        if (node == null)
        {
            return null;
        }

        if (scope == EnvDTE.vsCMElement.vsCMElementAttribute ||
            scope == EnvDTE.vsCMElement.vsCMElementImportStmt ||
            scope == EnvDTE.vsCMElement.vsCMElementParameter ||
            scope == EnvDTE.vsCMElement.vsCMElementOptionStmt ||
            scope == EnvDTE.vsCMElement.vsCMElementInheritsStmt ||
            scope == EnvDTE.vsCMElement.vsCMElementImplementsStmt ||
            (scope == EnvDTE.vsCMElement.vsCMElementFunction && CodeModelService.IsAccessorNode(node)))
        {
            // Attributes, imports, parameters, Option, Inherits and Implements
            // don't have node keys of their own and won't be included in our
            // collection of elements. Delegate to the service to create these.
            return CodeModelService.CreateInternalCodeElement(State, this, node);
        }

        return GetOrCreateCodeElement<EnvDTE.CodeElement>(node);
    }

    public EnvDTE.CodeElements CodeElements
    {
        get { return NamespaceCollection.Create(this.State, this, this, SyntaxNodeKey.Empty); }
    }

    public EnvDTE.ProjectItem? Parent
    {
        get { return _parentHandle.Object as EnvDTE.ProjectItem; }
    }

    public void Remove(object element)
    {
        var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);

        codeElement ??= ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.CodeElements.Item(element));

        if (codeElement == null)
        {
            throw new ArgumentException(ServicesVSResources.Element_is_not_valid, nameof(element));
        }

        codeElement.Delete();
    }

    int IVBFileCodeModelEvents.StartEdit()
    {
        try
        {
            InitializeEditor();

            if (_editCount == 1)
            {
                _batchMode = true;
                _batchElements = [];
            }

            return VSConstants.S_OK;
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }
    }

    int IVBFileCodeModelEvents.EndEdit()
    {
        try
        {
            if (_editCount == 1)
            {
                RoslynDebug.AssertNotNull(_batchElements);
                RoslynDebug.AssertNotNull(_invisibleEditor);

                List<ValueTuple<AbstractKeyedCodeElement, SyntaxPath>>? elementAndPaths = null;
                if (_batchElements.Count > 0)
                {
                    foreach (var element in _batchElements)
                    {
                        var node = element.LookupNode();
                        if (node != null)
                        {
                            elementAndPaths ??= [];
                            elementAndPaths.Add(ValueTuple.Create(element, new SyntaxPath(node)));
                        }
                    }
                }

                if (_batchDocument != null)
                {
                    // perform expensive operations at once
                    var newDocument = State.ThreadingContext.JoinableTaskFactory.Run(async () =>
                    {
                        return await Simplifier.ReduceAsync(_batchDocument, Simplifier.Annotation, CancellationToken.None).ConfigureAwait(false);
                    });

                    _batchDocument.Project.Solution.Workspace.TryApplyChanges(newDocument.Project.Solution);

                    // done using batch document
                    _batchDocument = null;
                }

                // Ensure the file is prettylisted, even if we didn't make any edits
                CodeModelService.EnsureBufferFormatted(_invisibleEditor.TextBuffer);

                if (elementAndPaths != null)
                {
                    foreach (var elementAndPath in elementAndPaths)
                    {
                        // make sure the element is there.
                        if (_codeElementTable.TryGetValue(elementAndPath.Item1.NodeKey, out var existingElement))
                        {
                            elementAndPath.Item1.ReacquireNodeKey(elementAndPath.Item2, CancellationToken.None);
                        }

                        // make sure existing element doesn't go away (weak reference) in the middle of
                        // updating the node key
                        GC.KeepAlive(existingElement);
                    }
                }

                _batchMode = false;
                _batchElements = null;
            }

            return VSConstants.S_OK;
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }
        finally
        {
            ReleaseEditor();
        }
    }

    public void BeginBatch()
    {
        IVBFileCodeModelEvents temp = this;
        ErrorHandler.ThrowOnFailure(temp.StartEdit());
    }

    public void EndBatch()
    {
        IVBFileCodeModelEvents temp = this;
        ErrorHandler.ThrowOnFailure(temp.EndEdit());
    }

    [MemberNotNullWhen(true, nameof(_batchElements))]
    public bool IsBatchOpen
    {
        get
        {
            return _batchMode && _editCount > 0;
        }
    }

    public EnvDTE.CodeElement ElementFromID(string id)
        => throw new NotImplementedException();

    public EnvDTE80.vsCMParseStatus ParseStatus
    {
        get
        {
            var syntaxTree = GetSyntaxTree();
            return syntaxTree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)
                ? EnvDTE80.vsCMParseStatus.vsCMParseStatusError
                : EnvDTE80.vsCMParseStatus.vsCMParseStatusComplete;
        }
    }

    public void Synchronize()
        => FireEvents();

    EnvDTE.CodeElements ICodeElementContainer<AbstractCodeElement>.GetCollection()
        => CodeElements;

    internal List<GlobalNodeKey> GetCurrentNodeKeys()
    {
        var currentNodeKeys = new List<GlobalNodeKey>();

        foreach (var element in _codeElementTable.Values)
        {
            var keyedElement = ComAggregate.TryGetManagedObject<AbstractKeyedCodeElement>(element);
            if (keyedElement == null)
            {
                continue;
            }

            if (keyedElement.TryLookupNode(out var node))
            {
                var nodeKey = keyedElement.NodeKey;
                currentNodeKeys.Add(new GlobalNodeKey(nodeKey, new SyntaxPath(node)));
            }
        }

        return currentNodeKeys;
    }

    internal void ResetElementKeys(List<GlobalNodeKey> globalNodeKeys)
    {
        foreach (var globalNodeKey in globalNodeKeys)
        {
            ResetElementKey(globalNodeKey);
        }
    }

    private void ResetElementKey(GlobalNodeKey globalNodeKey)
    {
        // Failure to find the element is not an error -- it just means the code
        // element didn't exist...
        if (_codeElementTable.TryGetValue(globalNodeKey.NodeKey, out var element))
        {
            var keyedElement = ComAggregate.GetManagedObject<AbstractKeyedCodeElement>(element);
            keyedElement?.ReacquireNodeKey(globalNodeKey.Path, default);
        }
    }
}
