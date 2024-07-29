// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldKeywordTests : CSharpTestBase
    {
        private static string IncludeExpectedOutput(string expectedOutput) => ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null;

        [Fact]
        public void Field_01()
        {
            string source = """
                using System;
                using System.Reflection;
                class C
                {
                    public object P => field = 1;
                    public object Q { get => field = 2; }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P, c.Q));
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                            Console.WriteLine("{0}: {1}", field.Name, field.IsInitOnly);
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: """
                (1, 2)
                <P>k__BackingField: False
                <Q>k__BackingField: False
                """);
            verifier.VerifyIL("C.P.get", """
                {
                  // Code size       16 (0x10)
                  .maxstack  3
                  .locals init (object V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.1
                  IL_0002:  box        "int"
                  IL_0007:  dup
                  IL_0008:  stloc.0
                  IL_0009:  stfld      "object C.<P>k__BackingField"
                  IL_000e:  ldloc.0
                  IL_000f:  ret
                }
                """);
            verifier.VerifyIL("C.Q.get", """
                {
                  // Code size       16 (0x10)
                  .maxstack  3
                  .locals init (object V_0)
                  IL_0000:  ldarg.0
                  IL_0001:  ldc.i4.2
                  IL_0002:  box        "int"
                  IL_0007:  dup
                  IL_0008:  stloc.0
                  IL_0009:  stfld      "object C.<Q>k__BackingField"
                  IL_000e:  ldloc.0
                  IL_000f:  ret
                }
                """);
        }

        [Fact]
        public void Field_02()
        {
            string source = """
                using System;
                class C
                {
                    public object P => Initialize(out field, 1);
                    public object Q { get => Initialize(out field, 2); }
                    static object Initialize(out object field, object value)
                    {
                        field = value;
                        return field;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        Console.WriteLine((c.P, c.Q));
                    }
                }
                """;
            var verifier = CompileAndVerify(source, expectedOutput: "(1, 2)");
            verifier.VerifyIL("C.P.get", """
                {
                  // Code size       18 (0x12)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "object C.<P>k__BackingField"
                  IL_0006:  ldc.i4.1
                  IL_0007:  box        "int"
                  IL_000c:  call       "object C.Initialize(out object, object)"
                  IL_0011:  ret
                }
                """);
            verifier.VerifyIL("C.Q.get", """
                {
                  // Code size       18 (0x12)
                  .maxstack  2
                  IL_0000:  ldarg.0
                  IL_0001:  ldflda     "object C.<Q>k__BackingField"
                  IL_0006:  ldc.i4.2
                  IL_0007:  box        "int"
                  IL_000c:  call       "object C.Initialize(out object, object)"
                  IL_0011:  ret
                }
                """);
        }

        [Fact]
        public void Field_03()
        {
            string source = """
                using System;
                using System.Reflection;
                class C
                {
                    public object P { get { return field; } init { field = 1; } }
                    public object Q { init { field = 2; } }
                }
                class Program
                {
                    static void Main()
                    {
                        foreach (var field in typeof(C).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                            Console.WriteLine("{0}: {1}", field.Name, field.IsInitOnly);
                    }
                }
                """;
            CompileAndVerify(source, targetFramework: TargetFramework.Net80, verify: Verification.Skipped, expectedOutput: IncludeExpectedOutput("""
                <P>k__BackingField: False
                <Q>k__BackingField: False
                """));
        }

        [Fact]
        public void FieldReference_01()
        {
            string source = """
                class C
                {
                    static C _other = new();
                    object P => _other.field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS1061: 'C' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //     object P => _other.field;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("C", "field").WithLocation(4, 24));
        }

        [Fact]
        public void FieldReference_02()
        {
            string source = """
                class C
                {
                    C P
                    {
                        get { return null; }
                        set { field = value.field; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,15): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //         set { field = value.field; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(6, 15),
                // (6,29): error CS1061: 'C' does not contain a definition for 'field' and no accessible extension method 'field' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         set { field = value.field; }
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "field").WithArguments("C", "field").WithLocation(6, 29));
        }

        [Fact]
        public void FieldReference_03()
        {
            string source = """
                class C
                {
                    int P
                    {
                        set { _ = this is { field: 0 }; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,29): error CS0117: 'C' does not contain a definition for 'field'
                //         set { _ = this is { field: 0 }; }
                Diagnostic(ErrorCode.ERR_NoSuchMember, "field").WithArguments("C", "field").WithLocation(5, 29));
        }

        [Fact]
        public void FieldInInitializer_01()
        {
            string source = """
                class C
                {
                    object P { get; } = F(field);
                    static object F(object value) => value;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS0103: The name 'field' does not exist in the current context
                //     object P { get; } = F(field);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 27));
        }

        [Fact]
        public void FieldInInitializer_02()
        {
            string source = """
                class C
                {
                    object P { get => null; } = field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { get => null; } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,33): error CS0103: The name 'field' does not exist in the current context
                //     object P { get => null; } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 33));
        }

        [Fact]
        public void FieldInInitializer_03()
        {
            string source = """
                class C
                {
                    object P { set { } } = field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,28): error CS0103: The name 'field' does not exist in the current context
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 28));
        }

        [Fact]
        public void Attribute_01()
        {
            string source = """
                using System;
                class A : Attribute
                {
                    public A(object o) { }
                }
                class C
                {
                    [A(field)] object P1 { get { return null; } set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,8): error CS0103: The name 'field' does not exist in the current context
                //     [A(field)] object P1 { get { return null; } }
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(8, 8));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var attributeArguments = tree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Select(arg => arg.Expression).ToArray();

            var argument = attributeArguments[0];
            Assert.IsType<IdentifierNameSyntax>(argument);
            Assert.Null(model.GetSymbolInfo(argument).Symbol);
        }

        [Fact]
        public void Attribute_02()
        {
            string source = """
                using System;
                class A : Attribute
                {
                    public A(object o) { }
                }
                class C
                {
                    object P2 { [A(field)] get { return null; } set { } }
                    object P3 { get { return null; } [A(field)] set { } }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (8,20): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P2 { [A(field)] get { return null; } set { } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(8, 20),
                // (8,20): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     object P2 { [A(field)] get { return null; } set { } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(8, 20),
                // (9,41): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P3 { get { return null; } [A(field)] set { } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(9, 41),
                // (9,41): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                //     object P3 { get { return null; } [A(field)] set { } }
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "field").WithLocation(9, 41));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var attributeArguments = tree.GetRoot().DescendantNodes().OfType<AttributeArgumentSyntax>().Select(arg => arg.Expression).ToArray();

            var argument = attributeArguments[0];
            Assert.IsType<FieldExpressionSyntax>(argument);
            Assert.Equal("System.Object C.<P2>k__BackingField", model.GetSymbolInfo(argument).Symbol.ToTestDisplayString());

            argument = attributeArguments[1];
            Assert.IsType<FieldExpressionSyntax>(argument);
            Assert.Equal("System.Object C.<P3>k__BackingField", model.GetSymbolInfo(argument).Symbol.ToTestDisplayString());
        }
    }
}
