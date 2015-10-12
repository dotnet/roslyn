// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal abstract partial class AbstractCodeModelService : ICodeModelService
    {
        private readonly ConditionalWeakTable<SyntaxTree, IBidirectionalMap<SyntaxNodeKey, SyntaxNode>> _treeToNodeKeyMaps =
            new ConditionalWeakTable<SyntaxTree, IBidirectionalMap<SyntaxNodeKey, SyntaxNode>>();

        protected readonly ISyntaxFactsService SyntaxFactsService;

        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly AbstractNodeNameGenerator _nodeNameGenerator;
        private readonly AbstractNodeLocator _nodeLocator;
        private readonly AbstractCodeModelEventCollector _eventCollector;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        private readonly IFormattingRule _lineAdjustmentFormattingRule;
        private readonly IFormattingRule _endRegionFormattingRule;

        protected AbstractCodeModelService(
            HostLanguageServices languageServiceProvider,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            IFormattingRule lineAdjustmentFormattingRule,
            IFormattingRule endRegionFormattingRule)
        {
            Debug.Assert(languageServiceProvider != null);
            Debug.Assert(editorOptionsFactoryService != null);

            this.SyntaxFactsService = languageServiceProvider.GetService<ISyntaxFactsService>();

            _editorOptionsFactoryService = editorOptionsFactoryService;
            _lineAdjustmentFormattingRule = lineAdjustmentFormattingRule;
            _endRegionFormattingRule = endRegionFormattingRule;
            _refactorNotifyServices = refactorNotifyServices;
            _nodeNameGenerator = CreateNodeNameGenerator();
            _nodeLocator = CreateNodeLocator();
            _eventCollector = CreateCodeModelEventCollector();
        }

        protected string GetNewLineCharacter(SourceText text)
        {
            return _editorOptionsFactoryService.GetEditorOptions(text).GetNewLineCharacter();
        }

        protected int GetTabSize(SourceText text)
        {
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            return GetTabSize(snapshot);
        }

        protected int GetTabSize(ITextSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var textBuffer = snapshot.TextBuffer;
            return _editorOptionsFactoryService.GetOptions(textBuffer).GetTabSize();
        }

        protected SyntaxToken GetTokenWithoutAnnotation(SyntaxToken current, Func<SyntaxToken, SyntaxToken> nextTokenGetter)
        {
            while (current.ContainsAnnotations)
            {
                current = nextTokenGetter(current);
            }

            return current;
        }

        protected TextSpan GetEncompassingSpan(SyntaxNode root, SyntaxToken startToken, SyntaxToken endToken)
        {
            var startPosition = startToken.SpanStart;
            var endPosition = endToken.RawKind == 0 ? root.Span.End : endToken.Span.End;

            return TextSpan.FromBounds(startPosition, endPosition);
        }

        private IBidirectionalMap<SyntaxNodeKey, SyntaxNode> BuildNodeKeyMap(SyntaxTree syntaxTree)
        {
            var nameOrdinalMap = new Dictionary<string, int>();
            var nodeKeyMap = BidirectionalMap<SyntaxNodeKey, SyntaxNode>.Empty;

            foreach (var node in GetFlattenedMemberNodes(syntaxTree))
            {
                var name = _nodeNameGenerator.GenerateName(node);

                int ordinal;
                if (!nameOrdinalMap.TryGetValue(name, out ordinal))
                {
                    ordinal = 0;
                }

                nameOrdinalMap[name] = ++ordinal;

                var key = new SyntaxNodeKey(name, ordinal);
                nodeKeyMap = nodeKeyMap.Add(key, node);
            }

            return nodeKeyMap;
        }

        private IBidirectionalMap<SyntaxNodeKey, SyntaxNode> GetNodeKeyMap(SyntaxTree syntaxTree)
        {
            return _treeToNodeKeyMaps.GetValue(syntaxTree, BuildNodeKeyMap);
        }

        public SyntaxNodeKey GetNodeKey(SyntaxNode node)
        {
            var nodeKey = TryGetNodeKey(node);

            if (nodeKey.IsEmpty)
            {
                throw new ArgumentException();
            }

            return nodeKey;
        }

        public SyntaxNodeKey TryGetNodeKey(SyntaxNode node)
        {
            var nodeKeyMap = GetNodeKeyMap(node.SyntaxTree);

            SyntaxNodeKey nodeKey;
            if (!nodeKeyMap.TryGetKey(node, out nodeKey))
            {
                return SyntaxNodeKey.Empty;
            }

            return nodeKey;
        }

        public SyntaxNode LookupNode(SyntaxNodeKey nodeKey, SyntaxTree syntaxTree)
        {
            var nodeKeyMap = GetNodeKeyMap(syntaxTree);

            SyntaxNode node;
            if (!nodeKeyMap.TryGetValue(nodeKey, out node))
            {
                throw new ArgumentException();
            }

            return node;
        }

        public bool TryLookupNode(SyntaxNodeKey nodeKey, SyntaxTree syntaxTree, out SyntaxNode node)
        {
            var nodeKeyMap = GetNodeKeyMap(syntaxTree);
            return nodeKeyMap.TryGetValue(nodeKey, out node);
        }

        public abstract bool MatchesScope(SyntaxNode node, EnvDTE.vsCMElement scope);

        public abstract IEnumerable<SyntaxNode> GetOptionNodes(SyntaxNode parent);
        public abstract IEnumerable<SyntaxNode> GetImportNodes(SyntaxNode parent);
        public abstract IEnumerable<SyntaxNode> GetAttributeNodes(SyntaxNode parent);
        public abstract IEnumerable<SyntaxNode> GetAttributeArgumentNodes(SyntaxNode parent);
        public abstract IEnumerable<SyntaxNode> GetInheritsNodes(SyntaxNode parent);
        public abstract IEnumerable<SyntaxNode> GetImplementsNodes(SyntaxNode parent);
        public abstract IEnumerable<SyntaxNode> GetParameterNodes(SyntaxNode parent);

        protected IEnumerable<SyntaxNode> GetFlattenedMemberNodes(SyntaxTree syntaxTree)
        {
            return GetMemberNodes(syntaxTree.GetRoot(), includeSelf: true, recursive: true, logicalFields: true, onlySupportedNodes: true);
        }

        protected IEnumerable<SyntaxNode> GetLogicalMemberNodes(SyntaxNode container)
        {
            return GetMemberNodes(container, includeSelf: false, recursive: false, logicalFields: true, onlySupportedNodes: false);
        }

        public IEnumerable<SyntaxNode> GetLogicalSupportedMemberNodes(SyntaxNode container)
        {
            return GetMemberNodes(container, includeSelf: false, recursive: false, logicalFields: true, onlySupportedNodes: true);
        }

        /// <summary>
        /// Retrieves the members of a specified <paramref name="container"/> node. The members that are
        /// returned can be controlled by passing various parameters.
        /// </summary>
        /// <param name="container">The <see cref="SyntaxNode"/> from which to retrieve members.</param>
        /// <param name="includeSelf">If true, the container is returned as well.</param>
        /// <param name="recursive">If true, members are recursed to return descendent members as well
        /// as immediate children. For example, a namespace would return the namespaces and types within.
        /// However, if <paramref name="recursive"/> is true, members with the namespaces and types would
        /// also be returned.</param>
        /// <param name="logicalFields">If true, field declarations are broken into their respective declarators.
        /// For example, the field "int x, y" would return two declarators, one for x and one for y in place
        /// of the field.</param>
        /// <param name="onlySupportedNodes">If true, only members supported by Code Model are returned.</param>
        public abstract IEnumerable<SyntaxNode> GetMemberNodes(SyntaxNode container, bool includeSelf, bool recursive, bool logicalFields, bool onlySupportedNodes);

        public abstract string Language { get; }
        public abstract string AssemblyAttributeString { get; }

        public EnvDTE.CodeElement CreateExternalCodeElement(CodeModelState state, ProjectId projectId, ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                    return (EnvDTE.CodeElement)ExternalCodeEvent.Create(state, projectId, (IEventSymbol)symbol);
                case SymbolKind.Field:
                    return (EnvDTE.CodeElement)ExternalCodeVariable.Create(state, projectId, (IFieldSymbol)symbol);
                case SymbolKind.Method:
                    return (EnvDTE.CodeElement)ExternalCodeFunction.Create(state, projectId, (IMethodSymbol)symbol);
                case SymbolKind.Namespace:
                    return (EnvDTE.CodeElement)ExternalCodeNamespace.Create(state, projectId, (INamespaceSymbol)symbol);
                case SymbolKind.NamedType:
                    var namedType = (INamedTypeSymbol)symbol;
                    switch (namedType.TypeKind)
                    {
                        case TypeKind.Class:
                        case TypeKind.Module:
                            return (EnvDTE.CodeElement)ExternalCodeClass.Create(state, projectId, namedType);
                        case TypeKind.Delegate:
                            return (EnvDTE.CodeElement)ExternalCodeDelegate.Create(state, projectId, namedType);
                        case TypeKind.Enum:
                            return (EnvDTE.CodeElement)ExternalCodeEnum.Create(state, projectId, namedType);
                        case TypeKind.Interface:
                            return (EnvDTE.CodeElement)ExternalCodeInterface.Create(state, projectId, namedType);
                        case TypeKind.Struct:
                            return (EnvDTE.CodeElement)ExternalCodeStruct.Create(state, projectId, namedType);
                        default:
                            throw Exceptions.ThrowEFail();
                    }

                case SymbolKind.Property:
                    return (EnvDTE.CodeElement)ExternalCodeProperty.Create(state, projectId, (IPropertySymbol)symbol);
                default:
                    throw Exceptions.ThrowEFail();
            }
        }

        /// <summary>
        /// Do not use this method directly! Instead, go through <see cref="FileCodeModel.GetOrCreateCodeElement{T}(SyntaxNode)"/>
        /// </summary>
        public abstract EnvDTE.CodeElement CreateInternalCodeElement(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNode node);

        public EnvDTE.CodeElement CreateCodeType(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.Pointer ||
                typeSymbol.TypeKind == TypeKind.TypeParameter ||
                typeSymbol.TypeKind == TypeKind.Submission)
            {
                throw Exceptions.ThrowEFail();
            }

            if (typeSymbol.TypeKind == TypeKind.Error ||
                typeSymbol.TypeKind == TypeKind.Unknown)
            {
                return ExternalCodeUnknown.Create(state, projectId, typeSymbol);
            }

            var project = state.Workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                throw Exceptions.ThrowEFail();
            }

            if (typeSymbol.TypeKind == TypeKind.Dynamic)
            {
                var obj = project.GetCompilationAsync().Result.GetSpecialType(SpecialType.System_Object);
                return (EnvDTE.CodeElement)ExternalCodeClass.Create(state, projectId, obj);
            }

            EnvDTE.CodeElement element;
            if (TryGetElementFromSource(state, project, typeSymbol, out element))
            {
                return element;
            }

            EnvDTE.vsCMElement elementKind = GetElementKind(typeSymbol);
            switch (elementKind)
            {
                case EnvDTE.vsCMElement.vsCMElementClass:
                case EnvDTE.vsCMElement.vsCMElementModule:
                    return (EnvDTE.CodeElement)ExternalCodeClass.Create(state, projectId, typeSymbol);
                case EnvDTE.vsCMElement.vsCMElementInterface:
                    return (EnvDTE.CodeElement)ExternalCodeInterface.Create(state, projectId, typeSymbol);
                case EnvDTE.vsCMElement.vsCMElementDelegate:
                    return (EnvDTE.CodeElement)ExternalCodeDelegate.Create(state, projectId, typeSymbol);
                case EnvDTE.vsCMElement.vsCMElementEnum:
                    return (EnvDTE.CodeElement)ExternalCodeEnum.Create(state, projectId, typeSymbol);
                case EnvDTE.vsCMElement.vsCMElementStruct:
                    return (EnvDTE.CodeElement)ExternalCodeStruct.Create(state, projectId, typeSymbol);
                default:
                    Debug.Fail("Unsupported element kind: " + elementKind);
                    throw Exceptions.ThrowEInvalidArg();
            }
        }

        public abstract EnvDTE.CodeTypeRef CreateCodeTypeRef(CodeModelState state, ProjectId projectId, object type);

        public abstract EnvDTE.vsCMTypeRef GetTypeKindForCodeTypeRef(ITypeSymbol typeSymbol);
        public abstract string GetAsFullNameForCodeTypeRef(ITypeSymbol typeSymbol);
        public abstract string GetAsStringForCodeTypeRef(ITypeSymbol typeSymbol);

        public abstract bool IsParameterNode(SyntaxNode node);
        public abstract bool IsAttributeNode(SyntaxNode node);
        public abstract bool IsAttributeArgumentNode(SyntaxNode node);
        public abstract bool IsOptionNode(SyntaxNode node);
        public abstract bool IsImportNode(SyntaxNode node);

        public ISymbol ResolveSymbol(Workspace workspace, ProjectId projectId, SymbolKey symbolId)
        {
            var project = workspace.CurrentSolution.GetProject(projectId);

            if (project == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return symbolId.Resolve(project.GetCompilationAsync().Result).Symbol;
        }

        protected EnvDTE.CodeFunction CreateInternalCodeAccessorFunction(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            SyntaxNode parentNode = node
                .Ancestors()
                .FirstOrDefault(n => TryGetNodeKey(n) != SyntaxNodeKey.Empty);

            if (parentNode == null)
            {
                throw new InvalidOperationException();
            }

            var parent = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
            var parentObj = ComAggregate.GetManagedObject<AbstractCodeMember>(parent);
            var accessorKind = GetAccessorKind(node);

            return CodeAccessorFunction.Create(state, parentObj, accessorKind);
        }

        protected EnvDTE.CodeAttribute CreateInternalCodeAttribute(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            var parentNode = GetEffectiveParentForAttribute(node);

            AbstractCodeElement parentObject;

            if (IsParameterNode(parentNode))
            {
                var parentElement = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
                parentObject = ComAggregate.GetManagedObject<AbstractCodeElement>(parentElement);
            }
            else
            {
                var nodeKey = parentNode.AncestorsAndSelf()
                                    .Select(n => TryGetNodeKey(n))
                                    .FirstOrDefault(nk => nk != SyntaxNodeKey.Empty);

                if (nodeKey == SyntaxNodeKey.Empty)
                {
                    // This is an assembly-level attribute.
                    parentNode = fileCodeModel.GetSyntaxRoot();

                    parentObject = null;
                }
                else
                {
                    parentNode = fileCodeModel.LookupNode(nodeKey);

                    var parentElement = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
                    parentObject = ComAggregate.GetManagedObject<AbstractCodeElement>(parentElement);
                }
            }

            string name;
            int ordinal;
            GetAttributeNameAndOrdinal(parentNode, node, out name, out ordinal);

            return CodeAttribute.Create(state, fileCodeModel, parentObject, name, ordinal);
        }

        protected EnvDTE80.CodeImport CreateInternalCodeImport(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            SyntaxNode parentNode;
            string name;

            GetImportParentAndName(node, out parentNode, out name);

            AbstractCodeElement parentObj = null;
            if (parentNode != null)
            {
                var parent = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
                parentObj = ComAggregate.GetManagedObject<AbstractCodeElement>(parent);
            }

            return CodeImport.Create(state, fileCodeModel, parentObj, name);
        }

        protected EnvDTE.CodeParameter CreateInternalCodeParameter(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            SyntaxNode parentNode = node
                .Ancestors()
                .FirstOrDefault(n => TryGetNodeKey(n) != SyntaxNodeKey.Empty);

            if (parentNode == null)
            {
                throw new InvalidOperationException();
            }

            string name = GetParameterName(node);

            var parent = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
            var parentObj = ComAggregate.GetManagedObject<AbstractCodeMember>(parent);

            return CodeParameter.Create(state, parentObj, name);
        }

        protected EnvDTE80.CodeElement2 CreateInternalCodeOptionStatement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            string name;
            int ordinal;
            GetOptionNameAndOrdinal(node.Parent, node, out name, out ordinal);

            return CodeOptionsStatement.Create(state, fileCodeModel, name, ordinal);
        }

        protected EnvDTE80.CodeElement2 CreateInternalCodeInheritsStatement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            SyntaxNode parentNode = node
                .Ancestors()
                .FirstOrDefault(n => TryGetNodeKey(n) != SyntaxNodeKey.Empty);

            if (parentNode == null)
            {
                throw new InvalidOperationException();
            }

            string namespaceName;
            int ordinal;
            GetInheritsNamespaceAndOrdinal(parentNode, node, out namespaceName, out ordinal);

            var parent = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
            var parentObj = ComAggregate.GetManagedObject<AbstractCodeMember>(parent);

            return CodeInheritsStatement.Create(state, parentObj, namespaceName, ordinal);
        }

        protected EnvDTE80.CodeElement2 CreateInternalCodeImplementsStatement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            SyntaxNode parentNode = node
                .Ancestors()
                .FirstOrDefault(n => TryGetNodeKey(n) != SyntaxNodeKey.Empty);

            if (parentNode == null)
            {
                throw new InvalidOperationException();
            }

            string namespaceName;
            int ordinal;
            GetImplementsNamespaceAndOrdinal(parentNode, node, out namespaceName, out ordinal);

            var parent = fileCodeModel.GetOrCreateCodeElement<EnvDTE.CodeElement>(parentNode);
            var parentObj = ComAggregate.GetManagedObject<AbstractCodeMember>(parent);

            return CodeImplementsStatement.Create(state, parentObj, namespaceName, ordinal);
        }

        protected EnvDTE80.CodeAttributeArgument CreateInternalCodeAttributeArgument(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node)
        {
            SyntaxNode attributeNode;
            int index;

            GetAttributeArgumentParentAndIndex(node, out attributeNode, out index);

            var codeAttribute = CreateInternalCodeAttribute(state, fileCodeModel, attributeNode);
            var codeAttributeObj = ComAggregate.GetManagedObject<CodeAttribute>(codeAttribute);

            return CodeAttributeArgument.Create(state, codeAttributeObj, index);
        }

        public abstract EnvDTE.CodeElement CreateUnknownCodeElement(CodeModelState state, FileCodeModel fileCodeModel, SyntaxNode node);
        public abstract EnvDTE.CodeElement CreateUnknownRootNamespaceCodeElement(CodeModelState state, FileCodeModel fileCodeModel);

        public abstract string GetUnescapedName(string name);

        public abstract string GetName(SyntaxNode node);
        public abstract SyntaxNode GetNodeWithName(SyntaxNode node);
        public abstract SyntaxNode SetName(SyntaxNode node, string name);

        public abstract string GetFullName(SyntaxNode node, SemanticModel semanticModel);
        public abstract string GetFullName(ISymbol symbol);

        public abstract string GetFullyQualifiedName(string name, int position, SemanticModel semanticModel);

        public void Rename(ISymbol symbol, string newName, Solution solution)
        {
            // TODO: (tomescht) make this testable through unit tests.
            // Right now we have to go through the project system to find all
            // the outstanding CodeElements which might be affected by the
            // rename. This is silly. This functionality should be moved down
            // into the service layer.

            var workspace = solution.Workspace as VisualStudioWorkspaceImpl;
            if (workspace == null)
            {
                throw Exceptions.ThrowEFail();
            }

            // Save the node keys.
            var nodeKeyValidation = new NodeKeyValidation();
            foreach (var project in workspace.ProjectTracker.Projects)
            {
                nodeKeyValidation.AddProject(project);
            }

            var optionSet = workspace.Services.GetService<IOptionService>().GetOptions();

            // Rename symbol.
            var newSolution = Renamer.RenameSymbolAsync(solution, symbol, newName, optionSet).WaitAndGetResult(CancellationToken.None);
            var changedDocuments = newSolution.GetChangedDocuments(solution);

            // Notify third parties of the coming rename operation and let exceptions propagate out
            _refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: true);

            // Update the workspace.
            if (!workspace.TryApplyChanges(newSolution))
            {
                throw Exceptions.ThrowEFail();
            }

            // Notify third parties of the completed rename operation and let exceptions propagate out
            _refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: true);

            RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);

            // Update the node keys.
            nodeKeyValidation.RestoreKeys();
        }

        public VirtualTreePoint? GetStartPoint(SyntaxNode node, EnvDTE.vsCMPart? part)
        {
            return _nodeLocator.GetStartPoint(node, part);
        }

        public VirtualTreePoint? GetEndPoint(SyntaxNode node, EnvDTE.vsCMPart? part)
        {
            return _nodeLocator.GetEndPoint(node, part);
        }

        public abstract EnvDTE.vsCMAccess GetAccess(ISymbol symbol);
        public abstract EnvDTE.vsCMAccess GetAccess(SyntaxNode node);
        public abstract SyntaxNode GetNodeWithModifiers(SyntaxNode node);
        public abstract SyntaxNode GetNodeWithType(SyntaxNode node);
        public abstract SyntaxNode GetNodeWithInitializer(SyntaxNode node);
        public abstract SyntaxNode SetAccess(SyntaxNode node, EnvDTE.vsCMAccess access);

        public abstract EnvDTE.vsCMElement GetElementKind(SyntaxNode node);

        protected EnvDTE.vsCMElement GetElementKind(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.TypeKind)
            {
                case TypeKind.Array:
                case TypeKind.Class:
                    return EnvDTE.vsCMElement.vsCMElementClass;

                case TypeKind.Interface:
                    return EnvDTE.vsCMElement.vsCMElementInterface;

                case TypeKind.Struct:
                    return EnvDTE.vsCMElement.vsCMElementStruct;

                case TypeKind.Enum:
                    return EnvDTE.vsCMElement.vsCMElementEnum;

                case TypeKind.Delegate:
                    return EnvDTE.vsCMElement.vsCMElementDelegate;

                case TypeKind.Module:
                    return EnvDTE.vsCMElement.vsCMElementModule;

                default:
                    Debug.Fail("Unexpected TypeKind: " + typeSymbol.TypeKind);
                    throw Exceptions.ThrowEInvalidArg();
            }
        }

        protected bool TryGetElementFromSource(CodeModelState state, Project project, ITypeSymbol typeSymbol, out EnvDTE.CodeElement element)
        {
            element = null;

            if (!typeSymbol.IsDefinition)
            {
                return false;
            }

            // Here's the strategy for determine what source file we'd try to return an element from.
            //     1. Prefer source files that we don't heuristically flag as generated code.
            //     2. If all of the source files are generated code, pick the first one.

            var generatedCodeRecognitionService = project.Solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();

            Compilation compilation = null;
            Tuple<DocumentId, Location> generatedCode = null;

            DocumentId chosenDocumentId = null;
            Location chosenLocation = null;

            foreach (var location in typeSymbol.Locations)
            {
                if (location.IsInSource)
                {
                    compilation = compilation ?? project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

                    if (compilation.ContainsSyntaxTree(location.SourceTree))
                    {
                        var document = project.GetDocument(location.SourceTree);

                        if (generatedCodeRecognitionService?.IsGeneratedCode(document) == false)
                        {
                            chosenLocation = location;
                            chosenDocumentId = document.Id;
                            break;
                        }
                        else
                        {
                            generatedCode = generatedCode ?? Tuple.Create(document.Id, location);
                        }
                    }
                }
            }

            if (chosenDocumentId == null && generatedCode != null)
            {
                chosenDocumentId = generatedCode.Item1;
                chosenLocation = generatedCode.Item2;
            }

            if (chosenDocumentId != null)
            {
                var fileCodeModel = state.Workspace.GetFileCodeModel(chosenDocumentId);
                if (fileCodeModel != null)
                {
                    var underlyingFileCodeModel = ComAggregate.GetManagedObject<FileCodeModel>(fileCodeModel);
                    element = underlyingFileCodeModel.CodeElementFromPosition(chosenLocation.SourceSpan.Start, GetElementKind(typeSymbol));
                    return element != null;
                }
            }

            return false;
        }

        public abstract bool IsAccessorNode(SyntaxNode node);
        public abstract MethodKind GetAccessorKind(SyntaxNode node);

        public abstract bool TryGetAccessorNode(SyntaxNode parentNode, MethodKind kind, out SyntaxNode accessorNode);
        public abstract bool TryGetParameterNode(SyntaxNode parentNode, string name, out SyntaxNode parameterNode);
        public abstract bool TryGetImportNode(SyntaxNode parentNode, string dottedName, out SyntaxNode importNode);
        public abstract bool TryGetOptionNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode optionNode);
        public abstract bool TryGetInheritsNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode inheritsNode);
        public abstract bool TryGetImplementsNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode implementsNode);
        public abstract bool TryGetAttributeNode(SyntaxNode parentNode, string name, int ordinal, out SyntaxNode attributeNode);
        public abstract bool TryGetAttributeArgumentNode(SyntaxNode attributeNode, int index, out SyntaxNode attributeArgumentNode);

        public abstract void GetOptionNameAndOrdinal(SyntaxNode parentNode, SyntaxNode optionNode, out string name, out int ordinal);
        public abstract void GetInheritsNamespaceAndOrdinal(SyntaxNode inheritsNode, SyntaxNode optionNode, out string namespaceName, out int ordinal);
        public abstract void GetImplementsNamespaceAndOrdinal(SyntaxNode implementsNode, SyntaxNode optionNode, out string namespaceName, out int ordinal);

        public abstract void GetAttributeNameAndOrdinal(SyntaxNode parentNode, SyntaxNode attributeNode, out string name, out int ordinal);
        public abstract SyntaxNode GetAttributeTargetNode(SyntaxNode attributeNode);
        public abstract string GetAttributeTarget(SyntaxNode attributeNode);
        public abstract string GetAttributeValue(SyntaxNode attributeNode);
        public abstract SyntaxNode SetAttributeTarget(SyntaxNode attributeNode, string value);
        public abstract SyntaxNode SetAttributeValue(SyntaxNode attributeNode, string value);
        public abstract SyntaxNode GetNodeWithAttributes(SyntaxNode node);
        public abstract SyntaxNode GetEffectiveParentForAttribute(SyntaxNode node);
        public abstract SyntaxNode CreateAttributeNode(string name, string value, string target = null);

        public abstract void GetAttributeArgumentParentAndIndex(SyntaxNode attributeArgumentNode, out SyntaxNode attributeNode, out int index);
        public abstract SyntaxNode CreateAttributeArgumentNode(string name, string value);

        public abstract string GetAttributeArgumentValue(SyntaxNode attributeArgumentNode);

        public abstract string GetImportAlias(SyntaxNode node);
        public abstract string GetImportNamespaceOrType(SyntaxNode node);
        public abstract void GetImportParentAndName(SyntaxNode importNode, out SyntaxNode namespaceNode, out string name);
        public abstract SyntaxNode CreateImportNode(string name, string alias = null);

        public abstract string GetParameterName(SyntaxNode node);

        public virtual string GetParameterFullName(SyntaxNode node)
        {
            return GetParameterName(node);
        }

        public abstract EnvDTE80.vsCMParameterKind GetParameterKind(SyntaxNode node);
        public abstract SyntaxNode SetParameterKind(SyntaxNode node, EnvDTE80.vsCMParameterKind kind);
        public abstract SyntaxNode CreateParameterNode(string name, string type);

        public abstract EnvDTE.vsCMFunction ValidateFunctionKind(SyntaxNode containerNode, EnvDTE.vsCMFunction kind, string name);

        public abstract bool SupportsEventThrower { get; }

        public abstract bool GetCanOverride(SyntaxNode memberNode);
        public abstract SyntaxNode SetCanOverride(SyntaxNode memberNode, bool value);

        public abstract EnvDTE80.vsCMClassKind GetClassKind(SyntaxNode typeNode, INamedTypeSymbol typeSymbol);
        public abstract SyntaxNode SetClassKind(SyntaxNode typeNode, EnvDTE80.vsCMClassKind kind);

        public abstract string GetComment(SyntaxNode node);
        public abstract SyntaxNode SetComment(SyntaxNode node, string value);

        public abstract EnvDTE80.vsCMConstKind GetConstKind(SyntaxNode variableNode);
        public abstract SyntaxNode SetConstKind(SyntaxNode variableNode, EnvDTE80.vsCMConstKind kind);

        public abstract EnvDTE80.vsCMDataTypeKind GetDataTypeKind(SyntaxNode typeNode, INamedTypeSymbol symbol);
        public abstract SyntaxNode SetDataTypeKind(SyntaxNode typeNode, EnvDTE80.vsCMDataTypeKind kind);

        public abstract string GetDocComment(SyntaxNode node);
        public abstract SyntaxNode SetDocComment(SyntaxNode node, string value);

        public abstract EnvDTE.vsCMFunction GetFunctionKind(IMethodSymbol symbol);

        public abstract EnvDTE80.vsCMInheritanceKind GetInheritanceKind(SyntaxNode typeNode, INamedTypeSymbol typeSymbol);
        public abstract SyntaxNode SetInheritanceKind(SyntaxNode typeNode, EnvDTE80.vsCMInheritanceKind kind);

        public abstract bool GetIsAbstract(SyntaxNode memberNode, ISymbol symbol);
        public abstract SyntaxNode SetIsAbstract(SyntaxNode memberNode, bool value);

        public abstract bool GetIsConstant(SyntaxNode variableNode);
        public abstract SyntaxNode SetIsConstant(SyntaxNode variableNode, bool value);

        public abstract bool GetIsDefault(SyntaxNode propertyNode);
        public abstract SyntaxNode SetIsDefault(SyntaxNode propertyNode, bool value);

        public abstract bool GetIsGeneric(SyntaxNode memberNode);

        public abstract bool GetIsPropertyStyleEvent(SyntaxNode eventNode);

        public abstract bool GetIsShared(SyntaxNode memberNode, ISymbol symbol);
        public abstract SyntaxNode SetIsShared(SyntaxNode memberNode, bool value);

        public abstract bool GetMustImplement(SyntaxNode memberNode);
        public abstract SyntaxNode SetMustImplement(SyntaxNode memberNode, bool value);

        public abstract EnvDTE80.vsCMOverrideKind GetOverrideKind(SyntaxNode memberNode);
        public abstract SyntaxNode SetOverrideKind(SyntaxNode memberNode, EnvDTE80.vsCMOverrideKind kind);

        public abstract EnvDTE80.vsCMPropertyKind GetReadWrite(SyntaxNode memberNode);

        public abstract SyntaxNode SetType(SyntaxNode node, ITypeSymbol typeSymbol);

        public abstract Document Delete(Document document, SyntaxNode node);

        public abstract string GetMethodXml(SyntaxNode node, SemanticModel semanticModel);

        public abstract string GetInitExpression(SyntaxNode node);
        public abstract SyntaxNode AddInitExpression(SyntaxNode node, string value);

        public abstract CodeGenerationDestination GetDestination(SyntaxNode containerNode);

        protected abstract Accessibility GetDefaultAccessibility(SymbolKind targetSymbolKind, CodeGenerationDestination destination);

        public Accessibility GetAccessibility(EnvDTE.vsCMAccess access, SymbolKind targetSymbolKind, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
        {
            // Note: Some EnvDTE.vsCMAccess members aren't "bitwise-mutually-exclusive"
            // Specifically, vsCMAccessProjectOrProtected (12) is a combination of vsCMAccessProject (4) and vsCMAccessProtected (8)
            // We therefore check for this first.

            if ((access & EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected) == EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
            {
                return Accessibility.ProtectedOrInternal;
            }
            else if ((access & EnvDTE.vsCMAccess.vsCMAccessPrivate) != 0)
            {
                return Accessibility.Private;
            }
            else if ((access & EnvDTE.vsCMAccess.vsCMAccessProject) != 0)
            {
                return Accessibility.Internal;
            }
            else if ((access & EnvDTE.vsCMAccess.vsCMAccessProtected) != 0)
            {
                return Accessibility.Protected;
            }
            else if ((access & EnvDTE.vsCMAccess.vsCMAccessPublic) != 0)
            {
                return Accessibility.Public;
            }
            else if ((access & EnvDTE.vsCMAccess.vsCMAccessDefault) != 0)
            {
                return GetDefaultAccessibility(targetSymbolKind, destination);
            }
            else
            {
                throw new ArgumentException(ServicesVSResources.InvalidAccess, "access");
            }
        }

        public bool GetWithEvents(EnvDTE.vsCMAccess access)
        {
            return (access & EnvDTE.vsCMAccess.vsCMAccessWithEvents) != 0;
        }

        protected SpecialType GetSpecialType(EnvDTE.vsCMTypeRef type)
        {
            // TODO(DustinCa): Verify this list against VB

            switch (type)
            {
                case EnvDTE.vsCMTypeRef.vsCMTypeRefBool:
                    return SpecialType.System_Boolean;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefByte:
                    return SpecialType.System_Byte;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefChar:
                    return SpecialType.System_Char;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefDecimal:
                    return SpecialType.System_Decimal;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefDouble:
                    return SpecialType.System_Double;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefFloat:
                    return SpecialType.System_Single;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefInt:
                    return SpecialType.System_Int32;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefLong:
                    return SpecialType.System_Int64;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefObject:
                    return SpecialType.System_Object;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefShort:
                    return SpecialType.System_Int16;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefString:
                    return SpecialType.System_String;
                case EnvDTE.vsCMTypeRef.vsCMTypeRefVoid:
                    return SpecialType.System_Void;
                default:
                    // TODO: Support vsCMTypeRef2? It doesn't appear that Dev10 C# does...
                    throw new ArgumentException();
            }
        }

        private ITypeSymbol GetSpecialType(EnvDTE.vsCMTypeRef type, Compilation compilation)
        {
            return compilation.GetSpecialType(GetSpecialType(type));
        }

        protected abstract ITypeSymbol GetTypeSymbolFromPartialName(string partialName, SemanticModel semanticModel, int position);
        public abstract ITypeSymbol GetTypeSymbolFromFullName(string fullName, Compilation compilation);

        public ITypeSymbol GetTypeSymbol(object type, SemanticModel semanticModel, int position)
        {
            if (type is EnvDTE.CodeTypeRef)
            {
                return GetTypeSymbolFromPartialName(((EnvDTE.CodeTypeRef)type).AsString, semanticModel, position);
            }
            else if (type is EnvDTE.CodeType)
            {
                return GetTypeSymbolFromFullName(((EnvDTE.CodeType)type).FullName, semanticModel.Compilation);
            }

            ITypeSymbol typeSymbol;
            if (type is EnvDTE.vsCMTypeRef || type is int)
            {
                typeSymbol = GetSpecialType((EnvDTE.vsCMTypeRef)type, semanticModel.Compilation);
            }
            else if (type is string)
            {
                typeSymbol = GetTypeSymbolFromPartialName((string)type, semanticModel, position);
            }
            else
            {
                throw new InvalidOperationException();
            }

            if (typeSymbol == null)
            {
                throw new ArgumentException();
            }

            return typeSymbol;
        }

        public abstract SyntaxNode CreateReturnDefaultValueStatement(ITypeSymbol type);

        protected abstract int GetAttributeIndexInContainer(
            SyntaxNode containerNode,
            Func<SyntaxNode, bool> predicate);

        /// <summary>
        /// The position argument is a VARIANT which may be an EnvDTE.CodeElement, an int or a string
        /// representing the name of a member. This function translates the argument and returns the
        /// 1-based position of the specified attribute.
        /// </summary>
        public int PositionVariantToAttributeInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel)
        {
            return PositionVariantToInsertionIndex(
                position,
                containerNode,
                fileCodeModel,
                GetAttributeIndexInContainer,
                GetAttributeNodes);
        }

        protected abstract int GetAttributeArgumentIndexInContainer(
            SyntaxNode containerNode,
            Func<SyntaxNode, bool> predicate);

        public int PositionVariantToAttributeArgumentInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel)
        {
            return PositionVariantToInsertionIndex(
                position,
                containerNode,
                fileCodeModel,
                GetAttributeArgumentIndexInContainer,
                GetAttributeArgumentNodes);
        }

        protected abstract int GetImportIndexInContainer(
            SyntaxNode containerNode,
            Func<SyntaxNode, bool> predicate);

        public int PositionVariantToImportInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel)
        {
            return PositionVariantToInsertionIndex(
                position,
                containerNode,
                fileCodeModel,
                GetImportIndexInContainer,
                GetImportNodes);
        }

        protected abstract int GetParameterIndexInContainer(
            SyntaxNode containerNode,
            Func<SyntaxNode, bool> predicate);

        public int PositionVariantToParameterInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel)
        {
            return PositionVariantToInsertionIndex(
                position,
                containerNode,
                fileCodeModel,
                GetParameterIndexInContainer,
                GetParameterNodes);
        }

        /// <summary>
        /// Finds the index of the first child within the container for which <paramref name="predicate"/> returns true.
        /// Note that the result is a 1-based as that is what code model expects. Returns -1 if no match is found.
        /// </summary>
        protected abstract int GetMemberIndexInContainer(
            SyntaxNode containerNode,
            Func<SyntaxNode, bool> predicate);

        /// <summary>
        /// The position argument is a VARIANT which may be an EnvDTE.CodeElement, an int or a string
        /// representing the name of a member. This function translates the argument and returns the
        /// 1-based position of the specified member.
        /// </summary>
        public int PositionVariantToMemberInsertionIndex(object position, SyntaxNode containerNode, FileCodeModel fileCodeModel)
        {
            return PositionVariantToInsertionIndex(
                position,
                containerNode,
                fileCodeModel,
                GetMemberIndexInContainer,
                n => GetMemberNodes(n, includeSelf: false, recursive: false, logicalFields: false, onlySupportedNodes: false));
        }

        private int PositionVariantToInsertionIndex(
            object position,
            SyntaxNode containerNode,
            FileCodeModel fileCodeModel,
            Func<SyntaxNode, Func<SyntaxNode, bool>, int> getIndexInContainer,
            Func<SyntaxNode, IEnumerable<SyntaxNode>> getChildNodes)
        {
            int result;

            if (position is int)
            {
                result = (int)position;
            }
            else if (position is EnvDTE.CodeElement)
            {
                var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(position);
                if (codeElement == null || codeElement.FileCodeModel != fileCodeModel)
                {
                    throw Exceptions.ThrowEInvalidArg();
                }

                var positionNode = codeElement.LookupNode();
                if (positionNode == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                result = getIndexInContainer(containerNode, child => child == positionNode);
            }
            else if (position is string)
            {
                var name = (string)position;
                result = getIndexInContainer(containerNode, child => GetName(child) == name);
            }
            else if (position == null || position == Type.Missing)
            {
                result = 0;
            }
            else
            {
                // Nothing we can handle...
                throw Exceptions.ThrowEInvalidArg();
            }

            // -1 means to insert at the end, so we'll return the last child.
            return result == -1
                ? getChildNodes(containerNode).ToArray().Length
                : result;
        }

        protected abstract SyntaxNode GetFieldFromVariableNode(SyntaxNode variableNode);
        protected abstract SyntaxNode GetVariableFromFieldNode(SyntaxNode fieldNode);
        protected abstract SyntaxNode GetAttributeFromAttributeDeclarationNode(SyntaxNode attributeDeclarationNode);

        protected void GetNodesAroundInsertionIndex<TSyntaxNode>(
            TSyntaxNode containerNode,
            int childIndexToInsertAfter,
            out TSyntaxNode insertBeforeNode,
            out TSyntaxNode insertAfterNode)
            where TSyntaxNode : SyntaxNode
        {
            var childNodes = GetLogicalMemberNodes(containerNode).ToArray();

            // Note: childIndexToInsertAfter is 1-based but can be 0, meaning insert before any other members.
            // If it isn't 0, it means to insert the member node *after* the node at the 1-based index.
            Debug.Assert(childIndexToInsertAfter >= 0 && childIndexToInsertAfter <= childNodes.Length);

            // Initialize the nodes that we'll insert the new member before and after.
            insertBeforeNode = null;
            insertAfterNode = null;

            if (childIndexToInsertAfter == 0)
            {
                if (childNodes.Length > 0)
                {
                    insertBeforeNode = (TSyntaxNode)childNodes[0];
                }
            }
            else
            {
                insertAfterNode = (TSyntaxNode)childNodes[childIndexToInsertAfter - 1];
                if (childIndexToInsertAfter < childNodes.Length)
                {
                    insertBeforeNode = (TSyntaxNode)childNodes[childIndexToInsertAfter];
                }
            }

            if (insertBeforeNode != null)
            {
                insertBeforeNode = (TSyntaxNode)GetFieldFromVariableNode(insertBeforeNode);
            }

            if (insertAfterNode != null)
            {
                insertAfterNode = (TSyntaxNode)GetFieldFromVariableNode(insertAfterNode);
            }
        }

        private int GetMemberInsertionIndex(SyntaxNode container, int insertionIndex)
        {
            var childNodes = GetLogicalMemberNodes(container).ToArray();

            // Note: childIndexToInsertAfter is 1-based but can be 0, meaning insert before any other members.
            // If it isn't 0, it means to insert the member node *after* the node at the 1-based index.
            Debug.Assert(insertionIndex >= 0 && insertionIndex <= childNodes.Length);

            if (insertionIndex == 0)
            {
                return 0;
            }
            else
            {
                var nodeAtIndex = GetFieldFromVariableNode(childNodes[insertionIndex - 1]);
                return GetMemberNodes(container, includeSelf: false, recursive: false, logicalFields: false, onlySupportedNodes: false).ToList().IndexOf(nodeAtIndex) + 1;
            }
        }

        private int GetAttributeArgumentInsertionIndex(SyntaxNode container, int insertionIndex)
        {
            return insertionIndex;
        }

        private int GetAttributeInsertionIndex(SyntaxNode container, int insertionIndex)
        {
            return insertionIndex;
        }

        private int GetImportInsertionIndex(SyntaxNode container, int insertionIndex)
        {
            return insertionIndex;
        }

        private int GetParameterInsertionIndex(SyntaxNode container, int insertionIndex)
        {
            return insertionIndex;
        }

        protected abstract bool IsCodeModelNode(SyntaxNode node);

        protected abstract TextSpan GetSpanToFormat(SyntaxNode root, TextSpan span);

        protected abstract SyntaxNode InsertMemberNodeIntoContainer(int index, SyntaxNode member, SyntaxNode container);
        protected abstract SyntaxNode InsertAttributeArgumentIntoContainer(int index, SyntaxNode attributeArgument, SyntaxNode container);
        protected abstract SyntaxNode InsertAttributeListIntoContainer(int index, SyntaxNode attribute, SyntaxNode container);
        protected abstract SyntaxNode InsertImportIntoContainer(int index, SyntaxNode import, SyntaxNode container);
        protected abstract SyntaxNode InsertParameterIntoContainer(int index, SyntaxNode parameter, SyntaxNode container);

        private Document FormatAnnotatedNode(Document document, SyntaxAnnotation annotation, IEnumerable<IFormattingRule> additionalRules, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var annotatedNode = root.GetAnnotatedNodesAndTokens(annotation).Single().AsNode();
            var formattingSpan = GetSpanToFormat(root, annotatedNode.FullSpan);

            var formattingRules = Formatter.GetDefaultFormattingRules(document);
            if (additionalRules != null)
            {
                formattingRules = additionalRules.Concat(formattingRules);
            }

            return Formatter.FormatAsync(
                document,
                new TextSpan[] { formattingSpan },
                options: null,
                rules: formattingRules,
                cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
        }

        private SyntaxNode InsertNode(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode node,
            Func<int, SyntaxNode, SyntaxNode, SyntaxNode> insertNodeIntoContainer,
            CancellationToken cancellationToken,
            out Document newDocument)
        {
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            // Annotate the member we're inserting so we can get back to it.
            var annotation = new SyntaxAnnotation();

            var gen = SyntaxGenerator.GetGenerator(document);
            node = node.WithAdditionalAnnotations(annotation);

            if (gen.GetDeclarationKind(node) != DeclarationKind.NamespaceImport)
            {
                // REVIEW: how simplifier ever worked for code model? nobody added simplifier.Annotation before?
                node = node.WithAdditionalAnnotations(Simplifier.Annotation);
            }

            var newContainerNode = insertNodeIntoContainer(insertionIndex, node, containerNode);
            var newRoot = root.ReplaceNode(containerNode, newContainerNode);
            document = document.WithSyntaxRoot(newRoot);

            if (!batchMode)
            {
                document = Simplifier.ReduceAsync(document, annotation, optionSet: null, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            }

            document = FormatAnnotatedNode(document, annotation, new[] { _lineAdjustmentFormattingRule, _endRegionFormattingRule }, cancellationToken);

            // out param
            newDocument = document;

            // new node
            return document
                .GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken)
                .GetAnnotatedNodesAndTokens(annotation)
                .Single()
                .AsNode();
        }

        /// <summary>
        /// Override to determine whether <param name="newNode"/> adds a method body to <param name="node"/>.
        /// This is used to determine whether a blank line should be added inside the body when formatting.
        /// </summary>
        protected abstract bool AddBlankLineToMethodBody(SyntaxNode node, SyntaxNode newNode);

        public Document UpdateNode(
            Document document,
            SyntaxNode node,
            SyntaxNode newNode,
            CancellationToken cancellationToken)
        {
            // Annotate the member we're inserting so we can get back to it.
            var annotation = new SyntaxAnnotation();

            // REVIEW: how simplifier ever worked for code model? nobody added simplifier.Annotation before?
            var annotatedNode = newNode.WithAdditionalAnnotations(annotation, Simplifier.Annotation);

            var oldRoot = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(node, annotatedNode);

            document = document.WithSyntaxRoot(newRoot);

            var additionalRules = AddBlankLineToMethodBody(node, newNode)
                ? SpecializedCollections.SingletonEnumerable(_lineAdjustmentFormattingRule)
                : null;

            document = FormatAnnotatedNode(document, annotation, additionalRules, cancellationToken);

            return document;
        }

        public SyntaxNode InsertAttribute(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode attributeNode,
            CancellationToken cancellationToken,
            out Document newDocument)
        {
            var finalNode = InsertNode(
                document,
                batchMode,
                GetAttributeInsertionIndex(containerNode, insertionIndex),
                containerNode,
                attributeNode,
                InsertAttributeListIntoContainer,
                cancellationToken,
                out newDocument);

            return GetAttributeFromAttributeDeclarationNode(finalNode);
        }

        public SyntaxNode InsertAttributeArgument(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode attributeArgumentNode,
            CancellationToken cancellationToken,
            out Document newDocument)
        {
            var finalNode = InsertNode(
                document,
                batchMode,
                GetAttributeArgumentInsertionIndex(containerNode, insertionIndex),
                containerNode,
                attributeArgumentNode,
                InsertAttributeArgumentIntoContainer,
                cancellationToken,
                out newDocument);

            return finalNode;
        }

        public SyntaxNode InsertImport(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode importNode,
            CancellationToken cancellationToken,
            out Document newDocument)
        {
            var finalNode = InsertNode(
                document,
                batchMode,
                GetImportInsertionIndex(containerNode, insertionIndex),
                containerNode,
                importNode,
                InsertImportIntoContainer,
                cancellationToken,
                out newDocument);

            return finalNode;
        }

        public SyntaxNode InsertParameter(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode parameterNode,
            CancellationToken cancellationToken,
            out Document newDocument)
        {
            var finalNode = InsertNode(
                document,
                batchMode,
                GetParameterInsertionIndex(containerNode, insertionIndex),
                containerNode,
                parameterNode,
                InsertParameterIntoContainer,
                cancellationToken,
                out newDocument);

            return finalNode;
        }

        public SyntaxNode InsertMember(
            Document document,
            bool batchMode,
            int insertionIndex,
            SyntaxNode containerNode,
            SyntaxNode memberNode,
            CancellationToken cancellationToken,
            out Document newDocument)
        {
            var finalNode = InsertNode(
                document,
                batchMode,
                GetMemberInsertionIndex(containerNode, insertionIndex),
                containerNode,
                memberNode,
                InsertMemberNodeIntoContainer,
                cancellationToken,
                out newDocument);

            return GetVariableFromFieldNode(finalNode);
        }

        public Queue<CodeModelEvent> CollectCodeModelEvents(SyntaxTree oldTree, SyntaxTree newTree)
        {
            return _eventCollector.Collect(oldTree, newTree);
        }

        public abstract bool IsNamespace(SyntaxNode node);
        public abstract bool IsType(SyntaxNode node);

        public virtual IList<string> GetHandledEventNames(SyntaxNode method, SemanticModel semanticModel)
        {
            // descendents may override (particularly VB).

            return SpecializedCollections.EmptyList<string>();
        }

        public virtual bool HandlesEvent(string eventName, SyntaxNode method, SemanticModel semanticModel)
        {
            // descendents may override (particularly VB).

            return false;
        }

        public virtual Document AddHandlesClause(Document document, string eventName, SyntaxNode method, CancellationToken cancellationToken)
        {
            // descendents may override (particularly VB).

            return document;
        }

        public virtual Document RemoveHandlesClause(Document document, string eventName, SyntaxNode method, CancellationToken cancellationToken)
        {
            // descendents may override (particularly VB).

            return document;
        }

        public abstract string[] GetFunctionExtenderNames();
        public abstract object GetFunctionExtender(string name, SyntaxNode node, ISymbol symbol);
        public abstract string[] GetPropertyExtenderNames();
        public abstract object GetPropertyExtender(string name, SyntaxNode node, ISymbol symbol);
        public abstract string[] GetExternalTypeExtenderNames();
        public abstract object GetExternalTypeExtender(string name, string externalLocation);
        public abstract string[] GetTypeExtenderNames();
        public abstract object GetTypeExtender(string name, AbstractCodeType codeType);

        public abstract bool IsValidBaseType(SyntaxNode node, ITypeSymbol typeSymbol);
        public abstract SyntaxNode AddBase(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel, int? position);
        public abstract SyntaxNode RemoveBase(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel);

        public abstract bool IsValidInterfaceType(SyntaxNode node, ITypeSymbol typeSymbol);
        public abstract SyntaxNode AddImplementedInterface(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel, int? position);
        public abstract SyntaxNode RemoveImplementedInterface(SyntaxNode node, ITypeSymbol typeSymbol, SemanticModel semanticModel);

        public abstract string GetPrototype(SyntaxNode node, ISymbol symbol, PrototypeFlags flags);

        public virtual void AttachFormatTrackingToBuffer(ITextBuffer buffer)
        {
            // can be override by languages if needed
        }

        public virtual void DetachFormatTrackingToBuffer(ITextBuffer buffer)
        {
            // can be override by languages if needed
        }

        public virtual void EnsureBufferFormatted(ITextBuffer buffer)
        {
            // can be override by languages if needed
        }
    }
}
