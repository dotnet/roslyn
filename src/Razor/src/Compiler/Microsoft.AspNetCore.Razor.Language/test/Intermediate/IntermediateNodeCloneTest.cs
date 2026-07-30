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

        AssertClone(source, RazorFileKind.Component);
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

        AssertClone(source, RazorFileKind.Legacy);
    }

    private void AssertClone(string content, RazorFileKind fileKind)
    {
        var documentNode = Lower(content, fileKind);

        var clone = (DocumentIntermediateNode)documentNode.Clone();

        Assert.Equal(Dump(documentNode), Dump(clone));

        // The declaration subtree, options, and code target are shared by reference on purpose.
        Assert.Same(documentNode.DeclDocumentNode, clone.DeclDocumentNode);
        Assert.Same(documentNode.Options, clone.Options);
        Assert.Same(documentNode.Target, clone.Target);
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
            if (property.Name is "Children" or "Parent" or "DeclDocumentNode" or "IsLazy")
            {
                // IsLazy is a content-storage detail: cloning a token materializes its lazy content into an
                // eager string, so IsLazy legitimately differs while Content is identical.
                continue;
            }

            object value;
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

    private static string Format(object value)
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
                return value.ToString();
        }
    }
}
