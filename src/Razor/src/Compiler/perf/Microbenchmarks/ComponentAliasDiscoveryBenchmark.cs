// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class ComponentAliasDiscoveryBenchmark
{
    private readonly DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor _visitor = new();
    private TagHelperCollection _components = [];
    private TagHelperDocumentContext _documentContext = null!;
    private RazorSyntaxTree _syntaxTree = null!;

    [Params("Unrelated", "Type", "Namespace")]
    public string Scenario { get; set; } = "";

    [Params(null, "App.Pages")]
    public string? CurrentNamespace { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        using var components = new PooledArrayBuilder<TagHelperDescriptor>(1000);
        for (var i = 0; i < 1000; i++)
        {
            var typeNamespace = $"Library.Namespace{i / 100}";
            var name = $"Component{i}";
            var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, $"{typeNamespace}.{name}", "Library");
            builder.SetTypeName($"{typeNamespace}.{name}", typeNamespace, name);
            builder.TagMatchingRule(rule => rule.TagName = name);

            for (var j = 0; j < 10; j++)
            {
                var attributeName = $"Parameter{j}";
                builder.BindAttribute(attribute =>
                {
                    attribute.Name = attributeName;
                    attribute.PropertyName = attributeName;
                    attribute.TypeName = "System.String";
                });
            }

            components.Add(builder.Build());
        }

        _components = TagHelperCollection.Create(components.ToImmutableAndClear());
        var content = Scenario switch
        {
            "Type" => "@using Alias = Library.Namespace0.Component0",
            "Namespace" => "@using UI = Library",
            _ => "@using UserId = System.Guid"
        };
        // Shared imports reuse parsed directives across documents, so parse the tree outside the benchmark.
        _syntaxTree = RazorSyntaxTree.Parse(RazorSourceDocument.Create(content, "Test.razor"));

        var visitor = new DefaultRazorTagHelperContextDiscoveryPhase.ComponentDirectiveVisitor();
        visitor.Initialize(_components, "Test.razor", CurrentNamespace, supportsComponentAliases: true);
        visitor.Visit(_syntaxTree);
        _documentContext = TagHelperDocumentContext.GetOrCreate(null, visitor.GetResults(), visitor.GetAliases());
        visitor.Reset();

        // Measure the repeated tooling path after alias names have been cached.
        foreach (var _ in _documentContext.GetCandidates())
        {
        }
    }

    [Benchmark]
    public TagHelperCollection Discover()
    {
        _visitor.Initialize(_components, "Test.razor", CurrentNamespace, supportsComponentAliases: true);
        _visitor.Visit(_syntaxTree);
        var result = _visitor.GetResults();
        _visitor.Reset();
        return result;
    }

    [Benchmark]
    public int EnumerateCandidates()
    {
        var count = 0;
        foreach (var _ in _documentContext.GetCandidates())
        {
            count++;
        }

        return count;
    }
}
