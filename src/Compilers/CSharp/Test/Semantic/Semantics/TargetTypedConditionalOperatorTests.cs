// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test binding of the target-typed conditional (aka ternary) operator.
    /// </summary>
    public class TargetTypedConditionalOperatorTests : CSharpTestBase
    {
        [Fact]
        public void TestImplicitConversions_Good()
        {
            // NOTE: Some of these are currently error cases, but they would become accepted (non-error) cases
            // if we extend the spec to permit target typing even when there is a natural type.  Until then,
            // they are error cases but included here for convenience.

            // Implicit constant expression conversions
            TestConditional("b ? 1 : 2", "System.Int16", "System.Int32",
                // (6,26): error CS0266: Cannot implicitly convert type 'int' to 'short'. An explicit conversion exists (are you missing a cast?)
                //         System.Int16 t = b ? 1 : 2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 1 : 2").WithArguments("int", "short").WithLocation(6, 26)
                );
            TestConditional("b ? -1L : 1UL", "System.Double", null);

            // Implicit reference conversions
            TestConditional("b ? GetB() : GetC()", "A", null);
            TestConditional("b ? Get<IOut<B>>() : Get<IOut<C>>()", "IOut<A>", null);
            TestConditional("b ? Get<IOut<IOut<B>>>() : Get<IOut<IOut<C>>>()", "IOut<IOut<A>>", null);
            TestConditional("b ? Get<IOut<B[]>>() : Get<IOut<C[]>>()", "IOut<A[]>", null);
            TestConditional("b ? Get<U>() : Get<V>()", "T", null);

            // Implicit numeric conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.Int64", null);

            // Implicit enumeration conversions
            TestConditional("b ? 0 : 0", "color", "System.Int32",
                // (6,19): error CS0266: Cannot implicitly convert type 'int' to 'color'. An explicit conversion exists (are you missing a cast?)
                //         color t = b ? 0 : 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 0 : 0").WithArguments("int", "color").WithLocation(6, 19)
                );

            // Implicit interpolated string conversions
            TestConditional(@"b ? $""x"" : $""x""", "System.FormattableString", "System.String",
                // (6,38): error CS0029: Cannot implicitly convert type 'string' to 'System.FormattableString'
                //         System.FormattableString t = b ? $"x" : $"x";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"b ? $""x"" : $""x""").WithArguments("string", "System.FormattableString").WithLocation(6, 38)
                );

            // Implicit nullable conversions
            // Null literal conversions
            TestConditional("b ? 1 : null", "System.Int64?", null);

            // Boxing conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.IComparable", null);

            // User - defined implicit conversions
            TestConditional("b ? GetB() : GetC()", "X", null);

            // Anonymous function conversions
            TestConditional("b ? a=>a : b=>b", "Del", null);

            // Method group conversions
            TestConditional("b ? M1 : M2", "Del", null);

            // Pointer conversions
            TestConditional("b ? GetIntp() : GetLongp()", "void*", null);
            TestConditional("b ? null : null", "System.Int32*", null);
        }

        [Fact]
        public void TestImplicitConversions_Bad()
        {
            // Implicit constant expression conversions
            TestConditional("b ? 1000000 : 2", "System.Int16", "System.Int32",
                // (6,26): error CS0266: Cannot implicitly convert type 'int' to 'short'. An explicit conversion exists (are you missing a cast?)
                //         System.Int16 t = b ? 1000000 : 2;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 1000000 : 2").WithArguments("int", "short").WithLocation(6, 26)
                );

            // Implicit reference conversions
            TestConditional("b ? GetB() : GetC()", "System.String", null,
                // (6,31): error CS0029: Cannot implicitly convert type 'B' to 'string'
                //         System.String t = b ? GetB() : GetC();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetB()").WithArguments("B", "string").WithLocation(6, 31),
                // (6,40): error CS0029: Cannot implicitly convert type 'C' to 'string'
                //         System.String t = b ? GetB() : GetC();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetC()").WithArguments("C", "string").WithLocation(6, 40)
                );

            // Implicit numeric conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.UInt64", null,
                // (6,43): error CS0029: Cannot implicitly convert type 'int' to 'ulong'
                //         System.UInt64 t = b ? GetUInt() : GetInt();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetInt()").WithArguments("int", "ulong").WithLocation(6, 43)
                );

            // Implicit enumeration conversions
            TestConditional("b ? 1 : 0", "color", "System.Int32",
                // (6,19): error CS0266: Cannot implicitly convert type 'int' to 'color'. An explicit conversion exists (are you missing a cast?)
                //         color t = b ? 1 : 0;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b ? 1 : 0").WithArguments("int", "color").WithLocation(6, 19)
                );

            // Implicit interpolated string conversions
            TestConditional(@"b ? $""x"" : ""x""", "System.FormattableString", "System.String",
                // (6,38): error CS0029: Cannot implicitly convert type 'string' to 'System.FormattableString'
                //         System.FormattableString t = b ? $"x" : "x";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"b ? $""x"" : ""x""").WithArguments("string", "System.FormattableString").WithLocation(6, 38)
                );

            // Implicit nullable conversions
            // Null literal conversions
            TestConditional(@"b ? """" : null", "System.Int64?", "System.String",
                // (6,27): error CS0029: Cannot implicitly convert type 'string' to 'long?'
                //         System.Int64? t = b ? "" : null;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"b ? """" : null").WithArguments("string", "long?").WithLocation(6, 27)
                );
            TestConditional(@"b ? 1 : """"", "System.Int64?", null,
                // (6,35): error CS0029: Cannot implicitly convert type 'string' to 'long?'
                //         System.Int64? t = b ? 1 : "";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "long?").WithLocation(6, 35)
                );

            // Boxing conversions
            TestConditional("b ? GetUInt() : GetInt()", "System.Collections.IList", null,
                // (6,42): error CS0029: Cannot implicitly convert type 'uint' to 'System.Collections.IList'
                //         System.Collections.IList t = b ? GetUInt() : GetInt();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetUInt()").WithArguments("uint", "System.Collections.IList").WithLocation(6, 42),
                // (6,54): error CS0029: Cannot implicitly convert type 'int' to 'System.Collections.IList'
                //         System.Collections.IList t = b ? GetUInt() : GetInt();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetInt()").WithArguments("int", "System.Collections.IList").WithLocation(6, 54)
                );

            // User - defined implicit conversions
            TestConditional("b ? GetB() : GetD()", "X", null,
                // (6,28): error CS0619: 'D.implicit operator X(D)' is obsolete: 'D'
                //         X t = b ? GetB() : GetD();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "GetD()").WithArguments("D.implicit operator X(D)", "D").WithLocation(6, 28)
                );

            // Anonymous function conversions
            TestConditional(@"b ? a=>a : b=>""""", "Del", null,
                // (6,31): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         Del t = b ? a=>a : b=>"";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(6, 31),
                // (6,31): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Del t = b ? a=>a : b=>"";
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, @"""""").WithArguments("lambda expression").WithLocation(6, 31)
                );

            // Method group conversions
            TestConditional("b ? M1 : M3", "Del", null,
                // (6,26): error CS0123: No overload for 'M3' matches delegate 'Del'
                //         Del t = b ? M1 : M3;
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M3").WithArguments("M3", "Del").WithLocation(6, 26)
                );
        }

        [Fact]
        public void SpeculatingOnATargetTypedExpression()
        {

        }

        private static void TestConditional(string conditionalExpression, string targetType, string? naturalType, params DiagnosticDescription[] expectedDiagnostics)
        {
            TestConditional(conditionalExpression, targetType, naturalType, null, expectedDiagnostics);
        }

        private static void TestConditional(
            string conditionalExpression,
            string targetType,
            string? naturalType,
            CSharpParseOptions? parseOptions,
            params DiagnosticDescription[] expectedDiagnostics)
        {
            string source = $@"
class Program
{{
    unsafe void Test<T, U, V>(bool b) where T : class where U : class, T where V : class, T
    {{
        {targetType} t = {conditionalExpression};
        Use(t);
    }}

    A GetA() {{ return null; }}
    B GetB() {{ return null; }}
    C GetC() {{ return null; }}
    D GetD() {{ return null; }}
    int GetInt() {{ return 1; }}
    uint GetUInt() {{ return 1; }}
    T Get<T>() where T : class {{ return null; }}
    void Use(object t) {{ }}
    unsafe void Use(void* t) {{ }}
    unsafe int* GetIntp() {{ return null; }}
    unsafe long* GetLongp() {{ return null; }}

    static int M1(int x) => x;
    static int M2(int x) => x;
    static int M3(int x, int y) => x;
}}

public enum color {{ Red, Blue, Green }};

class A {{ }}
class B : A {{ public static implicit operator X(B self) => new X(); }}
class C : A {{ public static implicit operator X(C self) => new X(); }}
class D : A {{ [System.Obsolete(""D"", true)] public static implicit operator X(D self) => new X(); }}

class X {{ }}

interface IOut<out T> {{ }}
interface IIn<in T> {{ }}

delegate int Del(int x);
";

            parseOptions ??= TestOptions.Regular;
            parseOptions = parseOptions.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion());
            var tree = Parse(source, options: parseOptions);

            var comp = CreateCompilation(tree, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(expectedDiagnostics);

            var compUnit = tree.GetCompilationUnitRoot();
            var classC = (TypeDeclarationSyntax)compUnit.Members.First();
            var methodTest = (MethodDeclarationSyntax)classC.Members.First();
            var stmt = (LocalDeclarationStatementSyntax)methodTest.Body!.Statements.First();
            var conditionalExpr = (ConditionalExpressionSyntax)stmt.Declaration.Variables[0].Initializer!.Value;

            var model = comp.GetSemanticModel(tree);

            if (naturalType is null)
            {
                var actualType = model.GetTypeInfo(conditionalExpr).Type;
                if (actualType is { })
                {
                    Assert.NotEmpty(expectedDiagnostics);
                    Assert.Equal("?", model.GetTypeInfo(conditionalExpr).Type.ToTestDisplayString());
                }
            }
            else
            {
                Assert.Equal(naturalType, model.GetTypeInfo(conditionalExpr).Type.ToTestDisplayString());
            }

            var convertedType = targetType switch { "void*" => "System.Void*", _ => targetType };
            Assert.Equal(convertedType, model.GetTypeInfo(conditionalExpr).ConvertedType.ToTestDisplayString());

            if (!expectedDiagnostics.Any())
            {
                Assert.Equal(SpecialType.System_Boolean, model.GetTypeInfo(conditionalExpr.Condition).Type!.SpecialType);
                Assert.Equal(convertedType, model.GetTypeInfo(conditionalExpr.WhenTrue).ConvertedType.ToTestDisplayString()); //in parent to catch conversion
                Assert.Equal(convertedType, model.GetTypeInfo(conditionalExpr.WhenFalse).ConvertedType.ToTestDisplayString()); //in parent to catch conversion
            }
        }
    }
}
