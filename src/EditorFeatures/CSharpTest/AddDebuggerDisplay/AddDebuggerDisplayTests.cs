// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddDebuggerDisplay
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddDebuggerDisplay)]
    public sealed class AddDebuggerDisplayTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        {
            return new CSharpAddDebuggerDisplayCodeRefactoringProvider();
        }

        [Fact]
        public async Task OfferedOnEmptyClass()
        {
            await TestInRegularAndScriptAsync(@"
[||]class C
{
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class C
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task NotOfferedOnWrongOverloadOfToString()
        {
            await TestMissingInRegularAndScriptAsync(@"
class A
{
    public virtual string ToString(int bar) => ""Foo"";
}

class B : A
{
    [||]public override string ToString(int bar) => ""Bar"";
}");
        }

        [Fact]
        public async Task OfferedOnEmptyStruct()
        {
            await TestInRegularAndScriptAsync(@"
[||]struct Foo
{
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
struct Foo
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task NotOfferedOnInterfaceWithToString()
        {
            await TestMissingInRegularAndScriptAsync(@"
[||]interface IFoo
{
    string ToString();
}");
        }

        [Fact]
        public async Task NotOfferedOnEnum()
        {
            await TestMissingInRegularAndScriptAsync(@"
[||]enum Foo
{
}");
        }

        [Fact]
        public async Task NotOfferedOnDelegate()
        {
            await TestMissingInRegularAndScriptAsync(@"
[||]delegate void Foo();");
        }

        [Fact]
        public async Task NotOfferedOnUnrelatedClassMembers()
        {
            await TestMissingInRegularAndScriptAsync(@"
class C
{
    [||]public int Foo { get; }
}");
        }

        [Fact]
        public async Task OfferedOnOverriddenToString()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public override string [||]ToString() => ""Foo"";
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task NamespaceImportIsNotDuplicated()
        {
            await TestInRegularAndScriptAsync(@"
using System.Diagnostics;

[||]class C
{
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class C
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task NamespaceImportIsSorted()
        {
            await TestInRegularAndScriptAsync(@"
using System.Xml;

[||]class C
{
}", @"
using System.Diagnostics;
using System.Xml;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class C
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task NotOfferedWhenAlreadySpecified()
        {
            await TestMissingInRegularAndScriptAsync(@"
[System.Diagnostics.DebuggerDisplay(""Foo"")]
[||]class C
{
}");
        }

        [Fact]
        public async Task NotOfferedWhenAlreadySpecifiedWithSuffix()
        {
            await TestMissingInRegularAndScriptAsync(@"
[System.Diagnostics.DebuggerDisplayAttribute(""Foo"")]
[||]class C
{
}");
        }

        [Fact]
        public async Task OfferedWhenAttributeWithTheSameNameIsSpecified()
        {
            await TestInRegularAndScriptAsync(@"
[BrokenCode.DebuggerDisplay(""Foo"")]
[||]class C
{
}", @"
using System.Diagnostics;

[BrokenCode.DebuggerDisplay(""Foo"")]
[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
[||]class C
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task OfferedWhenAttributeWithTheSameNameIsSpecifiedWithSuffix()
        {
            await TestInRegularAndScriptAsync(@"
[BrokenCode.DebuggerDisplayAttribute(""Foo"")]
[||]class C
{
}", @"
using System.Diagnostics;

[BrokenCode.DebuggerDisplayAttribute(""Foo"")]
[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
[||]class C
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task AliasedTypeIsRecognized()
        {
            await TestMissingInRegularAndScriptAsync(@"
using DD = System.Diagnostics.DebuggerDisplayAttribute;

[DD(""Foo"")]
[||]class C
{
}");
        }

        [Fact]
        public async Task OfferedWhenBaseClassHasDebuggerDisplay()
        {
            await TestInRegularAndScriptAsync(@"
using System.Diagnostics;

[DebuggerDisplay(""Foo"")]
class A
{
}

[||]class B : A
{
}", @"
using System.Diagnostics;

[DebuggerDisplay(""Foo"")]
class A
{
}

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class B : A
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }

        [Fact]
        public async Task ExistingDebuggerDisplayMethodIsUsedEvenWhenPublicStaticNonString()
        {
            await TestInRegularAndScriptAsync(@"
[||]class C
{
    public static object GetDebuggerDisplay() => ""Foo"";
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class C
{
    public static object GetDebuggerDisplay() => ""Foo"";
}");
        }

        [Fact]
        public async Task ExistingDebuggerDisplayMethodWithParameterIsNotUsed()
        {
            await TestInRegularAndScriptAsync(@"
[||]class C
{
    private string GetDebuggerDisplay(int foo = 0) => foo.ToString();
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{GetDebuggerDisplay(),nq}"")]
class C
{
    private string GetDebuggerDisplay(int foo = 0) => foo.ToString();

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}");
        }
    }
}
