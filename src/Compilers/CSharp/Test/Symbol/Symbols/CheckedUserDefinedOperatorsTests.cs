// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class CheckedUserDefinedOperators : CSharpTestBase
    {
        [Theory]
        [InlineData("-", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_CheckedIncrement")]
        [InlineData("--", "op_CheckedDecrement")]
        public void UnaryOperators_Supported_01(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular10);
            compilation1.VerifyDiagnostics(
                // (4,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked -(C x) => x;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(4, 30)
                );
            validator(compilation1.SourceModule);

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbol.MethodKind);
                Assert.Equal(name, opSymbol.Name);
                Assert.Equal("C C." + name + "(C x)", opSymbol.ToTestDisplayString());
                Assert.Equal("C.operator checked " + op + "(C)", opSymbol.ToDisplayString());
                Assert.True(opSymbol.IsStatic);
                Assert.True(opSymbol.HasSpecialName);
                Assert.False(opSymbol.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, opSymbol.DeclaredAccessibility);
            }
        }

        [Theory]
        [InlineData("-", "op_UnaryNegation", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_Increment", "op_CheckedIncrement")]
        [InlineData("--", "op_Decrement", "op_CheckedDecrement")]
        public void UnaryOperators_Supported_02(string op, string name, string checkedName)
        {
            var source1 =
@"
class C 
{
    public static C operator " + op + @"(C x) => x;
    public static C operator checked " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

                Assert.Equal("C C." + name + "(C x)", opSymbols[0].ToTestDisplayString());
                Assert.Equal("C C." + checkedName + "(C x)", opSymbols[1].ToTestDisplayString());
            }
        }

        [Theory]
        [InlineData("-", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_CheckedIncrement")]
        [InlineData("--", "op_CheckedDecrement")]
        public void UnaryOperators_Supported_03(string op, string name)
        {
            var source1 =
@"
struct C 
{
    public static C operator checked" + op + @"(C x) => x;
    public static C? operator checked " + op + @"(C? x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

                Assert.Equal("C C." + name + "(C x)", opSymbols[0].ToTestDisplayString());
                Assert.Equal("C? C." + name + "(C? x)", opSymbols[1].ToTestDisplayString());
            }
        }

        [Theory]
        [InlineData("-", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_CheckedIncrement")]
        [InlineData("--", "op_CheckedDecrement")]
        public void UnaryOperators_Supported_04(string op, string name)
        {
            var source1 =
@"
struct C 
{
    public static C operator checked" + op + @"(C x) => x;
    public static C? operator checked " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            if (op == "-")
            {
                compilation1.VerifyDiagnostics(
                    // (5,39): error CS0111: Type 'C' already defines a member called 'op_CheckedDecrement' with the same parameter types
                    //     public static C? operator checked --(C x) => x;
                    Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(5, 39)
                    );
            }
            else
            {
                compilation1.VerifyDiagnostics(
                    // (5,39): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                    //     public static C? operator checked --(C x) => x;
                    Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(5, 39),
                    // (5,39): error CS0111: Type 'C' already defines a member called 'op_CheckedDecrement' with the same parameter types
                    //     public static C? operator checked --(C x) => x;
                    Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(5, 39)
                    );
            }

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(2, opSymbols.Length);

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

            Assert.Equal("C C." + name + "(C x)", opSymbols[0].ToTestDisplayString());
            Assert.Equal("C? C." + name + "(C x)", opSymbols[1].ToTestDisplayString());
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_05(string op)
        {
            var source1 =
@"
class C 
{
    public static int operator checked" + op + @"(C x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,39): error CS0448: The return type for ++ or -- operator must match the parameter type or be derived from the parameter type
                //     public static int operator checked--(C x) => default;
                Diagnostic(ErrorCode.ERR_BadIncDecRetType, op).WithLocation(4, 39)
                );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_06(string op)
        {
            var source1 =
@"
class C 
{
    public static C operator checked" + op + @"(int x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,37): error CS0559: The parameter type for ++ or -- operator must be the containing type
                //     public static C operator checked++(int x) => default;
                Diagnostic(ErrorCode.ERR_BadIncDecSignature, op).WithLocation(4, 37)
                );
        }

        [Fact]
        public void UnaryOperators_Supported_07()
        {
            var source1 =
@"
class C 
{
    public static C operator checked -(int x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS0562: The parameter of a unary operator must be the containing type
                //     public static C operator checked -(int x) => default;
                Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, "-").WithLocation(4, 38)
                );
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_08(string op)
        {
            var source1 =
@"
class C 
{
    C operator checked" + op + @"(C x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,23): error CS0558: User-defined operator 'C.operator checked --(C)' must be declared static and public
                //     C operator checked--(C x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, op).WithArguments("C.operator checked " + op + "(C)").WithLocation(4, 23)
                );
        }

        [Theory]
        [InlineData("-", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_CheckedIncrement")]
        [InlineData("--", "op_CheckedDecrement")]
        public void UnaryOperators_Supported_09(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x) => x;

    C Test(C x)
    {
        return C." + name + @"(x);
    }
}

class C1 
{
    public static C1 operator checked " + op + @"(C1 x) => x;

    static C1 " + name + @"(C1 x) => x; 
}

class C2 
{
    static C2 " + name + @"(C2 x) => x; 

    public static C2 operator checked " + op + @"(C2 x) => x;
}

class C3 
{
    public static C3 operator checked " + op + @"(C3 x) => x;

    int " + name + @" { get; } 
}

class C4 
{
    C4 " + name + @" { get; } 

    public static C4 operator checked " + op + @"(C4 x) => x;
}

class C5 
{
    public static C5 operator checked " + op + @"(C5 x) => x;

    public static C5 operator checked " + op + @"(C5 y) => y;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (8,18): error CS0571: 'C.operator checked -(C)': cannot explicitly call operator or accessor
                //         return C.op_CheckedUnaryNegation(x);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, name).WithArguments("C.operator checked " + op + "(C)").WithLocation(8, 18),
                // (16,15): error CS0111: Type 'C1' already defines a member called 'op_CheckedUnaryNegation' with the same parameter types
                //     static C1 op_CheckedUnaryNegation(C1 x) => x; 
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, name).WithArguments(name, "C1").WithLocation(16, 15),
                // (23,39): error CS0111: Type 'C2' already defines a member called 'op_CheckedUnaryNegation' with the same parameter types
                //     public static C2 operator checked -(C2 x) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C2").WithLocation(23, 39),
                // (30,9): error CS0102: The type 'C3' already contains a definition for 'op_CheckedUnaryNegation'
                //     int op_CheckedUnaryNegation { get; } 
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, name).WithArguments("C3", name).WithLocation(30, 9),
                // (37,39): error CS0102: The type 'C4' already contains a definition for 'op_CheckedUnaryNegation'
                //     public static C4 operator checked -(C4 x) => x;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, op).WithArguments("C4", name).WithLocation(37, 39),
                // (44,39): error CS0111: Type 'C5' already defines a member called 'op_CheckedSubtraction' with the same parameter types
                //     public static C5 operator checked -(C5 x, C5 y) => y;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C5").WithLocation(44, 39)
                );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_10(string op)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x, C y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1020: Overloadable binary operator expected
                //     public static C operator checked --(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, op).WithLocation(4, 38)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator checked " + op + "(C, C)", opSymbol.ToDisplayString());
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_11(string op)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"() => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1535: Overloaded unary operator '--' takes one parameter
                //     public static C operator checked --() => default;
                Diagnostic(ErrorCode.ERR_BadUnOpArgs, op).WithArguments(op).WithLocation(4, 38)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator checked " + op + "()", opSymbol.ToDisplayString());
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_12(string op)
        {
            var source1 =
@"
static class C 
{
    public static C operator checked " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (4,19): error CS0722: 'C': static types cannot be used as return types
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(4, 19),
                // (4,38): error CS0715: 'C.operator checked ++(C)': static classes cannot contain user-defined operators
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C.operator checked " + op + "(C)").WithLocation(4, 38),
                // (4,38): error CS0721: 'C': static types cannot be used as parameters
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, op).WithArguments("C").WithLocation(4, 38)
                );
        }

        [Fact]
        public void UnaryOperators_Missing_01()
        {
            var source1 =
@"
class C 
{
    public static C operator checked (C x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1019: Overloadable unary operator expected
                //     public static C operator checked (C x) => default;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "(").WithLocation(4, 38),
                // (4,39): error CS1003: Syntax error, '(' expected
                //     public static C operator checked (C x) => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(", "").WithLocation(4, 39)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator +(C)", opSymbol.ToDisplayString());
            Assert.Equal("op_UnaryPlus", opSymbol.Name);
        }

        [Fact]
        public void UnaryOperators_Missing_02()
        {
            var source1 =
@"
class C 
{
    public static C operator (C x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,30): error CS1019: Overloadable unary operator expected
                //     public static C operator (C x) => default;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "(").WithLocation(4, 30),
                // (4,31): error CS1003: Syntax error, '(' expected
                //     public static C operator (C x) => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(", "").WithLocation(4, 31)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator +(C)", opSymbol.ToDisplayString());
            Assert.Equal("op_UnaryPlus", opSymbol.Name);
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperator_Supported_CRef_NoParameters_01(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked --'
                // /// See <see cref="operator checked --"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked --"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked --(C c)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperator_Supported_CRef_NoParameters_02(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator ++' that could not be resolved
                // /// See <see cref="operator ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op).WithArguments("operator " + op).WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperator_Supported_CRef_NoParameters_03(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked -' that could not be resolved
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20)
                };
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        /// <summary>
        /// The behavior is consistent with <see cref="CrefTests.UnaryOperator_NoParameters_02"/>
        /// </summary>
        [Fact]
        public void UnaryOperator_Supported_CRef_NoParameters_04()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked -""/>.
/// </summary>
class C
{
    public static C operator checked -(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked -' that could not be resolved
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked -").WithArguments("operator checked -").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked -'
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked -").WithArguments("operator checked -").WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked -(C c)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperator_Supported_CRef_OneParameter_01(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C)""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked -(C)'
                // /// See <see cref="operator checked -(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked -(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked -(C c)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperator_Supported_CRef_OneParameter_02(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"(C)""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator --(C)' that could not be resolved
                // /// See <see cref="operator --(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op + "(C)").WithArguments("operator " + op + "(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperator_Supported_CRef_OneParameter_03(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C)""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked -(C)' that could not be resolved
                // /// See <see cref="operator checked -(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void UnaryOperator_MissingToken_CRef_OneParameter_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked (C)""/>.
/// </summary>
class C
{
    public static C operator +(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked (C)'
                // /// See <see cref="operator checked (C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked (C)").WithArguments("operator checked (C)").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked (C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "(").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void UnaryOperator_MissingToken_CRef_OneParameter_02()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator (C)""/>.
/// </summary>
class C
{
    public static C operator +(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator (C)'
                // /// See <see cref="operator (C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator (C)").WithArguments("operator (C)").WithLocation(3, 20),
                // (3,29): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator (C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "(").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 29)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("+", "op_UnaryPlus")]
        [InlineData("!", "op_LogicalNot")]
        [InlineData("~", "op_OnesComplement")]
        public void UnaryOperators_Unsupported_01(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x) => x;
}
";
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.RegularNext })
            {
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: options);

                compilation1.VerifyDiagnostics(
                    // (4,30): error CS9150: User-defined operator '~' cannot be declared checked
                    //     public static C operator checked ~(C x) => x;
                    Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(4, 30)
                    );

                var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbol.MethodKind);
                Assert.Equal(name, opSymbol.Name);
                Assert.Equal("C C." + name + "(C x)", opSymbol.ToTestDisplayString());
                Assert.Equal("C.operator " + op + "(C)", opSymbol.ToDisplayString());
            }
        }

        [Fact]
        public void UnaryOperators_Unsupported_02()
        {
            var source1 =
@"
class C 
{
    public static bool operator checked true(C x) => true;
    public static bool operator checked false(C x) => false;
}
";
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.RegularNext })
            {
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: options);

                compilation1.VerifyDiagnostics(
                    // (4,33): error CS9150: User-defined operator 'true' cannot be declared checked
                    //     public static bool operator checked true(C x) => true;
                    Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("true").WithLocation(4, 33),
                    // (5,33): error CS9150: User-defined operator 'false' cannot be declared checked
                    //     public static bool operator checked false(C x) => false;
                    Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments("false").WithLocation(5, 33)
                    );

                var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                var opSymbol1 = opSymbols[0];
                Assert.Equal(MethodKind.UserDefinedOperator, opSymbol1.MethodKind);
                Assert.Equal("op_True", opSymbol1.Name);
                Assert.Equal("System.Boolean C.op_True(C x)", opSymbol1.ToTestDisplayString());
                Assert.Equal("C.operator true(C)", opSymbol1.ToDisplayString());

                var opSymbol2 = opSymbols[1];
                Assert.Equal(MethodKind.UserDefinedOperator, opSymbol2.MethodKind);
                Assert.Equal("op_False", opSymbol2.Name);
                Assert.Equal("System.Boolean C.op_False(C x)", opSymbol2.ToTestDisplayString());
                Assert.Equal("C.operator false(C)", opSymbol2.ToDisplayString());
            }
        }

        [Theory]
        [InlineData("+", "op_UnaryPlus")]
        [InlineData("!", "op_LogicalNot")]
        [InlineData("~", "op_OnesComplement")]
        public void UnaryOperators_Unsupported_03(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator " + op + @"(C x) => x;
    public static C operator checked " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (5,30): error CS9150: User-defined operator '~' cannot be declared checked
                //     public static C operator checked ~(C x) => x;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(5, 30),
                // (5,38): error CS0111: Type 'C' already defines a member called 'op_OnesComplement' with the same parameter types
                //     public static C operator checked ~(C x) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(5, 38)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(2, opSymbols.Length);

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

            Assert.Equal("C C." + name + "(C x)", opSymbols[0].ToTestDisplayString());
            Assert.Equal("C C." + name + "(C x)", opSymbols[1].ToTestDisplayString());
        }

        [Theory]
        [InlineData("true", "op_True")]
        [InlineData("false", "op_False")]
        public void UnaryOperators_Unsupported_04(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static bool operator true(C x) => true;
    public static bool operator false(C x) => false;
    public static bool operator checked " + op + @"(C x) => false;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (6,33): error CS9150: User-defined operator 'false' cannot be declared checked
                //     public static bool operator checked false(C x) => false;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(6, 33),
                // (6,41): error CS0111: Type 'C' already defines a member called 'op_False' with the same parameter types
                //     public static bool operator checked false(C x) => false;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(6, 41)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(3, opSymbols.Length);

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
            Assert.Equal("System.Boolean C.op_True(C x)", opSymbols[0].ToTestDisplayString());

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);
            Assert.Equal("System.Boolean C.op_False(C x)", opSymbols[1].ToTestDisplayString());

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[2].MethodKind);
            Assert.Equal("System.Boolean C." + name + "(C x)", opSymbols[2].ToTestDisplayString());
        }

        [Theory]
        [InlineData("+")]
        [InlineData("!")]
        [InlineData("~")]
        public void UnaryOperator_Unsupported_CRef_NoParameters_01(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked !' that could not be resolved
                // /// See <see cref="operator checked !"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked +'
                // /// See <see cref="operator checked +"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked +"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void UnaryOperator_Unsupported_CRef_NoParameters_02(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C
{
    public static bool operator true(C x) => true;
    public static bool operator false(C x) => false;
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked true' that could not be resolved
                // /// See <see cref="operator checked true"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked false'
                // /// See <see cref="operator checked false"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked false"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("!")]
        [InlineData("~")]
        public void UnaryOperator_Unsupported_CRef_OneParameter_01(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C)""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked ~(C)' that could not be resolved
                // /// See <see cref="operator checked ~(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked +(C)'
                // /// See <see cref="operator checked +(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked +(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("false")]
        public void UnaryOperator_Unsupported_CRef_OneParameter_02(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C)""/>.
/// </summary>
class C
{
    public static bool operator true(C x) => true;
    public static bool operator false(C x) => false;
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked true(C)' that could not be resolved
                // /// See <see cref="operator checked true(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked true(C)'
                // /// See <see cref="operator checked true(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked true(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("+", "op_CheckedAddition")]
        [InlineData("-", "op_CheckedSubtraction")]
        [InlineData("*", "op_CheckedMultiply")]
        [InlineData("/", "op_CheckedDivision")]
        public void BinaryOperators_Supported_01(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x, C y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular10);
            compilation1.VerifyDiagnostics(
                // (4,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(4, 30)
                );
            validator(compilation1.SourceModule);

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbol.MethodKind);
                Assert.Equal(name, opSymbol.Name);
                Assert.Equal("C C." + name + "(C x, C y)", opSymbol.ToTestDisplayString());
                Assert.Equal("C.operator checked " + op + "(C, C)", opSymbol.ToDisplayString());
                Assert.True(opSymbol.IsStatic);
                Assert.True(opSymbol.HasSpecialName);
                Assert.False(opSymbol.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, opSymbol.DeclaredAccessibility);
            }
        }

        [Theory]
        [InlineData("+", "op_Addition", "op_CheckedAddition")]
        [InlineData("-", "op_Subtraction", "op_CheckedSubtraction")]
        [InlineData("*", "op_Multiply", "op_CheckedMultiply")]
        [InlineData("/", "op_Division", "op_CheckedDivision")]
        public void BinaryOperators_Supported_02(string op, string name, string checkedName)
        {
            var source1 =
@"
class C 
{
    public static C operator " + op + @"(C x, C y) => x;
    public static C operator checked " + op + @"(C x, C y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

                Assert.Equal("C C." + name + "(C x, C y)", opSymbols[0].ToTestDisplayString());
                Assert.Equal("C C." + checkedName + "(C x, C y)", opSymbols[1].ToTestDisplayString());
            }
        }

        [Theory]
        [InlineData("+", "op_CheckedAddition")]
        [InlineData("-", "op_CheckedSubtraction")]
        [InlineData("*", "op_CheckedMultiply")]
        [InlineData("/", "op_CheckedDivision")]
        public void BinaryOperators_Supported_03(string op, string name)
        {
            var source1 =
@"
struct C 
{
    public static C operator checked" + op + @"(C x, C y) => x;
    public static C? operator checked " + op + @"(C? x, C? y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
                Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

                Assert.Equal("C C." + name + "(C x, C y)", opSymbols[0].ToTestDisplayString());
                Assert.Equal("C? C." + name + "(C? x, C? y)", opSymbols[1].ToTestDisplayString());
            }
        }

        [Theory]
        [InlineData("+", "op_CheckedAddition")]
        [InlineData("-", "op_CheckedSubtraction")]
        [InlineData("*", "op_CheckedMultiply")]
        [InlineData("/", "op_CheckedDivision")]
        public void BinaryOperators_Supported_04(string op, string name)
        {
            var source1 =
@"
struct C 
{
    public static C operator checked" + op + @"(C x, C y) => x;
    public static C? operator checked " + op + @"(C x, C y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (5,39): error CS0111: Type 'C' already defines a member called 'op_CheckedSubtraction' with the same parameter types
                //     public static C? operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(5, 39)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(2, opSymbols.Length);

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

            Assert.Equal("C C." + name + "(C x, C y)", opSymbols[0].ToTestDisplayString());
            Assert.Equal("C? C." + name + "(C x, C y)", opSymbols[1].ToTestDisplayString());
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperators_Supported_06(string op)
        {
            var source1 =
@"
class C 
{
    public static C operator checked" + op + @"(int x, int y) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,37): error CS0563: One of the parameters of a binary operator must be the containing type
                //     public static C operator checked/(int x, int y) => default;
                Diagnostic(ErrorCode.ERR_BadBinaryOperatorSignature, op).WithLocation(4, 37)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperators_Supported_08(string op)
        {
            var source1 =
@"
class C 
{
    C operator checked" + op + @"(C x, C y) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,23): error CS0558: User-defined operator 'C.operator checked -(C, C)' must be declared static and public
                //     C operator checked-(C x, C y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, op).WithArguments("C.operator checked " + op + "(C, C)").WithLocation(4, 23)
                );
        }

        [Theory]
        [InlineData("+", "op_CheckedAddition")]
        [InlineData("-", "op_CheckedSubtraction")]
        [InlineData("*", "op_CheckedMultiply")]
        [InlineData("/", "op_CheckedDivision")]
        public void BinaryOperators_Supported_09(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x, C y) => x;

    C Test(C x)
    {
        return C." + name + @"(x, x);
    }
}

class C1 
{
    public static C1 operator checked " + op + @"(C1 x, C1 y) => x;

    static C1 " + name + @"(C1 x, C1 y) => x; 
}

class C2 
{
    static C2 " + name + @"(C2 x, C2 y) => x; 

    public static C2 operator checked " + op + @"(C2 x, C2 y) => x;
}

class C3 
{
    public static C3 operator checked " + op + @"(C3 x, C3 y) => x;

    int " + name + @" { get; } 
}

class C4 
{
    C4 " + name + @" { get; } 

    public static C4 operator checked " + op + @"(C4 x, C4 y) => x;
}

class C5 
{
    public static C5 operator checked " + op + @"(C5 x, C5 y) => x;

    public static C5 operator checked " + op + @"(C5 x, C5 y) => y;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (8,18): error CS0571: 'C.operator checked -(C, C)': cannot explicitly call operator or accessor
                //         return C.op_CheckedSubtraction(x, x);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, name).WithArguments("C.operator checked " + op + "(C, C)").WithLocation(8, 18),
                // (16,15): error CS0111: Type 'C1' already defines a member called 'op_CheckedSubtraction' with the same parameter types
                //     static C1 op_CheckedSubtraction(C1 x, C1 y) => x; 
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, name).WithArguments(name, "C1").WithLocation(16, 15),
                // (23,39): error CS0111: Type 'C2' already defines a member called 'op_CheckedSubtraction' with the same parameter types
                //     public static C2 operator checked -(C2 x, C2 y) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C2").WithLocation(23, 39),
                // (30,9): error CS0102: The type 'C3' already contains a definition for 'op_CheckedSubtraction'
                //     int op_CheckedSubtraction { get; } 
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, name).WithArguments("C3", name).WithLocation(30, 9),
                // (37,39): error CS0102: The type 'C4' already contains a definition for 'op_CheckedSubtraction'
                //     public static C4 operator checked -(C4 x, C4 y) => x;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, op).WithArguments("C4", name).WithLocation(37, 39),
                // (44,39): error CS0111: Type 'C5' already defines a member called 'op_CheckedSubtraction' with the same parameter types
                //     public static C5 operator checked -(C5 x, C5 y) => y;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C5").WithLocation(44, 39)
                );
        }

        [Theory]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperators_Supported_10(string op)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1019: Overloadable unary operator expected
                //     public static C operator checked /(C x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, op).WithLocation(4, 38)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator checked " + op + "(C)", opSymbol.ToDisplayString());
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperators_Supported_11(string op)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"() => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1534: Overloaded binary operator '*' takes two parameters
                //     public static C operator checked *() => default;
                Diagnostic(ErrorCode.ERR_BadBinOpArgs, op).WithArguments(op).WithLocation(4, 38)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator checked " + op + "()", opSymbol.ToDisplayString());
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperators_Supported_12(string op)
        {
            var source1 =
@"
static class C 
{
    public static C operator checked " + op + @"(C x, C y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (4,19): error CS0722: 'C': static types cannot be used as return types
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(4, 19),
                // (4,38): error CS0715: 'C.operator checked -(C, C)': static classes cannot contain user-defined operators
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C.operator checked " + op + "(C, C)").WithLocation(4, 38),
                // (4,38): error CS0721: 'C': static types cannot be used as parameters
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, op).WithArguments("C").WithLocation(4, 38),
                // (4,38): error CS0721: 'C': static types cannot be used as parameters
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, op).WithArguments("C").WithLocation(4, 38)
                );
        }

        [Fact]
        public void BinaryOperators_Missing_01()
        {
            var source1 =
@"
class C 
{
    public static C operator checked (C x, C y) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1020: Overloadable binary operator expected
                //     public static C operator checked (C x, C y) => default;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "(").WithLocation(4, 38),
                // (4,39): error CS1003: Syntax error, '(' expected
                //     public static C operator checked (C x, C y) => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(", "").WithLocation(4, 39)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator checked +(C, C)", opSymbol.ToDisplayString());
            Assert.Equal("op_CheckedAddition", opSymbol.Name);
        }

        [Fact]
        public void BinaryOperators_Missing_02()
        {
            var source1 =
@"
class C 
{
    public static C operator (C x, C y) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,30): error CS1020: Overloadable binary operator expected
                //     public static C operator (C x, C y) => default;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "(").WithLocation(4, 30),
                // (4,31): error CS1003: Syntax error, '(' expected
                //     public static C operator (C x, C y) => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(", "").WithLocation(4, 31)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator +(C, C)", opSymbol.ToDisplayString());
            Assert.Equal("op_Addition", opSymbol.Name);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperator_Supported_CRef_NoParameters_01(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked -'
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked -(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperator_Supported_CRef_NoParameters_02(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator +' that could not be resolved
                // /// See <see cref="operator +"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op).WithArguments("operator " + op).WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperator_Supported_CRef_NoParameters_03(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked -' that could not be resolved
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20)
                };
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void BinaryOperator_MissingToken_CRef_NoParameters_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked ""/>.
/// </summary>
class C
{
    public static C operator checked +(C c, C y)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked'
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked").WithArguments("operator checked").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37)
                );

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked'
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked").WithArguments("operator checked").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked +(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked'
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked").WithArguments("operator checked").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_MissingToken_CRef_NoParameters_02()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator ""/>.
/// </summary>
class C
{
    public static C operator +(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator'
                // /// See <see cref="operator "/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator").WithArguments("operator").WithLocation(3, 20),
                // (3,29): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator "/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 29)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperator_Supported_CRef_TwoParameters_01(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C, C)""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked -(C)'
                // /// See <see cref="operator checked -(C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C, C)").WithArguments("operator checked " + op + "(C, C)").WithLocation(3, 20),
                // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="operator checked -(C, C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked -(C c, C)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperator_Supported_CRef_TwoParameters_02(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator " + op + @"(C, C)""/>.
/// </summary>
class C
{
    public static C operator checked " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator -(C)' that could not be resolved
                // /// See <see cref="operator -(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator " + op + "(C, C)").WithArguments("operator " + op + "(C, C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinarOperator_Supported_CRef_TwoParameters_03(string op)
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked " + op + @"(C, C)""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked -(C, C)' that could not be resolved
                // /// See <see cref="operator checked -(C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + op + "(C, C)").WithArguments("operator checked " + op + "(C, C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void BinaryOperator_MissingToken_CRef_TwoParameters_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator checked (C, C)""/>.
/// </summary>
class C
{
    public static C operator checked +(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked (C, C)'
                // /// See <see cref="operator checked (C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked (C, C)").WithArguments("operator checked (C, C)").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked (C, C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "(").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked (C, C)'
                // /// See <see cref="operator checked (C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked (C, C)").WithArguments("operator checked (C, C)").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked (C, C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "(").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37),
                // (7,30): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static C operator checked +(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void BinaryOperator_MissingToken_CRef_TwoParameters_02()
        {
            var source = @"
/// <summary>
/// See <see cref=""operator (C, C)""/>.
/// </summary>
class C
{
    public static C operator +(C c, C y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator (C, C)'
                // /// See <see cref="operator (C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator (C, C)").WithArguments("operator (C, C)").WithLocation(3, 20),
                // (3,29): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator (C, C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "(").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 29)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Theory]
        [InlineData("%", "op_Modulus")]
        [InlineData("&", "op_BitwiseAnd")]
        [InlineData("|", "op_BitwiseOr")]
        [InlineData("^", "op_ExclusiveOr")]
        [InlineData("<<", "op_LeftShift")]
        [InlineData(">>", "op_RightShift")]
        [InlineData("==", "op_Equality")]
        [InlineData("!=", "op_Inequality")]
        [InlineData(">", "op_GreaterThan")]
        [InlineData("<", "op_LessThan")]
        [InlineData(">=", "op_GreaterThanOrEqual")]
        [InlineData("<=", "op_LessThanOrEqual")]
        public void BinaryOperators_Unsupported_01(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator checked " + op + @"(C x, int y) => x;
}
";
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.RegularNext })
            {
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: options);

                compilation1.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).Verify(
                    // (4,30): error CS9150: User-defined operator '%' cannot be declared checked
                    //     public static C operator checked %(C x, int y) => x;
                    Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(4, 30)
                    );

                var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();

                Assert.Equal(MethodKind.UserDefinedOperator, opSymbol.MethodKind);
                Assert.Equal(name, opSymbol.Name);
                Assert.Equal("C C." + name + "(C x, System.Int32 y)", opSymbol.ToTestDisplayString());
                Assert.Equal("C.operator " + op + "(C, int)", opSymbol.ToDisplayString());
            }
        }

        [Theory]
        [InlineData("%", "op_Modulus")]
        [InlineData("&", "op_BitwiseAnd")]
        [InlineData("|", "op_BitwiseOr")]
        [InlineData("^", "op_ExclusiveOr")]
        [InlineData("<<", "op_LeftShift")]
        [InlineData(">>", "op_RightShift")]
        [InlineData("==", "op_Equality")]
        [InlineData("!=", "op_Inequality")]
        [InlineData(">", "op_GreaterThan")]
        [InlineData("<", "op_LessThan")]
        [InlineData(">=", "op_GreaterThanOrEqual")]
        [InlineData("<=", "op_LessThanOrEqual")]
        public void BinaryOperators_Unsupported_03(string op, string name)
        {
            var source1 =
@"
class C 
{
    public static C operator " + op + @"(C x, int y) => x;
    public static C operator checked " + op + @"(C x, int y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).Verify(
                // (5,30): error CS9150: User-defined operator '%' cannot be declared checked
                //     public static C operator checked %(C x) => x;
                Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(5, 30),
                // (5,38): error CS0111: Type 'C' already defines a member called 'op_Modulus' with the same parameter types
                //     public static C operator checked %(C x) => x;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(5, 38)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(2, opSymbols.Length);

            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[0].MethodKind);
            Assert.Equal(MethodKind.UserDefinedOperator, opSymbols[1].MethodKind);

            Assert.Equal("C C." + name + "(C x, System.Int32 y)", opSymbols[0].ToTestDisplayString());
            Assert.Equal("C C." + name + "(C x, System.Int32 y)", opSymbols[1].ToTestDisplayString());
        }

        [Theory]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData("==")]
        [InlineData("!=")]
        [InlineData(">")]
        [InlineData("<")]
        [InlineData(">=")]
        [InlineData("<=")]
        public void BinaryOperator_Unsupported_CRef_NoParameters_01(string op)
        {
            string opForXml = GetOperatorTokenForXml(op);

            var source = @"
/// <summary>
/// See <see cref=""operator checked " + opForXml + @"""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c, int y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked !' that could not be resolved
                // /// See <see cref="operator checked %"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + opForXml).WithArguments("operator checked " + opForXml).WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(
                    // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked %'
                    // /// See <see cref="operator checked %"/>.
                    Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + opForXml).WithArguments("operator checked " + opForXml).WithLocation(3, 20),
                    // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                    // /// See <see cref="operator checked %"/>.
                    Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29)
                    );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        private static string GetOperatorTokenForXml(string op)
        {
            return op switch { "&" => "&amp;", "<<" => "{{", ">>" => "}}", ">" => "}", "<" => "{", ">=" => "}=", "<=" => "{=", _ => op };
        }

        [Theory]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData("==")]
        [InlineData("!=")]
        [InlineData(">")]
        [InlineData("<")]
        [InlineData(">=")]
        [InlineData("<=")]
        public void BinaryOperator_Unsupported_CRef_TwoParameters_01(string op)
        {
            string opForXml = GetOperatorTokenForXml(op);

            var source = @"
/// <summary>
/// See <see cref=""operator checked " + opForXml + @"(C, int)""/>.
/// </summary>
class C
{
    public static C operator " + op + @"(C c, int y)
    {
        return null;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'operator checked %(C, int)' that could not be resolved
                // /// See <see cref="operator checked %(C, int)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "operator checked " + opForXml + "(C, int)").WithArguments("operator checked " + opForXml + "(C, int)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(
                    // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked %(C, int)'
                    // /// See <see cref="operator checked %(C, int)"/>.
                    Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + opForXml + "(C, int)").WithArguments("operator checked " + opForXml + "(C, int)").WithLocation(3, 20),
                    // (3,29): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                    // /// See <see cref="operator checked %(C, int)"/>.
                    Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 29)
                    );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void MissingOperatorTokenAndNoParameters_01()
        {
            var source1 =
@"
class C 
{
    public static C operator checked () => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,38): error CS1037: Overloadable operator expected
                //     public static C operator checked () => default;
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "(").WithLocation(4, 38),
                // (4,39): error CS1003: Syntax error, '(' expected
                //     public static C operator checked () => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(", ")").WithLocation(4, 39)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator checked +()", opSymbol.ToDisplayString());
            Assert.Equal("op_CheckedAddition", opSymbol.Name);
        }

        [Fact]
        public void MissingOperatorTokenAndNoParameters_02()
        {
            var source1 =
@"
class C 
{
    public static C operator () => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,30): error CS1037: Overloadable operator expected
                //     public static C operator () => default;
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "(").WithLocation(4, 30),
                // (4,31): error CS1003: Syntax error, '(' expected
                //     public static C operator () => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(", ")").WithLocation(4, 31)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.operator +()", opSymbol.ToDisplayString());
            Assert.Equal("op_Addition", opSymbol.Name);
        }

        [Fact]
        public void ConversionOperators_Supported_01()
        {
            var source1 =
@"
class C 
{
    public static explicit operator checked int(C x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular10);
            compilation1.VerifyDiagnostics(
                // (4,37): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(4, 37)
                );
            validator(compilation1.SourceModule);

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularNext);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();

                Assert.Equal(MethodKind.Conversion, opSymbol.MethodKind);
                Assert.Equal("op_CheckedExplicit", opSymbol.Name);
                Assert.Equal("System.Int32 C.op_CheckedExplicit(C x)", opSymbol.ToTestDisplayString());
                Assert.Equal("C.explicit operator checked int(C)", opSymbol.ToDisplayString());
                Assert.True(opSymbol.IsStatic);
                Assert.True(opSymbol.HasSpecialName);
                Assert.False(opSymbol.HasRuntimeSpecialName);
                Assert.Equal(Accessibility.Public, opSymbol.DeclaredAccessibility);
            }
        }

        [Fact]
        public void ConversionOperators_Supported_02()
        {
            var source1 =
@"
class C 
{
    public static explicit operator int(C x) => 0;
    public static explicit operator checked int(C x) => 0;
}

class C1 
{
    public static explicit operator checked int(C1 x) => 0;
    public static explicit operator int(C1 x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                Assert.Equal(MethodKind.Conversion, opSymbols[0].MethodKind);
                Assert.Equal(MethodKind.Conversion, opSymbols[1].MethodKind);

                Assert.Equal("System.Int32 C.op_Explicit(C x)", opSymbols[0].ToTestDisplayString());
                Assert.Equal("System.Int32 C.op_CheckedExplicit(C x)", opSymbols[1].ToTestDisplayString());
            }
        }

        [Fact]
        public void ConversionOperators_Supported_03()
        {
            var source1 =
@"
struct C 
{
    public static explicit operator checked int(C x) => 0;
    public static explicit operator checked long(C x) => 0;
}

struct C1 
{
    public static explicit operator checked long(C1 x) => 0;
    public static explicit operator checked int(C1 x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
                Assert.Equal(2, opSymbols.Length);

                Assert.Equal(MethodKind.Conversion, opSymbols[0].MethodKind);
                Assert.Equal(MethodKind.Conversion, opSymbols[1].MethodKind);

                Assert.Equal("System.Int32 C.op_CheckedExplicit(C x)", opSymbols[0].ToTestDisplayString());
                Assert.Equal("System.Int64 C.op_CheckedExplicit(C x)", opSymbols[1].ToTestDisplayString());
            }
        }

        [Fact]
        public void ConversionOperators_Supported_04()
        {
            var source1 =
@"
struct C 
{
    public static implicit operator int(C x) => 0;
    public static explicit operator checked int(C x) => 0;
}

struct C1 
{
    public static explicit operator checked int(C1 x) => 0;
    public static implicit operator int(C1 x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (5,45): error CS0557: Duplicate user-defined conversion in type 'C'
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "int").WithArguments("C").WithLocation(5, 45),
                // (11,37): error CS0557: Duplicate user-defined conversion in type 'C1'
                //     public static implicit operator int(C1 x) => 0;
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "int").WithArguments("C1").WithLocation(11, 37)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(2, opSymbols.Length);

            Assert.Equal(MethodKind.Conversion, opSymbols[0].MethodKind);
            Assert.Equal(MethodKind.Conversion, opSymbols[1].MethodKind);

            Assert.Equal("System.Int32 C.op_Implicit(C x)", opSymbols[0].ToTestDisplayString());
            Assert.Equal("System.Int32 C.op_CheckedExplicit(C x)", opSymbols[1].ToTestDisplayString());
        }

        [Fact]
        public void ConversionOperators_Supported_05()
        {
            var source1 =
@"
class C 
{
    public static explicit operator checked int(string x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,45): error CS0556: User-defined conversion must convert to or from the enclosing type
                //     public static explicit operator checked int(string x) => 0;
                Diagnostic(ErrorCode.ERR_ConversionNotInvolvingContainedType, "int").WithLocation(4, 45)
                );
        }

        [Fact]
        public void ConversionOperators_Supported_08()
        {
            var source1 =
@"
class C 
{
    explicit operator checked int(C x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,31): error CS0558: User-defined operator 'C.explicit operator checked int(C)' must be declared static and public
                //     explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStatic, "int").WithArguments("C.explicit operator checked int(C)").WithLocation(4, 31)
                );
        }

        [Fact]
        public void ConversionOperators_Supported_09()
        {
            var source1 =
@"
class C 
{
    public static explicit operator checked int(C x) => 0;

    int Test(C x)
    {
        return C.op_CheckedExplicit(x);
    }
}

class C1 
{
    public static explicit operator checked int(C1 x) => 0;

    static C1 op_CheckedExplicit(C1 x) => x; 
}

class C2 
{
    static C2 op_CheckedExplicit(C2 x) => x; 

    public static explicit operator checked int(C2 x) => 0;
}

class C3 
{
    public static explicit operator checked int(C3 x) => 0;

    int op_CheckedExplicit { get; } 
}

class C4 
{
    C4 op_CheckedExplicit { get; } 

    public static explicit operator checked int(C4 x) => 0;
}

class C5 
{
    public static explicit operator checked int(C5 x) => 0;

    public static explicit operator checked int(C5 y) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (8,18): error CS0571: 'C.explicit operator checked int(C)': cannot explicitly call operator or accessor
                //         return C.op_CheckedExplicit(x);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "op_CheckedExplicit").WithArguments("C.explicit operator checked int(C)").WithLocation(8, 18),
                // (16,15): error CS0111: Type 'C1' already defines a member called 'op_CheckedExplicit' with the same parameter types
                //     static C1 op_CheckedExplicit(C1 x) => x; 
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_CheckedExplicit").WithArguments("op_CheckedExplicit", "C1").WithLocation(16, 15),
                // (23,45): error CS0111: Type 'C2' already defines a member called 'op_CheckedExplicit' with the same parameter types
                //     public static explicit operator checked int(C2 x) => 0;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "int").WithArguments("op_CheckedExplicit", "C2").WithLocation(23, 45),
                // (30,9): error CS0102: The type 'C3' already contains a definition for 'op_CheckedExplicit'
                //     int op_CheckedExplicit { get; } 
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "op_CheckedExplicit").WithArguments("C3", "op_CheckedExplicit").WithLocation(30, 9),
                // (37,45): error CS0102: The type 'C4' already contains a definition for 'op_CheckedExplicit'
                //     public static explicit operator checked int(C4 x) => 0;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "int").WithArguments("C4", "op_CheckedExplicit").WithLocation(37, 45),
                // (44,45): error CS0557: Duplicate user-defined conversion in type 'C5'
                //     public static explicit operator checked int(C5 y) => 0;
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "int").WithArguments("C5").WithLocation(44, 45)
                );
        }

        [Fact]
        public void ConversionOperators_Supported_10()
        {
            var source1 =
@"
class C 
{
    public static explicit operator checked int(C x, C y) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,48): error CS1019: Overloadable unary operator expected
                //     public static explicit operator checked int(C x, C y) => 0;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "(C x, C y)").WithLocation(4, 48)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.explicit operator checked int(C, C)", opSymbol.ToDisplayString());
        }

        [Fact]
        public void ConversionOperators_Supported_11()
        {
            var source1 =
@"
class C 
{
    public static explicit operator checked int() => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,48): error CS1019: Overloadable unary operator expected
                //     public static explicit operator checked int() => 0;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "()").WithLocation(4, 48)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            Assert.Equal("C.explicit operator checked int()", opSymbol.ToDisplayString());
        }

        [Fact]
        public void ConversionOperators_Supported_12()
        {
            var source1 =
@"
static class C 
{
    public static explicit operator checked int(C x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,45): error CS0715: 'C.explicit operator checked int(C)': static classes cannot contain user-defined operators
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, "int").WithArguments("C.explicit operator checked int(C)").WithLocation(4, 45),
                // (4,45): error CS0721: 'C': static types cannot be used as parameters
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "int").WithArguments("C").WithLocation(4, 45)
                );
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_NoParameters_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator checked int""/>.
/// </summary>
class C
{
    public static explicit operator checked int(C c)
    {
        return 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'explicit operator checked int'
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "explicit operator checked int").WithArguments("explicit operator checked int").WithLocation(3, 20),
                // (3,38): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 38),
                // (7,37): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static explicit operator checked int(C c)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 37)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_NoParameters_02()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator int""/>.
/// </summary>
class C
{
    public static explicit operator checked int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator int' that could not be resolved
                // /// See <see cref="explicit operator int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator int").WithArguments("explicit operator int").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_NoParameters_03()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator checked int""/>.
/// </summary>
class C
{
    public static explicit operator int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator checked int' that could not be resolved
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator checked int").WithArguments("explicit operator checked int").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_NoParameters_05()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int""/>.
/// </summary>
class C
{
    public static explicit operator checked int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'implicit operator int' that could not be resolved
                // /// See <see cref="implicit operator int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "implicit operator int").WithArguments("implicit operator int").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_NoParameters_06()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator checked int""/>.
/// </summary>
class C
{
    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator checked int' that could not be resolved
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator checked int").WithArguments("explicit operator checked int").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_OneParameter_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator checked int(C)""/>.
/// </summary>
class C
{
    public static explicit operator checked int(C c)
    {
        return 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'explicit operator checked int'
                // /// See <see cref="explicit operator checked int(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "explicit operator checked int(C)").WithArguments("explicit operator checked int(C)").WithLocation(3, 20),
                // (3,38): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="explicit operator checked int(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 38),
                // (7,37): error CS8652: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public static explicit operator checked int(C c)
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "checked").WithArguments("checked user-defined operators").WithLocation(7, 37)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_OneParameter_02()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator int(C)""/>.
/// </summary>
class C
{
    public static explicit operator checked int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator int' that could not be resolved
                // /// See <see cref="explicit operator int(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator int(C)").WithArguments("explicit operator int(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_OneParameter_03()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator checked int(C)""/>.
/// </summary>
class C
{
    public static explicit operator int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator checked int' that could not be resolved
                // /// See <see cref="explicit operator checked int(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator checked int(C)").WithArguments("explicit operator checked int(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_OneParameter_05()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator int(C)""/>.
/// </summary>
class C
{
    public static explicit operator checked int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'implicit operator int' that could not be resolved
                // /// See <see cref="implicit operator int(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "implicit operator int(C)").WithArguments("implicit operator int(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Supported_CRef_OneParameter_06()
        {
            var source = @"
/// <summary>
/// See <see cref=""explicit operator checked int(C)""/>.
/// </summary>
class C
{
    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'explicit operator checked int' that could not be resolved
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "explicit operator checked int(C)").WithArguments("explicit operator checked int(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperators_Unsupported_01()
        {
            var source1 =
@"
class C 
{
    public static implicit operator checked int(C x) => 0;
}
";
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.RegularNext })
            {
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: options);

                compilation1.VerifyDiagnostics(
                    // (4,37): error CS9151: An 'implicit' user-defined conversion operator cannot be declared checked
                    //     public static implicit operator checked int(C x) => 0;
                    Diagnostic(ErrorCode.ERR_ImplicitConversionOperatorCantBeChecked, "checked").WithLocation(4, 37)
                    );

                var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();

                Assert.Equal(MethodKind.Conversion, opSymbol.MethodKind);
                Assert.Equal("op_Implicit", opSymbol.Name);
                Assert.Equal("System.Int32 C.op_Implicit(C x)", opSymbol.ToTestDisplayString());
                Assert.Equal("C.implicit operator int(C)", opSymbol.ToDisplayString());
            }
        }

        [Fact]
        public void ConversionOperators_Unsupported_03()
        {
            var source1 =
@"
class C 
{
    public static implicit operator int(C x) => 0;
    public static implicit operator checked int(C x) => 1;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (5,37): error CS9151: An 'implicit' user-defined conversion operator cannot be declared checked
                //     public static implicit operator checked int(C x) => 1;
                Diagnostic(ErrorCode.ERR_ImplicitConversionOperatorCantBeChecked, "checked").WithLocation(5, 37),
                // (5,45): error CS0557: Duplicate user-defined conversion in type 'C'
                //     public static implicit operator checked int(C x) => 1;
                Diagnostic(ErrorCode.ERR_DuplicateConversionInClass, "int").WithArguments("C").WithLocation(5, 45)
                );

            var c = compilation1.SourceModule.GlobalNamespace.GetTypeMember("C");
            var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();
            Assert.Equal(2, opSymbols.Length);

            Assert.Equal(MethodKind.Conversion, opSymbols[0].MethodKind);
            Assert.Equal(MethodKind.Conversion, opSymbols[1].MethodKind);

            Assert.Equal("System.Int32 C.op_Implicit(C x)", opSymbols[0].ToTestDisplayString());
            Assert.Equal("System.Int32 C.op_Implicit(C x)", opSymbols[1].ToTestDisplayString());
        }

        [Fact]
        public void ConversionOperator_Unsupported_CRef_NoParameters_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator checked int"" />.
/// </summary>
class C
{
    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'implicit operator checked int' that could not be resolved
                // /// See <see cref="implicit operator checked int" />.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "implicit operator checked int").WithArguments("implicit operator checked int").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'implicit operator checked int'
                // /// See <see cref="implicit operator checked int" />.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "implicit operator checked int").WithArguments("implicit operator checked int").WithLocation(3, 20),
                // (3,38): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="implicit operator checked int" />.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 38)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Fact]
        public void ConversionOperator_Unsupported_CRef_OneParameter_01()
        {
            var source = @"
/// <summary>
/// See <see cref=""implicit operator checked int(C)"" />.
/// </summary>
class C
{
    public static implicit operator int(C c)
    {
        return 0;
    }
}
";
            var expected = new[] {
                // (3,20): warning CS1574: XML comment has cref attribute 'implicit operator checked int' that could not be resolved
                // /// See <see cref="implicit operator checked int(C)" />.
                Diagnostic(ErrorCode.WRN_BadXMLRef, "implicit operator checked int(C)").WithArguments("implicit operator checked int(C)").WithLocation(3, 20)
                };

            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'implicit operator checked int'
                // /// See <see cref="implicit operator checked int(C)" />.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "implicit operator checked int(C)").WithArguments("implicit operator checked int(C)").WithLocation(3, 20),
                // (3,38): warning CS1658: The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.. See also error CS8652.
                // /// See <see cref="implicit operator checked int(C)" />.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("The feature 'checked user-defined operators' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.", "8652").WithLocation(3, 38)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularNext.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }
    }
}
