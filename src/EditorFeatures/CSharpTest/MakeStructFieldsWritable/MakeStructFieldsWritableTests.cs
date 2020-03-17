// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable.CSharpMakeStructFieldsWritableDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable.CSharpMakeStructFieldsWritableCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructFieldsWritable
{
    public class MakeStructFieldsWritableTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public void TestStandardProperties()
            => VerifyCS.VerifyStandardProperties();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
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
            await VerifyCS.VerifyCodeFixAsync(
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
            var code = @"struct MyStruct
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task MultipleMixedFields_ThisAssigmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
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
            var code = @"struct MyStruct
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_NoThisAssigment()
        {
            var code = @"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInMethod_ReportDiagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
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
        public async Task SingleReadonlyField_InClass()
        {
            var code = @"class MyClass
{
    public readonly int Value;

    public MyClass(int value)
    {
        Value = value;
    }

    public void Test()
    {
        // error CS1604: Cannot assign to 'this' because it is read-only
        {|CS1604:this|} = new MyClass(5);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task StructWithoutField()
        {
            var code = @"struct MyStruct
{
    public void Test()
    {
        this = new MyStruct();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleProperty_ThisAssigmentInMethod()
        {
            var code = @"struct MyStruct
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleGetterProperty_ThisAssigmentInMethod()
        {
            var code = @"struct MyStruct
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task MultipleStructDeclaration_SingleReadonlyField_ThisAssigmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
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
            await VerifyCS.VerifyCodeFixAsync(
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
        public async Task NestedStructDeclaration_SingleNestedReadonlyField_ThisAssigmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
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
            await VerifyCS.VerifyCodeFixAsync(
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
        public async Task StructDeclaration_MixedFields_MixedAssigmentsInMethods()
        {
            await VerifyCS.VerifyCodeFixAsync(
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
            await VerifyCS.VerifyCodeFixAsync(
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
