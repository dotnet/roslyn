// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.MakeMemberStatic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeMemberStatic
{
    public class MakeMemberStaticTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeMemberStaticCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestField()
        {
            await TestInRegularAndScript1Async(
@"
public static class Foo
{
    int [||]i;
}",
@"
public static class Foo
{
    static int i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestMethod()
        {
            await TestInRegularAndScript1Async(
@"
public static class Foo
{
    void [||]M() { }
}",
@"
public static class Foo
{
    static void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestProperty()
        {
            await TestInRegularAndScript1Async(
@"
public static class Foo
{
    object [||]P { get; set; }
}",
@"
public static class Foo
{
    static object P { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task TestEventField()
        {
            await TestInRegularAndScript1Async(
@"
public static class Foo
{
    event System.Action [||]E;
}",
@"
public static class Foo
{
    static event System.Action E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMemberStatic)]
        public async Task FixAll()
        {
            await TestInRegularAndScript1Async(
@"namespace NS
{
    public static class Foo
    {
        int {|FixAllInDocument:|}i;
        void M() { }
        object P { get; set; }
        event System.Action E;
    }
}",
@"namespace NS
{
    public static class Foo
    {
        static int i;

        static void M() { }

        static object P { get; set; }

        static event System.Action E;
    }
}");
        }
    }
}
