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
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        [|this = new MyStruct(5)|];
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
        public async Task SingleNonReadonlyField_ThisAssigmentInMethod()
        {
            await TestDiagnosticMissingAsync(
@"struct MyStruct
{
    public int Third;

    public MyStruct(int first, int second, int third)
    {
        First = first;
        Second = second;
        Third = third;
    }

    public void Test()
    {
        [|this = new MyStruct(5, 3, 1)|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task MultipleMixedFields_ThisAssigmentInMethod()
        {
            await TestInRegularAndScriptAsync(
@"struct MyStruct
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
        [|this = new MyStruct(5, 3, 1)|];
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
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        [|this = new MyStruct(value, 0)|];
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
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        [|Value = value|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
        public async Task SingleReadonlyField_ThisAssigmentInMethod_ReportDiagnostic()
        {
            await TestDiagnosticsAsync(
@"struct MyStruct
{
    public readonly int Value;

    public MyStruct(int value)
    {
        Value = value;
    }

    public void Test()
    {
        [|this = new MyStruct(5)|];
    }
}",
    expected: Diagnostic(IDEDiagnosticIds.MakeStructFieldsWritable));
        }
    }
}
