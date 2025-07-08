// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public sealed class PrimaryConstructorBaseTypeSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(PrimaryConstructorBaseTypeSignatureHelpProvider);

    [Fact]
    public async Task PrimaryConstructorBaseType_FirstParameter()
    {
        await TestAsync("""
            record Base(int Identifier)
            {
                private Base(string ignored) : this(1, 2) { }
            }
            record Derived(int Other) : [|Base($$1|]);
            """, [
            new("Base(Base original)", string.Empty, null, currentParameterIndex: 0),
            new("Base(int Identifier)", string.Empty, null, currentParameterIndex: 0, isSelected: true)]);
    }

    [Fact]
    public async Task PrimaryConstructorClassBaseType_FirstParameter()
    {
        await TestAsync("""
            class Base(int Identifier)
            {
                private Base(string ignored) : this(1, 2) { }
            }
            class Derived(int Other) : [|Base($$1|]);
            """, [new("Base(int Identifier)", string.Empty, null, currentParameterIndex: 0, isSelected: true)]);
    }

    [Fact]
    public async Task PrimaryConstructorBaseType_SecondParameter()
    {
        await TestAsync("""
            record Base(int Identifier1, int Identifier2)
            {
                protected Base(string name) : this(1, 2) { }
            }
            record Derived(int Other) : [|Base(1, $$2|]);
            """, [
            new("Base(Base original)", string.Empty, null, currentParameterIndex: 1),
            new("Base(string name)", string.Empty, null, currentParameterIndex: 1),
            new("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 1, isSelected: true)]);
    }

    [Fact]
    public async Task PrimaryConstructorClassBaseType_SecondParameter()
    {
        await TestAsync("""
            class Base(int Identifier1, int Identifier2)
            {
                protected Base(string name) : this(1, 2) { }
            }
            class Derived(int Other) : [|Base(1, $$2|]);
            """, [
            new("Base(string name)", string.Empty, null, currentParameterIndex: 1),
            new("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 1, isSelected: true)]);
    }

    [Fact]
    public async Task CommentOnBaseConstructor()
    {
        await TestAsync("""
            record Base(int Identifier1, int Identifier2)
            {
                /// <summary>Summary for constructor</summary>
                protected Base(string name) : this(1, 2) { }
            }
            record Derived(int Other) : [|Base(1, $$2|]);
            """, [
            new("Base(Base original)", string.Empty, null, currentParameterIndex: 1),
            new("Base(string name)", "Summary for constructor", null, currentParameterIndex: 1),
            new("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 1, isSelected: true)]);
    }

    [Fact]
    public async Task CommentOnClassBaseConstructor()
    {
        await TestAsync("""
            class Base(int Identifier1, int Identifier2)
            {
                /// <summary>Summary for constructor</summary>
                protected Base(string name) : this(1, 2) { }
            }
            class Derived(int Other) : [|Base(1, $$2|]);
            """, [
            new("Base(string name)", "Summary for constructor", null, currentParameterIndex: 1),
            new("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 1, isSelected: true)]);
    }

    [Fact]
    public async Task CommentOnBaseConstructorAndParameters()
    {
        await TestAsync("""
            record Base(int Identifier1, int Identifier2)
            {
                /// <summary>Summary for constructor</summary>
                /// <param name="name">Param name</param>
                protected Base(string name) : this(1, 2) { }
            }
            record Derived(int Other) : [|Base($$1, 2|]);
            """, [
            new("Base(Base original)", string.Empty, null, currentParameterIndex: 0),
            new("Base(string name)", "Summary for constructor", "Param name", currentParameterIndex: 0),
            new("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 0, isSelected: true)]);
    }

    [Fact]
    public async Task CommentOnClassBaseConstructorAndParameters()
    {
        await TestAsync("""
            class Base(int Identifier1, int Identifier2)
            {
                /// <summary>Summary for constructor</summary>
                /// <param name="name">Param name</param>
                protected Base(string name) : this(1, 2) { }
            }
            class Derived(int Other) : [|Base($$1, 2|]);
            """, [
            new("Base(string name)", "Summary for constructor", "Param name", currentParameterIndex: 0),
            new("Base(int Identifier1, int Identifier2)", string.Empty, null, currentParameterIndex: 0, isSelected: true)]);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70106")]
    public async Task PrimaryConstructorBaseType_AbstractBaseType()
    {
        await TestAsync("""
            abstract class Base(int Identifier)
            {
            }
            class Derived(int Other) : [|Base($$1|]);
            """, [new("Base(int Identifier)", string.Empty, null, currentParameterIndex: 0, isSelected: true)]);
    }
}
