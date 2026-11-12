// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class TagHelperDocumentContextTest
{
    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void IdenticalAliasesReuseTheDocumentContext()
    {
        var firstDescriptor = CreateDescriptor("Library.Button");
        var secondDescriptor = CreateDescriptor("Library.Button");

        var first = CreateContext(new TagHelperAlias(firstDescriptor, "UI", 0, IsTypeAlias: true));
        var second = CreateContext(new TagHelperAlias(secondDescriptor, "UI", 0, IsTypeAlias: true));

        Assert.NotSame(firstDescriptor, secondDescriptor);
        Assert.Same(first, second);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void DescriptorChangesTheDocumentContext()
    {
        var first = CreateContext(new TagHelperAlias(CreateDescriptor("Library.Button"), "UI", 0, IsTypeAlias: true));
        var second = CreateContext(new TagHelperAlias(CreateDescriptor("Library.OtherButton"), "UI", 0, IsTypeAlias: true));

        Assert.NotSame(first, second);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void AliasNameChangesTheDocumentContext()
    {
        var descriptor = CreateDescriptor("Library.Button");

        var first = CreateContext(new TagHelperAlias(descriptor, "First", 0, IsTypeAlias: true));
        var second = CreateContext(new TagHelperAlias(descriptor, "Second", 0, IsTypeAlias: true));

        Assert.NotSame(first, second);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void NamespaceSuffixLengthChangesTheDocumentContext()
    {
        var descriptor = CreateDescriptor("Library.Controls.Button");

        var first = CreateContext(new TagHelperAlias(descriptor, "UI", 0, IsTypeAlias: false));
        var second = CreateContext(new TagHelperAlias(descriptor, "UI", ".Controls".Length, IsTypeAlias: false));

        Assert.NotSame(first, second);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void AliasKindChangesTheDocumentContext()
    {
        var descriptor = CreateDescriptor("Library.Button");

        var first = CreateContext(new TagHelperAlias(descriptor, "Button", 0, IsTypeAlias: true));
        var second = CreateContext(new TagHelperAlias(descriptor, "Button", 0, IsTypeAlias: false));

        Assert.NotSame(first, second);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void AliasCountChangesTheDocumentContext()
    {
        var descriptor = CreateDescriptor("Library.Button");
        var single = CreateContext([new TagHelperAlias(descriptor, "Button", 0, IsTypeAlias: true)]);
        var multiple = CreateContext([
            new TagHelperAlias(descriptor, "Button", 0, IsTypeAlias: true),
            new TagHelperAlias(descriptor, "UI", 0, IsTypeAlias: false),
        ]);

        Assert.NotSame(single, multiple);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void AliasTagNameIsReusedWhenEnumeratingCandidates()
    {
        var descriptor = CreateDescriptor("Library.Controls.Button");
        var context = CreateContext(new TagHelperAlias(descriptor, "UI", ".Controls".Length, IsTypeAlias: false));

        var firstTagName = GetAliasTagName(context);
        var secondTagName = GetAliasTagName(context);

        Assert.Equal("UI.Controls.Button", firstTagName);
        Assert.Same(firstTagName, secondTagName);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public async Task AliasTagNameIsReusedByConcurrentReaders()
    {
        var descriptor = CreateDescriptor("Concurrent.Library.Controls.ConcurrentButton");
        var context = CreateContext(new TagHelperAlias(descriptor, "ConcurrentUI", ".Controls".Length, IsTypeAlias: false));
        var readers = new Task<string>[8];
        using var start = new Barrier(readers.Length);

        // Start the readers together so they race to initialize the cached alias name.
        for (var i = 0; i < readers.Length; i++)
        {
            readers[i] = Task.Factory.StartNew(() =>
            {
                Assert.True(start.SignalAndWait(TimeSpan.FromSeconds(10)));
                return GetAliasTagName(context);
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        var tagNames = await Task.WhenAll(readers);
        Assert.All(tagNames, tagName =>
        {
            Assert.Equal("ConcurrentUI.Controls.ConcurrentButton", tagName);
            Assert.Same(tagNames[0], tagName);
        });
    }

    private static string GetAliasTagName(TagHelperDocumentContext context)
    {
        string? tagName = null;
        foreach (var candidate in context.GetCandidates())
        {
            if (candidate.AliasTagName is not null)
            {
                tagName = candidate.AliasTagName;
                break;
            }
        }

        return Assert.IsType<string>(tagName);
    }

    private static TagHelperDocumentContext CreateContext(TagHelperAlias alias)
        => CreateContext([alias]);

    private static TagHelperDocumentContext CreateContext(List<TagHelperAlias> aliases)
        => TagHelperDocumentContext.GetOrCreate(prefix: null, tagHelpers: TagHelperCollection.Empty, aliases: aliases);

    private static TagHelperDescriptor CreateDescriptor(string typeName)
    {
        var lastDot = typeName.LastIndexOf('.');
        var typeNamespace = typeName[..lastDot];
        var typeNameIdentifier = typeName[(lastDot + 1)..];
        var builder = TagHelperDescriptorBuilder.Create(TagHelperKind.Component, typeName, typeNamespace);
        builder.SetTypeName(typeName, typeNamespace, typeNameIdentifier);
        builder.TagMatchingRule(rule => rule.TagName = typeNameIdentifier);
        return builder.Build();
    }
}
