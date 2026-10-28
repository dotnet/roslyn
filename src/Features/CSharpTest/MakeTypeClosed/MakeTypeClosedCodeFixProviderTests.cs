// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeTypeClosed;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeTypeClosed;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeTypeClosed)]
public sealed class MakeTypeClosedCodeFixProviderTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpMakeTypeClosedCodeFixProvider());

    [Fact]
    public Task AbstractBaseAllDerivedHandled()
        => TestInRegularAndScriptAsync(
            """
            abstract class Shape;
            class Circle : Shape;
            class Square : Shape;

            class C
            {
                int M(Shape s) => s [|switch|]
                {
                    Circle c => 1,
                    Square q => 2,
                };
            }
            """,
            """
            closed class Shape;
            class Circle : Shape;
            class Square : Shape;

            class C
            {
                int M(Shape s) => s switch
                {
                    Circle c => 1,
                    Square q => 2,
                };
            }
            """);

    [Fact]
    public Task MissingWhenDerivedTypeNotHandled()
        => TestMissingInRegularAndScriptAsync(
            """
            abstract class Shape;
            class Circle : Shape;
            class Square : Shape;

            class C
            {
                int M(Shape s) => s [|switch|]
                {
                    Circle c => 1,
                };
            }
            """);

    [Fact]
    public Task MissingWhenBaseIsInstantiated()
        => TestMissingInRegularAndScriptAsync(
            """
            class Shape;
            class Circle : Shape;

            class C
            {
                Shape Create() => new Shape();
                int M(Shape s) => s [|switch|]
                {
                    Circle c => 1,
                };
            }
            """);

    [Fact]
    public Task MissingWhenSealed()
        => TestMissingInRegularAndScriptAsync(
            """
            sealed class Shape;

            class C
            {
                int M(Shape s) => s [|switch|] { };
            }
            """);

    [Fact]
    public Task MissingWhenStatic()
        => TestMissingInRegularAndScriptAsync(
            """
            static class Shape;

            class C
            {
                int M(Shape s) => s [|switch|] { };
            }
            """);

    [Fact]
    public Task MissingWhenAlreadyClosed()
        => TestMissingInRegularAndScriptAsync(
            """
            closed class Shape;
            class Circle : Shape;

            class C
            {
                int M(Shape s) => s [|switch|]
                {
                    Circle c => 1,
                };
            }
            """);

    [Theory]
    [InlineData("", "", "s")]
    [InlineData("", "Shape? local = s;", "local")]
    [InlineData("Shape? _shape;", "", "_shape")]
    [InlineData("Shape? Current => null;", "", "Current")]
    [InlineData("Shape? GetShape() => null;", "", "GetShape()")]
    [InlineData("", "Shape?[] values = new Shape?[1];", "values[0]")]
    public Task NullableTypeWithNullArm(string members, string setup, string expression)
        => TestNullableSwitchAsync(members, setup, expression, includeNullArm: true);

    [Theory]
    [InlineData("", "", "s")]
    [InlineData("", "Shape? local = s;", "local")]
    [InlineData("Shape? _shape;", "", "_shape")]
    [InlineData("Shape? Current => null;", "", "Current")]
    [InlineData("Shape? GetShape() => null;", "", "GetShape()")]
    [InlineData("", "Shape?[] values = new Shape?[1];", "values[0]")]
    public Task MissingNullableTypeWithoutNullArm(string members, string setup, string expression)
        => TestNullableSwitchAsync(members, setup, expression, includeNullArm: false);

    private Task TestNullableSwitchAsync(string members, string setup, string expression, bool includeNullArm)
    {
        var testCode = CreateNullableSwitchTestCode(members, setup, expression, includeNullArm, isFixed: false, markSwitch: true);
        if (!includeNullArm)
            return TestMissingInRegularAndScriptAsync(testCode);

        var fixedCode = CreateNullableSwitchTestCode(members, setup, expression, includeNullArm, isFixed: true, markSwitch: false);
        return TestInRegularAndScriptAsync(testCode, fixedCode);
    }

    private static string CreateNullableSwitchTestCode(
        string members,
        string setup,
        string expression,
        bool includeNullArm,
        bool isFixed,
        bool markSwitch)
    {
        var classModifier = isFixed ? "closed" : "abstract";
        var switchKeyword = markSwitch ? "[|switch|]" : "switch";
        var indentedMembers = members.Length == 0 ? "" : "    " + members;
        var indentedSetup = setup.Length == 0 ? "" : "            " + setup;
        var nullArm = includeNullArm ? "null => 0," : "";

        return $$"""
            #nullable enable
            {{classModifier}} class Shape;
            class Circle : Shape;
            class Square : Shape;

            class C
            {
            {{indentedMembers}}
                int M(Shape? s)
                {
            {{indentedSetup}}
                    return {{expression}} {{switchKeyword}}
                    {
                        Circle c => 1,
                        Square q => 2,
                        {{nullArm}}
                    };
                }
            }
            """;
    }

    [Fact]
    public Task NestedDerivedTypesHandledViaIntermediate()
        => TestInRegularAndScriptAsync(
            """
            abstract class Shape;
            abstract class Polygon : Shape;
            class Circle : Shape;
            class Square : Polygon;
            class Triangle : Polygon;

            class C
            {
                int M(Shape s) => s [|switch|]
                {
                    Circle c => 1,
                    Square q => 2,
                    Triangle t => 3,
                };
            }
            """,
            """
            closed class Shape;
            abstract class Polygon : Shape;
            class Circle : Shape;
            class Square : Polygon;
            class Triangle : Polygon;

            class C
            {
                int M(Shape s) => s switch
                {
                    Circle c => 1,
                    Square q => 2,
                    Triangle t => 3,
                };
            }
            """);

    [Fact]
    public Task MissingWhenDerivedTypeInOtherAssembly()
        => TestMissingInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public abstract class Shape;
            public class Circle : Shape;

            class C
            {
                int M(Shape s) => s [|switch|]
                {
                    Circle c => 1,
                };
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            public class Square : Shape;
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task FixAll()
        => TestInRegularAndScriptAsync(
            """
            abstract class A;
            class A1 : A;
            abstract class B;
            class B1 : B;

            class C
            {
                int M1(A a) => a {|FixAllInDocument:switch|} { A1 x => 1 };
                int M2(B b) => b switch { B1 x => 1 };
            }
            """,
            """
            closed class A;
            class A1 : A;
            closed class B;
            class B1 : B;

            class C
            {
                int M1(A a) => a switch { A1 x => 1 };
                int M2(B b) => b switch { B1 x => 1 };
            }
            """);
}
