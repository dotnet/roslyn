// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

// Verifies that IntermediateNode.Clone() produces a faithful deep copy -- every field, child, and
// deep-cloned side reference. The dump used for the comparison reflects over every property rather than
// reusing FormatNode (which writes only the handful of properties chosen for display), so a dropped field
// is caught.
public class IntermediateNodeCloneTest
{
    private readonly RazorProjectEngine _projectEngine = RazorProjectEngine.Create(
        RazorConfiguration.Default,
        RazorProjectFileSystem.Create(AppContext.BaseDirectory));

    [Fact]
    public void Clone_Component_ProducesFaithfulDeepCopy()
    {
        var source = """
            @page "/counter"
            @using System.Text
            @inject System.IServiceProvider Services
            @typeparam TItem

            <h1 class="title" data-x="@Value">Hello @Name</h1>
            <section>
                <p>Line1</p>
                <MyChild Title="@Value" @onclick="OnClick">child body</MyChild>
            </section>

            @code {
                [Parameter] public string Name { get; set; }
                private int Value = 1;
                private void OnClick() { Value++; }
            }
            """;

        // The tree contains at least one aliased child (a tag helper's unbound HTML attribute), so the
        // clone's alias preservation is actually exercised rather than vacuously passing.
        var aliasCount = AssertClone(source, RazorFileKind.Component);
        Assert.True(aliasCount > 0);
    }

    [Fact]
    public void Clone_LegacyView_ProducesFaithfulDeepCopy()
    {
        var source = """
            @using System.Text
            <!DOCTYPE html>
            <html>
            <head><title>Test</title></head>
            <body>
                <div class="c">@DateTime.Now</div>
                @{ var x = 1; }
                <p>value is @x</p>
            </body>
            </html>
            """;

        // The tree contains at least one aliased child (a tag helper's unbound HTML attribute), so the
        // clone's alias preservation is actually exercised rather than vacuously passing.
        var aliasCount = AssertClone(source, RazorFileKind.Legacy);
        Assert.True(aliasCount > 0);
    }

    [Fact]
    public void Clone_EdgeConstructs_ProducesFaithfulDeepCopy()
    {
        // Malformed directives and markup-element fallback containers are lowered node kinds that the other
        // documents don't produce; a malformed @addTagHelper yields a MalformedDirectiveIntermediateNode and
        // the mixed literal/expression attribute value yields a MarkupElementIntermediateNode fallback.
        var source = """
            @addTagHelper *
            <MyTag data-a="x @DateTime.Now y" class="c">body</MyTag>
            """;

        var kinds = CollectKinds(Lower(source, RazorFileKind.Legacy));
        Assert.Contains(nameof(MalformedDirectiveIntermediateNode), kinds);
        Assert.Contains(nameof(MarkupElementIntermediateNode), kinds);

        AssertClone(source, RazorFileKind.Legacy);
    }

    private int AssertClone(string content, RazorFileKind fileKind)
    {
        var documentNode = Lower(content, fileKind);

        var clone = (DocumentIntermediateNode)documentNode.Clone();

        Assert.Equal(Dump(documentNode), Dump(clone));

        // The declaration subtree, options, and code target are shared by reference on purpose.
        Assert.Same(documentNode.DeclDocumentNode, clone.DeclDocumentNode);
        Assert.Same(documentNode.FallbackDiscoveryDeclDocumentNode, clone.FallbackDiscoveryDeclDocumentNode);
        Assert.Same(documentNode.Options, clone.Options);
        Assert.Same(documentNode.Target, clone.Target);

        return AssertAliasesPreserved(documentNode, clone);
    }

    // A node-typed property that holds one of the node's own Children is an alias (e.g.
    // UnresolvedAttributeIntermediateNode.HtmlAttributeNode == Children[^1]). Cloning must preserve that
    // aliasing: the cloned property has to point at the cloned child, not an independent copy. Otherwise the
    // clone carries two divergent instances and a phase that mutates one while walking the other sees stale
    // state. Walk the original and clone trees in lockstep and assert every alias is preserved by reference.
    // Returns the number of aliases verified so a test can assert the tree actually exercised one.
    private static int AssertAliasesPreserved(IntermediateNode original, IntermediateNode clone)
    {
        Assert.Equal(original.GetType(), clone.GetType());
        Assert.Equal(original.Children.Count, clone.Children.Count);

        var aliasCount = 0;

        foreach (var (name, node) in NodeProperties(original))
        {
            var index = IndexOfReference(original.Children, node);
            if (index < 0)
            {
                continue;
            }

            Assert.Same(clone.Children[index], GetPropertyValue(clone, name));
            aliasCount++;
        }

        for (var i = 0; i < original.Children.Count; i++)
        {
            aliasCount += AssertAliasesPreserved(original.Children[i], clone.Children[i]);
        }

        return aliasCount;
    }

