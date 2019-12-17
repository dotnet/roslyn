// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeClassAbstract;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeClassAbstract
{
    public class MakeClassAbstractTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
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
        public async Task FixAll()
        {
            await TestInRegularAndScript1Async(
@"namespace NS
{
    using System;

    public class Foo
    {
        public abstract void {|FixAllInDocument:|}M();
        public abstract object P { get; set; }
        public abstract object this[object o] { get; set; }
    }
}",
@"namespace NS
{
    using System;

    public abstract class Foo
    {
        public abstract void M();
        public abstract object P { get; set; }
        public abstract object this[object o] { get; set; }
    }
}");
        }
    }
}
