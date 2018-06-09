// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StaticNullChecking_TypeInference : CSharpTestBase
    {
        [Fact]
        public void Default_NonNullable()
        {
            var source =
@"class C
{
    static void Main()
    {
        var s = default(string);
        s.ToString();
        var i = default(int);
        i.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
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
        var i = default(int?);
        i.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("System.Int32?", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        // PROTOTYPE(NullableReferenceTypes): Report CS0453 for T?.
        [Fact(Skip = "CS0453")]
        public void Default_TUnconstrained()
        {
            var source =
@"class C
{
    static void F<T>()
    {
        var s = default(T);
        s.ToString();
        var t = default(T?);
        t.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,25): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var t = default(T?);
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T?").WithArguments("System.Nullable<T>", "T", "T").WithLocation(7, 25));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("T", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("T?", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void Default_TClass()
        {
            var source =
@"class C
{
    static void F<T>() where T : class
    {
        var s = default(T);
        s.ToString();
        var t = default(T?);
        t.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(8, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("T", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            // PROTOTYPE(NullableReferenceTypes): Should be "T?".
            Assert.Equal("T", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void DefaultInferred_NonNullable()
        {
            var source =
@"class C
{
    static void Main()
    {
        string s = default;
        s.ToString();
        int i = default;
        i.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         string s = default;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "default").WithLocation(5, 20),
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
        }

        [Fact]
        public void DefaultInferred_Nullable()
        {
            var source =
@"class C
{
    static void Main()
    {
        string? s = default;
        s.ToString();
        int? i = default;
        i.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("System.Int32?", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        // PROTOTYPE(NullableReferenceTypes): Report CS0453 for T?.
        [Fact(Skip = "CS0453")]
        public void DefaultInferred_TUnconstrained()
        {
            var source =
@"class C
{
    static void F<T>()
    {
        T s = default;
        s.ToString();
        T? t = default;
        t.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,9): error CS0453: The type 'T' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         T? t = default;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "T?").WithArguments("System.Nullable<T>", "T", "T").WithLocation(7, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("T", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("T?", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void DefaultInferred_TClass()
        {
            var source =
@"class C
{
    static void F<T>() where T : class
    {
        T s = default;
        s.ToString();
        T? t = default;
        t.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,15): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         T s = default;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "default").WithLocation(5, 15),
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(6, 9),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(8, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("T", symbol.Type.ToTestDisplayString());
            Assert.Equal(false, symbol.Type.IsNullable);
            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            // PROTOTYPE(NullableReferenceTypes): Should be "T?".
            Assert.Equal("T", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_NonNull()
        {
            var source =
@"class C
{
    static void F(string str)
    {
        var s = str;
        s.ToString();
        s = null;
        s.ToString();
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         s = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(7, 13),
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         s.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s").WithLocation(8, 9));

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

            var comp = CreateCompilation(
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
        public void LocalVar_FlowAnalysis_01()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        var t = s;
        t.ToString();
        t = null;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,9): warning CS8602: Possible dereference of a null reference.
                //         t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(6, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_02()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        t = null;
        var t = s;
        t.ToString();
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,9): error CS0841: Cannot use local variable 't' before it is declared
                //         t = null;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "t").WithArguments("t").WithLocation(5, 9),
                // (7,9): warning CS8602: Possible dereference of a null reference.
                //         t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(7, 9));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_03()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        if (s == null)
        {
            return;
        }
        var t = s;
        t.ToString();
        t = null;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         t = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(11, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            // PROTOTYPE(NullableReferenceTypes): IsNullable should be inferred nullable state: false.
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_04()
        {
            var source =
@"class C
{
    static void F(int n)
    {
        string? s = string.Empty;
        while (n-- > 0)
        {
            var t = s;
            t.ToString();
            t = null;
            s = null;
        }
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(9, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_05()
        {
            var source =
@"class C
{
    static void F(int n, string? s)
    {
        while (n-- > 0)
        {
            var t = s;
            t.ToString();
            t = null;
            s = string.Empty;
        }
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,13): warning CS8602: Possible dereference of a null reference.
                //             t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(8, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_06()
        {
            var source =
@"class C
{
    static void F(int n)
    {
        string? s = string.Empty;
        while (n-- > 0)
        {
            var t = s;
            t.ToString();
            t = null;
            if (n % 2 == 0) s = null;
        }
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(9, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_07()
        {
            var source =
@"class C
{
    static void F(int n)
    {
        string? s = string.Empty;
        while (n-- > 0)
        {
            var t = s;
            t.ToString();
            t = null;
            if (n % 2 == 0) s = string.Empty;
            else s = null;
        }
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             t.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t").WithLocation(9, 13));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarator);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(true, symbol.Type.IsNullable);
        }

        [Fact]
        public void LocalVar_FlowAnalysis_08()
        {
            var source =
@"class C
{
    static void F(string? s)
    {
        var t = s!;
        t/*T:string!*/.ToString();
        t = null;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         t = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(7, 13));
            comp.VerifyTypes();

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

            var comp = CreateCompilation(
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

        [Fact]
        public void LocalVar_ConditionalOperator()
        {
            var source =
@"class C
{
    static void F(bool b, string s)
    {
        var s0 = b ? s : s;
        var s1 = b ? s : null;
        var s2 = b ? null : s;
    }
}";

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var declarators = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();

            var symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[0]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(null, symbol.Type.IsNullable);  // PROTOTYPE(NullableReferenceTypes): Inferred nullability: false

            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[1]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(null, symbol.Type.IsNullable); // PROTOTYPE(NullableReferenceTypes): Inferred nullability: true

            symbol = (LocalSymbol)model.GetDeclaredSymbol(declarators[2]);
            Assert.Equal("System.String", symbol.Type.ToTestDisplayString());
            Assert.Equal(null, symbol.Type.IsNullable); // PROTOTYPE(NullableReferenceTypes): Inferred nullability: true
        }

        [Fact]
        public void LocalVar_Array_01()
        {
            var source =
@"class C
{
    static void F(string str)
    {
        var s = new[] { str };
        s[0].ToString();
        var t = new[] { str, null };
        t[0].ToString();
        var u = new[] { 1, null };
        u[0].ToString();
        var v = new[] { null, (int?)2 };
        v[0].ToString();
    }
}";
            var comp = CreateCompilation(
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

        [Fact]
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
    static void G1(I<string> x1, I<string?> y1)
    {
        F(x1, x1).ToString();
        F(x1, y1).ToString();
        F(y1, x1).ToString();
        F(y1, y1).ToString();
    }
    static T F<T>(IIn<T> x, IIn<T> y)
    {
        throw new System.Exception();
    }
    static void G2(IIn<string> x2, IIn<string?> y2)
    {
        F(x2, x2).ToString();
        F(x2, y2).ToString();
        F(y2, x2).ToString();
        F(y2, y2).ToString();
    }
    static T F<T>(IOut<T> x, IOut<T> y)
    {
        throw new System.Exception();
    }
    static void G3(IOut<string> x3, IOut<string?> y3)
    {
        F(x3, x3).ToString();
        F(x3, y3).ToString();
        F(y3, x3).ToString();
        F(y3, y3).ToString();
    }
}";
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,11): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'x' in 'string? C.F<string?>(I<string?> x, I<string?> y)'.
                //         F(x1, y1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x1").WithArguments("I<string>", "I<string?>", "x", "string? C.F<string?>(I<string?> x, I<string?> y)").WithLocation(13, 11),
                // (13,9): warning CS8602: Possible dereference of a null reference.
                //         F(x1, y1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x1, y1)").WithLocation(13, 9),
                // (14,15): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'y' in 'string? C.F<string?>(I<string?> x, I<string?> y)'.
                //         F(y1, x1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x1").WithArguments("I<string>", "I<string?>", "y", "string? C.F<string?>(I<string?> x, I<string?> y)").WithLocation(14, 15),
                // (14,9): warning CS8602: Possible dereference of a null reference.
                //         F(y1, x1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y1, x1)").WithLocation(14, 9),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         F(y1, y1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y1, y1)").WithLocation(15, 9),
                // (26,9): warning CS8602: Possible dereference of a null reference.
                //         F(y2, y2).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y2, y2)").WithLocation(26, 9),
                // (35,9): warning CS8602: Possible dereference of a null reference.
                //         F(x3, y3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x3, y3)").WithLocation(35, 9),
                // (36,9): warning CS8602: Possible dereference of a null reference.
                //         F(y3, x3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y3, x3)").WithLocation(36, 9),
                // (37,9): warning CS8602: Possible dereference of a null reference.
                //         F(y3, y3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y3, y3)").WithLocation(37, 9));
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
    static void G1(I<string> x1, I<string?> y1)
    {
        F(x1, x1).ToString();
        F(x1, y1).ToString();
        F(y1, x1).ToString();
        F(y1, y1).ToString();
    }
    static T F<T>(IIn<T> x, IIn<T?> y)
    {
        throw new System.Exception();
    }
    static void G2(IIn<string> x2, IIn<string?> y2)
    {
        F(x2, x2).ToString();
        F(x2, y2).ToString();
        F(y2, x2).ToString();
        F(y2, y2).ToString();
    }
    static T F<T>(IOut<T> x, IOut<T?> y)
    {
        throw new System.Exception();
    }
    static void G3(IOut<string> x3, IOut<string?> y3)
    {
        F(x3, x3).ToString();
        F(x3, y3).ToString();
        F(y3, x3).ToString();
        F(y3, y3).ToString();
    }
}";
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'y' in 'string C.F<string>(I<string> x, I<string?> y)'.
                //         F(x1, x1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x1").WithArguments("I<string>", "I<string?>", "y", "string C.F<string>(I<string> x, I<string?> y)").WithLocation(12, 15),
                // (14,15): warning CS8620: Nullability of reference types in argument of type 'I<string>' doesn't match target type 'I<string?>' for parameter 'y' in 'string? C.F<string?>(I<string?> x, I<string?> y)'.
                //         F(y1, x1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x1").WithArguments("I<string>", "I<string?>", "y", "string? C.F<string?>(I<string?> x, I<string?> y)").WithLocation(14, 15),
                // (14,9): warning CS8602: Possible dereference of a null reference.
                //         F(y1, x1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y1, x1)").WithLocation(14, 9),
                // (15,9): warning CS8602: Possible dereference of a null reference.
                //         F(y1, y1).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y1, y1)").WithLocation(15, 9),
                // (23,15): warning CS8620: Nullability of reference types in argument of type 'IIn<string>' doesn't match target type 'IIn<string?>' for parameter 'y' in 'string C.F<string>(IIn<string> x, IIn<string?> y)'.
                //         F(x2, x2).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x2").WithArguments("IIn<string>", "IIn<string?>", "y", "string C.F<string>(IIn<string> x, IIn<string?> y)").WithLocation(23, 15),
                // (25,15): warning CS8620: Nullability of reference types in argument of type 'IIn<string>' doesn't match target type 'IIn<string?>' for parameter 'y' in 'string C.F<string>(IIn<string> x, IIn<string?> y)'.
                //         F(y2, x2).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x2").WithArguments("IIn<string>", "IIn<string?>", "y", "string C.F<string>(IIn<string> x, IIn<string?> y)").WithLocation(25, 15),
                // (36,9): warning CS8602: Possible dereference of a null reference.
                //         F(y3, x3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y3, x3)").WithLocation(36, 9),
                // (37,9): warning CS8602: Possible dereference of a null reference.
                //         F(y3, y3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y3, y3)").WithLocation(37, 9));
        }

        [Fact]
        public void TypeInference_11()
        {
            var source0 =
@"public class A<T>
{
    public T F;
}
public class UnknownNull
{
    public A<object> A1;
}";
            var comp0 = CreateCompilation(source0, parseOptions: TestOptions.Regular7);
            comp0.VerifyDiagnostics();
            var ref0 = comp0.EmitToImageReference();

            var source1 =
@"#pragma warning disable 8618
public class MaybeNull
{
    public A<object?> A2;
}
public class NotNull
{
    public A<object> A3;
}";
            var comp1 = CreateCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp1.VerifyDiagnostics();
            var ref1 = comp1.EmitToImageReference();

            var source =
@"class C
{
    static T F<T>(T x, T y) => throw null;
    static void F1(UnknownNull x1, UnknownNull y1)
    {
        F(x1.A1, y1.A1)/*T:A<object>*/.F.ToString();
    }
    static void F2(UnknownNull x2, MaybeNull y2)
    {
        F(x2.A1, y2.A2)/*T:A<object>*/.F.ToString();
    }
    static void F3(MaybeNull x3, UnknownNull y3)
    {
        F(x3.A2, y3.A1)/*T:A<object?>!*/.F.ToString();
    }
    static void F4(MaybeNull x4, MaybeNull y4)
    {
        F(x4.A2, y4.A2)/*T:A<object?>!*/.F.ToString();
    }
    static void F5(UnknownNull x5, NotNull y5)
    {
        F(x5.A1, y5.A3)/*T:A<object>*/.F.ToString();
    }
    static void F6(NotNull x6, UnknownNull y6)
    {
        F(x6.A3, y6.A1)/*T:A<object!>!*/.F.ToString();
    }
    static void F7(MaybeNull x7, NotNull y7)
    {
        F(x7.A2, y7.A3)/*T:A<object?>!*/.F.ToString();
    }
    static void F8(NotNull x8, MaybeNull y8)
    {
        F(x8.A3, y8.A2)/*T:A<object!>!*/.F.ToString();
    }
    static void F9(NotNull x9, NotNull y9)
    {
        F(x9.A3, y9.A3)/*T:A<object!>!*/.F.ToString();
    }
}";
            var comp = CreateCompilation(source, references: new[] { ref0, ref1 }, parseOptions: TestOptions.Regular8);
            comp.VerifyTypes();
            comp.VerifyDiagnostics(
                // (14,9): warning CS8602: Possible dereference of a null reference.
                //         F(x3.A2, y3.A1)/*T:A<object?>!*/.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x3.A2, y3.A1)/*T:A<object?>!*/.F").WithLocation(14, 9),
                // (18,9): warning CS8602: Possible dereference of a null reference.
                //         F(x4.A2, y4.A2)/*T:A<object?>!*/.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x4.A2, y4.A2)/*T:A<object?>!*/.F").WithLocation(18, 9),
                // (30,18): warning CS8620: Nullability of reference types in argument of type 'A<object>' doesn't match target type 'A<object?>' for parameter 'y' in 'A<object?> C.F<A<object?>>(A<object?> x, A<object?> y)'.
                //         F(x7.A2, y7.A3)/*T:A<object?>!*/.F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y7.A3").WithArguments("A<object>", "A<object?>", "y", "A<object?> C.F<A<object?>>(A<object?> x, A<object?> y)").WithLocation(30, 18),
                // (30,9): warning CS8602: Possible dereference of a null reference.
                //         F(x7.A2, y7.A3)/*T:A<object?>!*/.F.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(x7.A2, y7.A3)/*T:A<object?>!*/.F").WithLocation(30, 9),
                // (34,18): warning CS8620: Nullability of reference types in argument of type 'A<object?>' doesn't match target type 'A<object>' for parameter 'y' in 'A<object> C.F<A<object>>(A<object> x, A<object> y)'.
                //         F(x8.A3, y8.A2)/*T:A<object!>!*/.F.ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y8.A2").WithArguments("A<object?>", "A<object>", "y", "A<object> C.F<A<object>>(A<object> x, A<object> y)").WithLocation(34, 18));
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
    static (T, T) F<T>((T, T?) t) where T : class => (t.Item1, t.Item1);
    static void G(string x, string? y)
    {
        F((x, x)).Item2.ToString();
        F((x, y)).Item2.ToString();
        F((y, x)).Item2.ToString();
        F((y, y)).Item2.ToString();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
    static T F<T>((T, T?) t) where T : class => t.Item1;
    static void G((string, string) x, (string, string?) y, (string?, string) z, (string?, string?) w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(8, 9),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(9, 9));
        }

        [Fact]
        public void TupleTypeInference_04_Ref()
        {
            var source =
@"class C
{
    static T F<T>(ref (T, T?) t) where T : class => throw new System.Exception();
    static void G(string x, string? y)
    {
        (string, string) t1 = (x, x);
        F(ref t1).ToString();
        (string, string?) t2 = (x, y);
        F(ref t2).ToString();
        (string?, string) t3 = (y, x);
        F(ref t3).ToString();
        (string?, string?) t4 = (y, y);
        F(ref t4).ToString();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (7,15): warning CS8620: Nullability of reference types in argument of type '(string, string)' doesn't match target type '(string, string?)' for parameter 't' in 'string C.F<string>(ref (string, string?) t)'.
                //         F(ref t1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "t1").WithArguments("(string, string)", "(string, string?)", "t", "string C.F<string>(ref (string, string?) t)").WithLocation(7, 15),
                // (11,15): warning CS8620: Nullability of reference types in argument of type '(string?, string)' doesn't match target type '(string?, string?)' for parameter 't' in 'string? C.F<string?>(ref (string?, string?) t)'.
                //         F(ref t3).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "t3").WithArguments("(string?, string)", "(string?, string?)", "t", "string? C.F<string?>(ref (string?, string?) t)").WithLocation(11, 15),
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         F(ref t3).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(ref t3)").WithLocation(11, 9),
                // (13,9): warning CS8602: Possible dereference of a null reference.
                //         F(ref t4).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(ref t4)").WithLocation(13, 9));
        }

        [Fact]
        public void TupleTypeInference_04_Out()
        {
            var source =
@"class C
{
    static T F<T>(out (T, T?) t) where T : class => throw new System.Exception();
    static void G()
    {
        F(out (string, string) t1).ToString();
        F(out (string, string?) t2).ToString();
        F(out (string?, string) t3).ToString();
        F(out (string?, string?) t4).ToString();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,15): warning CS8620: Nullability of reference types in argument of type '(string, string)' doesn't match target type '(string, string?)' for parameter 't' in 'string C.F<string>(out (string, string?) t)'.
                //         F(out (string, string) t1).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(string, string) t1").WithArguments("(string, string)", "(string, string?)", "t", "string C.F<string>(out (string, string?) t)").WithLocation(6, 15),
                // (8,15): warning CS8620: Nullability of reference types in argument of type '(string?, string)' doesn't match target type '(string?, string?)' for parameter 't' in 'string? C.F<string?>(out (string?, string?) t)'.
                //         F(out (string?, string) t3).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(string?, string) t3").WithArguments("(string?, string)", "(string?, string?)", "t", "string? C.F<string?>(out (string?, string?) t)").WithLocation(8, 15),
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
    static T F<T>(I<(T, T?)> t) where T : class => throw new System.Exception();
    static void G(I<(string, string)> x, I<(string, string?)> y, I<(string?, string)> z, I<(string?, string?)> w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
    static T F<T>(IIn<(T, T?)> t) where T : class => throw new System.Exception();
    static void G(IIn<(string, string)> x, IIn<(string, string?)> y, IIn<(string?, string)> z, IIn<(string?, string?)> w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
    static T F<T>(IOut<(T, T?)> t) where T : class => throw new System.Exception();
    static void G(IOut<(string, string)> x, IOut<(string, string?)> y, IOut<(string?, string)> z, IOut<(string?, string?)> w)
    {
        F(x).ToString();
        F(y).ToString();
        F(z).ToString();
        F(w).ToString();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (9,11): warning CS8620: Nullability of reference types in argument of type 'I<(string, string)>' doesn't match target type 'I<(string, string?)>' for parameter 't' in 'string C.F<string>(I<(string, string?)> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("I<(string, string)>", "I<(string, string?)>", "t", "string C.F<string>(I<(string, string?)> t)").WithLocation(9, 11),
                // (11,11): warning CS8620: Nullability of reference types in argument of type 'I<(string?, string)>' doesn't match target type 'I<(string?, string?)>' for parameter 't' in 'string? C.F<string?>(I<(string?, string?)> t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("I<(string?, string)>", "I<(string?, string?)>", "t", "string? C.F<string?>(I<(string?, string?)> t)").WithLocation(11, 11),
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(11, 9),
                // (12,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(12, 9),
                // (17,11): warning CS8620: Nullability of reference types in argument of type 'IIn<(string, string)>' doesn't match target type 'IIn<(string, string?)>' for parameter 't' in 'string C.F<string>(IIn<(string, string?)> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IIn<(string, string)>", "IIn<(string, string?)>", "t", "string C.F<string>(IIn<(string, string?)> t)").WithLocation(17, 11),
                // (19,11): warning CS8620: Nullability of reference types in argument of type 'IIn<(string?, string)>' doesn't match target type 'IIn<(string?, string?)>' for parameter 't' in 'string? C.F<string?>(IIn<(string?, string?)> t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IIn<(string?, string)>", "IIn<(string?, string?)>", "t", "string? C.F<string?>(IIn<(string?, string?)> t)").WithLocation(19, 11),
                // (19,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(19, 9),
                // (20,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(20, 9),
                // (25,11): warning CS8620: Nullability of reference types in argument of type 'IOut<(string, string)>' doesn't match target type 'IOut<(string, string?)>' for parameter 't' in 'string C.F<string>(IOut<(string, string?)> t)'.
                //         F(x).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "x").WithArguments("IOut<(string, string)>", "IOut<(string, string?)>", "t", "string C.F<string>(IOut<(string, string?)> t)").WithLocation(25, 11),
                // (27,11): warning CS8620: Nullability of reference types in argument of type 'IOut<(string?, string)>' doesn't match target type 'IOut<(string?, string?)>' for parameter 't' in 'string? C.F<string?>(IOut<(string?, string?)> t)'.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "z").WithArguments("IOut<(string?, string)>", "IOut<(string?, string?)>", "t", "string? C.F<string?>(IOut<(string?, string?)> t)").WithLocation(27, 11),
                // (27,9): warning CS8602: Possible dereference of a null reference.
                //         F(z).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(z)").WithLocation(27, 9),
                // (28,9): warning CS8602: Possible dereference of a null reference.
                //         F(w).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(w)").WithLocation(28, 9));
        }

        [Fact]
        public void TupleTypeInference_06()
        {
            var source =
@"class C
{
    static void F(object? x, object? y)
    {
        if (y != null)
        {
            ((object? x, object? y), object? z) t = ((x, y), y);
            t.Item1.Item1.ToString();
            t.Item1.Item2.ToString();
            t.Item2.ToString();
            t.Item1.x.ToString();
            t.Item1.y.ToString();
            t.z.ToString();
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should not report warning for
            // `t.Item1.Item2`, `t.Item2`, `t.Item1.y`, or `t.z`.
            comp.VerifyDiagnostics(
                // (8,13): warning CS8602: Possible dereference of a null reference.
                //             t.Item1.Item1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item1.Item1").WithLocation(8, 13),
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             t.Item1.Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item1.Item2").WithLocation(9, 13),
                // (10,13): warning CS8602: Possible dereference of a null reference.
                //             t.Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item2").WithLocation(10, 13),
                // (11,13): warning CS8602: Possible dereference of a null reference.
                //             t.Item1.x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item1.x").WithLocation(11, 13),
                // (12,13): warning CS8602: Possible dereference of a null reference.
                //             t.Item1.y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Item1.y").WithLocation(12, 13),
                // (13,13): warning CS8602: Possible dereference of a null reference.
                //             t.z.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.z").WithLocation(13, 13));
        }

        [Fact]
        public void TupleTypeInference_07()
        {
            var source =
@"class C
{
    static void F(object? x, object? y)
    {
        if (y != null)
        {
            (object? _1, object? _2, object? _3, object? _4, object? _5, object? _6, object? _7, object? _8, object? _9) t = (null, null, null, null, null, null, null, x, y);
            t._7.ToString();
            t._8.ToString();
            t._9.ToString();
            t.Rest.Item1.ToString();
            t.Rest.Item2.ToString();
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should not report warning for `t._9` or `t.Rest.Item2`.
            comp.VerifyDiagnostics(
                // (8,13): warning CS8602: Possible dereference of a null reference.
                //             t._7.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t._7").WithLocation(8, 13),
                // (9,13): warning CS8602: Possible dereference of a null reference.
                //             t._8.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t._8").WithLocation(9, 13),
                // (10,13): warning CS8602: Possible dereference of a null reference.
                //             t._9.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t._9").WithLocation(10, 13),
                // (11,13): warning CS8602: Possible dereference of a null reference.
                //             t.Rest.Item1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Rest.Item1").WithLocation(11, 13),
                // (12,13): warning CS8602: Possible dereference of a null reference.
                //             t.Rest.Item2.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.Rest.Item2").WithLocation(12, 13));
        }

        [Fact]
        public void Tuple_Constructor()
        {
            var source =
@"class C
{
    C((string x, string? y) t) { }
    static void M(string x, string? y)
    {
        C c;
        c = new C((x, x));
        c = new C((x, y));
        c = new C((y, x));
        c = new C((y, y));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatchInArgument for `(y, x)` and `(y, y)`.
            comp.VerifyDiagnostics();
                //// (9,19): warning CS8620: Nullability of reference types in argument of type '(string? y, string x)' doesn't match target type '(string x, string? y)' for parameter 't' in 'C.C((string x, string? y) t)'.
                ////         c = new C((y, x));
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(y, x)").WithArguments("(string? y, string x)", "(string x, string? y)", "t", "C.C((string x, string? y) t)").WithLocation(9, 19),
                //// (10,19): warning CS8620: Nullability of reference types in argument of type '(string?, string?)' doesn't match target type '(string x, string? y)' for parameter 't' in 'C.C((string x, string? y) t)'.
                ////         c = new C((y, y));
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(y, y)").WithArguments("(string?, string?)", "(string x, string? y)", "t", "C.C((string x, string? y) t)").WithLocation(10, 19));
        }

        [Fact]
        public void Tuple_Indexer()
        {
            var source =
@"class C
{
    object? this[(string x, string? y) t] => null;
    static void M(string x, string? y)
    {
        var c = new C();
        object? o;
        o = c[(x, x)];
        o = c[(x, y)];
        o = c[(y, x)];
        o = c[(y, y)];
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatchInArgument for `(y, x)` and `(y, y)`.
            comp.VerifyDiagnostics();
                //// (10,15): warning CS8620: Nullability of reference types in argument of type '(string? y, string x)' doesn't match target type '(string x, string? y)' for parameter 't' in 'object? C.this[(string x, string? y) t].get'.
                ////         o = c[(y, x)];
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(y, x)").WithArguments("(string? y, string x)", "(string x, string? y)", "t", "object? C.this[(string x, string? y) t].get").WithLocation(10, 15),
                //// (11,15): warning CS8620: Nullability of reference types in argument of type '(string?, string?)' doesn't match target type '(string x, string? y)' for parameter 't' in 'object? C.this[(string x, string? y) t].get'.
                ////         o = c[(y, y)];
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(y, y)").WithArguments("(string?, string?)", "(string x, string? y)", "t", "object? C.this[(string x, string? y) t].get").WithLocation(11, 15));
        }

        [Fact]
        public void Tuple_CollectionInitializer()
        {
            var source =
@"using System.Collections.Generic;
class C
{
    static void M(string x, string? y)
    {
        var c = new List<(string, string?)>
        {
            (x, x),
            (x, y),
            (y, x),
            (y, y),
        };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatchInArgument for `(y, x)`.
            comp.VerifyDiagnostics(
                //// (10,13): warning CS8620: Nullability of reference types in argument of type '(string? y, string x)' doesn't match target type '(string, string?)' for parameter 'item' in 'void List<(string, string?)>.Add((string, string?) item)'.
                ////             (y, x),
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(y, x)").WithArguments("(string? y, string x)", "(string, string?)", "item", "void List<(string, string?)>.Add((string, string?) item)").WithLocation(10, 13),
                // (11,13): warning CS8620: Nullability of reference types in argument of type '(string?, string?)' doesn't match target type '(string, string?)' for parameter 'item' in 'void List<(string, string?)>.Add((string, string?) item)'.
                //             (y, y),
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "(y, y)").WithArguments("(string?, string?)", "(string, string?)", "item", "void List<(string, string?)>.Add((string, string?) item)").WithLocation(11, 13));
        }

        [Fact]
        public void ImplicitConversion_CollectionInitializer()
        {
            var source =
@"using System.Collections.Generic;
class A<T> { }
class B<T> : A<T> { }
class C
{
    static void M(B<object>? x, B<object?> y)
    {
        var c = new List<A<object>>
        {
            x,
            y,
        };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatchInArgument for `y`.
            comp.VerifyDiagnostics(
                // (10,13): warning CS8604: Possible null reference argument for parameter 'item' in 'void List<A<object>>.Add(A<object> item)'.
                //             x,
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("item", "void List<A<object>>.Add(A<object> item)").WithLocation(10, 13));
                //// (11,13): warning CS8620: Nullability of reference types in argument of type 'B<object?>' doesn't match target type 'A<object>' for parameter 'item' in 'void List<A<object>>.Add(A<object> item)'.
                ////             y,
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInArgument, "y").WithArguments("B<object?>", "A<object>", "item", "void List<A<object>>.Add(A<object> item)").WithLocation(11, 13));
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilationWithMscorlib45(
                source,
                parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Deconstruction should infer `string?` for `var x`.
            comp.VerifyDiagnostics(
                // (8,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(8, 13),
                // (9,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         y = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(9, 13));
        }

        [Fact]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Deconstruction should infer `string?` for `var x`.
            comp.VerifyDiagnostics(
                // (9,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(9, 13),
                // (10,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         y = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(10, 13));
        }

        [Fact]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Deconstruction should infer `string?` for `var x`.
            comp.VerifyDiagnostics(
                // (13,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         x = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(13, 13),
                // (14,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         y = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(14, 13));
        }

        [Fact]
        public void DeconstructionTypeInference_04()
        {
            var source =
@"class C
{
    static (string?, string) F() => (null, string.Empty);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (10,9): warning CS8602: Possible dereference of a null reference.
                //         t.y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.y").WithLocation(10, 9),
                // (11,15): warning CS8625: Cannot convert null literal to non-nullable reference or unconstrained type parameter.
                //         t.x = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 15));
        }

        [Fact]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Deconstruction should infer `string?` for `var y`.
            comp.VerifyDiagnostics();
                //// (11,13): warning CS8602: Possible dereference of a null reference.
                ////             y.ToString();
                //Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y").WithLocation(11, 13));
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (13,22): error CS1061: '(object, int)' does not contain a definition for 'x' and no extension method 'x' accepting a first argument of type '(object, int)' could be found (are you missing a using directive or an assembly reference?)
                //         c.F((o, -1)).x.ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "x").WithArguments("(object, int)", "x").WithLocation(13, 22));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (13,22): error CS1061: '(object?, int)' does not contain a definition for 'x' and no extension method 'x' accepting a first argument of type '(object?, int)' could be found (are you missing a using directive or an assembly reference?)
                //         c.F((o, -1)).x.ToString();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "x").WithArguments("(object?, int)", "x").WithLocation(13, 22));
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

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
        }

        // Assert failure in ConversionsBase.IsValidExtensionMethodThisArgConversion.
        [WorkItem(22317, "https://github.com/dotnet/roslyn/issues/22317")]
        [Fact(Skip = "22317")]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
        y = null;
        F(y).ToString();
    }
}";
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (11,9): warning CS8602: Possible dereference of a null reference.
                //         F(y).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(y)").WithLocation(11, 9));
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            // ErrorCode.WRN_NullReferenceReceiver is reported for F(default).ToString() because F(v)
            // has type T from initial binding (see https://github.com/dotnet/roslyn/issues/25778).
            var comp = CreateCompilation(
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
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(default(string))").WithLocation(8, 9),
                // (9,9): warning CS8602: Possible dereference of a null reference.
                //         F(default).ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "F(default)").WithLocation(9, 9));
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
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
        public void TypeInference_Tuple_03()
        {
            var source =
@"class C
{
    static void F(object? x, object? y)
    {
        if (x == null) return;
        var t = (x, y);
        t.x.ToString();
        t.y.ToString();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (8,9): warning CS8602: Possible dereference of a null reference.
                //         t.y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "t.y").WithLocation(8, 9));
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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
            var comp = CreateCompilation(
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

        [Fact]
        public void TypeInference_DelegateConversion_01()
        {
            var source =
@"delegate T D<T>();
class C
{
    static void F(object? o)
    {
        D<object?> d = () => o;
        D<object> e = () => o;
        if (o == null) return;
        d = () => o;
        e = () => o;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Report WRN_NullabilityMismatchInReturnTypeOfTargetDelegate.
            comp.VerifyDiagnostics(
                //// (7,29): warning CS8621: Nullability of reference types in return type of 'lambda' doesn't match the target delegate 'D<object>'.
                ////         D<object> e = () => o;
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "() => o").WithArguments("lambda", "D<object>").WithLocation(7, 29),
                // (7,29): warning CS8603: Possible null reference return.
                //         D<object> e = () => o;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "o").WithLocation(7, 29));
        }

        [Fact]
        public void TypeInference_DelegateConversion_02()
        {
            var source =
@"delegate T D<T>();
class A<T>
{
    internal T M() => throw new System.NotImplementedException();
}
class B
{
    static A<T> F<T>(T t) => throw null;
    static void G(object? o)
    {
        var x = F(o);
        D<object?> d = x.M;
        D<object> e = x.M;
        if (o == null) return;
        var y = F(o);
        d = y.M;
        e = y.M;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            // PROTOTYPE(NullableReferenceTypes): Should report WRN_NullabilityMismatchInReturnTypeOfTargetDelegate for `e = x.M` rather than  `d = x.M`.
            comp.VerifyDiagnostics(
                // (12,24): warning CS8621: Nullability of reference types in return type of 'object A<object>.M()' doesn't match the target delegate 'D<object?>'.
                //         D<object?> d = x.M;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "x.M").WithArguments("object A<object>.M()", "D<object?>").WithLocation(12, 24),
                //// (13,23): warning CS8621: Nullability of reference types in return type of 'object? A<object?>.M()' doesn't match the target delegate 'D<object>'.
                ////         D<object> e = x.M;
                //Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "x.M").WithArguments("object? A<object?>.M()", "D<object>").WithLocation(13, 23),
                // (16,13): warning CS8621: Nullability of reference types in return type of 'object A<object>.M()' doesn't match the target delegate 'D<object?>'.
                //         d = y.M;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "y.M").WithArguments("object A<object>.M()", "D<object?>").WithLocation(16, 13));
        }
    }
}