    // Enumerates the node-typed properties of a node, using the same reflection/exclusion rules as the dump.
    private static IEnumerable<(string Name, IntermediateNode Node)> NodeProperties(IntermediateNode node)
    {
        foreach (var property in node.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (property.Name is "Children" or "Parent" or "DeclDocumentNode" or "FallbackDiscoveryDeclDocumentNode")
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch
            {
                continue;
            }

            if (value is IntermediateNode childNode)
            {
                yield return (property.Name, childNode);
            }
        }
    }

    private static IntermediateNode GetPropertyValue(IntermediateNode node, string name)
        => (IntermediateNode)node.GetType()
            .GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(node)!;

    private static int IndexOfReference(IntermediateNodeCollection children, IntermediateNode node)
    {
        for (var i = 0; i < children.Count; i++)
        {
            if (ReferenceEquals(children[i], node))
            {
                return i;
            }
        }

        return -1;
    }

    // Collects the distinct node-kind names present in a tree, walking node-typed side references and
    // Children, so a test can assert a document actually exercises a given kind.
    private static HashSet<string> CollectKinds(IntermediateNode root)
    {
        var kinds = new HashSet<string>();
        Collect(root);
        return kinds;

        void Collect(IntermediateNode node)
        {
            kinds.Add(node.GetType().Name);

            foreach (var (_, child) in NodeProperties(node))
            {
                Collect(child);
            }

            foreach (var child in node.Children)
            {
                Collect(child);
            }
        }
    }

    // Runs the engine phases up to (but not including) tag-helper discovery, producing the lowered but
    // still unresolved intermediate tree.
    private DocumentIntermediateNode Lower(string content, RazorFileKind fileKind)
    {
        var source = RazorSourceDocument.Create(content, "test.razor");
        var codeDocument = _projectEngine.CreateCodeDocument(source, fileKind);

        foreach (var phase in _projectEngine.Engine.Phases)
        {
            if (phase is DefaultRazorTagHelperContextDiscoveryPhase)
            {
                break;
            }

            codeDocument = phase.Execute(codeDocument);
        }

        return codeDocument.GetRequiredDocumentNode();
    }

    // Serializes the full state of a node tree: every readable data property, then each deep-cloned side
    // reference, then Children. DeclDocumentNode is shared by reference (asserted separately), so it is not
    // recursed.
    private static string Dump(IntermediateNode root)
    {
        var builder = new StringBuilder();
        DumpNode(root, builder, depth: 0, activePath: new HashSet<IntermediateNode>());
        return builder.ToString();
    }

    // `activePath` tracks the current recursion stack so a true cycle is broken, while a node reachable from
    // two places (incidental sharing) is still dumped fully at each site. That keeps the dump symmetric
    // between the original (which may share an instance) and its clone (which deep-copies each reference).
    private static void DumpNode(IntermediateNode node, StringBuilder builder, int depth, HashSet<IntermediateNode> activePath)
    {
        if (!activePath.Add(node))
        {
            builder.Append(' ', depth * 2).AppendLine("<cycle>");
            return;
        }

        builder.Append(' ', depth * 2).Append(node.GetType().Name);

        var sideReferences = new List<(string Name, IntermediateNode Node)>();

        foreach (var property in node.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (property.Name is "Children" or "Parent" or "DeclDocumentNode" or "FallbackDiscoveryDeclDocumentNode" or "IsLazy")
            {
                // IsLazy is a content-storage detail: cloning a token materializes its lazy content into an
                // eager string, so IsLazy legitimately differs while Content is identical.
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch
            {
                continue;
            }

            if (value is IntermediateNode childNode)
            {
                sideReferences.Add((property.Name, childNode));
            }
            else
            {
                builder.Append(' ').Append(property.Name).Append('=').Append(Format(value));
            }
        }

        builder.AppendLine();

        foreach (var (name, child) in sideReferences)
        {
            builder.Append(' ', (depth + 1) * 2).Append('.').Append(name).Append(':').AppendLine();
            DumpNode(child, builder, depth + 2, activePath);
        }

        foreach (var child in node.Children)
        {
            DumpNode(child, builder, depth + 1, activePath);
        }

        activePath.Remove(node);
    }

    private static string Format(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case string s:
                return "\"" + s + "\"";
            case IEnumerable enumerable:
                var items = enumerable.Cast<object>().Select(Format);
                return "[" + string.Join(", ", items) + "]";
            default:
                return value.ToString() ?? "null";
        }
    }
}
