// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeStructFieldsWritable
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpMakeStructFieldsWritableDiagnosticAnalyzer, CSharpMakeStructFieldsWritableCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeStructFieldsWritable)]
    public class MakeStructFieldsWritable
    {
        [Theory, CombinatorialData]
        public void TestStandardProperty(AnalyzerProperty property)
            => VerifyCS.VerifyStandardProperty(property);

        [Fact]
        public async Task SingleReadonlyField_ThisAssignmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                """,
                """
                struct MyStruct
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
                """);
        }

        [Fact]
        public async Task SingleReadonlyField_ThisAssignmentInMultipleMethods()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task SingleNonReadonlyField_ThisAssignmentInMethod()
        {
            var code = """
                struct MyStruct
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
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task MultipleMixedFields_ThisAssignmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task SingleReadonlyField_ThisAssignmentInCtor()
        {
            var code = """
                struct MyStruct
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
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task SingleReadonlyField_NoThisAssignment()
        {
            var code = """
                struct MyStruct
                {
                    public readonly int Value;

                    public MyStruct(int value)
                    {
                        Value = value;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task SingleReadonlyField_ThisAssignmentInMethod_ReportDiagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                """,
                """
                struct MyStruct
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
                """);
        }

        [Fact]
        public async Task SingleReadonlyField_InClass()
        {
            var code = """
                class MyClass
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
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task StructWithoutField()
        {
            var code = """
                struct MyStruct
                {
                    public void Test()
                    {
                        this = new MyStruct();
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task SingleProperty_ThisAssignmentInMethod()
        {
            var code = """
                struct MyStruct
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
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task SingleGetterProperty_ThisAssignmentInMethod()
        {
            var code = """
                struct MyStruct
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
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task MultipleStructDeclaration_SingleReadonlyField_ThisAssignmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task MultipleStructDeclaration_SingleReadonlyField_ThisAssignmentInMethod_ShouldNotReportDiagnostic()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct MyStruct
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task NestedStructDeclaration_SingleNestedReadonlyField_ThisAssignmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task NestedStructDeclaration_SingleReadonlyField_ThisAssignmentInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task StructDeclaration_MixedFields_MixedAssignmentsInMethods()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact]
        public async Task StructDeclaration_ChangedOrderOfConstructorDeclaration()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                struct [|MyStruct|]
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
                }
                """,
                """
                struct MyStruct
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
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57920")]
        public async Task ReadonlyStaticField()
        {
            var test = """
                struct Repro
                {
                    public static readonly Repro DefaultValue = new Repro();

                    public int IrrelevantValue;

                    public void Overwrite(Repro other) => this = other;
                }
                """;
            await VerifyCS.VerifyCodeFixAsync(test, test);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57920")]
        public async Task ConstField()
        {
            var test = """
                struct Repro
                {
                    public const int X = 0;

                    public int IrrelevantValue;

                    public void Overwrite(Repro other) => this = other;
                }
                """;
            await VerifyCS.VerifyCodeFixAsync(test, test);
        }
    }
}
