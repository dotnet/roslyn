// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Progression.CodeSchema;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed partial class GraphBuilder
    {
        private readonly Graph _graph = new();
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        private readonly ISet<GraphNode> _createdNodes = new HashSet<GraphNode>();
        private readonly IList<Tuple<GraphNode, GraphProperty, object>> _deferredPropertySets = new List<Tuple<GraphNode, GraphProperty, object>>();

        private readonly Dictionary<GraphNode, Project> _nodeToContextProjectMap = new();
        private readonly Dictionary<GraphNode, Document> _nodeToContextDocumentMap = new();
        private readonly Dictionary<GraphNode, ISymbol> _nodeToSymbolMap = new();

        /// <summary>
        /// The input solution. Never null.
        /// </summary>
        private readonly Solution _solution;

        public GraphBuilder(Solution solution)
        {
            _solution = solution;
        }

        public static async Task<GraphBuilder> CreateForInputNodesAsync(
            Solution solution, IEnumerable<GraphNode> inputNodes, CancellationToken cancellationToken)
        {
            var builder = new GraphBuilder(solution);

            foreach (var inputNode in inputNodes)
            {
                if (inputNode.HasCategory(CodeNodeCategories.File))
                {
                    builder.PopulateMapsForFileInputNode(inputNode, cancellationToken);
                }
                else if (!inputNode.HasCategory(CodeNodeCategories.SourceLocation))
                {
                    await builder.PopulateMapsForSymbolInputNodeAsync(inputNode, cancellationToken).ConfigureAwait(false);
                }
            }

            return builder;
        }

        private void PopulateMapsForFileInputNode(GraphNode inputNode, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                var projectPath = inputNode.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.Assembly);
                var filePath = inputNode.Id.GetNestedValueByName<Uri>(CodeGraphNodeIdName.File);

                if (projectPath == null || filePath == null)
                {
                    return;
                }

                var project = _solution.Projects.FirstOrDefault(
                    p => string.Equals(p.FilePath, projectPath.OriginalString, StringComparison.OrdinalIgnoreCase));
                if (project == null)
                {
                    return;
                }

                _nodeToContextProjectMap.Add(inputNode, project);

                var document = project.Documents.FirstOrDefault(
                    d => string.Equals(d.FilePath, filePath.OriginalString, StringComparison.OrdinalIgnoreCase));
                if (document == null)
                {
                    return;
                }

                _nodeToContextDocumentMap.Add(inputNode, document);
            }
        }

        private async Task PopulateMapsForSymbolInputNodeAsync(GraphNode inputNode, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var projectId = (ProjectId)inputNode[RoslynGraphProperties.ContextProjectId];
                if (projectId == null)
                {
                    return;
                }

                var project = _solution.GetProject(projectId);
                if (project == null)
                {
                    return;
                }

                _nodeToContextProjectMap.Add(inputNode, project);

                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbolId = (SymbolKey?)inputNode[RoslynGraphProperties.SymbolId];
                var symbol = symbolId.Value.Resolve(compilation, cancellationToken: cancellationToken).Symbol;
                if (symbol != null)
                {
                    _nodeToSymbolMap.Add(inputNode, symbol);
                }

                var documentId = (DocumentId)inputNode[RoslynGraphProperties.ContextDocumentId];
                if (documentId != null)
                {
                    var document = project.GetDocument(documentId);
                    if (document != null)
                    {
                        _nodeToContextDocumentMap.Add(inputNode, document);
                    }
                }
            }
        }

        public Project GetContextProject(GraphNode node, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                _nodeToContextProjectMap.TryGetValue(node, out var project);
                return project;
            }
        }

        public ProjectId GetContextProjectId(Project project, ISymbol symbol)
        {
            var thisProject = project.Solution.GetProject(symbol.ContainingAssembly) ?? project;
            return thisProject.Id;
        }

        public Document GetContextDocument(GraphNode node, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                _nodeToContextDocumentMap.TryGetValue(node, out var document);
                return document;
            }
        }

        public ISymbol GetSymbol(GraphNode node, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                _nodeToSymbolMap.TryGetValue(node, out var symbol);
                return symbol;
            }
        }

        public Task<GraphNode> AddNodeAsync(ISymbol symbol, GraphNode relatedNode, CancellationToken cancellationToken)
        {
            // The lack of a lock here is acceptable, since each of the functions lock, and GetContextProject/GetContextDocument
            // never change for the same input.
            return AddNodeAsync(
                symbol,
                GetContextProject(relatedNode, cancellationToken),
                GetContextDocument(relatedNode, cancellationToken),
                cancellationToken);
        }

        public async Task<GraphNode> AddNodeAsync(
            ISymbol symbol, Project contextProject, Document contextDocument, CancellationToken cancellationToken)
        {
            // Figure out what the location for this node should be. We'll arbitrarily pick the
            // first one, unless we have a contextDocument to restrict it
            var preferredLocation = symbol.Locations.FirstOrDefault(l => l.SourceTree != null);

            if (contextDocument != null)
            {
                var syntaxTree = await contextDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                // If we have one in that tree, use it
                preferredLocation = symbol.Locations.FirstOrDefault(l => l.SourceTree == syntaxTree) ?? preferredLocation;
            }

            // We may need to look up source code within this solution
            if (preferredLocation == null && symbol.Locations.Any(loc => loc.IsInMetadata))
            {
                var newSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, contextProject.Solution, cancellationToken).ConfigureAwait(false);
                if (newSymbol != null)
                    preferredLocation = newSymbol.Locations.Where(loc => loc.IsInSource).FirstOrDefault();
            }

            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var node = await GetOrCreateNodeAsync(_graph, symbol, _solution, cancellationToken).ConfigureAwait(false);

                node[RoslynGraphProperties.SymbolId] = (SymbolKey?)symbol.GetSymbolKey(cancellationToken);
                node[RoslynGraphProperties.ContextProjectId] = GetContextProjectId(contextProject, symbol);
                node[RoslynGraphProperties.ExplicitInterfaceImplementations] = symbol.ExplicitInterfaceImplementations().Select(s => s.GetSymbolKey()).ToList();
                node[RoslynGraphProperties.DeclaredAccessibility] = symbol.DeclaredAccessibility;
                node[RoslynGraphProperties.SymbolModifiers] = symbol.GetSymbolModifiers();
                node[RoslynGraphProperties.SymbolKind] = symbol.Kind;

                if (contextDocument != null)
                    node[RoslynGraphProperties.ContextDocumentId] = contextDocument.Id;

                if (preferredLocation?.SourceTree != null)
                {
                    var lineSpan = preferredLocation.GetLineSpan();
                    var sourceLocation = TryCreateSourceLocation(
                        preferredLocation.SourceTree.FilePath,
                        lineSpan.Span);
                    if (sourceLocation != null)
                        node[CodeNodeProperties.SourceLocation] = sourceLocation.Value;
                }

                // Keep track of this as a node we have added. Note this is a HashSet, so if the node was already added
                // we won't double-count.
                _createdNodes.Add(node);

                _nodeToSymbolMap[node] = symbol;
                _nodeToContextProjectMap[node] = contextProject;

                if (contextDocument != null)
                    _nodeToContextDocumentMap[node] = contextDocument;

                return node;
            }
        }

        internal static async Task<GraphNode> GetOrCreateNodeAsync(Graph graph, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            GraphNode node;

            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    node = await GetOrCreateNodeAssemblyAsync(graph, (IAssemblySymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Namespace:
                    node = await GetOrCreateNodeForNamespaceAsync(graph, (INamespaceSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    node = await GetOrCreateNodeForNamedTypeAsync(graph, (INamedTypeSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Method:
                    node = await GetOrCreateNodeForMethodAsync(graph, (IMethodSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Field:
                    node = await GetOrCreateNodeForFieldAsync(graph, (IFieldSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Property:
                    node = await GetOrCreateNodeForPropertyAsync(graph, (IPropertySymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Event:
                    node = await GetOrCreateNodeForEventAsync(graph, (IEventSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Parameter:
                    node = await GetOrCreateNodeForParameterAsync(graph, (IParameterSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                case SymbolKind.Local:
                case SymbolKind.RangeVariable:
                    node = await GetOrCreateNodeForLocalVariableAsync(graph, symbol, solution, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    throw new ArgumentException("symbol");
            }

            UpdatePropertiesForNode(symbol, node);
            UpdateLabelsForNode(symbol, solution, node);

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForParameterAsync(Graph graph, IParameterSymbol parameterSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForParameterAsync(parameterSymbol, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);
            node.AddCategory(CodeNodeCategories.Parameter);

            node.SetValue<bool>(Properties.IsByReference, parameterSymbol.RefKind == RefKind.Ref);
            node.SetValue<bool>(Properties.IsOut, parameterSymbol.RefKind == RefKind.Out);
            node.SetValue<bool>(Properties.IsParameterArray, parameterSymbol.IsParams);

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForLocalVariableAsync(Graph graph, ISymbol localSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForLocalVariableAsync(localSymbol, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);
            node.AddCategory(NodeCategories.LocalExpression);

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeAssemblyAsync(Graph graph, IAssemblySymbol assemblySymbol, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForAssemblyAsync(assemblySymbol, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);
            node.AddCategory(CodeNodeCategories.Assembly);

            return node;
        }

        private static void UpdateLabelsForNode(ISymbol symbol, Solution solution, GraphNode node)
        {
            var progressionLanguageService = solution.Workspace.Services.GetLanguageServices(symbol.Language).GetService<IProgressionLanguageService>();

            // A call from unittest may not have a proper language service.
            if (progressionLanguageService != null)
            {
                node[RoslynGraphProperties.Description] = progressionLanguageService.GetDescriptionForSymbol(symbol, includeContainingSymbol: false);
                node[RoslynGraphProperties.DescriptionWithContainingSymbol] = progressionLanguageService.GetDescriptionForSymbol(symbol, includeContainingSymbol: true);

                node[RoslynGraphProperties.FormattedLabelWithoutContainingSymbol] = progressionLanguageService.GetLabelForSymbol(symbol, includeContainingSymbol: false);
                node[RoslynGraphProperties.FormattedLabelWithContainingSymbol] = progressionLanguageService.GetLabelForSymbol(symbol, includeContainingSymbol: true);
            }

            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    var typeSymbol = (INamedTypeSymbol)symbol;
                    if (typeSymbol.IsGenericType)
                    {
                        // Symbol.name does not contain type params for generic types, so we populate them here for some requiring cases like VS properties panel.
                        node.Label = (string)node[RoslynGraphProperties.FormattedLabelWithoutContainingSymbol];

                        // Some consumers like CodeMap want to show types in an unified way for both C# and VB.
                        // Therefore, populate a common label property using only name and its type parameters.
                        // For example, VB's "Goo(Of T)" or C#'s "Goo<T>(): T" will be shown as "Goo<T>".
                        // This property will be used for drag-and-drop case.
                        var commonLabel = new System.Text.StringBuilder();
                        commonLabel.Append(typeSymbol.Name);
                        commonLabel.Append("<");
                        commonLabel.Append(string.Join(", ", typeSymbol.TypeParameters.Select(t => t.Name)));
                        commonLabel.Append(">");
                        node[Microsoft.VisualStudio.ArchitectureTools.ProgressiveReveal.ProgressiveRevealSchema.CommonLabel] = commonLabel.ToString();

                        return;
                    }
                    else
                    {
                        node.Label = symbol.Name;
                    }

                    break;

                case SymbolKind.Method:
                    var methodSymbol = (IMethodSymbol)symbol;
                    if (methodSymbol.MethodKind == MethodKind.Constructor)
                    {
                        node.Label = CodeQualifiedIdentifierBuilder.SpecialNames.GetConstructorLabel(methodSymbol.ContainingSymbol.Name);
                    }
                    else if (methodSymbol.MethodKind == MethodKind.StaticConstructor)
                    {
                        node.Label = CodeQualifiedIdentifierBuilder.SpecialNames.GetStaticConstructorLabel(methodSymbol.ContainingSymbol.Name);
                    }
                    else if (methodSymbol.MethodKind == MethodKind.Destructor)
                    {
                        node.Label = CodeQualifiedIdentifierBuilder.SpecialNames.GetFinalizerLabel(methodSymbol.ContainingSymbol.Name);
                    }
                    else
                    {
                        node.Label = methodSymbol.Name;
                    }

                    break;

                case SymbolKind.Property:
                    node.Label = symbol.MetadataName;

                    var propertySymbol = (IPropertySymbol)symbol;
                    if (propertySymbol.IsIndexer && LanguageNames.CSharp == propertySymbol.Language)
                    {
                        // For C# indexer, we will strip off the "[]"
                        node.Label = symbol.Name.Replace("[]", string.Empty);
                    }

                    break;

                case SymbolKind.Namespace:
                    // Use a name with its parents (e.g., A.B.C)
                    node.Label = symbol.ToDisplayString();
                    break;

                default:
                    node.Label = symbol.Name;
                    break;
            }

            // When a node is dragged and dropped from SE to CodeMap, its label could be reset during copying to clipboard.
            // So, we try to keep its label that we computed above in a common label property, which CodeMap can access later.
            node[Microsoft.VisualStudio.ArchitectureTools.ProgressiveReveal.ProgressiveRevealSchema.CommonLabel] = node.Label;
        }

        private static void UpdatePropertiesForNode(ISymbol symbol, GraphNode node)
        {
            // Set accessibility properties
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    node[Properties.IsPublic] = true;
                    break;

                case Accessibility.Internal:
                    node[Properties.IsInternal] = true;
                    break;

                case Accessibility.Protected:
                    node[Properties.IsProtected] = true;
                    break;

                case Accessibility.Private:
                    node[Properties.IsPrivate] = true;
                    break;

                case Accessibility.ProtectedOrInternal:
                    node[Properties.IsProtectedOrInternal] = true;
                    break;

                case Accessibility.ProtectedAndInternal:
                    node[Properties.IsProtected] = true;
                    node[Properties.IsInternal] = true;
                    break;

                case Accessibility.NotApplicable:
                    break;
            }

            // Set common properties
            if (symbol.IsAbstract)
            {
                node[Properties.IsAbstract] = true;
            }

            if (symbol.IsSealed)
            {
                // For VB module, do not set IsFinal since it's not inheritable.
                if (!symbol.IsModuleType())
                {
                    node[Properties.IsFinal] = true;
                }
            }

            if (symbol.IsStatic)
            {
                node[Properties.IsStatic] = true;
            }

            if (symbol.IsVirtual)
            {
                node[Properties.IsVirtual] = true;
            }

            if (symbol.IsOverride)
            {
                // The property name is a misnomer, but this is what the previous providers do.
                node[Microsoft.VisualStudio.Progression.DgmlProperties.IsOverloaded] = true;
            }

            // Set type-specific properties
            if (symbol is ITypeSymbol typeSymbol && typeSymbol.IsAnonymousType)
            {
                node[Properties.IsAnonymous] = true;
            }
            else if (symbol is IMethodSymbol methodSymbol)
            {
                UpdateMethodPropertiesForNode(methodSymbol, node);
            }
        }

        private static void UpdateMethodPropertiesForNode(IMethodSymbol symbol, GraphNode node)
        {
            if (symbol.HidesBaseMethodsByName)
            {
                node[Properties.IsHideBySignature] = true;
            }

            if (symbol.IsExtensionMethod)
            {
                node[Properties.IsExtension] = true;
            }

            switch (symbol.MethodKind)
            {
                case MethodKind.AnonymousFunction:
                    node[Properties.IsAnonymous] = true;
                    break;

                case MethodKind.BuiltinOperator:
                case MethodKind.UserDefinedOperator:
                    node[Properties.IsOperator] = true;
                    break;

                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    node[Properties.IsConstructor] = true;
                    break;

                case MethodKind.Conversion:
                    // Operator implicit/explicit
                    node[Properties.IsOperator] = true;
                    break;

                case MethodKind.Destructor:
                    node[Properties.IsFinalizer] = true;
                    break;

                case MethodKind.PropertyGet:
                    node[Properties.IsPropertyGet] = true;
                    break;

                case MethodKind.PropertySet:
                    node[Properties.IsPropertySet] = true;
                    break;
            }
        }

        private static async Task<GraphNode> GetOrCreateNodeForNamespaceAsync(Graph graph, INamespaceSymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForNamespaceAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);
            node.AddCategory(CodeNodeCategories.Namespace);

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForNamedTypeAsync(Graph graph, INamedTypeSymbol namedType, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForTypeAsync(namedType, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);
            string iconGroupName;

            switch (namedType.TypeKind)
            {
                case TypeKind.Class:
                    node.AddCategory(CodeNodeCategories.Class);
                    iconGroupName = "Class";
                    break;

                case TypeKind.Delegate:
                    node.AddCategory(CodeNodeCategories.Delegate);
                    iconGroupName = "Delegate";
                    break;

                case TypeKind.Enum:
                    node.AddCategory(CodeNodeCategories.Enum);
                    iconGroupName = "Enum";
                    break;

                case TypeKind.Interface:
                    node.AddCategory(CodeNodeCategories.Interface);
                    iconGroupName = "Interface";
                    break;

                case TypeKind.Module:
                    node.AddCategory(CodeNodeCategories.Module);
                    iconGroupName = "Module";
                    break;

                case TypeKind.Struct:
                    node.AddCategory(CodeNodeCategories.Struct);
                    iconGroupName = "Struct";
                    break;

                case TypeKind.Error:
                    node.AddCategory(CodeNodeCategories.Type);
                    iconGroupName = "Error";
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(namedType.TypeKind);
            }

            node[DgmlNodeProperties.Icon] = IconHelper.GetIconName(iconGroupName, namedType.DeclaredAccessibility);
            node[RoslynGraphProperties.TypeKind] = namedType.TypeKind;

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForMethodAsync(Graph graph, IMethodSymbol method, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForMemberAsync(method, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);

            node.AddCategory(CodeNodeCategories.Method);

            var isOperator = method.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion;
            node[DgmlNodeProperties.Icon] = isOperator
                ? IconHelper.GetIconName("Operator", method.DeclaredAccessibility)
                : IconHelper.GetIconName("Method", method.DeclaredAccessibility);

            node[RoslynGraphProperties.TypeKind] = method.ContainingType.TypeKind;
            node[RoslynGraphProperties.MethodKind] = method.MethodKind;

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForFieldAsync(Graph graph, IFieldSymbol field, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForMemberAsync(field, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);

            node.AddCategory(CodeNodeCategories.Field);

            if (field.ContainingType.TypeKind == TypeKind.Enum)
            {
                node[DgmlNodeProperties.Icon] = IconHelper.GetIconName("EnumMember", field.DeclaredAccessibility);
            }
            else
            {
                node[DgmlNodeProperties.Icon] = IconHelper.GetIconName("Field", field.DeclaredAccessibility);
            }

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForPropertyAsync(Graph graph, IPropertySymbol property, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForMemberAsync(property, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);

            node.AddCategory(CodeNodeCategories.Property);

            node[DgmlNodeProperties.Icon] = IconHelper.GetIconName("Property", property.DeclaredAccessibility);
            node[RoslynGraphProperties.TypeKind] = property.ContainingType.TypeKind;

            return node;
        }

        private static async Task<GraphNode> GetOrCreateNodeForEventAsync(Graph graph, IEventSymbol eventSymbol, Solution solution, CancellationToken cancellationToken)
        {
            var id = await GraphNodeIdCreation.GetIdForMemberAsync(eventSymbol, solution, cancellationToken).ConfigureAwait(false);
            var node = graph.Nodes.GetOrCreate(id);

            node.AddCategory(CodeNodeCategories.Event);

            node[DgmlNodeProperties.Icon] = IconHelper.GetIconName("Event", eventSymbol.DeclaredAccessibility);
            node[RoslynGraphProperties.TypeKind] = eventSymbol.ContainingType.TypeKind;

            return node;
        }

        public void AddLink(GraphNode from, GraphCategory category, GraphNode to, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                _graph.Links.GetOrCreate(from, to).AddCategory(category);
            }
        }

        public GraphNode? TryAddNodeForDocument(Document document, CancellationToken cancellationToken)
        {
            // Under the covers, progression will attempt to convert a label into a URI.  Ensure that we
            // can do this safely. before proceeding.
            //
            // The corresponding code on the progression side does: new Uri(text, UriKind.RelativeOrAbsolute)
            // so we check that same kind here.
            var fileName = Path.GetFileName(document.FilePath);
            if (!Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out _))
                return null;

            using (_gate.DisposableWait(cancellationToken))
            {
                var id = GraphNodeIdCreation.GetIdForDocument(document);

                var node = _graph.Nodes.GetOrCreate(id, fileName, CodeNodeCategories.ProjectItem);

                _nodeToContextDocumentMap[node] = document;
                _nodeToContextProjectMap[node] = document.Project;

                _createdNodes.Add(node);

                return node;
            }
        }

        public async Task<GraphNode?> CreateNodeAsync(INavigateToSearchResult result, CancellationToken cancellationToken)
        {
            var document = result.NavigableItem.Document;
            var project = document.Project;

            // If it doesn't belong to a document or project we can navigate to, then ignore entirely.
            if (document.FilePath == null || project.FilePath == null)
                return null;

            var category = result.Kind switch
            {
                NavigateToItemKind.Class => CodeNodeCategories.Class,
                NavigateToItemKind.Delegate => CodeNodeCategories.Delegate,
                NavigateToItemKind.Enum => CodeNodeCategories.Enum,
                NavigateToItemKind.Interface => CodeNodeCategories.Interface,
                NavigateToItemKind.Module => CodeNodeCategories.Module,
                NavigateToItemKind.Structure => CodeNodeCategories.Struct,
                NavigateToItemKind.Method => CodeNodeCategories.Method,
                NavigateToItemKind.Property => CodeNodeCategories.Property,
                NavigateToItemKind.Event => CodeNodeCategories.Event,
                NavigateToItemKind.Constant or
                NavigateToItemKind.EnumItem or
                NavigateToItemKind.Field => CodeNodeCategories.Field,
                _ => null,
            };

            // If it's not a category that progression understands, then ignore.
            if (category == null)
                return null;

            // Get or make a node for this symbol's containing document that will act as the parent node in the UI.
            var documentNode = this.TryAddNodeForDocument(document, cancellationToken);
            if (documentNode == null)
                return null;

            // For purposes of keying this node, just use the display text we will show.  In practice, outside of error
            // scenarios this will be unique and suitable as an ID (esp. as these names are joined with their parent
            // document name to form the full ID).
            var label = result.NavigableItem.DisplayTaggedParts.JoinText();
            var id = documentNode.Id.Add(GraphNodeId.GetLiteral(label));

            // If we already have a node that matches this (say there are multiple identical sibling symbols in an error
            // situation).  We just ignore the second match.
            var existing = _graph.Nodes.Get(id);
            if (existing != null)
                return null;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var span = text.Lines.GetLinePositionSpan(NavigateToUtilities.GetBoundedSpan(result.NavigableItem, text));
            var sourceLocation = TryCreateSourceLocation(document.FilePath, span);
            if (sourceLocation == null)
                return null;

            var symbolNode = _graph.Nodes.GetOrCreate(id);

            symbolNode.Label = label;
            symbolNode.AddCategory(category);
            symbolNode[DgmlNodeProperties.Icon] = GetIconString(result.NavigableItem.Glyph);
            symbolNode[RoslynGraphProperties.ContextDocumentId] = document.Id;
            symbolNode[RoslynGraphProperties.ContextProjectId] = project.Id;

            symbolNode[CodeNodeProperties.SourceLocation] = sourceLocation.Value;

            this.AddLink(documentNode, GraphCommonSchema.Contains, symbolNode, cancellationToken);

            return symbolNode;
        }

        public static SourceLocation? TryCreateSourceLocation(string path, LinePositionSpan span)
        {
            // SourceLocation's constructor attempts to create an absolute uri.  So if we can't do that
            // bail out immediately.
            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
                return null;

            return new SourceLocation(
                uri,
                new Position(span.Start.Line, span.Start.Character),
                new Position(span.End.Line, span.End.Character));
        }

        private static string? GetIconString(Glyph glyph)
        {
            var groupName = glyph switch
            {
                Glyph.ClassPublic or Glyph.ClassProtected or Glyph.ClassPrivate or Glyph.ClassInternal => "Class",
                Glyph.ConstantPublic or Glyph.ConstantProtected or Glyph.ConstantPrivate or Glyph.ConstantInternal => "Field",
                Glyph.DelegatePublic or Glyph.DelegateProtected or Glyph.DelegatePrivate or Glyph.DelegateInternal => "Delegate",
                Glyph.EnumPublic or Glyph.EnumProtected or Glyph.EnumPrivate or Glyph.EnumInternal => "Enum",
                Glyph.EnumMemberPublic or Glyph.EnumMemberProtected or Glyph.EnumMemberPrivate or Glyph.EnumMemberInternal => "EnumMember",
                Glyph.ExtensionMethodPublic or Glyph.ExtensionMethodProtected or Glyph.ExtensionMethodPrivate or Glyph.ExtensionMethodInternal => "Method",
                Glyph.EventPublic or Glyph.EventProtected or Glyph.EventPrivate or Glyph.EventInternal => "Event",
                Glyph.FieldPublic or Glyph.FieldProtected or Glyph.FieldPrivate or Glyph.FieldInternal => "Field",
                Glyph.InterfacePublic or Glyph.InterfaceProtected or Glyph.InterfacePrivate or Glyph.InterfaceInternal => "Interface",
                Glyph.MethodPublic or Glyph.MethodProtected or Glyph.MethodPrivate or Glyph.MethodInternal => "Method",
                Glyph.ModulePublic or Glyph.ModuleProtected or Glyph.ModulePrivate or Glyph.ModuleInternal => "Module",
                Glyph.PropertyPublic or Glyph.PropertyProtected or Glyph.PropertyPrivate or Glyph.PropertyInternal => "Property",
                Glyph.StructurePublic or Glyph.StructureProtected or Glyph.StructurePrivate or Glyph.StructureInternal => "Structure",
                _ => null,
            };

            if (groupName == null)
                return null;

            return IconHelper.GetIconName(groupName, GlyphExtensions.GetAccessibility(GlyphTags.GetTags(glyph)));
        }

        public void ApplyToGraph(Graph graph, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                using var graphTransaction = new GraphTransactionScope();
                graph.Merge(this.Graph);

                foreach (var deferredProperty in _deferredPropertySets)
                {
                    var nodeToSet = graph.Nodes.Get(deferredProperty.Item1.Id);
                    nodeToSet.SetValue(deferredProperty.Item2, deferredProperty.Item3);
                }

                graphTransaction.Complete();
            }
        }

        public void AddDeferredPropertySet(GraphNode node, GraphProperty property, object value, CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                _deferredPropertySets.Add(Tuple.Create(node, property, value));
            }
        }

        public Graph Graph
        {
            get
            {
                return _graph;
            }
        }

        public IEnumerable<GraphNode> GetCreatedNodes(CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                return _createdNodes.ToArray();
            }
        }
    }
}
