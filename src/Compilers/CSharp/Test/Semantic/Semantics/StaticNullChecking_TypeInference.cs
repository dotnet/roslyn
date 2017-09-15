// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public partial class StaticNullChecking : CSharpTestBase
    {
        // PROTOTYPE(NullableReferenceTypes): `default(string)` should be non-nullable string.
        [Fact(Skip = "TODO")]
        public void Default_NonNullable()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = default(string);
        s.ToString();
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,17): warning CS8600: Cannot convert null to non-nullable reference.
                //         var s = default(string);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "default(string)").WithLocation(5, 17));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
        }

        [Fact]
        public void Default_Nullable()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = default(string?);
        s.ToString();
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_NonNull()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = string.Empty;
        s.ToString();
        s = null;
        s.ToString();
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         s = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(7, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_NonNull_CSharp7()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = string.Empty;
        s.ToString();
        s = null;
        s.ToString();
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(null, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_Cycle()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = s;
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,17): error CS0841: Cannot use local variable 's' before it is declared
                //         var s = s;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "s").WithArguments("s").WithLocation(5, 17));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            var type = symbol.Type;
            Assert.True(type.IsErrorType());
            Assert.Equal("var", type.ToTestDisplayString());
            Assert.Equal(null, type.IsNullable);
        }

        // PROTOTYPE(NullableReferenceTypes): `var s0 = b ? string.Empty : string.Empty;`
        // should declare non-nullable string.
        [Fact(Skip = "TODO")]
        public void LocalVar_ConditionalOperator()
        {
            var source =
@"class C
{
    static void F(bool b)
    {
        var s0 = b ? string.Empty : string.Empty;
        var s1 = b ? string.Empty : null;
        var s2 = b ? null : string.Empty;
    }
}";

            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);

            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);

            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[2]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_Array_01()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = new[] { string.Empty };
        s[0].ToString();
        var t = new[] { string.Empty, null };
        t[0].ToString();
        var u = new[] { 1, null };
        u[0].ToString();
        var v = new[] { null, (int?)2 };
        v[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,28): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         var u = new[] { 1, null };
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(9, 28),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         t[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t[0]").WithLocation(8, 9));
        }

        // PROTOTYPE(NullableReferenceTypes): Ignore untyped
        // expressions in BestTypeInferrer.GetBestType.
        [Fact(Skip = "TODO")]
        public void LocalVar_Array_02()
        {
            var source =
@"delegate void D();
class C
{
    static void Main()
    {
        var a = new[] { new D(Main), () => { } };
        a[0].ToString();
        var b = new[] { new D(Main), null };
        b[0].ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         b[0].ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b[0]").WithLocation(9, 9));
        }

        [Fact]
        public void TypeInference_01()
        {
            var source =
@"class A { }
class B { }
class C
{
    static void F<T>(T? t) where T : A { }
    static void G(B? b)
    {
        F(b);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): error CS0311: The type 'B' cannot be used as type parameter 'T' in the generic type or method 'C.F<T>(T?)'. There is no implicit reference conversion from 'B' to 'A'.
                //         F(b);
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "F").WithArguments("C.F<T>(T?)", "A", "T", "B").WithLocation(8, 9));
        }

        [Fact]
        public void TypeInference_02()
        {
            var source =
@"interface I<T> { }
class C
{
    static T F<T>(I<T> t)
    {
        throw new System.Exception();
    }
    static void G(I<string> x, I<string?> y)
    {
        F(x).ToString();
        F(y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         F(y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y)").WithLocation(11, 9));
        }

        [Fact]
        public void TypeInference_03()
        {
            var source =
@"interface I<T> { }
class C
{
    static T F<T>(I<T?> t)
    {
        throw new System.Exception();
    }
    static void G(I<string> x, I<string?> y)
    {
        F(x).ToString();
        F(y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,11): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 't' in 'string C.F<string>(I<string?> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string>", "I<string?>", "t", "string C.F<string>(I<string?> t)").WithLocation(10, 11));
        }

        [Fact]
        public void TypeInference_04()
        {
            var source =
@"class C
{
    static T F<T>(T x, T y) => x;
    static void G(C? x, C y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, x)").WithLocation(6, 9),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, y)").WithLocation(7, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, x)").WithLocation(8, 9));
        }

        [Fact]
        public void TypeInference_05()
        {
            var source =
@"class C
{
    static T F<T>(T x, T? y) => x;
    static void G(C? x, C y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, x)").WithLocation(6, 9),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, y)").WithLocation(7, 9));
        }

        [Fact]
        public void TypeInference_06()
        {
            var source =
@"class C
{
    static T F<T, U>(T t, U u) where U : T => t;
    static void G(C? x, C y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, x)").WithLocation(6, 9),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, y)").WithLocation(7, 9));
        }

        [Fact]
        public void TypeInference_07()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static T F<T>(T x, List<T> y) => x;
    static void G(C x, C? y)
    {
        F(x, new List<C?>() { y }).ToString();
        F(y, new List<C>() { x }).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, new List<C?>() { y }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, new List<C?>() { y })").WithLocation(7, 9),
                // (8,11): warning CS8604: Possible null reference argument for parameter 'x' in 'C C.F<C>(C x, List<C> y)'.
                //         F(y, new List<C>() { x }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "y").WithArguments("x", "C C.F<C>(C x, List<C> y)").WithLocation(8, 11));
        }

        [Fact]
        public void TypeInference_08()
        {
            var source =
@"class A { }
class B { }
class C<T, U> { }
class C
{
    static void F<T>(T x, T y) { }
    static void G(C<A, B?> x, C<A?, B> y, C<A, B> z, C<A?, B?> w)
    {
        F(x, y);
        F(y, x);
        F(z, w);
        F(w, z);
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,14): warning CS8620: Nullability of reference types in argument of type 'C<A?, B>' doesn't match target type 'C<A, B?>' for parameter 'y' in 'void C.F<C<A, B?>>(C<A, B?> x, C<A, B?> y)'.
                //         F(x, y);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("C<A?, B>", "C<A, B?>", "y", "void C.F<C<A, B?>>(C<A, B?> x, C<A, B?> y)").WithLocation(9, 14),
                // (10,14): warning CS8620: Nullability of reference types in argument of type 'C<A, B?>' doesn't match target type 'C<A?, B>' for parameter 'y' in 'void C.F<C<A?, B>>(C<A?, B> x, C<A?, B> y)'.
                //         F(y, x);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("C<A, B?>", "C<A?, B>", "y", "void C.F<C<A?, B>>(C<A?, B> x, C<A?, B> y)").WithLocation(10, 14),
                // (11,14): warning CS8620: Nullability of reference types in argument of type 'C<A?, B?>' doesn't match target type 'C<A, B>' for parameter 'y' in 'void C.F<C<A, B>>(C<A, B> x, C<A, B> y)'.
                //         F(z, w);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "w").WithArguments("C<A?, B?>", "C<A, B>", "y", "void C.F<C<A, B>>(C<A, B> x, C<A, B> y)").WithLocation(11, 14),
                // (12,14): warning CS8620: Nullability of reference types in argument of type 'C<A, B>' doesn't match target type 'C<A?, B?>' for parameter 'y' in 'void C.F<C<A?, B?>>(C<A?, B?> x, C<A?, B?> y)'.
                //         F(w, z);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("C<A, B>", "C<A?, B?>", "y", "void C.F<C<A?, B?>>(C<A?, B?> x, C<A?, B?> y)").WithLocation(12, 14));
        }

        [Fact]
        public void TypeInference_09()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static T F<T>(I<T> x, I<T> y)
    {
        throw new System.Exception();
    }
    static void G(I<string> x, I<string?> y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
    static T F<T>(IIn<T> x, IIn<T> y)
    {
        throw new System.Exception();
    }
    static void G(IIn<string> x, IIn<string?> y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
    static T F<T>(IOut<T> x, IOut<T> y)
    {
        throw new System.Exception();
    }
    static void G(IOut<string> x, IOut<string?> y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,11): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'x' in 'string? C.F<string?>(I<string?> x, I<string?> y)'.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string>", "I<string?>", "x", "string? C.F<string?>(I<string?> x, I<string?> y)").WithLocation(13, 11),
                // (13,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, y)").WithLocation(13, 9),
                // (14,14): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'y' in 'string? C.F<string?>(I<string?> x, I<string?> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string>", "I<string?>", "y", "string? C.F<string?>(I<string?> x, I<string?> y)").WithLocation(14, 14),
                // (14,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, x)").WithLocation(14, 9),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, y)").WithLocation(15, 9),
                // (24,14): warning CS8620: Nullability of reference types in argument of type 'IIn<string?>' doesn't match target type 'IIn<string>' for parameter 'y' in 'string C.F<string>(IIn<string> x, IIn<string> y)'.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<string?>", "IIn<string>", "y", "string C.F<string>(IIn<string> x, IIn<string> y)").WithLocation(24, 14),
                // (25,11): warning CS8620: Nullability of reference types in argument of type 'IIn<string?>' doesn't match target type 'IIn<string>' for parameter 'x' in 'string C.F<string>(IIn<string> x, IIn<string> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<string?>", "IIn<string>", "x", "string C.F<string>(IIn<string> x, IIn<string> y)").WithLocation(25, 11),
                // (26,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, y)").WithLocation(26, 9),
                // (35,11): warning CS8620: Nullability of reference types in argument of type 'IOut<string>' doesn't match target type 'IOut<string?>' for parameter 'x' in 'string? C.F<string?>(IOut<string?> x, IOut<string?> y)'.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IOut<string>", "IOut<string?>", "x", "string? C.F<string?>(IOut<string?> x, IOut<string?> y)").WithLocation(35, 11),
                // (35,9): warning CS8602: Possible dereference of a null reference.
                //         F(x, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x, y)").WithLocation(35, 9),
                // (36,14): warning CS8620: Nullability of reference types in argument of type 'IOut<string>' doesn't match target type 'IOut<string?>' for parameter 'y' in 'string? C.F<string?>(IOut<string?> x, IOut<string?> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IOut<string>", "IOut<string?>", "y", "string? C.F<string?>(IOut<string?> x, IOut<string?> y)").WithLocation(36, 14),
                // (36,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, x)").WithLocation(36, 9),
                // (37,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, y)").WithLocation(37, 9));
        }

        [Fact]
        public void TypeInference_10()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static T F<T>(I<T> x, I<T?> y)
    {
        throw new System.Exception();
    }
    static void G(I<string> x, I<string?> y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
    static T F<T>(IIn<T> x, IIn<T?> y)
    {
        throw new System.Exception();
    }
    static void G(IIn<string> x, IIn<string?> y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
    static T F<T>(IOut<T> x, IOut<T?> y)
    {
        throw new System.Exception();
    }
    static void G(IOut<string> x, IOut<string?> y)
    {
        F(x, x).ToString();
        F(x, y).ToString();
        F(y, x).ToString();
        F(y, y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,14): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'y' in 'string C.F<string>(I<string> x, I<string?> y)'.
                //         F(x, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string>", "I<string?>", "y", "string C.F<string>(I<string> x, I<string?> y)").WithLocation(12, 14),
                // (14,14): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'y' in 'string? C.F<string?>(I<string?> x, I<string?> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<string>", "I<string?>", "y", "string? C.F<string?>(I<string?> x, I<string?> y)").WithLocation(14, 14),
                // (14,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, x)").WithLocation(14, 9),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, y)").WithLocation(15, 9),
                // (23,14): warning CS8620: Nullability of reference types in argument of type 'IIn<string>' doesn't match target type 'IIn<string?>' for parameter 'y' in 'string C.F<string>(IIn<string> x, IIn<string?> y)'.
                //         F(x, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IIn<string>", "IIn<string?>", "y", "string C.F<string>(IIn<string> x, IIn<string?> y)").WithLocation(23, 14),
                // (25,11): warning CS8620: Nullability of reference types in argument of type 'IIn<string?>' doesn't match target type 'IIn<string>' for parameter 'x' in 'string C.F<string>(IIn<string> x, IIn<string?> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<string?>", "IIn<string>", "x", "string C.F<string>(IIn<string> x, IIn<string?> y)").WithLocation(25, 11),
                // (25,14): warning CS8620: Nullability of reference types in argument of type 'IIn<string>' doesn't match target type 'IIn<string?>' for parameter 'y' in 'string C.F<string>(IIn<string> x, IIn<string?> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IIn<string>", "IIn<string?>", "y", "string C.F<string>(IIn<string> x, IIn<string?> y)").WithLocation(25, 14),
                // (26,11): warning CS8620: Nullability of reference types in argument of type 'IIn<string?>' doesn't match target type 'IIn<string>' for parameter 'x' in 'string C.F<string>(IIn<string> x, IIn<string?> y)'.
                //         F(y, y).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("IIn<string?>", "IIn<string>", "x", "string C.F<string>(IIn<string> x, IIn<string?> y)").WithLocation(26, 11),
                // (34,14): warning CS8620: Nullability of reference types in argument of type 'IOut<string>' doesn't match target type 'IOut<string?>' for parameter 'y' in 'string C.F<string>(IOut<string> x, IOut<string?> y)'.
                //         F(x, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IOut<string>", "IOut<string?>", "y", "string C.F<string>(IOut<string> x, IOut<string?> y)").WithLocation(34, 14),
                // (36,14): warning CS8620: Nullability of reference types in argument of type 'IOut<string>' doesn't match target type 'IOut<string?>' for parameter 'y' in 'string? C.F<string?>(IOut<string?> x, IOut<string?> y)'.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IOut<string>", "IOut<string?>", "y", "string? C.F<string?>(IOut<string?> x, IOut<string?> y)").WithLocation(36, 14),
                // (36,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, x).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, x)").WithLocation(36, 9),
                // (37,9): warning CS8602: Possible dereference of a null reference.
                //         F(y, y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y, y)").WithLocation(37, 9));
        }

        [Fact]
        public void TupleTypeInference_01()
        {
            var source =
@"class C
{
    static (T, T) F<T>((T, T) t) => t;
    static void G(string x, string? y)
    {
        F((x, x)).Item2.ToString();
        F((x, y)).Item2.ToString();
        F((y, x)).Item2.ToString();
        F((y, y)).Item2.ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         F((x, y)).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F((x, y)).Item2").WithLocation(7, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F((y, x)).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F((y, x)).Item2").WithLocation(8, 9),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F((y, y)).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F((y, y)).Item2").WithLocation(9, 9));
        }

        [Fact]
        public void TupleTypeInference_02()
        {
            var source =
@"class C
{
    static (T, T) F<T>((T, T?) t) => (t.Item1, t.Item1);
    static void G(string x, string? y)
    {
        F((x, x)).Item2.ToString();
        F((x, y)).Item2.ToString();
        F((y, x)).Item2.ToString();
        F((y, y)).Item2.ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F((y, x)).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F((y, x)).Item2").WithLocation(8, 9),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F((y, y)).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F((y, y)).Item2").WithLocation(9, 9));
        }

        [Fact]
        public void TupleTypeInference_03()
        {
            var source =
@"class C
{
    static T F<T>((T, T?) t) => t.Item1;
    static void G((string, string) x, (string, string?) y, (string?, string) z, (string?, string?) w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,11): warning CS8620: Nullability of reference types in argument of type '(string, string)' doesn't match target type '(string, string)' for parameter 't' in 'string C.F<string>((string, string) t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("(string, string)", "(string, string)", "t", "string C.F<string>((string, string) t)").WithLocation(6, 11),
                // (8,11): warning CS8620: Nullability of reference types in argument of type '(string, string)' doesn't match target type '(string, string)' for parameter 't' in 'string? C.F<string?>((string, string) t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("(string, string)", "(string, string)", "t", "string? C.F<string?>((string, string) t)").WithLocation(8, 11),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(8, 9),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(9, 9));
        }

        [Fact]
        public void TupleTypeInference_04()
        {
            var source =
@"class C
{
    static T F<T>(out (T, T?) t) => throw new System.Exception();
    static void G()
    {
        F(out (string, string) t1).ToString();
        F(out (string, string?) t2).ToString();
        F(out (string?, string) t3).ToString();
        F(out (string?, string?) t4).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,15): warning CS8619: Nullability of reference types in value of type '(string, string)' doesn't match target type '(string, string)'.
                //         F(out (string, string) t1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(string, string) t1").WithArguments("(string, string)", "(string, string)").WithLocation(6, 15),
                // (8,15): warning CS8619: Nullability of reference types in value of type '(string, string)' doesn't match target type '(string, string)'.
                //         F(out (string?, string) t3).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "(string?, string) t3").WithArguments("(string, string)", "(string, string)").WithLocation(8, 15),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(out (string?, string) t3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(out (string?, string) t3)").WithLocation(8, 9),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(out (string?, string?) t4).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(out (string?, string?) t4)").WithLocation(9, 9));
        }

        [Fact]
        public void TupleTypeInference_05()
        {
            var source =
@"interface I<T> { }
interface IIn<in T> { }
interface IOut<out T> { }
class C
{
    static T F<T>(I<(T, T?)> t) => throw new System.Exception();
    static void G(I<(string, string)> x, I<(string, string?)> y, I<(string?, string)> z, I<(string?, string?)> w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
    static T F<T>(IIn<(T, T?)> t) => throw new System.Exception();
    static void G(IIn<(string, string)> x, IIn<(string, string?)> y, IIn<(string?, string)> z, IIn<(string?, string?)> w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
    static T F<T>(IOut<(T, T?)> t) => throw new System.Exception();
    static void G(IOut<(string, string)> x, IOut<(string, string?)> y, IOut<(string?, string)> z, IOut<(string?, string?)> w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,11): warning CS8620: Nullability of reference types in argument of type 'I<(string, string)>' doesn't match target type 'I<(string, string)>' for parameter 't' in 'string C.F<string>(I<(string, string)> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<(string, string)>", "I<(string, string)>", "t", "string C.F<string>(I<(string, string)> t)").WithLocation(9, 11),
                // (11,11): warning CS8620: Nullability of reference types in argument of type 'I<(string, string)>' doesn't match target type 'I<(string, string)>' for parameter 't' in 'string? C.F<string?>(I<(string, string)> t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("I<(string, string)>", "I<(string, string)>", "t", "string? C.F<string?>(I<(string, string)> t)").WithLocation(11, 11),
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(11, 9),
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(12, 9),
                // (17,11): warning CS8620: Nullability of reference types in argument of type 'IIn<(string, string)>' doesn't match target type 'IIn<(string, string)>' for parameter 't' in 'string C.F<string>(IIn<(string, string)> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IIn<(string, string)>", "IIn<(string, string)>", "t", "string C.F<string>(IIn<(string, string)> t)").WithLocation(17, 11),
                // (19,11): warning CS8620: Nullability of reference types in argument of type 'IIn<(string, string)>' doesn't match target type 'IIn<(string, string)>' for parameter 't' in 'string? C.F<string?>(IIn<(string, string)> t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IIn<(string, string)>", "IIn<(string, string)>", "t", "string? C.F<string?>(IIn<(string, string)> t)").WithLocation(19, 11),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(19, 9),
                // (20,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(20, 9),
                // (25,11): warning CS8620: Nullability of reference types in argument of type 'IOut<(string, string)>' doesn't match target type 'IOut<(string, string)>' for parameter 't' in 'string C.F<string>(IOut<(string, string)> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IOut<(string, string)>", "IOut<(string, string)>", "t", "string C.F<string>(IOut<(string, string)> t)").WithLocation(25, 11),
                // (27,11): warning CS8620: Nullability of reference types in argument of type 'IOut<(string, string)>' doesn't match target type 'IOut<(string, string)>' for parameter 't' in 'string? C.F<string?>(IOut<(string, string)> t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<(string, string)>", "IOut<(string, string)>", "t", "string? C.F<string?>(IOut<(string, string)> t)").WithLocation(27, 11),
                // (27,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(27, 9),
                // (28,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(28, 9));
        }

        [Fact]
        public void ReturnTypeInference_01()
        {
            var source =
@"class C
{
    static T F<T>(System.Func<T> f)
    {
        return f();
    }
    static void G(string x, string? y)
    {
        F(() => x).ToString();
        F(() => y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         F(() => y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => y)").WithLocation(10, 9));
        }

        // Multiple returns, one of which is null.
        [Fact]
        public void ReturnTypeInference_02()
        {
            var source =
@"class C
{
    static T F<T>(System.Func<T> f)
    {
        return f();
    }
    static void G(string x)
    {
        F(() => { if (x.Length > 0) return x; return null; }).ToString();
        F(() => { if (x.Length == 0) return null; return x; }).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(() => { if (x.Length > 0) return x; return null; }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => { if (x.Length > 0) return x; return null; })").WithLocation(9, 9),
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         F(() => { if (x.Length == 0) return null; return x; }).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(() => { if (x.Length == 0) return null; return x; })").WithLocation(10, 9));
        }

        [Fact]
        public void ReturnTypeInference_CSharp7()
        {
            var source =
@"using System;
class C
{
    static void Main(string[] args)
    {
        args.F(arg => arg.Length);
    }
}
static class E
{
    internal static U[] F<T, U>(this T[] a, Func<T, U> f) => throw new Exception();
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Deconstruction declaration ignores nullability.
        [Fact(Skip = "TODO")]
        public void DeconstructionTypeInference_01()
        {
            var source =
@"class C
{
    static void M()
    {
        (var x, var y) = ((string?)null, string.Empty);
        x.ToString();
        y.ToString();
        x = null;
        y = null;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(6, 9),
                // (9,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         y = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(9, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Deconstruction declaration ignores nullability.
        [Fact(Skip = "TODO")]
        public void DeconstructionTypeInference_02()
        {
            var source =
@"class C
{
    static (string?, string) F() => (string.Empty, string.Empty);
    static void G()
    {
        (var x, var y) = F();
        x.ToString();
        y.ToString();
        x = null;
        y = null;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(7, 9),
                // (10,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         y = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(10, 13));
        }

        // PROTOTYPE(NullableReferenceTypes): Deconstruction declaration ignores nullability.
        [Fact(Skip = "TODO")]
        public void DeconstructionTypeInference_03()
        {
            var source =
@"class C
{
    void Deconstruct(out string? x, out string y)
    {
        x = string.Empty;
        y = string.Empty;
    }
    static void M()
    {
        (var x, var y) = new C();
        x.ToString();
        y.ToString();
        x = null;
        y = null;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(11, 9),
                // (14,13): warning CS8600: Cannot convert null to non-nullable reference.
                //         y = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(14, 13));
        }

        [Fact]
        public void DeconstructionTypeInference_04()
        {
            var source =
@"class C
{
    static (string?, string) F() => (string.Empty, string.Empty);
    static void G()
    {
        string x;
        string? y;
        var t = ((x, y) = F());
        t.x.ToString();
        t.y.ToString();
        t.x = null;
        t.y = null;
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         t.y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.y").WithLocation(10, 9),
                // (11,15): warning CS8600: Cannot convert null to non-nullable reference.
                //         t.x = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 15));
        }

        // PROTOTYPE(NullableReferenceTypes): Deconstruction declaration ignores nullability.
        [Fact(Skip = "TODO")]
        public void DeconstructionTypeInference_05()
        {
            var source =
@"using System;
using System.Collections.Generic;
class C
{
    static IEnumerable<(string, string?)> F() => throw new Exception();
    static void G()
    {
        foreach ((var x, var y) in F())
        {
            x.ToString();
            y.ToString();
        }
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8602: Possible dereference of a null reference.
                //             y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y").WithLocation(11, 13));
        }

        [Fact]
        public void TypeInference_TupleNameDifferences_01()
        {
            var source =
@"class C<T>
{
}
static class E
{
    public static T F<T>(this C<T> c, T t) => t;
}
class C
{
    static void F(object o)
    {
        var c = new C<(object x, int y)>();
        c.F((o, -1)).x.ToString();
    }
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (13,22): error CS1061: '(object, int)' does not contain a definition for 'x' and no extension method 'x' accepting a first argument of type '(object, int)' could be found (are you missing a using directive or an assembly reference?)
                //         c.F((o, -1)).x.ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "x").WithArguments("(object, int)", "x").WithLocation(13, 22));

            comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,22): error CS1061: '(object, int)' does not contain a definition for 'x' and no extension method 'x' accepting a first argument of type '(object, int)' could be found (are you missing a using directive or an assembly reference?)
                //         c.F((o, -1)).x.ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "x").WithArguments("(object, int)", "x").WithLocation(13, 22));
        }

        [Fact]
        public void TypeInference_TupleNameDifferences_02()
        {
            var source =
@"class C<T>
{
}
static class E
{
    public static T F<T>(this C<T> c, T t) => t;
}
class C
{
    static void F(object o)
    {
        var c = new C<(object? x, int y)>();
        c.F((o, -1)).x.ToString();
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,22): error CS1061: '(object, int)' does not contain a definition for 'x' and no extension method 'x' accepting a first argument of type '(object, int)' could be found (are you missing a using directive or an assembly reference?)
                //         c.F((o, -1)).x.ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "x").WithArguments("(object, int)", "x").WithLocation(13, 22));
        }

        [Fact]
        public void TypeInference_DynamicDifferences_01()
        {
            var source =
@"class C<T>
{
}
static class E
{
    public static T F<T>(this C<T> c, T t) => t;
}
class C
{
    static void F(dynamic x, object y)
    {
        var c = new C<(object, object)>();
        c.F((x, y)).Item1.G();
    }
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();

            comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_DynamicDifferences_02()
        {
            var source =
@"class C<T>
{
}
static class E
{
    public static T F<T>(this C<T> c, T t) => t;
}
class C
{
    static void F(dynamic x, object y)
    {
        var c = new C<(object, object?)>();
        c.F((x, y)).Item1.G();
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // PROTOTYPE(NullableReferenceTypes): Assert failure in
        // ConversionsBase.IsValidExtensionMethodThisArgConversion.
        [Fact(Skip = "TODO")]
        public void TypeInference_DynamicDifferences_03()
        {
            var source =
@"interface I<T>
{
}
static class E
{
    public static T F<T>(this I<T> i, T t) => t;
}
class C
{
    static void F(I<object> i, dynamic? d)
    {
        i.F(d).G();
    }
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,9): error CS1929: 'I<object>' does not contain a definition for 'F' and the best extension method overload 'E.F<T>(I<T>, T)' requires a receiver of type 'I<T>'
                //         i.F(d).G();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "i").WithArguments("I<object>", "F", "E.F<T>(I<T>, T)", "I<T>").WithLocation(12, 9));
        }

        [Fact]
        public void TypeInference_Local()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G()
    {
        object x = new object();
        object? y = x;
        F(x).ToString();
        F(y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y)").WithLocation(9, 9));
        }

        [Fact]
        public void TypeInference_Call()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static object F1() => new object();
    static object? F2() => null;
    static void G()
    {
        F(F1()).ToString();
        F(F2()).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(F2()).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(F2())").WithLocation(9, 9));
        }

        [Fact]
        public void TypeInference_Property()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static object P => new object();
    static object? Q => null;
    static void G()
    {
        F(P).ToString();
        F(Q).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(Q).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(Q)").WithLocation(9, 9));
        }

        [Fact]
        public void TypeInference_FieldAccess()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static object F1 = new object();
    static object? F2 = null;
    static void G()
    {
        F(F1).ToString();
        F(F2).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(F2).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(F2)").WithLocation(9, 9));
        }

        [Fact]
        public void TypeInference_Literal()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G()
    {
        F(0).ToString();
        F('A').ToString();
        F(""B"").ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_Default()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G()
    {
        F(default(object)).ToString();
        F(default(int)).ToString();
        F(default(string)).ToString();
        F(default).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,9): error CS0411: The type arguments for method 'C.F<T>(T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(default).ToString();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("C.F<T>(T)").WithLocation(9, 9),
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         F(default(object)).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(default(object))").WithLocation(6, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(default(string)).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(default(string))").WithLocation(8, 9));
        }

        [Fact]
        public void TypeInference_Tuple_01()
        {
            var source =
@"class C
{
    static (T, U) F<T, U>((T, U) t) => t;
    static void G(string x, string? y)
    {
        var t = (x, y);
        F(t).Item1.ToString();
        F(t).Item2.ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(t).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(t).Item2").WithLocation(8, 9));
        }

        [Fact]
        public void TypeInference_Tuple_02()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G(string x, string? y)
    {
        var t = (x, y);
        F(t).Item1.ToString();
        F(t).Item2.ToString();
        F(t).x.ToString();
        F(t).y.ToString();
        var u = (a: x, b: y);
        F(u).Item1.ToString();
        F(u).Item2.ToString();
        F(u).a.ToString();
        F(u).b.ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(t).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(t).Item2").WithLocation(8, 9),
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         F(t).y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(t).y").WithLocation(10, 9),
                // (13,9): warning CS8602: Possible dereference of a null reference.
                //         F(u).Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(u).Item2").WithLocation(13, 9),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         F(u).b.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(u).b").WithLocation(15, 9));
        }

        [Fact]
        public void TypeInference_ObjectCreation()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G()
    {
        F(new C { }).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_DelegateCreation()
        {
            var source =
@"delegate void D();
class C
{
    static T F<T>(T t) => t;
    static void G()
    {
        F(new D(G)).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_BinaryOperator()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G(string x, string? y)
    {
        F(x + x).ToString();
        F(x + y).ToString();
        F(y + x).ToString();
        F(y + y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeInference_NullCoalescingOperator()
        {
            var source =
@"class C
{
    static T F<T>(T t) => t;
    static void G(object x, object? y)
    {
        F(x ?? x).ToString();
        F(x ?? y).ToString();
        F(y ?? x).ToString();
        F(y ?? y).ToString();
    }
}";
            var comp = CreateStandardCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,11): hidden CS8607: Expression is probably never null.
                //         F(x ?? x).ToString();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x").WithLocation(6, 11),
                // (7,11): hidden CS8607: Expression is probably never null.
                //         F(x ?? y).ToString();
                Diagnostic(ErrorCode.HDN_ExpressionIsProbablyNeverNull, "x").WithLocation(7, 11),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(y ?? y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y ?? y)").WithLocation(9, 9));
        }
    }
}
