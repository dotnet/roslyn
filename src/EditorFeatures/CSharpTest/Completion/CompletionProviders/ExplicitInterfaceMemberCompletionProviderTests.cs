// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class ExplicitInterfaceMemberCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(ExplicitInterfaceMemberCompletionProvider);

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitInterfaceMember_01()
        {
            var markup = @"
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
}";

            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
            await VerifyItemExistsAsync(markup, "Prop");
            await VerifyItemExistsAsync(markup, "Generic", displayTextSuffix: "<K, V>(K key, V value)");
            await VerifyItemExistsAsync(markup, "this", displayTextSuffix: "[int i]");
            await VerifyItemExistsAsync(markup, "With_Underscore", displayTextSuffix: "()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitInterfaceMember_02()
        {
            var markup = @"
interface IGoo
{
    void Goo();
    void Goo(int x);
    int Prop { get; }
}

interface IBar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
            await VerifyItemExistsAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitInterfaceMember_03()
        {
            var markup = @"
interface IGoo
{
    virtual void Goo() {}
    virtual void Goo(int x) {}
    virtual int Prop { get => 0; }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
            await VerifyItemExistsAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitInterfaceMember_04()
        {
            var markup = @"
interface IGoo
{
    virtual void Goo() {}
    virtual void Goo(int x) {}
    virtual int Prop { get => 0; }
}

interface IBar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Goo", displayTextSuffix: "(int x)");
            await VerifyItemExistsAsync(markup, "Prop");
        }

        [WorkItem(709988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnNotParen()
        {
            var markup = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.Goo()
}";

            await VerifyProviderCommitAsync(markup, "Goo()", expected, null);
        }

        [WorkItem(709988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/709988")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitOnParen()
        {
            var markup = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    void Goo();
}

class Bar : IGoo
{
     void IGoo.Goo(
}";

            await VerifyProviderCommitAsync(markup, "Goo()", expected, '(');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(19947, "https://github.com/dotnet/roslyn/issues/19947")]
        public async Task ExplicitInterfaceMemberCompletionContainsOnlyValidValues()
        {
            var markup = @"
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
}";

            await VerifyItemIsAbsentAsync(markup, "Equals(object obj)");
            await VerifyItemIsAbsentAsync(markup, "Goo()");
            await VerifyItemIsAbsentAsync(markup, "GetHashCode()");
            await VerifyItemIsAbsentAsync(markup, "GetType()");
            await VerifyItemIsAbsentAsync(markup, "ToString()");

            await VerifyItemExistsAsync(markup, "Goo2", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(26595, "https://github.com/dotnet/roslyn/issues/26595")]
        public async Task ExplicitInterfaceMemberCompletionDoesNotContainAccessors()
        {
            var markup = @"

interface I1
{
    void Foo();
    int Prop { get; }
    event EventHandler TestEvent;
}

class Bar : I1
{
     void I1.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Prop.get");
            await VerifyItemIsAbsentAsync(markup, "TestEvent.add");
            await VerifyItemIsAbsentAsync(markup, "TestEvent.remove");

            await VerifyItemExistsAsync(markup, "Foo", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Prop");
            await VerifyItemExistsAsync(markup, "TestEvent");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotStaticMember_01()
        {
            var markup = @"
interface IGoo
{
    static void Goo() {}
    static int Prop { get => 0; }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo()");
            await VerifyItemIsAbsentAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotStaticMember_02()
        {
            var markup = @"
interface IGoo
{
    static void Goo() {}
    static int Prop { get => 0; }
}

interface IBar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo()");
            await VerifyItemIsAbsentAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotSealedMember_01()
        {
            var markup = @"
interface IGoo
{
    sealed void Goo() {}
    sealed int Prop { get => 0; }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo()");
            await VerifyItemIsAbsentAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotSealedMember_02()
        {
            var markup = @"
interface IGoo
{
    sealed void Goo() {}
    sealed int Prop { get => 0; }
}

interface IBar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo()");
            await VerifyItemIsAbsentAsync(markup, "Prop");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotNestedType_01()
        {
            var markup = @"
interface IGoo
{
    public abstract class Goo
    {
    }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotNestedType_02()
        {
            var markup = @"
interface IGoo
{
    public abstract class Goo
    {
    }
}

interface IBar : IGoo
{
     void IGoo.$$
}";

            await VerifyItemIsAbsentAsync(markup, "Goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(34456, "https://github.com/dotnet/roslyn/issues/34456")]
        public async Task NotInaccessibleMember_01()
        {
            var markup =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <ProjectReference>Assembly2</ProjectReference>
        <Document FilePath=""Test1.cs"">
<![CDATA[
class Bar : IGoo
{
     void IGoo.$$
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" LanguageVersion=""Preview"">
        <Document FilePath=""Test2.cs"">
public interface IGoo
{
    internal void Goo1() {}
    internal int Prop1 { get => 0; }
    protected void Goo2() {}
    protected int Prop2 { get => 0; }
}
        </Document>
    </Project>
</Workspace>";

            await VerifyItemIsAbsentAsync(markup, "Goo1", displayTextSuffix: "()");
            await VerifyItemIsAbsentAsync(markup, "Prop1");
            await VerifyItemExistsAsync(markup, "Goo2", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Prop2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(34456, "https://github.com/dotnet/roslyn/issues/34456")]
        public async Task NotInaccessibleMember_02()
        {
            var markup =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <ProjectReference>Assembly2</ProjectReference>
        <Document FilePath=""Test1.cs"">
<![CDATA[
interface IBar : IGoo
{
     void IGoo.$$
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"" LanguageVersion=""Preview"">
        <Document FilePath=""Test2.cs"">
public interface IGoo
{
    internal void Goo1() {}
    internal int Prop1 { get => 0; }
    protected void Goo2() {}
    protected int Prop2 { get => 0; }
}
        </Document>
    </Project>
</Workspace>";

            await VerifyItemIsAbsentAsync(markup, "Goo1", displayTextSuffix: "()");
            await VerifyItemIsAbsentAsync(markup, "Prop1");
            await VerifyItemExistsAsync(markup, "Goo2", displayTextSuffix: "()");
            await VerifyItemExistsAsync(markup, "Prop2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifySignatureCommit_Generic_Tab()
        {
            var markup = @"
interface IGoo
{
    int Generic<K, V>(K key, V value);
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    int Generic<K, V>(K key, V value);
}

class Bar : IGoo
{
     void IGoo.Generic<K, V>(K key, V value)
}";

            await VerifyProviderCommitAsync(markup, "Generic<K, V>(K key, V value)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifySignatureCommit_Generic_OpenBrace()
        {
            var markup = @"
interface IGoo
{
    int Generic<K, V>(K key, V value);
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    int Generic<K, V>(K key, V value);
}

class Bar : IGoo
{
     void IGoo.Generic<
}";

            await VerifyProviderCommitAsync(markup, "Generic<K, V>(K key, V value)", expected, '<');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifySignatureCommit_Method_Tab()
        {
            var markup = @"
interface IGoo
{
    int Generic(K key, V value);
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    int Generic(K key, V value);
}

class Bar : IGoo
{
     void IGoo.Generic(K key, V value)
}";

            await VerifyProviderCommitAsync(markup, "Generic(K key, V value)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifySignatureCommit_Method_OpenBrace()
        {
            var markup = @"
interface IGoo
{
    int Generic(K key, V value);
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    int Generic(K key, V value);
}

class Bar : IGoo
{
     void IGoo.Generic(
}";

            await VerifyProviderCommitAsync(markup, "Generic(K key, V value)", expected, '(');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifySignatureCommit_Indexer_Tab()
        {
            var markup = @"
interface IGoo
{
    int this[K key, V value] { get; }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    int this[K key, V value] { get; }
}

class Bar : IGoo
{
     void IGoo.this[K key, V value]
}";

            await VerifyProviderCommitAsync(markup, "this[K key, V value]", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VerifySignatureCommit_Indexer_OpenBrace()
        {
            var markup = @"
interface IGoo
{
    int this[K key, V value] { get; }
}

class Bar : IGoo
{
     void IGoo.$$
}";

            var expected = @"
interface IGoo
{
    int this[K key, V value] { get; }
}

class Bar : IGoo
{
     void IGoo.this[
}";

            await VerifyProviderCommitAsync(markup, "this[K key, V value]", expected, '[');
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [InlineData("ref")]
        [InlineData("in")]
        [InlineData("out")]
        public async Task TestWithRefKind(string refKind)
        {
            var markup = $@"
interface I
{{
    void M({refKind} string s);
}}

class C : I
{{
    void I.$$
}}
";

            var expected = $@"
interface I
{{
    void M({refKind} string s);
}}

class C : I
{{
    void I.M({refKind} string s)
}}
";

            await VerifyProviderCommitAsync(markup, $"M({refKind} string s)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(53924, "https://github.com/dotnet/roslyn/issues/53924")]
        public async Task TestStaticAbstractInterfaceMember()
        {
            var markup = @"
interface I2<T> where T : I2<T>
{
    abstract static implicit operator int(T x);
}

class Test2 : I2<Test2>
{
    static implicit I2<Test2>.$$
}
";

            var expected = @"
interface I2<T> where T : I2<T>
{
    abstract static implicit operator int(T x);
}

class Test2 : I2<Test2>
{
    static implicit I2<Test2>.operator int(Test2 x)
}
";

            await VerifyProviderCommitAsync(markup, "operator int(Test2 x)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(53924, "https://github.com/dotnet/roslyn/issues/53924")]
        public async Task TestStaticAbstractInterfaceMember_TrueOperator()
        {
            var markup = @"
interface I<T> where T : I<T>
{
    abstract static bool operator true(T x);
    abstract static bool operator false(T x);
}

class C : I<C>
{
    static bool I<C>.$$
}
";

            var expected = @"
interface I<T> where T : I<T>
{
    abstract static bool operator true(T x);
    abstract static bool operator false(T x);
}

class C : I<C>
{
    static bool I<C>.operator true(C x)
}
";

            await VerifyProviderCommitAsync(markup, "operator true(C x)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(53924, "https://github.com/dotnet/roslyn/issues/53924")]
        public async Task TestStaticAbstractInterfaceMember_UnaryPlusOperator()
        {
            var markup = @"
interface I<T> where T : I<T>
{
    abstract static T operator +(T x);
}

class C : I<C>
{
    static C I<C>.$$
}
";

            var expected = @"
interface I<T> where T : I<T>
{
    abstract static T operator +(T x);
}

class C : I<C>
{
    static C I<C>.operator +(C x)
}
";

            await VerifyProviderCommitAsync(markup, "operator +(C x)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(53924, "https://github.com/dotnet/roslyn/issues/53924")]
        public async Task TestStaticAbstractInterfaceMember_BinaryPlusOperator()
        {
            var markup = @"
interface I<T> where T : I<T>
{
    abstract static T operator +(T x, T y);
}

class C : I<C>
{
    static C I<C>.$$
}
";

            var expected = @"
interface I<T> where T : I<T>
{
    abstract static T operator +(T x, T y);
}

class C : I<C>
{
    static C I<C>.operator +(C x, C y)
}
";

            await VerifyProviderCommitAsync(markup, "operator +(C x, C y)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestWithParamsParameter()
        {
            var markup = @"
interface I
{
    void M(params string[] args);
}

class C : I
{
    void I.$$
}
";

            var expected = @"
interface I
{
    void M(params string[] args);
}

class C : I
{
    void I.M(params string[] args)
}
";

            await VerifyProviderCommitAsync(markup, "M(params string[] args)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestWithNullable()
        {
            var markup = @"
#nullable enable

interface I
{
    void M<T>(T? x);
}

class C : I
{
    void I.$$
}
";

            var expected = @"
#nullable enable

interface I
{
    void M<T>(T? x);
}

class C : I
{
    void I.M<T>(T? x)
}
";

            await VerifyProviderCommitAsync(markup, "M<T>(T? x)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEscapeIdentifier()
        {
            var markup = @"
interface I
{
    void M(string @class);
}

class C : I
{
    void I.$$
}
";

            var expected = @"
interface I
{
    void M(string @class);
}

class C : I
{
    void I.M(string @class)
}
";

            await VerifyProviderCommitAsync(markup, "M(string @class)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestEscapeIdentifier2()
        {
            var markup = @"
interface I
{
    void M<@class>();
}

class C : I
{
    void I.$$
}
";

            var expected = @"
interface I
{
    void M<@class>();
}

class C : I
{
    void I.M<@class>()
}
";

            await VerifyProviderCommitAsync(markup, "M<@class>()", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestParameterWithDefaultValue()
        {
            var markup = @"
interface I
{
    void M(int x = 10);
}

class C : I
{
    void I.$$
}
";

            var expected = @"
interface I
{
    void M(int x = 10);
}

class C : I
{
    void I.M(int x)
}
";
            // TODO: Consider adding the default value too.
            await VerifyProviderCommitAsync(markup, "M(int x)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(60215, "https://github.com/dotnet/roslyn/issues/60215")]
        public async Task TestStaticAbstractCheckedUnaryOperator()
        {
            var markup = @"
interface I1<T> where T : I1<T>
{
    abstract static T operator checked -(T x);

    abstract static T operator -(T x);
}

class C : I1<C>
{
    static C I1<C>.$$
}
";

            var expected = @"
interface I1<T> where T : I1<T>
{
    abstract static T operator checked -(T x);

    abstract static T operator -(T x);
}

class C : I1<C>
{
    static C I1<C>.operator checked -(C x)
}
";

            await VerifyProviderCommitAsync(markup, "operator checked -(C x)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(60215, "https://github.com/dotnet/roslyn/issues/60215")]
        public async Task TestStaticAbstractCheckedBinaryOperator()
        {
            var markup = @"
interface I1<T> where T : I1<T>
{
    abstract static T operator checked +(T x, T y);

    abstract static T operator +(T x, T y);
}

class C : I1<C>
{
    static C I1<C>.$$
}
";

            var expected = @"
interface I1<T> where T : I1<T>
{
    abstract static T operator checked +(T x, T y);

    abstract static T operator +(T x, T y);
}

class C : I1<C>
{
    static C I1<C>.operator checked +(C x, C y)
}
";

            await VerifyProviderCommitAsync(markup, "operator checked +(C x, C y)", expected, '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(60215, "https://github.com/dotnet/roslyn/issues/60215")]
        public async Task TestStaticAbstractCheckedCastOperator()
        {
            var markup = @"
interface I1<T> where T : I1<T>
{
    abstract static explicit operator checked string(T x);
    abstract static explicit operator string(T x);
}


class C3 : I1<C3>
{
    static C3 I1<C3>.$$
}
";

            var expected = @"
interface I1<T> where T : I1<T>
{
    abstract static explicit operator checked string(T x);
    abstract static explicit operator string(T x);
}


class C3 : I1<C3>
{
    static C3 I1<C3>.operator checked string(C3 x)
}
";

            await VerifyProviderCommitAsync(markup, "operator checked string(C3 x)", expected, '\t');
        }
    }
}
