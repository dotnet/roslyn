﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class ExplicitInterfaceMemberCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(ExplicitInterfaceMemberCompletionProvider);

    [Fact]
    public async Task ExplicitInterfaceMember_01()
    {
        var markup = """
            interface IGoo
            {
                void Goo();
                void Goo(int x);
                int Prop { get; }
                int Generic<K, V>(K key, V value);
                string this[int i] { get; }
                void With_Underscore();
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
        await VerifyItemExistsAsync(markup, "Prop");
        await VerifyItemExistsAsync(markup, "Generic", displayTextSuffix: "<K, V>(K key, V value)");
        await VerifyItemExistsAsync(markup, "this", displayTextSuffix: "[int i]");
        await VerifyItemExistsAsync(markup, "With_Underscore", displayTextSuffix: "()");
    }

    [Fact]
    public async Task ExplicitInterfaceMember_02()
    {
        var markup = """
            interface IGoo
            {
                void Goo();
                void Goo(int x);
                int Prop { get; }
            }

            interface IBar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
        await VerifyItemExistsAsync(markup, "Prop");
    }

    [Fact]
    public async Task ExplicitInterfaceMember_03()
    {
        var markup = """
            interface IGoo
            {
                virtual void Goo() {}
                virtual void Goo(int x) {}
                virtual int Prop { get => 0; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
        await VerifyItemExistsAsync(markup, "Prop");
    }

    [Fact]
    public async Task ExplicitInterfaceMember_04()
    {
        var markup = """
            interface IGoo
            {
                virtual void Goo() {}
                virtual void Goo(int x) {}
                virtual int Prop { get => 0; }
            }

            interface IBar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
        await VerifyItemExistsAsync(markup, "Prop");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
    public async Task CommitOnNotParen()
    {
        var markup = """
            interface IGoo
            {
                void Goo();
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                void Goo();
            }

            class Bar : IGoo
            {
                void IGoo.Goo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Goo()", expected, null);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
    public async Task CommitOnParen()
    {
        var markup = """
            interface IGoo
            {
                void Goo();
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                void Goo();
            }

            class Bar : IGoo
            {
                void IGoo.Goo()
                {
                    throw new System.NotImplementedException();$$
                }
            }
            """;

        await VerifyCustomCommitProviderAsync(markup, "Goo", expected, commitChar: '(');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19947")]
    public async Task ExplicitInterfaceMemberCompletionContainsOnlyValidValues()
    {
        var markup = """
            interface I1
            {
                void Goo();
            }

            interface I2 : I1
            {
                void Goo2();
                int Prop { get; }
            }

            class Bar : I2
            {
                 void I2.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Equals(object obj)");
        await VerifyItemIsAbsentAsync(markup, "Goo()");
        await VerifyItemIsAbsentAsync(markup, "GetHashCode()");
        await VerifyItemIsAbsentAsync(markup, "GetType()");
        await VerifyItemIsAbsentAsync(markup, "ToString()");

        await VerifyItemExistsAsync(markup, "Goo2", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Prop");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26595")]
    public async Task ExplicitInterfaceMemberCompletionDoesNotContainAccessors()
    {
        var markup = """
            interface I1
            {
                void Foo();
                int Prop { get; }
                event EventHandler TestEvent;
            }

            class Bar : I1
            {
                 void I1.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Prop.get");
        await VerifyItemIsAbsentAsync(markup, "TestEvent.add");
        await VerifyItemIsAbsentAsync(markup, "TestEvent.remove");

        await VerifyItemExistsAsync(markup, "Foo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Prop");
        await VerifyItemExistsAsync(markup, "TestEvent");
    }

    [Fact]
    public async Task NotStaticMember_01()
    {
        var markup = """
            interface IGoo
            {
                static void Goo() {}
                static int Prop { get => 0; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo()");
        await VerifyItemIsAbsentAsync(markup, "Prop");
    }

    [Fact]
    public async Task NotStaticMember_02()
    {
        var markup = """
            interface IGoo
            {
                static void Goo() {}
                static int Prop { get => 0; }
            }

            interface IBar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo()");
        await VerifyItemIsAbsentAsync(markup, "Prop");
    }

    [Fact]
    public async Task NotSealedMember_01()
    {
        var markup = """
            interface IGoo
            {
                sealed void Goo() {}
                sealed int Prop { get => 0; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo()");
        await VerifyItemIsAbsentAsync(markup, "Prop");
    }

    [Fact]
    public async Task NotSealedMember_02()
    {
        var markup = """
            interface IGoo
            {
                sealed void Goo() {}
                sealed int Prop { get => 0; }
            }

            interface IBar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo()");
        await VerifyItemIsAbsentAsync(markup, "Prop");
    }

    [Fact]
    public async Task NotNestedType_01()
    {
        var markup = """
            interface IGoo
            {
                public abstract class Goo
                {
                }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo");
    }

    [Fact]
    public async Task NotNestedType_02()
    {
        var markup = """
            interface IGoo
            {
                public abstract class Goo
                {
                }
            }

            interface IBar : IGoo
            {
                 void IGoo.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34456")]
    public async Task NotInaccessibleMember_01()
    {
        var markup =
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <ProjectReference>Assembly2</ProjectReference>
                    <Document FilePath="Test1.cs">
            <![CDATA[
            class Bar : IGoo
            {
                 void IGoo.$$
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" LanguageVersion="Preview">
                    <Document FilePath="Test2.cs">
            public interface IGoo
            {
                internal void Goo1() {}
                internal int Prop1 { get => 0; }
                protected void Goo2() {}
                protected int Prop2 { get => 0; }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo1", displayTextSuffix: "()");
        await VerifyItemIsAbsentAsync(markup, "Prop1");
        await VerifyItemExistsAsync(markup, "Goo2", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Prop2");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34456")]
    public async Task NotInaccessibleMember_02()
    {
        var markup =
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <ProjectReference>Assembly2</ProjectReference>
                    <Document FilePath="Test1.cs">
            <![CDATA[
            interface IBar : IGoo
            {
                 void IGoo.$$
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" LanguageVersion="Preview">
                    <Document FilePath="Test2.cs">
            public interface IGoo
            {
                internal void Goo1() {}
                internal int Prop1 { get => 0; }
                protected void Goo2() {}
                protected int Prop2 { get => 0; }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo1", displayTextSuffix: "()");
        await VerifyItemIsAbsentAsync(markup, "Prop1");
        await VerifyItemExistsAsync(markup, "Goo2", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Prop2");
    }

    [Fact]
    public async Task VerifySignatureCommit_Generic_Tab()
    {
        var markup = """
            interface IGoo
            {
                int Generic<K, V>(K key, V value);
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int Generic<K, V>(K key, V value);
            }

            class Bar : IGoo
            {
                int IGoo.Generic<K, V>(K key, V value)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Generic<K, V>(K key, V value)", expected, '\t');
    }

    [Fact]
    public async Task VerifySignatureCommit_Generic_OpenBrace()
    {
        var markup = """
            interface IGoo
            {
                int Generic<K, V>(K key, V value);
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int Generic<K, V>(K key, V value);
            }

            class Bar : IGoo
            {
                 void IGoo.<
            }
            """;

        await VerifyProviderCommitAsync(markup, "Generic<K, V>(K key, V value)", expected, '<');
    }

    [Fact]
    public async Task VerifySignatureCommit_Method_Tab()
    {
        var markup = """
            interface IGoo
            {
                int Generic(K key, V value);
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int Generic(K key, V value);
            }

            class Bar : IGoo
            {
                int IGoo.Generic(K key, V value)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Generic(K key, V value)", expected, '\t');
    }

    [WpfFact]
    public async Task VerifySignatureCommit_Method_OpenBrace()
    {
        var markup = """
            interface IGoo
            {
                int Generic(K key, V value);
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int Generic(K key, V value);
            }

            class Bar : IGoo
            {
                int IGoo.Generic(K key, V value)
                {
                    throw new System.NotImplementedException();$$
                }
            }
            """;

        await VerifyCustomCommitProviderAsync(markup, "Generic", expected, commitChar: '(');
    }

    [Fact]
    public async Task VerifySignatureCommit_Indexer_Tab()
    {
        var markup = """
            interface IGoo
            {
                int this[K key, V value] { get; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int this[K key, V value] { get; }
            }

            class Bar : IGoo
            {
                int IGoo.this[K key, V value] => throw new System.NotImplementedException();
            }
            """;

        await VerifyProviderCommitAsync(markup, "this[K key, V value]", expected, '\t');
    }

    [Fact]
    public async Task VerifySignatureCommit_Indexer_OpenBrace()
    {
        var markup = """
            interface IGoo
            {
                int this[K key, V value] { get; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int this[K key, V value] { get; }
            }

            class Bar : IGoo
            {
                 void IGoo.[
            }
            """;

        await VerifyProviderCommitAsync(markup, "this[K key, V value]", expected, '[');
    }

    [Fact]
    public async Task VerifySignatureCommit_IndexerGetSet_Tab()
    {
        var markup = """
            interface IGoo
            {
                int this[K key, V value] { get; set; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int this[K key, V value] { get; set; }
            }

            class Bar : IGoo
            {
                int IGoo.this[K key, V value] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """;

        await VerifyProviderCommitAsync(markup, "this[K key, V value]", expected, '\t');
    }

    [Fact]
    public async Task VerifySignatureCommit_IndexerGetSet_OpenBrace()
    {
        var markup = """
            interface IGoo
            {
                int this[K key, V value] { get; set; }
            }

            class Bar : IGoo
            {
                 void IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int this[K key, V value] { get; set; }
            }

            class Bar : IGoo
            {
                 void IGoo.[
            }
            """;

        await VerifyProviderCommitAsync(markup, "this[K key, V value]", expected, '[');
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("in")]
    [InlineData("out")]
    [InlineData("ref readonly")]
    [InlineData("scoped")]
    [InlineData("scoped ref")]
    public async Task TestWithRefKind(string refKind)
    {
        var markup = $$"""
            using System;

            ref struct S { }

            interface I
            {
                void M({{refKind}} S s);
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = $$"""
            using System;
            
            ref struct S { }

            interface I
            {
                void M({{refKind}} S s);
            }

            class C : I
            {
                void I.M({{refKind}} S s)
                {
                    throw new NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, $"M({refKind} S s)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53924")]
    public async Task TestStaticAbstractInterfaceMember()
    {
        var markup = """
            interface I2<T> where T : I2<T>
            {
                abstract static implicit operator int(T x);
            }

            class Test2 : I2<Test2>
            {
                static implicit I2<Test2>.$$
            }
            """;

        var expected = """
            interface I2<T> where T : I2<T>
            {
                abstract static implicit operator int(T x);
            }

            class Test2 : I2<Test2>
            {
                static implicit I2<Test2>.operator int(Test2 x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator int(Test2 x)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53924")]
    public async Task TestStaticAbstractInterfaceMember_TrueOperator()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static bool operator true(T x);
                abstract static bool operator false(T x);
            }

            class C : I<C>
            {
                static bool I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                abstract static bool operator true(T x);
                abstract static bool operator false(T x);
            }

            class C : I<C>
            {
                static bool I<C>.operator true(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator true(C x)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53924")]
    public async Task TestStaticAbstractInterfaceMember_UnaryPlusOperator()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static T operator +(T x);
            }

            class C : I<C>
            {
                static C I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                abstract static T operator +(T x);
            }

            class C : I<C>
            {
                static C I<C>.operator +(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator +(C x)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53924")]
    public async Task TestStaticAbstractInterfaceMember_BinaryPlusOperator()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static T operator +(T x, T y);
            }

            class C : I<C>
            {
                static C I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                abstract static T operator +(T x, T y);
            }

            class C : I<C>
            {
                static C I<C>.operator +(C x, C y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator +(C x, C y)", expected, '\t');
    }

    [Fact]
    public async Task TestWithParamsArrayParameter()
    {
        var markup = """
            interface I
            {
                void M(params string[] args);
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = """
            interface I
            {
                void M(params string[] args);
            }

            class C : I
            {
                void I.M(params string[] args)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "M(params string[] args)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72224")]
    public async Task TestWithParamsCollectionParameter()
    {
        var markup = """
            using System.Collections.Generic;

            interface I
            {
                void M(params IEnumerable<string> args);
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = """
            using System.Collections.Generic;

            interface I
            {
                void M(params IEnumerable<string> args);
            }

            class C : I
            {
                void I.M(params IEnumerable<string> args)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "M(params IEnumerable<string> args)", expected, '\t');
    }

    [Fact]
    public async Task TestWithNullable()
    {
        var markup = """
            #nullable enable

            interface I
            {
                void M<T>(T? x);
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = """
            #nullable enable

            interface I
            {
                void M<T>(T? x);
            }

            class C : I
            {
                void I.M<T>(T? x) where T : default
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "M<T>(T? x)", expected, '\t');
    }

    [Fact]
    public async Task TestEscapeIdentifier()
    {
        var markup = """
            interface I
            {
                void M(string @class);
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = """
            interface I
            {
                void M(string @class);
            }

            class C : I
            {
                void I.M(string @class)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "M(string @class)", expected, '\t');
    }

    [Fact]
    public async Task TestEscapeIdentifier2()
    {
        var markup = """
            interface I
            {
                void M<@class>();
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = """
            interface I
            {
                void M<@class>();
            }

            class C : I
            {
                void I.M<@class>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "M<@class>()", expected, '\t');
    }

    [Fact]
    public async Task TestParameterWithDefaultValue()
    {
        var markup = """
            interface I
            {
                void M(int x = 10);
            }

            class C : I
            {
                void I.$$
            }
            """;

        var expected = """
            interface I
            {
                void M(int x = 10);
            }

            class C : I
            {
                void I.M(int x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;
        await VerifyProviderCommitAsync(markup, "M(int x)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60215")]
    public async Task TestStaticAbstractCheckedUnaryOperator()
    {
        var markup = """
            interface I1<T> where T : I1<T>
            {
                abstract static T operator checked -(T x);

                abstract static T operator -(T x);
            }

            class C : I1<C>
            {
                static C I1<C>.$$
            }
            """;

        var expected = """
            interface I1<T> where T : I1<T>
            {
                abstract static T operator checked -(T x);

                abstract static T operator -(T x);
            }

            class C : I1<C>
            {
                static C I1<C>.operator checked -(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator checked -(C x)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60215")]
    public async Task TestStaticAbstractCheckedBinaryOperator()
    {
        var markup = """
            interface I1<T> where T : I1<T>
            {
                abstract static T operator checked +(T x, T y);

                abstract static T operator +(T x, T y);
            }

            class C : I1<C>
            {
                static C I1<C>.$$
            }
            """;

        var expected = """
            interface I1<T> where T : I1<T>
            {
                abstract static T operator checked +(T x, T y);

                abstract static T operator +(T x, T y);
            }

            class C : I1<C>
            {
                static C I1<C>.operator checked +(C x, C y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator checked +(C x, C y)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60215")]
    public async Task TestStaticAbstractCheckedCastOperator()
    {
        var markup = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);
            }


            class C3 : I1<C3>
            {
                static C3 I1<C3>.$$
            }
            """;

        var expected = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);
            }


            class C3 : I1<C3>
            {
                static explicit I1<C3>.operator checked string(C3 x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator checked string(C3 x)", expected, '\t');
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/70458")]
    public async Task TestExlicitImplementationWithAttributesOnNullableParameters()
    {
        var markup = """
            #nullable enable

            using Example.Namespace;

            interface IFoo
            {
                static abstract bool TryDecode([NotNullWhen(false)] out DecodeError? decodeError, [NotNullWhen(false)] out string? errorMessage);
            }

            class C : IFoo
            {
                IFoo.$$
            }

            class NotNullWhenAttribute(bool _) : System.Attribute;

            namespace Example.Namespace
            {
                public record DecodeError;
            }
            """;

        var expected = """
            #nullable enable

            using Example.Namespace;
            
            interface IFoo
            {
                static abstract bool TryDecode([NotNullWhen(false)] out DecodeError? decodeError, [NotNullWhen(false)] out string? errorMessage);
            }
            
            class C : IFoo
            {
                static bool IFoo.TryDecode(out DecodeError? decodeError, out string? errorMessage)
                {
                    throw new System.NotImplementedException();$$
                }
            }
            
            class NotNullWhenAttribute(bool _) : System.Attribute;
            
            namespace Example.Namespace
            {
                public record DecodeError;
            }
            """;

        await VerifyCustomCommitProviderAsync(markup, "TryDecode", expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_01()
    {
        var markup = """
            interface IGoo
            {
                void Goo();
                static abstract void StaticGoo();
            }

            class Bar : IGoo
            {
                IGoo.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "StaticGoo", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_02()
    {
        var markup = """
            interface IGoo
            {
                void Goo();
            }

            class Bar : IGoo
            {
                void Test()
                {
                    IGoo.$$
                }
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Goo");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_03()
    {
        var markup = """
            class Outer
            {
                public interface IGoo
                {
                    void Goo();
                    static abstract void StaticGoo();
                }
            }

            class Bar : Outer.IGoo
            {
                Outer.IGoo.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "StaticGoo", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_04()
    {
        var markup = """
            interface IGoo
            {
                int Generic<K, V>(K key, V value);
            }

            class Bar : IGoo
            {
                IGoo.$$
            }
            """;

        var expected = """
            interface IGoo
            {
                int Generic<K, V>(K key, V value);
            }

            class Bar : IGoo
            {
                int IGoo.Generic<K, V>(K key, V value)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Generic<K, V>(K key, V value)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_05()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static bool operator true(T x);
                abstract static bool operator false(T x);
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                abstract static bool operator true(T x);
                abstract static bool operator false(T x);
            }

            class C : I<C>
            {
                static bool I<C>.operator true(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "operator true(C x)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_06()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static ref bool Goo(ref int a, out int b);
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                abstract static ref bool Goo(ref int a, out int b);
            }

            class C : I<C>
            {
                static ref bool I<C>.Goo(ref int a, out int b)
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Goo(ref int a, out int b)", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_07()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static bool Goo { set; }
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                abstract static bool Goo { set; }
            }

            class C : I<C>
            {
                static bool I<C>.Goo { set => throw new System.NotImplementedException(); }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Goo", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_08()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                abstract static event System.Action Goo;
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            using System;

            interface I<T> where T : I<T>
            {
                abstract static event System.Action Goo;
            }

            class C : I<C>
            {
                static event Action I<C>.Goo
                {
                    add
                    {
                        throw new NotImplementedException();
                    }

                    remove
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Goo", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_09()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                bool Goo { get; set; }
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                bool Goo { get; set; }
            }

            class C : I<C>
            {
                bool I<C>.Goo { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Goo", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_10()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                event System.Action Goo;
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            using System;

            interface I<T> where T : I<T>
            {
                event System.Action Goo;
            }

            class C : I<C>
            {
                event Action I<C>.Goo
                {
                    add
                    {
                        throw new NotImplementedException();
                    }

                    remove
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Goo", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_11()
    {
        var markup = """
            interface I<T> where T : I<T>
            {
                bool this[int a, T b] { get; set; }
            }

            class C : I<C>
            {
                I<C>.$$
            }
            """;

        var expected = """
            interface I<T> where T : I<T>
            {
                bool this[int a, T b] { get; set; }
            }

            class C : I<C>
            {
                bool I<C>.this[int a, C b] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """;

        await VerifyProviderCommitAsync(markup, "this[int a, C b]", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_12()
    {
        var markup = """
            interface IOuter
            {
                void Method();

                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }

            class C : IOuter
            {
                IOuter.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Method", displayTextSuffix: "()");
        await VerifyItemIsAbsentAsync(markup, "Method1", displayTextSuffix: "()");
        await VerifyItemIsAbsentAsync(markup, "Method2", displayTextSuffix: "()");
        await VerifyItemIsAbsentAsync(markup, "IInner");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_13()
    {
        var markup = """
            interface IOuter
            {
                void Method();

                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }

            class C : IOuter
            {
                IOuter.$$
            }
            """;

        var expected = """
            interface IOuter
            {
                void Method();
            
                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }
            
            class C : IOuter
            {
                void IOuter.Method()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Method()", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_14()
    {
        var markup = """
            interface IOuter
            {
                void Method();

                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }

            class C : IOuter.IInner
            {
                IOuter.IInner.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Method", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Method1", displayTextSuffix: "()");
        await VerifyItemExistsAsync(markup, "Method2", displayTextSuffix: "()");
        await VerifyItemIsAbsentAsync(markup, "IInner");

        // We do not provide that item, maybe consider this expansion in the future
        await VerifyItemIsAbsentAsync(markup, "IInner.Method1", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_15()
    {
        var markup = """
            interface IOuter
            {
                void Method();

                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }

            class C : IOuter.IInner
            {
                IOuter.IInner.$$
            }
            """;

        var expected = """
            interface IOuter
            {
                void Method();
            
                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }
            
            class C : IOuter.IInner
            {
                void IOuter.IInner.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyProviderCommitAsync(markup, "Method1()", expected, '\t');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_16()
    {
        var markup = """
            interface IOuter
            {
                void Method();

                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }

            class C : IOuter
            {
                IOuter.IInner.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Method1", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_17()
    {
        var markup = """
            interface IInterface
            {
                void Method();
            }

            class C
            {
                IInterface.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Method", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_18()
    {
        var markup = """
            interface IOuter
            {
                void Method();
            
                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }
            
            class C
            {
                IOuter.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Method", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_19()
    {
        var markup = """
            interface IOuter
            {
                void Method();
            
                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }
            
            class C : IOuter.IInner
            {
                IOuter.$$
            }
            """;

        await VerifyItemIsAbsentAsync(markup, "Method", displayTextSuffix: "()");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75435")]
    public async Task MissingReturnTypeQualifiedInterface_20()
    {
        var markup = """
            class Outer
            {
                public interface IInner
                {
                    void Method1();
                    void Method2();
                }
            }
            
            class C : Outer.IInner
            {
                Outer.IInner.$$
            }
            """;

        await VerifyItemExistsAsync(markup, "Method1", displayTextSuffix: "()");
    }
}
