// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeClassAbstract;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeClassAbstract
{
    public class MakeClassAbstractTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public MakeClassAbstractTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeClassAbstractCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestMethod()
        {
            await TestInRegularAndScript1Async(
@"
public class Foo
{
    public abstract void [|M|]();
}",
@"
public abstract class Foo
{
    public abstract void M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestMethodEnclosingClassWithoutAccessibility()
        {
            await TestInRegularAndScript1Async(
@"
class Foo
{
    public abstract void [|M|]();
}",
@"
abstract class Foo
{
    public abstract void M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestMethodEnclosingClassDocumentationComment()
        {
            await TestInRegularAndScript1Async(
@"
/// <summary>
/// Some class comment.
/// </summary>
public class Foo
{
    public abstract void [|M|]();
}",
@"
/// <summary>
/// Some class comment.
/// </summary>
public abstract class Foo
{
    public abstract void M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestPropertyGetter()
        {
            await TestInRegularAndScript1Async(
@"
public class Foo
{
    public abstract object P { [|get|]; }
}",
@"
public abstract class Foo
{
    public abstract object P { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestPropertySetter()
        {
            await TestInRegularAndScript1Async(
@"
public class Foo
{
    public abstract object P { [|set|]; }
}",
@"
public abstract class Foo
{
    public abstract object P { set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestIndexerGetter()
        {
            await TestInRegularAndScript1Async(
@"
public class Foo
{
    public abstract object this[object o] { [|get|]; }
}",
@"
public abstract class Foo
{
    public abstract object this[object o] { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestIndexerSetter()
        {
            await TestInRegularAndScript1Async(
@"
public class Foo
{
    public abstract object this[object o] { [|set|]; }
}",
@"
public abstract class Foo
{
    public abstract object this[object o] { set; }
}");
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestEventAdd()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class Foo
{
    public abstract event System.EventHandler E { [|add|]; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestEventRemove()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class Foo
{
    public abstract event System.EventHandler E { [|remove|]; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestMethodWithBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class Foo
{
    public abstract int [|M|]() => 3;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestPropertyGetterWithArrowBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class Foo
{
    public abstract int [|P|] => 3;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestPropertyGetterWithBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class Foo
{
    public abstract int P
    {
        [|get|] { return 1; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestStructNestedInClass()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public class C
{
    public struct S
    {
        public abstract void [|Foo|]();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
        public async Task TestMethodEnclosingClassStatic()
        {
            await TestMissingInRegularAndScriptAsync(
@"
public static class Foo
{
    public abstract void [|M|]();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeClassAbstract)]
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

    public class C2
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

    public abstract class C2
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
