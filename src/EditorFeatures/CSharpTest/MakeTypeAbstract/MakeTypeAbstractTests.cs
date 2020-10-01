// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeTypeAbstract;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeTypeAbstract
{
    public class MakeTypeAbstractTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public MakeTypeAbstractTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeTypeAbstractCodeFixProvider());

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMethod(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
public {typeKind} Foo
{{
    public abstract void [|M|]();
}}",
$@"
public abstract {typeKind} Foo
{{
    public abstract void M();
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMethodEnclosingClassWithoutAccessibility(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
{typeKind} Foo
{{
    public abstract void [|M|]();
}}",
$@"
abstract {typeKind} Foo
{{
    public abstract void M();
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMethodEnclosingClassDocumentationComment(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
/// <summary>
/// Some class comment.
/// </summary>
public {typeKind} Foo
{{
    public abstract void [|M|]();
}}",
@"
/// <summary>
/// Some class comment.
/// </summary>
public abstract {typeKind} Foo
{{
    public abstract void M();
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestPropertyGetter(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
public {typeKind} Foo
{{
    public abstract object P {{ [|get|]; }}
}}",
$@"
public abstract {typeKind} Foo
{{
    public abstract object P {{ get; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestPropertySetter(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
public {typeKind} Foo
{{
    public abstract object P {{ [|set|]; }}
}}",
$@"
public abstract {typeKind} Foo
{{
    public abstract object P {{ set; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestIndexerGetter(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
public {typeKind} Foo
{{
    public abstract object this[object o] {{ [|get|]; }}
}}",
$@"
public abstract {typeKind} Foo
{{
    public abstract object this[object o] {{ get; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestIndexerSetter(string typeKind)
        {
            await TestInRegularAndScript1Async(
$@"
public {typeKind} Foo
{{
    public abstract object this[object o] {{ [|set|]; }}
}}",
$@"
public abstract {typeKind} Foo
{{
    public abstract object this[object o] {{ set; }}
}}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/41654"), Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestPartialClass()
        {
            await TestInRegularAndScript1Async(
@"
public partial class Foo
{
    public abstract void [|M|]();
}

public partial class Foo
{
}",
@"
public partial abstract class Foo
{
    public abstract void M();
}

public partial class Foo
{
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestEventAdd(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public {typeKind} Foo
{{
    public abstract event System.EventHandler E {{ [|add|]; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestEventRemove(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public {typeKind} Foo
{{
    public abstract event System.EventHandler E {{ [|remove|]; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMethodWithBody(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public {typeKind} Foo
{{
    public abstract int [|M|]() => 3;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestPropertyGetterWithArrowBody(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public {typeKind} Foo
{{
    public abstract int [|P|] => 3;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestPropertyGetterWithBody(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public {typeKind} Foo
{{
    public abstract int P
    {{
        [|get|] {{ return 1; }}
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestStructNestedInClass(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public {typeKind} C
{{
    public struct S
    {{
        public abstract void [|Foo|]();
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        [InlineData("class")]
        [InlineData("record")]
        public async Task TestMethodEnclosingClassStatic(string typeKind)
        {
            await TestMissingInRegularAndScriptAsync(
$@"
public static {typeKind} Foo
{{
    public abstract void [|M|]();
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task FixAll()
        {
            await TestInRegularAndScript1Async(
@"namespace NS
{
    using System;

    public class C1
    {
        public abstract void {|FixAllInDocument:|}M();
        public abstract object P { get; set; }
        public abstract object this[object o] { get; set; }
    }

    public record C2
    {
        public abstract void M();
    }

    public class C3
    {
        public class InnerClass
        {
            public abstract void M();
        }
    }
}",
@"namespace NS
{
    using System;

    public abstract class C1
    {
        public abstract void M();
        public abstract object P { get; set; }
        public abstract object this[object o] { get; set; }
    }

    public abstract record C2
    {
        public abstract void M();
    }

    public class C3
    {
        public abstract class InnerClass
        {
            public abstract void M();
        }
    }
}");
        }
    }
}
