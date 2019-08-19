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
        public async Task OfferedOnClassWithOverriddenToString()
        {
            await TestInRegularAndScriptAsync(@"
[||]class C
{
    public override string ToString() => ""Foo"";
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{ToString(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NotOfferedWhenToStringIsNotOverriddenInSameClass()
        {
            await TestMissingAsync(@"
class A
{
    public override string ToString() => ""Foo"";
}

[||]class B : A
{
}");
        }

        [Fact]
        public async Task NotOfferedWhenToStringIsNotOverriddenInSameFile()
        {
            await TestMissingAsync(@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""Part1.cs""><![CDATA[
partial class A
{
    public override string ToString() => ""Foo"";
}
]]>
        </Document>
        <Document FilePath=""Part2.cs""><![CDATA[
[||]partial class A
{
    public int Foo { get; }
}
]]>
        </Document>
    </Project>
</Workspace>");
        }

        [Fact]
        public async Task NotOfferedOnWrongOverloadOfToString()
        {
            await TestMissingInRegularAndScriptAsync(@"
class A
{
    public virtual string ToString(int bar) => ""Foo"";
}

[||]class B : A
{
    public override string ToString(int bar) => ""Bar"";
}");
        }

        [Fact]
        public async Task OfferedOnStructWithOverriddenToString()
        {
            await TestInRegularAndScriptAsync(@"
[||]struct Foo
{
    public override string ToString() => ""Foo"";
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{ToString(),nq}"")]
struct Foo
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NotOfferedWhenToStringIsNotOverriddenInStruct()
        {
            await TestMissingAsync(@"
[||]struct Foo
{
    public int Bar { get; }
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

    public override string ToString() => ""Foo"";
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

[DebuggerDisplay(""{ToString(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NamespaceImportIsNotDuplicated()
        {
            await TestInRegularAndScriptAsync(@"
using System.Diagnostics;

[||]class C
{
    public override string ToString() => ""Foo"";
}", @"
using System.Diagnostics;

[DebuggerDisplay(""{ToString(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NamespaceImportIsSorted()
        {
            await TestInRegularAndScriptAsync(@"
using System.Xml;

[||]class C
{
    public override string ToString() => ""Foo"";
}", @"
using System.Diagnostics;
using System.Xml;

[DebuggerDisplay(""{ToString(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NotOfferedWhenAlreadySpecified()
        {
            await TestMissingInRegularAndScriptAsync(@"
[System.Diagnostics.DebuggerDisplay(""Foo"")]
[||]class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NotOfferedWhenAlreadySpecifiedWithSuffix()
        {
            await TestMissingInRegularAndScriptAsync(@"
[System.Diagnostics.DebuggerDisplayAttribute(""Foo"")]
[||]class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NotOfferedWhenAnyAttributeWithTheSameNameIsSpecified()
        {
            await TestMissingInRegularAndScriptAsync(@"
[BrokenCode.DebuggerDisplay(""Foo"")]
[||]class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task NotOfferedWhenAnyAttributeWithTheSameNameIsSpecifiedWithSuffix()
        {
            await TestMissingInRegularAndScriptAsync(@"
[BrokenCode.DebuggerDisplay(""Foo"")]
[||]class C
{
    public override string ToString() => ""Foo"";
}");
        }

        [Fact]
        public async Task AliasedTypeIsNotRecognized()
        {
            // This situation seems sufficiently unlikely that there is no need to make the majority of cases where
            // there is an attribute wait for the semantic model.

            await TestInRegularAndScriptAsync(@"
using DD = System.Diagnostics.DebuggerDisplayAttribute;

[DD(""Foo"")]
[||]class C
{
    public override string ToString() => ""Foo"";
}", @"
using System.Diagnostics;
using DD = System.Diagnostics.DebuggerDisplayAttribute;

[DD(""Foo"")]
[DD(""{ToString(),nq}"")]
class C
{
    public override string ToString() => ""Foo"";
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
    public override string ToString() => ""Foo"";
}

[||]class B : A
{
    public override string ToString() => base.ToString();
}", @"
using System.Diagnostics;

[DebuggerDisplay(""Foo"")]
class A
{
    public override string ToString() => ""Foo"";
}

[DebuggerDisplay(""{ToString(),nq}"")]
class B : A
{
    public override string ToString() => base.ToString();
}");
        }
    }
}
