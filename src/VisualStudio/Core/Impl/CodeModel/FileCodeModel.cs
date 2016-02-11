// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Implementations of EnvDTE.FileCodeModel for both languages.
    /// </summary>
    public sealed partial class FileCodeModel : AbstractCodeModelObject, EnvDTE.FileCodeModel, EnvDTE80.FileCodeModel2, ICodeElementContainer<AbstractCodeElement>, IVBFileCodeModelEvents, ICSCodeModelRefactoring
    {
        internal static ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> Create(
            CodeModelState state,
            object parent,
            DocumentId documentId,
            ITextManagerAdapter textManagerAdapter)
        {
            return new FileCodeModel(state, parent, documentId, textManagerAdapter).GetComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>();
        }

        private readonly ComHandle<object, object> _parentHandle;

        /// <summary>
        /// Don't use directly. Instead, call <see cref="GetDocumentId()"/>.
        /// </summary>
        private DocumentId _documentId;

        // Note: these are only valid when the underlying file is being renamed. Do not use.
        private ProjectId _incomingProjectId;
        private string _incomingFilePath;
        private Document _previousDocument;

        private readonly ITextManagerAdapter _textManagerAdapter;

        private readonly CleanableWeakComHandleTable<SyntaxNodeKey, EnvDTE.CodeElement> _codeElementTable;

        // These are used during batching.
        private bool _batchMode;
        private List<AbstractKeyedCodeElement> _batchElements;
        private Document _batchDocument;

        // track state to make sure we open editor only once
        private int _editCount;
        private IInvisibleEditor _invisibleEditor;

        private SyntaxTree _lastSyntaxTree;

        private FileCodeModel(
            CodeModelState state,
            object parent,
            DocumentId documentId,
            ITextManagerAdapter textManagerAdapter)
            : base(state)
        {
            Debug.Assert(documentId != null);
            Debug.Assert(textManagerAdapter != null);

            _parentHandle = new ComHandle<object, object>(parent);
            _documentId = documentId;
            _textManagerAdapter = textManagerAdapter;

            _codeElementTable = new CleanableWeakComHandleTable<SyntaxNodeKey, EnvDTE.CodeElement>();

            _batchMode = false;
            _batchDocument = null;
            _lastSyntaxTree = GetSyntaxTree();
        }

        internal ITextManagerAdapter TextManagerAdapter
        {
            get { return _textManagerAdapter; }
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

            if (_documentId != null)
            {
                _previousDocument = Workspace.CurrentSolution.GetDocument(_documentId);
            }

            _incomingFilePath = newFilePath;
            _incomingProjectId = _documentId.ProjectId;

            _documentId = null;
        }

        internal override void Shutdown()
        {
            if (_invisibleEditor != null)
            {
                // we are shutting down, so do not worry about editCount. If the editor is still alive, dispose it.
                CodeModelService.DetachFormatTrackingToBuffer(_invisibleEditor.TextBuffer);
                _invisibleEditor.Dispose();
                _invisibleEditor = null;
            }

            base.Shutdown();
        }

        private bool TryGetDocumentId(out DocumentId documentId)
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

            var project = ((VisualStudioWorkspaceImpl)this.State.Workspace).ProjectTracker.GetProject(_incomingProjectId);
            if (project == null)
            {
                return false;
            }

            var hostDocument = project.GetCurrentDocumentFromPath(_incomingFilePath);
            if (hostDocument == null)
            {
                return false;
            }

            _documentId = hostDocument.Id;
            _incomingProjectId = null;
            _incomingFilePath = null;
            _previousDocument = null;

            documentId = _documentId;
            return true;
        }

        internal DocumentId GetDocumentId()
        {
            if (_documentId != null)
            {
                return _documentId;
            }

            DocumentId documentId;
            if (TryGetDocumentId(out documentId))
            {
                return documentId;
            }

            throw Exceptions.ThrowEUnexpected();
        }

        internal void UpdateCodeElementNodeKey(AbstractKeyedCodeElement keyedElement, SyntaxNodeKey oldNodeKey, SyntaxNodeKey newNodeKey)
        {
            EnvDTE.CodeElement codeElement;
            if (!_codeElementTable.TryGetValue(oldNodeKey, out codeElement))
            {
                throw new InvalidOperationException($"Could not find {oldNodeKey} in Code Model element table.");
            }

            _codeElementTable.Remove(oldNodeKey);

            var managedElement = ComAggregate.GetManagedObject<AbstractKeyedCodeElement>(codeElement);
            if (!object.Equals(managedElement, keyedElement))
            {
                throw new InvalidOperationException($"Unexpected failure in Code Model while updating node keys {oldNodeKey} -> {newNodeKey}");
            }

            _codeElementTable.Add(newNodeKey, codeElement);
        }

        internal void OnCodeElementCreated(SyntaxNodeKey nodeKey, EnvDTE.CodeElement element)
        {
            _codeElementTable.Add(nodeKey, element);
        }

        internal void OnBeforeCodeElementCreated(SyntaxNode node)
        {
            // It's conceivable that a consumer is creating a code element with the same node key as a "dead" element
            // that hasn't been removed from the cache yet. For example, the element could have been "deleted" by
            // simply replacing its text in the underlying buffer. To handle this situation, we test to see if the
            // element is "dead" by checking whether it's underlying node is invalid (that is, it can't be found by
            // its node key). If the element is "dead", we'll go ahead and remove it from the cache here to avoid a
            // collision with the new element.

            var nodeKey = CodeModelService.TryGetNodeKey(node);
            EnvDTE.CodeElement codeElement;
            if (!nodeKey.IsEmpty && _codeElementTable.TryGetValue(nodeKey, out codeElement))
            {
                var managedElement = ComAggregate.GetManagedObject<AbstractKeyedCodeElement>(codeElement);
                if (managedElement?.IsValidNode() != true)
                {
                    _codeElementTable.Remove(nodeKey);
                }
            }
        }

        internal void OnCodeElementDeleted(SyntaxNodeKey nodeKey)
        {
            _codeElementTable.Remove(nodeKey);
        }

        internal T GetOrCreateCodeElement<T>(SyntaxNode node)
        {
            var nodeKey = CodeModelService.TryGetNodeKey(node);

            if (!nodeKey.IsEmpty)
            {
                // Since the node already has a key, check to see if a code element already
                // exists for it. If so, return that element it it's still valid; otherwise,
                // remove it from the table.

                EnvDTE.CodeElement codeElement;
                if (_codeElementTable.TryGetValue(nodeKey, out codeElement))
                {
                    if (codeElement != null)
                    {
                        var element = ComAggregate.TryGetManagedObject<AbstractCodeElement>(codeElement);
                        if (element.IsValidNode())
                        {
                            if (codeElement is T)
                            {
                                return (T)codeElement;
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
                Debug.Assert(_invisibleEditor != null);
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

                var formatted = Formatter.FormatAsync(result, Formatter.Annotation).WaitAndGetResult_CodeModel(CancellationToken.None);
                formatted = Formatter.FormatAsync(formatted, SyntaxAnnotation.ElasticAnnotation).WaitAndGetResult_CodeModel(CancellationToken.None);

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
                workspace.TryApplyChanges(document.Project.Solution);
            }
        }

        internal Document GetDocument()
        {
            Document document;
            if (!TryGetDocument(out document))
            {
                throw Exceptions.ThrowEFail();
            }

            return document;
        }

        internal bool TryGetDocument(out Document document)
        {
            if (IsBatchOpen && _batchDocument != null)
            {
                document = _batchDocument;
                return true;
            }

            DocumentId documentId;
            if (!TryGetDocumentId(out documentId) && _previousDocument != null)
            {
                document = _previousDocument;
            }
            else
            {
                document = Workspace.CurrentSolution.GetDocument(GetDocumentId());
            }

            return document != null;
        }

        internal SyntaxTree GetSyntaxTree()
        {
            return GetDocument()
                .GetSyntaxTreeAsync(CancellationToken.None)
                .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        internal SyntaxNode GetSyntaxRoot()
        {
            return GetDocument()
                .GetSyntaxRootAsync(CancellationToken.None)
                .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        internal SemanticModel GetSemanticModel()
        {
            return GetDocument()
                .GetSemanticModelAsync(CancellationToken.None)
                .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        internal Compilation GetCompilation()
        {
            return GetDocument().Project
                .GetCompilationAsync(CancellationToken.None)
                .WaitAndGetResult_CodeModel(CancellationToken.None);
        }

        internal ProjectId GetProjectId()
        {
            return GetDocumentId().ProjectId;
        }

        internal AbstractProject GetAbstractProject()
        {
            return ((VisualStudioWorkspaceImpl)Workspace).ProjectTracker.GetProject(GetProjectId());
        }

        internal SyntaxNode LookupNode(SyntaxNodeKey nodeKey)
        {
            return CodeModelService.LookupNode(nodeKey, GetSyntaxTree());
        }

        internal TSyntaxNode LookupNode<TSyntaxNode>(SyntaxNodeKey nodeKey)
            where TSyntaxNode : SyntaxNode
        {
            return CodeModelService.LookupNode(nodeKey, GetSyntaxTree()) as TSyntaxNode;
        }

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
        {
            throw Exceptions.ThrowEFail();
        }

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
        {
            throw Exceptions.ThrowEFail();
        }

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
            var line = GetDocument().GetTextAsync(CancellationToken.None).WaitAndGetResult_CodeModel(CancellationToken.None).Lines[lineNumber];
            var position = line.Start + column;

            return position;
        }

        internal EnvDTE.CodeElement CodeElementFromPosition(int position, EnvDTE.vsCMElement scope)
        {
            var root = GetSyntaxRoot();
            var leftToken = SyntaxFactsService.FindTokenOnLeftOfPosition(root, position);
            var rightToken = SyntaxFactsService.FindTokenOnRightOfPosition(root, position);

            // We apply a set of heuristics to determine which member we pick to start searching.
            var token = leftToken;
            if (leftToken != rightToken)
            {
                if (leftToken.Span.End == position && rightToken.SpanStart == position)
                {
                    // If both tokens are touching, we prefer identifiers and keywords to
                    // separators. Note that the language doesn't allow both tokens to be a
                    // keyword or identifier.
                    if (SyntaxFactsService.IsKeyword(rightToken) ||
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

            var node = parent != null
                ? parent.AncestorsAndSelf().FirstOrDefault(n => CodeModelService.MatchesScope(n, scope))
                : null;

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

        public EnvDTE.ProjectItem Parent
        {
            get { return _parentHandle.Object as EnvDTE.ProjectItem; }
        }

        public void Remove(object element)
        {
            var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);

            if (codeElement == null)
            {
                codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(this.CodeElements.Item(element));
            }

            if (codeElement == null)
            {
                throw new ArgumentException(ServicesVSResources.ElementIsNotValid, nameof(element));
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
                    _batchElements = new List<AbstractKeyedCodeElement>();
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
                    List<ValueTuple<AbstractKeyedCodeElement, SyntaxPath>> elementAndPaths = null;
                    if (_batchElements.Count > 0)
                    {
                        foreach (var element in _batchElements)
                        {
                            var node = element.LookupNode();
                            if (node != null)
                            {
                                elementAndPaths = elementAndPaths ?? new List<ValueTuple<AbstractKeyedCodeElement, SyntaxPath>>();
                                elementAndPaths.Add(ValueTuple.Create(element, new SyntaxPath(node)));
                            }
                        }
                    }

                    if (_batchDocument != null)
                    {
                        // perform expensive operations at once
                        var newDocument = Simplifier.ReduceAsync(_batchDocument, Simplifier.Annotation, cancellationToken: CancellationToken.None).WaitAndGetResult_CodeModel(CancellationToken.None);
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
                            EnvDTE.CodeElement existingElement;
                            if (_codeElementTable.TryGetValue(elementAndPath.Item1.NodeKey, out existingElement))
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

        public bool IsBatchOpen
        {
            get
            {
                return _batchMode && _editCount > 0;
            }
        }

        public EnvDTE.CodeElement ElementFromID(string id)
        {
            throw new NotImplementedException();
        }

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
        {
            FireEvents();
        }

        EnvDTE.CodeElements ICodeElementContainer<AbstractCodeElement>.GetCollection()
        {
            return CodeElements;
        }

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

                SyntaxNode node;
                if (keyedElement.TryLookupNode(out node))
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

            EnvDTE.CodeElement element;
            if (_codeElementTable.TryGetValue(globalNodeKey.NodeKey, out element))
            {
                var keyedElement = ComAggregate.GetManagedObject<AbstractKeyedCodeElement>(element);
                if (keyedElement != null)
                {
                    keyedElement.ReacquireNodeKey(globalNodeKey.Path, default(CancellationToken));
                }
            }
        }
    }
}
