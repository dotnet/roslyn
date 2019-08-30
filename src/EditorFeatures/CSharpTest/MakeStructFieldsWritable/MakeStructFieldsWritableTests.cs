// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructFieldsWritable
{
    public class MakeStructFieldsWritableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpMakeStructFieldsWritableDiagnosticAnalyzer(), new CSharpMakeStructFieldsWritableCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInMethod()
        {
            await TestInRegularAndScriptAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}",
@"struct MyStruct
{
    public int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInMultipleMethods()
        {
            await TestInRegularAndScriptAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    public void Test2()
    {
        this = new MyStruct(10);
    }
}",
@"struct MyStruct
{
    public int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    public void Test2()
    {
        this = new MyStruct(10);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleNonReadonlyField_ThisAssigmentInMethod()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task MultipleMixedFields_ThisAssigmentInMethod()
        {
            await TestInRegularAndScriptAsync(
@"struct [|MyStruct|]
{
    public readonly int First;
    public readonly int Second;
    public int Third;

    public MyStruct(int first, int second, int third)
    {
        First = first;
        Second = second;
        Third = third;
    }

    public void Test()
    {
        this = new MyStruct(5, 3, 1);
    }
}",
@"struct MyStruct
{
    public int First;
    public int Second;
    public int Third;

    public MyStruct(int first, int second, int third)
    {
        First = first;
        Second = second;
        Third = third;
    }

    public void Test()
    {
        this = new MyStruct(5, 3, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInCtor()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public MyStruct(int value)
    {
        this = new MyStruct(value, 0);
    }

    public MyStruct(int first, int second)
    {
        Value = first;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_NoThisAssigment()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInMethod_ReportDiagnostic()
        {
            await TestDiagnosticsAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}",
    expected: Diagnostic(IDEDiagnosticIds.MakeStructFieldsWritable));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_InClass()
        {
            await TestDiagnosticMissingAsync(
@"class [|MyClass|]
{
    public readonly int Value;

    public MyClass(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyClass(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task StructWithoutField()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public void Test()
    {
        this = new MyStruct();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleProperty_ThisAssigmentInMethod()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public int Value { get; set; }

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleGetterProperty_ThisAssigmentInMethod()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public int Value { get; }

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task MultipleStructDeclaration_SingleReadonlyField_ThisAssigmentInMethod()
        {
            await TestInRegularAndScriptAsync(
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}

struct [|MyStruct2|]
{
    public readonly int Value;

    public MyStruct2(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct2(5);
    }
}",
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}

struct MyStruct2
{
    public int Value;

    public MyStruct2(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct2(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task MultipleStructDeclaration_SingleReadonlyField_ThisAssigmentInMethod_ShouldNotReportDiagnostic()
        {
            await TestDiagnosticMissingAsync(
@"struct [|MyStruct|]
{
    public int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }
}

struct MyStruct2
{
    public readonly int Value;

    public MyStruct2(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct2(5);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task NestedStructDeclaration_SingleNestedReadonlyField_ThisAssigmentInMethod()
        {
            await TestInRegularAndScriptAsync(
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    struct [|NestedStruct|]
    {
        public readonly int NestedValue;

        public NestedStruct(int nestedValue)
        {
            NestedValue = nestedValue;
        }

        public void Test()
        {
            this = new NestedStruct(5);
        }
    }
}",
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    struct NestedStruct
    {
        public int NestedValue;

        public NestedStruct(int nestedValue)
        {
            NestedValue = nestedValue;
        }

        public void Test()
        {
            this = new NestedStruct(5);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task NestedStructDeclaration_SingleReadonlyField_ThisAssigmentInMethod()
        {
            await TestInRegularAndScriptAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    struct NestedStruct
    {
        public readonly int NestedValue;

        public NestedStruct(int nestedValue)
        {
            NestedValue = nestedValue;
        }

        public void Test()
        {
            this = new NestedStruct(5);
        }
    }
}",
@"struct MyStruct
{
    public int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    struct NestedStruct
    {
        public readonly int NestedValue;

        public NestedStruct(int nestedValue)
        {
            NestedValue = nestedValue;
        }

        public void Test()
        {
            this = new NestedStruct(5);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task StructDeclaration_MixedFields_MixedAssigmentsInMethods()
        {
            await TestInRegularAndScriptAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;
    public int TestValue;

    public MyStruct(int value)
    {
        Value = value;
        TestValue = 100;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    public void Test2()
    {
        TestValue = 0;
    }
}",
@"struct MyStruct
{
    public int Value;
    public int TestValue;

    public MyStruct(int value)
    {
        Value = value;
        TestValue = 100;
    }

    public void Test()
    {
        this = new MyStruct(5);
    }

    public void Test2()
    {
        TestValue = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task StructDeclaration_ChangedOrderOfConstructorDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"struct [|MyStruct|]
{
    public readonly int Value;

    public void Test()
    {
        this = new MyStruct(5);
    }

    public MyStruct(int value)
    {
        Value = value;
    }
}",
@"struct MyStruct
{
    public int Value;

    public void Test()
    {
        this = new MyStruct(5);
    }

    public MyStruct(int value)
    {
        Value = value;
    }
}");
        }
    }
}
