// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class ComponentAliasTargetMapTest
{
    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void EmptyInputReturnsSharedEmptyMap()
    {
        Assert.Same(ComponentAliasTargetMap.Empty, ComponentAliasTargetMap.Create([], CreateCompilation()));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void SharedImportUsesEachNamespaceContext()
    {
        var map = ComponentAliasTargetMap.Create(
            [[new("App", "Library")], [new("Other", "Library")], [new("App", "Library")]],
            CreateCompilation());

        Assert.True(map.TryGetTarget("App", "Library", out var nearer));
        Assert.Equal(new("App.Library", IsType: false), nearer);
        Assert.True(map.TryGetTarget("Other", "Library", out var outer));
        Assert.Equal(new("Library", IsType: false), outer);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void MissingMemberOfNearerNamespaceDoesNotFallBack()
    {
        var map = ComponentAliasTargetMap.Create([[new("App", "Library.Button")]], CreateCompilation());

        Assert.True(map.TryGetTarget("App", "Library.Button", out var target));
        Assert.Null(target.Name);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void GlobalTargetBypassesNearerNamespace()
    {
        var map = ComponentAliasTargetMap.Create([[new("App", "global::Library.Button")]], CreateCompilation());

        Assert.True(map.TryGetTarget("App", "global::Library.Button", out var target));
        Assert.Equal(new("Library.Button", IsType: true), target);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void AmbiguousTypeDoesNotProduceAResolvedTarget()
    {
        var first = CSharpCompilation.Create(
            "First",
            syntaxTrees: [CSharpSyntaxTree.ParseText(SourceText.From("namespace Duplicates { public class Widget { } }"))],
            references: ReferenceUtil.NetLatestAll);
        var second = first.WithAssemblyName("Second");
        var compilation = CreateCompilation().AddReferences(first.ToMetadataReference(), second.ToMetadataReference());
        var map = ComponentAliasTargetMap.Create([[new("App", "Duplicates.Widget")]], compilation);

        Assert.True(map.TryGetTarget("App", "Duplicates.Widget", out var target));
        Assert.Null(target.Name);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void GlobalNamespaceAndNamespacedAliasesStayInTheirOwnContexts()
    {
        var map = ComponentAliasTargetMap.Create(
            [[new("App", "Library")], [new("", "Library")], [new("", "Library.Button")]],
            CreateCompilation());

        Assert.True(map.TryGetTarget("App", "Library", out var nearer));
        Assert.Equal(new("App.Library", IsType: false), nearer);
        Assert.True(map.TryGetTarget("", "Library", out var outer));
        Assert.Equal(new("Library", IsType: false), outer);
        Assert.True(map.TryGetTarget("", "Library.Button", out var type));
        Assert.Equal(new("Library.Button", IsType: true), type);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void InvalidNamespaceDoesNotAffectOtherContexts()
    {
        var map = ComponentAliasTargetMap.Create(
            [[new("App.namespace", "Library.Button")], [new("Other", "Library.Button")]],
            CreateCompilation());

        Assert.False(map.TryGetTarget("App.namespace", "Library.Button", out _));
        Assert.True(map.TryGetTarget("Other", "Library.Button", out var target));
        Assert.Equal(new("Library.Button", IsType: true), target);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void MalformedGlobalTargetDoesNotAffectOtherContexts()
    {
        var map = ComponentAliasTargetMap.Create(
            [[new("", "namespace")], [new("", "Library.Button")], [new("Other", "Library.Button")]],
            CreateCompilation());

        Assert.True(map.TryGetTarget("", "Library.Button", out var global));
        Assert.Equal(new("Library.Button", IsType: true), global);
        Assert.True(map.TryGetTarget("Other", "Library.Button", out var other));
        Assert.Equal(new("Library.Button", IsType: true), other);
        Assert.False(map.TryGetTarget("", "namespace", out _));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void MalformedNamespacedTargetDoesNotAffectOtherContexts()
    {
        var map = ComponentAliasTargetMap.Create(
            [[new("App", "Foo<namespace{")], [new("Other", "Library.Button")]],
            CreateCompilation());

        Assert.True(map.TryGetTarget("Other", "Library.Button", out var target));
        Assert.Equal(new("Library.Button", IsType: true), target);
        Assert.False(map.TryGetTarget("App", "Foo<namespace{", out _));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85411")]
    public void EqualityDependsOnResolvedTargetsRatherThanCompilationIdentity()
    {
        ImmutableArray<ImmutableArray<ComponentAliasTargetMap.AliasInfo>> aliases = [[new("App", "Library")]];
        var first = ComponentAliasTargetMap.Create(aliases, CreateCompilation());
        var second = ComponentAliasTargetMap.Create(aliases, CreateCompilation());

        Assert.Equal(first, second);
    }

    private static CSharpCompilation CreateCompilation()
        => CSharpCompilation.Create(
            "Aliases",
            syntaxTrees: [CSharpSyntaxTree.ParseText(SourceText.From("""
                namespace Library { public class Button { } }
                namespace App.Library { public class Helper { } }
                """))],
            references: ReferenceUtil.NetLatestAll);
}
