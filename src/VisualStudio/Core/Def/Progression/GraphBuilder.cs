// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal sealed partial class GraphBuilder
{
    // Our usage of SemaphoreSlim is fine.  We don't perform blocking waits for it on the UI thread.
#pragma warning disable RS0030 // Do not use banned APIs
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
#pragma warning restore RS0030 // Do not use banned APIs

    private readonly ISet<GraphNode> _createdNodes = new HashSet<GraphNode>();

    public GraphBuilder()
    {
        // _solution = solution;
    }

    public void AddLink(GraphNode from, GraphCategory category, GraphNode to, CancellationToken cancellationToken)
    {
        using (_gate.DisposableWait(cancellationToken))
        {
            Graph.Links.GetOrCreate(from, to).AddCategory(category);
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

            var node = Graph.Nodes.GetOrCreate(id, fileName, CodeNodeCategories.ProjectItem);

            // _nodeToContextDocumentMap[node] = document;
            // _nodeToContextProjectMap[node] = document.Project;

            _createdNodes.Add(node);

            return node;
        }
    }

    public async Task<GraphNode?> CreateNodeAsync(Solution solution, INavigateToSearchResult result, CancellationToken cancellationToken)
    {
        var document = await result.NavigableItem.Document.GetRequiredDocumentAsync(solution, cancellationToken).ConfigureAwait(false);
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
        var existing = Graph.Nodes.Get(id);
        if (existing != null)
            return null;

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var span = text.Lines.GetLinePositionSpan(NavigateToUtilities.GetBoundedSpan(result.NavigableItem, text));
        var sourceLocation = TryCreateSourceLocation(document.FilePath, span);
        if (sourceLocation == null)
            return null;

        var symbolNode = Graph.Nodes.GetOrCreate(id);

        symbolNode.Label = label;
        symbolNode.AddCategory(category);
        symbolNode[DgmlNodeProperties.Icon] = GetIconString(result.NavigableItem.Glyph);
        // symbolNode[RoslynGraphProperties.ContextDocumentId] = document.Id;
        symbolNode[RoslynGraphProperties.ContextProjectId] = document.Project.Id;

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

            //foreach (var deferredProperty in _deferredPropertySets)
            //{
            //    var nodeToSet = graph.Nodes.Get(deferredProperty.Item1.Id);
            //    nodeToSet.SetValue(deferredProperty.Item2, deferredProperty.Item3);
            //}

            graphTransaction.Complete();
        }
    }

    public Graph Graph { get; } = new();

    public ImmutableArray<GraphNode> GetCreatedNodes(CancellationToken cancellationToken)
    {
        using (_gate.DisposableWait(cancellationToken))
        {
            return [.. _createdNodes];
        }
    }
}
