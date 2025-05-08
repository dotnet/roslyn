// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class CheckedUserDefinedOperatorsTests : CSharpTestBase
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

    public static C operator " + op + @"(C x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular10);
            compilation1.VerifyDiagnostics(
                // (4,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(4, 30)
                );
            validator(compilation1.SourceModule);

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();

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

    public static C operator " + op + @"(C x) => x;
    public static C? operator " + op + @"(C? x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();

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
                compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                    // (5,39): error CS0111: Type 'C' already defines a member called 'op_CheckedDecrement' with the same parameter types
                    //     public static C? operator checked --(C x) => x;
                    Diagnostic(ErrorCode.ERR_MemberAlreadyExists, op).WithArguments(name, "C").WithLocation(5, 39)
                    );
            }
            else
            {
                compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,38): error CS0562: The parameter of a unary operator must be the containing type
                //     public static C operator checked -(int x) => default;
                Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, "-").WithLocation(4, 38)
                );
        }

        [Theory]
        [InlineData("-")]
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,23): error CS0558: User-defined operator 'C.operator checked --(C)' must be declared static and public
                //     C operator checked--(C x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C.operator checked " + op + "(C)").WithLocation(4, 23)
                );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void IncrementOperators_Supported_08(string op)
        {
            var source1 =
@"
class C 
{
    C operator checked" + op + @"(C x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,23): error CS0558: User-defined operator 'C.operator checked ++(C)' must be declared static and public
                //     C operator checked++(C x) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C.operator checked " + op + "(C)").WithLocation(4, 23)
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,38): error CS0106: The modifier 'static' is not valid for this item
                //     public static C operator checked --() => default;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, op).WithArguments("static").WithLocation(4, 38),
                // (4,38): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute..ctor'
                //     public static C operator checked --() => default;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, op).WithArguments("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute", ".ctor").WithLocation(4, 38),
                // (4,38): error CS9310: The return type for this operator must be void
                //     public static C operator checked --() => default;
                Diagnostic(ErrorCode.ERR_OperatorMustReturnVoid, op).WithLocation(4, 38)
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
            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,19): error CS0722: 'C': static types cannot be used as return types
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(4, 19),
                // (4,38): error CS0715: 'C.operator checked ++(C)': static classes cannot contain user-defined operators
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C.operator checked " + op + "(C)").WithLocation(4, 38),
                // (4,41): error CS0721: 'C': static types cannot be used as parameters
                //     public static C operator checked ++(C x) => x;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(4, 39 + op.Length)
                );
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void UnaryOperators_Supported_13(string op)
        {
            var source1 =
@"
struct C 
{
    public static C operator checked " + op + @"(C x) => x;

    public static C? operator " + op + @"(C? x) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (4,38): error CS9152: The operator 'C.operator checked -(C)' requires a matching non-checked version of the operator to also be defined
                //     public static C operator checked -(C x) => x;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C.operator checked " + op + "(C)").WithLocation(4, 38)
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(").WithLocation(4, 39)
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(").WithLocation(4, 31)
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

    public static C operator " + op + @"(C c)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked ++'
                // /// See <see cref="operator checked ++"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked ++"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked ++(C c)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked -'
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked -").WithArguments("operator checked -").WithLocation(3, 20),
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked -"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked -(C c)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked ++(C)'
                // /// See <see cref="operator checked ++(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20),
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked ++(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked ++(C c)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify();

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
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
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
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
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked +"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked true"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked +(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
            compilation.VerifyDiagnostics( // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked true(C)'
                                           // /// See <see cref="operator checked true(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C)").WithArguments("operator checked " + op + "(C)").WithLocation(3, 20),
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked true(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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

    public static C operator " + op + @"(C x, C y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular10);
            compilation1.VerifyDiagnostics(
                // (4,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked +(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(4, 30)
                );
            validator(compilation1.SourceModule);

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();

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

    public static C operator " + op + @"(C x, C y) => x;
    public static C? operator " + op + @"(C? x, C? y) => x;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();

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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,23): error CS0558: User-defined operator 'C.operator checked -(C, C)' must be declared static and public
                //     C operator checked-(C x, C y) => default;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, op).WithArguments("C.operator checked " + op + "(C, C)").WithLocation(4, 23)
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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
            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,19): error CS0722: 'C': static types cannot be used as return types
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C").WithArguments("C").WithLocation(4, 19),
                // (4,38): error CS0715: 'C.operator checked -(C, C)': static classes cannot contain user-defined operators
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, op).WithArguments("C.operator checked " + op + "(C, C)").WithLocation(4, 38),
                // (4,40): error CS0721: 'C': static types cannot be used as parameters
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(4, 40),
                // (4,45): error CS0721: 'C': static types cannot be used as parameters
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(4, 45)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperators_Supported_13(string op)
        {
            var source1 =
@"
struct C 
{
    public static C operator checked " + op + @"(C x, C y) => x;

    public static C operator " + op + @"(C x, int y) => x;
    public static C operator " + op + @"(int x, C y) => y;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (4,38): error CS9152: The operator 'C.operator checked -(C, C)' requires a matching non-checked version of the operator to also be defined
                //     public static C operator checked -(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, op).WithArguments("C.operator checked " + op + "(C, C)").WithLocation(4, 38)
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,38): error CS1020: Overloadable binary operator expected
                //     public static C operator checked (C x, C y) => default;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "(").WithLocation(4, 38),
                // (4,39): error CS1003: Syntax error, '(' expected
                //     public static C operator checked (C x, C y) => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(").WithLocation(4, 39)
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments("(").WithLocation(4, 31)
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

    public static C operator " + op + @"(C c, C y)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));

            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked +'
                // /// See <see cref="operator checked +"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op).WithArguments("operator checked " + op).WithLocation(3, 20),
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked +"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked +(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked'
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked").WithArguments("operator checked").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked "/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, @"""").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked +(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked +(C, C)'
                // /// See <see cref="operator checked +(C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + op + "(C, C)").WithArguments("operator checked " + op + "(C, C)").WithLocation(3, 20),
                // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="operator checked +(C, C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked +(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify();

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void BinaryOperator_Supported_CRef_TwoParameters_03(string op)
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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked (C, C)'
                // /// See <see cref="operator checked (C, C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked (C, C)").WithArguments("operator checked (C, C)").WithLocation(3, 20),
                // (3,37): warning CS1658: Overloadable operator expected. See also error CS1037.
                // /// See <see cref="operator checked (C, C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "(").WithArguments("Overloadable operator expected", "1037").WithLocation(3, 37),
                // (7,30): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static C operator checked +(C c, C y)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 30)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
        [InlineData(">>>", "op_UnsignedRightShift")]
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
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: options);

                if (op == ">>>" && options == TestOptions.Regular10)
                {
                    compilation1.VerifyDiagnostics(
                        // (4,30): error CS9023: User-defined operator '>>>' cannot be declared checked
                        //     public static C operator checked >>>(C x, int y) => x;
                        Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(">>>").WithLocation(4, 30),
                        // (4,38): error CS8936: Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.
                        //     public static C operator checked >>>(C x, int y) => x;
                        Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, ">>>").WithArguments("unsigned right shift", "11.0").WithLocation(4, 38)
                        );
                }
                else
                {
                    compilation1.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).Verify(
                        // (4,30): error CS9150: User-defined operator '%' cannot be declared checked
                        //     public static C operator checked %(C x, int y) => x;
                        Diagnostic(ErrorCode.ERR_OperatorCantBeChecked, "checked").WithArguments(op).WithLocation(4, 30)
                        );
                }

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
        [InlineData(">>>", "op_UnsignedRightShift")]
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
        [InlineData(">>>")]
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

            if (op != ">>>")
            {
                compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                    Verify(
                        // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked }}'
                        // /// See <see cref="operator checked }}"/>.
                        Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + opForXml).WithArguments("operator checked " + opForXml).WithLocation(3, 20),
                        // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                        // /// See <see cref="operator checked }}"/>.
                        Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29)
                        );
            }
            else
            {
                compilation.VerifyDiagnostics(
                    // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked }}}'
                    // /// See <see cref="operator checked }}}"/>.
                    Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked }}}").WithArguments("operator checked }}}").WithLocation(3, 20),
                    // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                    // /// See <see cref="operator checked }}}"/>.
                    Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                    // (3,37): warning CS1658: Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                    // /// See <see cref="operator checked }}}"/>.
                    Diagnostic(ErrorCode.WRN_ErrorOverride, "}}}").WithArguments("Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 37),
                    // (7,30): error CS8936: Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.
                    //     public static C operator >>>(C c, int y)
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, ">>>").WithArguments("unsigned right shift", "11.0").WithLocation(7, 30)
                    );
            }

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                Verify(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        private static string GetOperatorTokenForXml(string op)
        {
            return op switch { "&" => "&amp;", "<<" => "{{", ">>" => "}}", ">>>" => "}}}", ">" => "}", "<" => "{", ">=" => "}=", "<=" => "{=", _ => op };
        }

        [Theory]
        [InlineData("%")]
        [InlineData("&")]
        [InlineData("|")]
        [InlineData("^")]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
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

            if (op != ">>>")
            {
                compilation.GetDiagnostics().Where(d => d.Code is not ((int)ErrorCode.ERR_OperatorNeedsMatch or (int)ErrorCode.WRN_EqualityOpWithoutEquals or (int)ErrorCode.WRN_EqualityOpWithoutGetHashCode)).
                    Verify(
                        // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked }}(C, int)'
                        // /// See <see cref="operator checked }}(C, int)"/>.
                        Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked " + opForXml + "(C, int)").WithArguments("operator checked " + opForXml + "(C, int)").WithLocation(3, 20),
                        // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                        // /// See <see cref="operator checked }}(C, int)"/>.
                        Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29)
                        );
            }
            else
            {
                compilation.VerifyDiagnostics(
                    // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'operator checked }}}(C, int)'
                    // /// See <see cref="operator checked }}}(C, int)"/>.
                    Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "operator checked }}}(C, int)").WithArguments("operator checked }}}(C, int)").WithLocation(3, 20),
                    // (3,29): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                    // /// See <see cref="operator checked }}}(C, int)"/>.
                    Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 29),
                    // (3,37): warning CS1658: Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                    // /// See <see cref="operator checked }}}(C, int)"/>.
                    Diagnostic(ErrorCode.WRN_ErrorOverride, "}}}").WithArguments("Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 37),
                    // (7,30): error CS8936: Feature 'unsigned right shift' is not available in C# 10.0. Please use language version 11.0 or greater.
                    //     public static C operator >>>(C c, int y)
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, ">>>").WithArguments("unsigned right shift", "11.0").WithLocation(7, 30)
                    );
            }

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,38): error CS1037: Overloadable operator expected
                //     public static C operator checked () => default;
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "(").WithLocation(4, 38),
                // (4,39): error CS1003: Syntax error, '(' expected
                //     public static C operator checked () => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(4, 39)
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
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(4, 31)
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

    public static explicit operator int(C x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular10);
            compilation1.VerifyDiagnostics(
                // (4,37): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(4, 37)
                );
            validator(compilation1.SourceModule);

            compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbol = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();

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

    public static explicit operator int(C x) => 0;
    public static explicit operator long(C x) => 0;
}

struct C1 
{
    public static explicit operator checked long(C1 x) => 0;
    public static explicit operator checked int(C1 x) => 0;

    public static explicit operator int(C1 x) => 0;
    public static explicit operator long(C1 x) => 0;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, sourceSymbolValidator: validator, symbolValidator: validator).VerifyDiagnostics();

            void validator(ModuleSymbol m)
            {
                var c = m.GlobalNamespace.GetTypeMember("C");
                var opSymbols = c.GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).ToArray();

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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,31): error CS0558: User-defined operator 'C.explicit operator checked int(C)' must be declared static and public
                //     explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_OperatorsMustBeStaticAndPublic, "int").WithArguments("C.explicit operator checked int(C)").WithLocation(4, 31)
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
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

            compilation1.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (4,45): error CS0715: 'C.explicit operator checked int(C)': static classes cannot contain user-defined operators
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_OperatorInStaticClass, "int").WithArguments("C.explicit operator checked int(C)").WithLocation(4, 45),
                // (4,49): error CS0721: 'C': static types cannot be used as parameters
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C").WithLocation(4, 49)
                );
        }

        [Fact]
        public void ConversionOperators_Supported_13()
        {
            var source1 =
@"
struct C 
{
    public static explicit operator checked int(C x) => 0;

    public static explicit operator C(int x) => default;
    public static explicit operator int(C? x) => default;
    public static explicit operator long(C x) => default;
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);

            compilation1.VerifyDiagnostics(
                // (4,45): error CS9152: The operator 'C.explicit operator checked int(C)' requires a matching non-checked version of the operator to also be defined
                //     public static explicit operator checked int(C x) => 0;
                Diagnostic(ErrorCode.ERR_CheckedOperatorNeedsMatch, "int").WithArguments("C.explicit operator checked int(C)").WithLocation(4, 45)
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

    public static explicit operator int(C c)
    {
        return 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'explicit operator checked int'
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "explicit operator checked int").WithArguments("explicit operator checked int").WithLocation(3, 20),
                // (3,38): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="explicit operator checked int"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 38),
                // (7,37): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static explicit operator checked int(C c)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 37)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics();

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).First();
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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify();

            var crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            var expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            var actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular10.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'explicit operator checked int(C)'
                // /// See <see cref="explicit operator checked int(C)"/>.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "explicit operator checked int(C)").WithArguments("explicit operator checked int(C)").WithLocation(3, 20),
                // (3,38): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="explicit operator checked int(C)"/>.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 38),
                // (7,37): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public static explicit operator checked int(C c)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "checked").WithArguments("checked user-defined operators", "11.0").WithLocation(7, 37)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            expectedSymbol = compilation.SourceModule.GlobalNamespace.GetTypeMember("C").GetMembers().OfType<MethodSymbol>().Where(m => m.MethodKind != MethodKind.Constructor).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation);
            Assert.Equal(expectedSymbol, actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify();

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            compilation.GetDiagnostics().Where(d => d.Code is not (int)ErrorCode.ERR_CheckedOperatorNeedsMatch).Verify(expected);

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
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
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
                // (3,38): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="implicit operator checked int" />.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 38)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
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
                // (3,20): warning CS1584: XML comment has syntactically incorrect cref attribute 'implicit operator checked int(C)'
                // /// See <see cref="implicit operator checked int(C)" />.
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "implicit operator checked int(C)").WithArguments("implicit operator checked int(C)").WithLocation(3, 20),
                // (3,38): warning CS1658: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.. See also error CS8936.
                // /// See <see cref="implicit operator checked int(C)" />.
                Diagnostic(ErrorCode.WRN_ErrorOverride, "checked").WithArguments("Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.", "8936").WithLocation(3, 38)
                );

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);

            compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source, parseOptions: TestOptions.Regular11.WithDocumentationMode(DocumentationMode.Diagnose));
            compilation.VerifyDiagnostics(expected);

            crefSyntax = CrefTests.GetCrefSyntaxes(compilation).Single();
            actualSymbol = CrefTests.GetReferencedSymbol(crefSyntax, compilation, expected);
            Assert.Null(actualSymbol);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void OverloadResolution_UnaryOperators_01(string op)
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked " + op + @"(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}

public class C1 : C0
{
    public static C1 operator checked " + op + @"(C1 x)
    {
        System.Console.WriteLine(""checked C1"");
        return x;
    }

    public static C1 operator " + op + @"(C1 x)
    {
        System.Console.WriteLine(""regular C1"");
        return x;
    }
}

public class C2 : C1
{
    public static C2 operator " + op + @"(C2 x)
    {
        System.Console.WriteLine(""regular C2"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
        TestUncheckedC0(new C0());
        TestCheckedC1(new C1());
        TestUncheckedC1(new C1());
        TestCheckedC2(new C2());
        TestUncheckedC2(new C2());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(" + op + @"x); // C0
    }

    public static C0 TestUncheckedC0(C0 x)
    {
        return unchecked(" + op + @"x);
    }

    public static C1 TestCheckedC1(C1 x)
    {
        return checked(" + op + @"x); // C1
    }

    public static C1 TestUncheckedC1(C1 x)
    {
        return unchecked(" + op + @"x);
    }

    public static C2 TestCheckedC2(C2 x)
    {
        return checked(" + op + @"x); // C2
    }

    public static C2 TestUncheckedC2(C2 x)
    {
        return unchecked(" + op + @"x);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (16,24): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         return checked(++x); // C0
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, op + "x").WithArguments("checked user-defined operators", "11.0").WithLocation(16, 24),
                // (26,24): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         return checked(++x); // C1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, op + "x").WithArguments("checked user-defined operators", "11.0").WithLocation(26, 24)
                );
        }

        [Fact]
        public void OverloadResolution_UnaryOperators_02()
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static C0 operator checked -(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            //   
            //     public static C0 operator -(C0 x)
            //     {
            //         System.Console.WriteLine(""regular C0"");
            //         return x;
            //     }
            // }
            //   
            // class C1 : C0
            // {
            //     public static C1 operator checked -(C1 x)
            //     {
            //         System.Console.WriteLine(""checked C1"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 op_CheckedUnaryNegation (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname static 
        class C0 op_UnaryNegation (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""regular C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit C1
    extends C0
{
    .method public hidebysig specialname static 
        class C1 op_CheckedUnaryNegation (
            class C1 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C1
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C1""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void C0::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C1());
        TestUncheckedC0(new C1());
        TestCheckedC1(new C1());
        TestUncheckedC1(new C1());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(-x);
    }

    public static C0 TestUncheckedC0(C0 x)
    {
        return unchecked(-x);
    }

    public static C1 TestCheckedC1(C1 x)
    {
        return checked(-x);
    }

    public static C0 TestUncheckedC1(C1 x)
    {
        return unchecked(-x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C0
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("-", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_CheckedIncrement")]
        [InlineData("--", "op_CheckedDecrement")]
        public void OverloadResolution_UnaryOperators_03(string op, string checkedName)
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static C0 operator checked " + op + @"(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 " + checkedName + @" (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(" + op + @"x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("-", "op_CheckedUnaryNegation")]
        [InlineData("++", "op_CheckedIncrement")]
        [InlineData("--", "op_CheckedDecrement")]
        public void OverloadResolution_UnaryOperators_04(string op, string checkedName)
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static C0 operator checked " + op + @"(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 " + checkedName + @" (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestUncheckedC0(new C0());
    }

    public static C0 TestUncheckedC0(C0 x)
    {
        return unchecked(" + op + @"x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (11,26): error CS0023: Operator '-' cannot be applied to operand of type 'C0'
                //         return unchecked(-x);
                Diagnostic(ErrorCode.ERR_BadUnaryOp, op + "x").WithArguments(op, "C0").WithLocation(11, 26)
                );
        }

        /// <summary>
        /// Lifted nullable
        /// </summary>
        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void OverloadResolution_UnaryOperators_05(string op)
        {
            var source1 =
@"
public struct C0 
{
    public static C0 operator checked " + op + @"(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestCheckedC0(new C0()) is null ? ""null"" : ""not null"");
        System.Console.WriteLine(TestCheckedC0(null) is null ? ""null"" : ""not null"");
    }

    public static C0? TestCheckedC0(C0? x)
    {
        return checked(" + op + @"x);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
not null
null
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
not null
null
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (12,24): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         return checked(++x);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, op + "x").WithArguments("checked user-defined operators", "11.0").WithLocation(12, 24)
                );
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public void ExpressionTree_UnaryOperators_01(string op)
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator checked " + op + @"(C0 x)
    {
        return x;
    }

    public static C0 operator " + op + @"(C0 x)
    {
        return x;
    }
}

class Program
{
    public static Expression<Func<C0, C0>> TestChecked()
    {
        return x => checked(" + op + @"x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return x => unchecked(" + op + @"x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (22,29): error CS0832: An expression tree may not contain an assignment operator
                //         return x => checked(++x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, op + "x").WithLocation(22, 29),
                // (27,31): error CS0832: An expression tree may not contain an assignment operator
                //         return x => unchecked(++x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, op + "x").WithLocation(27, 31)
                );
        }

        [Fact]
        public void ExpressionTree_UnaryOperators_02()
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator checked -(C0 x)
    {
        return x;
    }

    public static C0 operator -(C0 x)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestChecked());
        System.Console.WriteLine(TestUnchecked());
    }

    public static Expression<Func<C0, C0>> TestChecked()
    {
        return x => checked(-x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return x => unchecked(-x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput: @"
x => -x
x => -x
").VerifyDiagnostics();

            verifier.VerifyIL("Program.TestChecked", @"
{
  // Code size       63 (0x3f)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""C0 C0.op_CheckedUnaryNegation(C0)""
  IL_001c:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0021:  castclass  ""System.Reflection.MethodInfo""
  IL_0026:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.NegateChecked(System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldloc.0
  IL_0034:  stelem.ref
  IL_0035:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003a:  stloc.1
  IL_003b:  br.s       IL_003d
  IL_003d:  ldloc.1
  IL_003e:  ret
}
");

            verifier.VerifyIL("Program.TestUnchecked", @"
{
  // Code size       63 (0x3f)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""C0 C0.op_UnaryNegation(C0)""
  IL_001c:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0021:  castclass  ""System.Reflection.MethodInfo""
  IL_0026:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Negate(System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldloc.0
  IL_0034:  stelem.ref
  IL_0035:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003a:  stloc.1
  IL_003b:  br.s       IL_003d
  IL_003d:  ldloc.1
  IL_003e:  ret
}
");
        }

        [Fact]
        public void ExpressionTree_UnaryOperators_03()
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator -(C0 x)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestChecked());
        System.Console.WriteLine(TestUnchecked());
    }

    public static Expression<Func<C0, C0>> TestChecked()
    {
        return x => checked(-x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return x => unchecked(-x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput: @"
x => -x
x => -x
").VerifyDiagnostics();

            verifier.VerifyIL("Program.TestChecked", @"
{
  // Code size       63 (0x3f)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""C0 C0.op_UnaryNegation(C0)""
  IL_001c:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0021:  castclass  ""System.Reflection.MethodInfo""
  IL_0026:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Negate(System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldloc.0
  IL_0034:  stelem.ref
  IL_0035:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003a:  stloc.1
  IL_003b:  br.s       IL_003d
  IL_003d:  ldloc.1
  IL_003e:  ret
}
");

            verifier.VerifyIL("Program.TestUnchecked", @"
{
  // Code size       63 (0x3f)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""C0 C0.op_UnaryNegation(C0)""
  IL_001c:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0021:  castclass  ""System.Reflection.MethodInfo""
  IL_0026:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Negate(System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldloc.0
  IL_0034:  stelem.ref
  IL_0035:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003a:  stloc.1
  IL_003b:  br.s       IL_003d
  IL_003d:  ldloc.1
  IL_003e:  ret
}
");
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void OverloadResolution_BinaryOperators_01(string op)
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}

public class C1 : C0
{
    public static C1 operator checked " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""checked C1"");
        return x;
    }

    public static C1 operator " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""regular C1"");
        return x;
    }
}

public class C2 : C1
{
    public static C2 operator " + op + @"(C2 x, C2 y)
    {
        System.Console.WriteLine(""regular C2"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
        TestUncheckedC0(new C0());
        TestCheckedC1(new C1());
        TestUncheckedC1(new C1());
        TestCheckedC2(new C2());
        TestUncheckedC2(new C2());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(x " + op + @" x); // C0
    }

    public static C0 TestUncheckedC0(C0 x)
    {
        return unchecked(x " + op + @" x);
    }

    public static C1 TestCheckedC1(C1 x)
    {
        return checked(x " + op + @" x); // C1
    }

    public static C1 TestUncheckedC1(C1 x)
    {
        return unchecked(x " + op + @" x);
    }

    public static C2 TestCheckedC2(C2 x)
    {
        return checked(x " + op + @" x); // C2
    }

    public static C2 TestUncheckedC2(C2 x)
    {
        return unchecked(x " + op + @" x);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (16,24): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         return checked(x + x); // C0
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "x " + op + " x").WithArguments("checked user-defined operators", "11.0").WithLocation(16, 24),
                // (26,24): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         return checked(x + x); // C1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "x " + op + " x").WithArguments("checked user-defined operators", "11.0").WithLocation(26, 24)
                );
        }

        [Theory]
        [InlineData("+", "op_Addition", "op_CheckedAddition")]
        [InlineData("-", "op_Subtraction", "op_CheckedSubtraction")]
        [InlineData("*", "op_Multiply", "op_CheckedMultiply")]
        [InlineData("/", "op_Division", "op_CheckedDivision")]
        public void OverloadResolution_BinaryOperators_02(string op, string name, string checkedName)
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static C0 operator checked -(C0 x, C0 y)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            //   
            //     public static C0 operator -(C0 x, C0 y)
            //     {
            //         System.Console.WriteLine(""regular C0"");
            //         return x;
            //     }
            // }
            //   
            // class C1 : C0
            // {
            //     public static C1 operator checked -(C1 x, C1 y)
            //     {
            //         System.Console.WriteLine(""checked C1"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 " + checkedName + @" (
            class C0 x,
            class C0 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname static 
        class C0 " + name + @" (
            class C0 x,
            class C0 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""regular C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}

.class public auto ansi beforefieldinit C1
    extends C0
{
    .method public hidebysig specialname static 
        class C1 " + checkedName + @" (
            class C1 x,
            class C1 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C1
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C1""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void C0::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C1());
        TestUncheckedC0(new C1());
        TestCheckedC1(new C1());
        TestUncheckedC1(new C1());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(x " + op + @" x);
    }

    public static C0 TestUncheckedC0(C0 x)
    {
        return unchecked(x " + op + @" x);
    }

    public static C1 TestCheckedC1(C1 x)
    {
        return checked(x " + op + @" x);
    }

    public static C0 TestUncheckedC1(C1 x)
    {
        return unchecked(x " + op + @" x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C0
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+", "op_CheckedAddition")]
        [InlineData("-", "op_CheckedSubtraction")]
        [InlineData("*", "op_CheckedMultiply")]
        [InlineData("/", "op_CheckedDivision")]
        public void OverloadResolution_BinaryOperators_03(string op, string checkedName)
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static C0 operator checked -(C0 x, C0 y)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 " + checkedName + @" (
            class C0 x,
            class C0 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(x " + op + @" x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+", "op_CheckedAddition")]
        [InlineData("-", "op_CheckedSubtraction")]
        [InlineData("*", "op_CheckedMultiply")]
        [InlineData("/", "op_CheckedDivision")]
        public void OverloadResolution_BinaryOperators_04(string op, string checkedName)
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static C0 operator checked -(C0 x, C0 y)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 " + checkedName + @" (
            class C0 x,
            class C0 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestUncheckedC0(new C0());
    }

    public static C0 TestUncheckedC0(C0 x)
    {
        return unchecked(x " + op + @" x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (11,26): error CS0019: Operator '-' cannot be applied to operands of type 'C0' and 'C0'
                //         return unchecked(x - x);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "x " + op + " x").WithArguments(op, "C0", "C0").WithLocation(11, 26)
                );
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void OverloadResolution_BinaryOperators_05(string op)
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}

public class C1 : C0
{
    public static C1 operator checked " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""checked C1"");
        return x;
    }

    public static C1 operator " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""regular C1"");
        return x;
    }
}

public class C2 : C1
{
    public static C2 operator " + op + @"(C2 x, C2 y)
    {
        System.Console.WriteLine(""regular C2"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
        TestUncheckedC0(new C0());
        TestCheckedC1(new C1());
        TestUncheckedC1(new C1());
        TestCheckedC2(new C2());
        TestUncheckedC2(new C2());
    }

    public static void TestCheckedC0(C0 x)
    {
        checked { x " + op + @"= x; } // C0
    }

    public static void TestUncheckedC0(C0 x)
    {
        unchecked { x " + op + @"= x; }
    }

    public static void TestCheckedC1(C1 x)
    {
        checked { x " + op + @"= x; } // C1
    }

    public static void TestUncheckedC1(C1 x)
    {
        unchecked { x " + op + @"= x; }
    }

    public static void TestCheckedC2(C2 x)
    {
        checked { x " + op + @"= x; } // C2
    }

    public static void TestUncheckedC2(C2 x)
    {
        unchecked { x " + op + @"= x; }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (16,19): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         checked { x += x; } // C0
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "x " + op + "= x").WithArguments("checked user-defined operators", "11.0").WithLocation(16, 19),
                // (26,19): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         checked { x += x; } // C1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "x " + op + "= x").WithArguments("checked user-defined operators", "11.0").WithLocation(26, 19)
                );
        }

        [Fact]
        public void OverloadResolution_BinaryOperators_06()
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked /(C0 x, int y)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator /(C0 x, int y)
    {
        return x;
    }

    public static C0 operator /(C0 x, byte y)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}

class Program
{
    static void Main()
    {
        TestChecked(new C0(), 1);
    }

    public static void TestChecked(C0 x, byte y)
    {
        _ = checked(x / y);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
").VerifyDiagnostics();
        }

        /// <summary>
        /// Lifted nullable
        /// </summary>
        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void OverloadResolution_BinaryOperators_07(string op)
        {
            var source1 =
@"
public struct C0 
{
    public static C0 operator checked " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestCheckedC0(new C0(), new C0()) is null ? ""null"" : ""not null"");
        System.Console.WriteLine(TestCheckedC0(null, new C0()) is null ? ""null"" : ""not null"");
        System.Console.WriteLine(TestCheckedC0(new C0(), null) is null ? ""null"" : ""not null"");
        System.Console.WriteLine(TestCheckedC0(null, null) is null ? ""null"" : ""not null"");
    }

    public static C0? TestCheckedC0(C0? x, C0? y)
    {
        return checked(x " + op + @" y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
not null
null
null
null
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
not null
null
null
null
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (14,24): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         return checked(x + y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "x " + op + " y").WithArguments("checked user-defined operators", "11.0").WithLocation(14, 24)
                );
        }

        [Fact]
        public void ExpressionTree_BinaryOperators_01()
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator checked /(C0 x, C0 y)
    {
        return x;
    }

    public static C0 operator /(C0 x, C0 y)
    {
        return x;
    }
}

class Program
{
    public static Expression<Func<C0, C0>> TestChecked()
    {
        return x => checked(x / x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return x => unchecked(x / x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (22,29): error CS7053: An expression tree may not contain 'C0.operator checked /(C0, C0)'
                //         return x => checked(x / x);
                Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "x / x").WithArguments("C0.operator checked /(C0, C0)").WithLocation(22, 29)
                );
        }

        [Theory]
        [InlineData("+", "op_Addition", "op_CheckedAddition", "Add", "AddChecked")]
        [InlineData("-", "op_Subtraction", "op_CheckedSubtraction", "Subtract", "SubtractChecked")]
        [InlineData("*", "op_Multiply", "op_CheckedMultiply", "Multiply", "MultiplyChecked")]
        public void ExpressionTree_BinaryOperators_02(string op, string name, string checkedName, string factory, string checkedFactory)
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator checked " + op + @"(C0 x, C0 y)
    {
        return x;
    }

    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestChecked());
        System.Console.WriteLine(TestUnchecked());
    }

    public static Expression<Func<C0, C0>> TestChecked()
    {
        return x => checked(x " + op + @" x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return x => unchecked(x " + op + @" x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput: @"
x => (x " + op + @" x)
x => (x " + op + @" x)
").VerifyDiagnostics();

            verifier.VerifyIL("Program.TestChecked", @"
{
  // Code size       64 (0x40)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldtoken    ""C0 C0." + checkedName + @"(C0, C0)""
  IL_001d:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0022:  castclass  ""System.Reflection.MethodInfo""
  IL_0027:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression." + checkedFactory + @"(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002c:  ldc.i4.1
  IL_002d:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0032:  dup
  IL_0033:  ldc.i4.0
  IL_0034:  ldloc.0
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_003e
  IL_003e:  ldloc.1
  IL_003f:  ret
}
");

            verifier.VerifyIL("Program.TestUnchecked", @"
{
  // Code size       64 (0x40)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldtoken    ""C0 C0." + name + @"(C0, C0)""
  IL_001d:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0022:  castclass  ""System.Reflection.MethodInfo""
  IL_0027:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression." + factory + @"(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002c:  ldc.i4.1
  IL_002d:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0032:  dup
  IL_0033:  ldc.i4.0
  IL_0034:  ldloc.0
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_003e
  IL_003e:  ldloc.1
  IL_003f:  ret
}
");
        }

        [Theory]
        [InlineData("+", "op_Addition", "Add")]
        [InlineData("-", "op_Subtraction", "Subtract")]
        [InlineData("*", "op_Multiply", "Multiply")]
        [InlineData("/", "op_Division", "Divide")]
        public void ExpressionTree_BinaryOperators_03(string op, string name, string factory)
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestChecked());
        System.Console.WriteLine(TestUnchecked());
    }

    public static Expression<Func<C0, C0>> TestChecked()
    {
        return x => checked(x " + op + @" x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return x => unchecked(x " + op + @" x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput: @"
x => (x " + op + @" x)
x => (x " + op + @" x)
").VerifyDiagnostics();

            verifier.VerifyIL("Program.TestChecked", @"
{
  // Code size       64 (0x40)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldtoken    ""C0 C0." + name + @"(C0, C0)""
  IL_001d:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0022:  castclass  ""System.Reflection.MethodInfo""
  IL_0027:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression." + factory + @"(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002c:  ldc.i4.1
  IL_002d:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0032:  dup
  IL_0033:  ldc.i4.0
  IL_0034:  ldloc.0
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_003e
  IL_003e:  ldloc.1
  IL_003f:  ret
}
");

            verifier.VerifyIL("Program.TestUnchecked", @"
{
  // Code size       64 (0x40)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, C0>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldloc.0
  IL_0018:  ldtoken    ""C0 C0." + name + @"(C0, C0)""
  IL_001d:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0022:  castclass  ""System.Reflection.MethodInfo""
  IL_0027:  call       ""System.Linq.Expressions.BinaryExpression System.Linq.Expressions.Expression." + factory + @"(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression, System.Reflection.MethodInfo)""
  IL_002c:  ldc.i4.1
  IL_002d:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0032:  dup
  IL_0033:  ldc.i4.0
  IL_0034:  ldloc.0
  IL_0035:  stelem.ref
  IL_0036:  call       ""System.Linq.Expressions.Expression<System.Func<C0, C0>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, C0>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_003b:  stloc.1
  IL_003c:  br.s       IL_003e
  IL_003e:  ldloc.1
  IL_003f:  ret
}
");
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void ExpressionTree_BinaryOperators_04(string op)
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

class C0 
{
    public static C0 operator checked " + op + @"(C0 x, C0 y)
    {
        return x;
    }

    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        return x;
    }
}

class Program
{
    public static Expression<Func<C0, C0>> TestChecked()
    {
        return checked(x => x " + op + @"= x);
    }

    public static Expression<Func<C0, C0>> TestUnchecked()
    {
        return unchecked(x => x " + op + @"= x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (22,29): error CS0832: An expression tree may not contain an assignment operator
                //         return checked(x => x /= x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x " + op + "= x").WithLocation(22, 29),
                // (27,31): error CS0832: An expression tree may not contain an assignment operator
                //         return unchecked(x => x /= x);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x " + op + "= x").WithLocation(27, 31)
                );
        }

        [Fact]
        public void OverloadResolution_Conversion_01()
        {
            var source1 =
@"
public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }

    public static implicit operator int(C0 x)
    {
        System.Console.WriteLine(""implicit C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedImplicitLong(new C0());
        TestCheckedExplicitLong(new C0());
        TestUncheckedImplicitLong(new C0());
        TestUncheckedExplicitLong(new C0());
        TestCheckedImplicitInt(new C0());
        TestCheckedExplicitInt(new C0());
        TestUncheckedImplicitInt(new C0());
        TestUncheckedExplicitInt(new C0());
    }

    public static long TestCheckedImplicitLong(C0 x)
    {
        checked { return x; }
    }

    public static long TestCheckedExplicitLong(C0 x)
    {
        checked { return (long)x; }
    }

    public static long TestUncheckedImplicitLong(C0 x)
    {
        unchecked { return x; }
    }

    public static long TestUncheckedExplicitLong(C0 x)
    {
        unchecked { return (long)x; }
    }

    public static int TestCheckedImplicitInt(C0 x)
    {
        checked { return x; }
    }

    public static int TestCheckedExplicitInt(C0 x)
    {
        checked { return (int)x; }
    }

    public static int TestUncheckedImplicitInt(C0 x)
    {
        unchecked { return x; }
    }

    public static int TestUncheckedExplicitInt(C0 x)
    {
        unchecked { return (int)x; }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
implicit C0
checked C0
implicit C0
regular C0
implicit C0
implicit C0
implicit C0
implicit C0
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
implicit C0
checked C0
implicit C0
regular C0
implicit C0
implicit C0
implicit C0
implicit C0
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (23,26): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         checked { return (long)x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "(long)x").WithArguments("checked user-defined operators", "11.0").WithLocation(23, 26)
                );
        }

        [Fact]
        public void OverloadResolution_Conversion_02()
        {
            var source1 =
@"
public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedExplicitLong(new C0());
        TestUncheckedExplicitLong(new C0());
        TestCheckedExplicitInt(new C0());
        TestUncheckedExplicitInt(new C0());
    }

    public static long TestCheckedExplicitLong(C0 x)
    {
        checked { return (long)x; }
    }

    public static long TestUncheckedExplicitLong(C0 x)
    {
        unchecked { return (long)x; }
    }

    public static int TestCheckedExplicitInt(C0 x)
    {
        checked { return (int)x; }
    }

    public static int TestUncheckedExplicitInt(C0 x)
    {
        unchecked { return (int)x; }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
regular C0
checked C0
regular C0
").VerifyDiagnostics();

            var source3 =
@"
class Program
{
    static void Main()
    {
    }

    public static long TestCheckedImplicitLong(C0 x)
    {
        checked { return x; }
    }

    public static long TestUncheckedImplicitLong(C0 x)
    {
        unchecked { return x; }
    }

    public static int TestCheckedImplicitInt(C0 x)
    {
        checked { return x; }
    }

    public static int TestUncheckedImplicitInt(C0 x)
    {
        unchecked { return x; }
    }
}
";

            var compilation2 = CreateCompilation(source1 + source3, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation2.VerifyDiagnostics(
                // (25,26): error CS0266: Cannot implicitly convert type 'C0' to 'long'. An explicit conversion exists (are you missing a cast?)
                //         checked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("C0", "long").WithLocation(25, 26),
                // (30,28): error CS0266: Cannot implicitly convert type 'C0' to 'long'. An explicit conversion exists (are you missing a cast?)
                //         unchecked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("C0", "long").WithLocation(30, 28),
                // (35,26): error CS0266: Cannot implicitly convert type 'C0' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         checked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("C0", "int").WithLocation(35, 26),
                // (40,28): error CS0266: Cannot implicitly convert type 'C0' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         unchecked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("C0", "int").WithLocation(40, 28)
                );
        }

        [Fact]
        public void OverloadResolution_Conversion_03()
        {
            // The IL is equivalent to
            //
            // public class C0 
            // {
            //     public static explicit operator checked long(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return 0;
            //     }
            //
            //     public static implicit operator int(C0 x)
            //     {
            //         System.Console.WriteLine(""implicit C0"");
            //         return 0;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        int64 op_CheckedExplicit (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] int64
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: conv.i8
        IL_000e: stloc.0
        IL_000f: br.s IL_0011

        IL_0011: ldloc.0
        IL_0012: ret
    }

    .method public hidebysig specialname static 
        int32 op_Implicit (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] int32
        )

        IL_0000: nop
        IL_0001: ldstr ""implicit C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedImplicitLong(new C0());
        TestCheckedExplicitLong(new C0());
        TestUncheckedImplicitLong(new C0());
        TestUncheckedExplicitLong(new C0());
        TestCheckedImplicitInt(new C0());
        TestCheckedExplicitInt(new C0());
        TestUncheckedImplicitInt(new C0());
        TestUncheckedExplicitInt(new C0());
    }

    public static long TestCheckedImplicitLong(C0 x)
    {
        checked { return x; }
    }

    public static long TestCheckedExplicitLong(C0 x)
    {
        checked { return (long)x; }
    }

    public static long TestUncheckedImplicitLong(C0 x)
    {
        unchecked { return x; }
    }

    public static long TestUncheckedExplicitLong(C0 x)
    {
        unchecked { return (long)x; }
    }

    public static int TestCheckedImplicitInt(C0 x)
    {
        checked { return x; }
    }

    public static int TestCheckedExplicitInt(C0 x)
    {
        checked { return (int)x; }
    }

    public static int TestUncheckedImplicitInt(C0 x)
    {
        unchecked { return x; }
    }

    public static int TestUncheckedExplicitInt(C0 x)
    {
        unchecked { return (int)x; }
    }
}
";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
implicit C0
checked C0
implicit C0
implicit C0
implicit C0
implicit C0
implicit C0
implicit C0
").VerifyDiagnostics();
        }

        [Fact]
        public void OverloadResolution_Conversion_04()
        {
            // The IL is equivalent to
            //
            // public class C0 
            // {
            //     public static explicit operator checked long(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return 0;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        int64 op_CheckedExplicit (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] int64
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: conv.i8
        IL_000e: stloc.0
        IL_000f: br.s IL_0011

        IL_0011: ldloc.0
        IL_0012: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedExplicitLong(new C0());
        TestCheckedExplicitInt(new C0());
    }

    public static long TestCheckedExplicitLong(C0 x)
    {
        checked { return (long)x; }
    }

    public static int TestCheckedExplicitInt(C0 x)
    {
        checked { return (int)x; }
    }
}
";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
checked C0
").VerifyDiagnostics();

            var source2 =
@"
class Program
{
    static void Main()
    {
    }

    public static long TestCheckedImplicitLong(C0 x)
    {
        checked { return x; }
    }

    public static long TestUncheckedImplicitLong(C0 x)
    {
        unchecked { return x; }
    }

    public static long TestUncheckedExplicitLong(C0 x)
    {
        unchecked { return (long)x; }
    }

    public static int TestCheckedImplicitInt(C0 x)
    {
        checked { return x; }
    }

    public static int TestUncheckedImplicitInt(C0 x)
    {
        unchecked { return x; }
    }

    public static int TestUncheckedExplicitInt(C0 x)
    {
        unchecked { return (int)x; }
    }
}
";

            var compilation2 = CreateCompilationWithIL(source2, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            compilation2.VerifyDiagnostics(
                // (10,26): error CS0266: Cannot implicitly convert type 'C0' to 'long'. An explicit conversion exists (are you missing a cast?)
                //         checked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("C0", "long").WithLocation(10, 26),
                // (15,28): error CS0029: Cannot implicitly convert type 'C0' to 'long'
                //         unchecked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("C0", "long").WithLocation(15, 28),
                // (20,28): error CS0030: Cannot convert type 'C0' to 'long'
                //         unchecked { return (long)x; }
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(long)x").WithArguments("C0", "long").WithLocation(20, 28),
                // (25,26): error CS0266: Cannot implicitly convert type 'C0' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         checked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "x").WithArguments("C0", "int").WithLocation(25, 26),
                // (30,28): error CS0029: Cannot implicitly convert type 'C0' to 'int'
                //         unchecked { return x; }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("C0", "int").WithLocation(30, 28),
                // (35,28): error CS0030: Cannot convert type 'C0' to 'int'
                //         unchecked { return (int)x; }
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(int)x").WithArguments("C0", "int").WithLocation(35, 28)
                );
        }

        /// <summary>
        /// Lifted nullable
        /// </summary>
        [Fact]
        public void OverloadResolution_Conversion_05()
        {
            var source1 =
@"
public struct C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestCheckedExplicitLong(new C0()) is null ? ""null"" : ""not null"");
        System.Console.WriteLine(TestCheckedExplicitLong(null) is null ? ""null"" : ""not null"");
    }

    public static long? TestCheckedExplicitLong(C0? x)
    {
        checked { return (long?)x; }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
not null
null
").VerifyDiagnostics();

            compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
not null
null
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            compilation3.VerifyDiagnostics(
                // (12,26): error CS8936: Feature 'checked user-defined operators' is not available in C# 10.0. Please use language version 11.0 or greater.
                //         checked { return (long?)x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "(long?)x").WithArguments("checked user-defined operators", "11.0").WithLocation(12, 26)
                );
        }

        [Fact]
        public void ExpressionTree_Conversion_01()
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        return 0;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestCheckedLong());
        System.Console.WriteLine(TestUncheckedLong());
        System.Console.WriteLine(TestCheckedInt());
        System.Console.WriteLine(TestUncheckedInt());
        System.Console.WriteLine(TestCheckedNullableLong());
        System.Console.WriteLine(TestUncheckedNullableLong());
    }

    public static Expression<Func<C0, long>> TestCheckedLong()
    {
        return x => checked((long)x);
    }

    public static Expression<Func<C0, long>> TestUncheckedLong()
    {
        return x => unchecked((long)x);
    }

    public static Expression<Func<C0, int>> TestCheckedInt()
    {
        return x => checked((int)x);
    }

    public static Expression<Func<C0, int>> TestUncheckedInt()
    {
        return x => unchecked((int)x);
    }

    public static Expression<Func<C0, long?>> TestCheckedNullableLong()
    {
        return x => checked((long?)x);
    }

    public static Expression<Func<C0, long?>> TestUncheckedNullableLong()
    {
        return x => unchecked((long?)x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput:
                ExecutionConditionUtil.IsDesktop ?
@"
x => ConvertChecked(x)
x => Convert(x)
x => ConvertChecked(ConvertChecked(x))
x => Convert(Convert(x))
x => Convert(ConvertChecked(x))
x => Convert(Convert(x))
"
:
@"
x => ConvertChecked(x, Int64)
x => Convert(x, Int64)
x => ConvertChecked(ConvertChecked(x, Int64), Int32)
x => Convert(Convert(x, Int64), Int32)
x => Convert(ConvertChecked(x, Int64), Nullable`1)
x => Convert(Convert(x, Int64), Nullable`1)
"
).VerifyDiagnostics();

            verifier.VerifyIL("Program.TestCheckedLong", @"
{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_CheckedExplicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.ConvertChecked(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldloc.0
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.1
  IL_0048:  ret
}
");

            verifier.VerifyIL("Program.TestUncheckedLong", @"
{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Explicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldloc.0
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.1
  IL_0048:  ret
}
");

            verifier.VerifyIL("Program.TestCheckedInt", @"
{
  // Code size       88 (0x58)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, int>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_CheckedExplicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.ConvertChecked(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldtoken    ""int""
  IL_003a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003f:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.ConvertChecked(System.Linq.Expressions.Expression, System.Type)""
  IL_0044:  ldc.i4.1
  IL_0045:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_004a:  dup
  IL_004b:  ldc.i4.0
  IL_004c:  ldloc.0
  IL_004d:  stelem.ref
  IL_004e:  call       ""System.Linq.Expressions.Expression<System.Func<C0, int>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, int>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0053:  stloc.1
  IL_0054:  br.s       IL_0056
  IL_0056:  ldloc.1
  IL_0057:  ret
}
");

            verifier.VerifyIL("Program.TestUncheckedInt", @"
{
  // Code size       88 (0x58)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, int>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Explicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldtoken    ""int""
  IL_003a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003f:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_0044:  ldc.i4.1
  IL_0045:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_004a:  dup
  IL_004b:  ldc.i4.0
  IL_004c:  ldloc.0
  IL_004d:  stelem.ref
  IL_004e:  call       ""System.Linq.Expressions.Expression<System.Func<C0, int>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, int>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0053:  stloc.1
  IL_0054:  br.s       IL_0056
  IL_0056:  ldloc.1
  IL_0057:  ret
}
");

            verifier.VerifyIL("Program.TestCheckedNullableLong", @"
{
  // Code size       88 (0x58)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long?>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_CheckedExplicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.ConvertChecked(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldtoken    ""long?""
  IL_003a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003f:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_0044:  ldc.i4.1
  IL_0045:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_004a:  dup
  IL_004b:  ldc.i4.0
  IL_004c:  ldloc.0
  IL_004d:  stelem.ref
  IL_004e:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long?>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long?>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0053:  stloc.1
  IL_0054:  br.s       IL_0056
  IL_0056:  ldloc.1
  IL_0057:  ret
}
");

            verifier.VerifyIL("Program.TestUncheckedNullableLong", @"
{
  // Code size       88 (0x58)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long?>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Explicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldtoken    ""long?""
  IL_003a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_003f:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type)""
  IL_0044:  ldc.i4.1
  IL_0045:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_004a:  dup
  IL_004b:  ldc.i4.0
  IL_004c:  ldloc.0
  IL_004d:  stelem.ref
  IL_004e:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long?>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long?>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0053:  stloc.1
  IL_0054:  br.s       IL_0056
  IL_0056:  ldloc.1
  IL_0057:  ret
}
");
        }

        [Fact]
        public void ExpressionTree_Conversion_02()
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

public class C0 
{
    public static explicit operator long(C0 x)
    {
        return 0;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestChecked());
        System.Console.WriteLine(TestUnchecked());
    }

    public static Expression<Func<C0, long>> TestChecked()
    {
        return x => checked((long)x);
    }

    public static Expression<Func<C0, long>> TestUnchecked()
    {
        return x => unchecked((long)x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput:
                ExecutionConditionUtil.IsDesktop ?
@"
x => Convert(x)
x => Convert(x)
"
:
@"
x => Convert(x, Int64)
x => Convert(x, Int64)
"
).VerifyDiagnostics();

            verifier.VerifyIL("Program.TestChecked", @"
{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Explicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldloc.0
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.1
  IL_0048:  ret
}
");

            verifier.VerifyIL("Program.TestUnchecked", @"
{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Explicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldloc.0
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.1
  IL_0048:  ret
}
");
        }

        [Fact]
        public void ExpressionTree_Conversion_03()
        {
            var source1 =
@"
using System;
using System.Linq.Expressions;

public class C0 
{
    public static implicit operator long(C0 x)
    {
        return 0;
    }
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(TestChecked());
        System.Console.WriteLine(TestUnchecked());
    }

    public static Expression<Func<C0, long>> TestChecked()
    {
        return x => checked((long)x);
    }

    public static Expression<Func<C0, long>> TestUnchecked()
    {
        return x => unchecked((long)x);
    }
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(compilation1, expectedOutput:
                ExecutionConditionUtil.IsDesktop ?
@"
x => Convert(x)
x => Convert(x)
"
:
@"
x => Convert(x, Int64)
x => Convert(x, Int64)
"
).VerifyDiagnostics();

            verifier.VerifyIL("Program.TestChecked", @"
{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Implicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldloc.0
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.1
  IL_0048:  ret
}
");

            verifier.VerifyIL("Program.TestUnchecked", @"
{
  // Code size       73 (0x49)
  .maxstack  5
  .locals init (System.Linq.Expressions.ParameterExpression V_0,
                System.Linq.Expressions.Expression<System.Func<C0, long>> V_1)
  IL_0000:  nop
  IL_0001:  ldtoken    ""C0""
  IL_0006:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000b:  ldstr      ""x""
  IL_0010:  call       ""System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldtoken    ""long""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""long C0.op_Implicit(C0)""
  IL_0026:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_002b:  castclass  ""System.Reflection.MethodInfo""
  IL_0030:  call       ""System.Linq.Expressions.UnaryExpression System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type, System.Reflection.MethodInfo)""
  IL_0035:  ldc.i4.1
  IL_0036:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_003b:  dup
  IL_003c:  ldc.i4.0
  IL_003d:  ldloc.0
  IL_003e:  stelem.ref
  IL_003f:  call       ""System.Linq.Expressions.Expression<System.Func<C0, long>> System.Linq.Expressions.Expression.Lambda<System.Func<C0, long>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0044:  stloc.1
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.1
  IL_0048:  ret
}
");
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void Dynamic_UnaryOperators_01(string op)
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked " + op + @"(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}

public class C1 : C0
{
    public static C1 operator checked " + op + @"(C1 x)
    {
        System.Console.WriteLine(""checked C1"");
        return x;
    }

    public static C1 operator " + op + @"(C1 x)
    {
        System.Console.WriteLine(""regular C1"");
        return x;
    }
}

public class C2 : C1
{
    public static C2 operator " + op + @"(C2 x)
    {
        System.Console.WriteLine(""regular C2"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestChecked(new C0());
        TestUnchecked(new C0());
        TestChecked(new C1());
        TestUnchecked(new C1());
        TestChecked(new C2());
        TestUnchecked(new C2());
    }

    public static dynamic TestChecked(dynamic x)
    {
        return checked(" + op + @"x);
    }

    public static dynamic TestUnchecked(dynamic x)
    {
        return unchecked(" + op + @"x);
    }
}
";
            var compilation1 = CreateCompilationWithCSharp(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
regular C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            compilation1 = CreateCompilationWithCSharp(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
regular C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilationWithCSharp(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            CompileAndVerify(compilation3, expectedOutput: @"
regular C0
regular C0
regular C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void Dynamic_BinaryOperators_01(string op)
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}

public class C1 : C0
{
    public static C1 operator checked " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""checked C1"");
        return x;
    }

    public static C1 operator " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""regular C1"");
        return x;
    }
}

public class C2 : C1
{
    public static C2 operator " + op + @"(C2 x, C2 y)
    {
        System.Console.WriteLine(""regular C2"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestChecked(new C0());
        TestUnchecked(new C0());
        TestChecked(new C1());
        TestUnchecked(new C1());
        TestChecked(new C2());
        TestUnchecked(new C2());
    }

    public static dynamic TestChecked(dynamic x)
    {
        return checked(x " + op + @" x);
    }

    public static dynamic TestUnchecked(dynamic x)
    {
        return unchecked(x " + op + @" x);
    }
}
";
            var compilation1 = CreateCompilationWithCSharp(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
regular C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            compilation1 = CreateCompilationWithCSharp(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
regular C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilationWithCSharp(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            CompileAndVerify(compilation3, expectedOutput: @"
regular C0
regular C0
regular C1
regular C1
regular C2
regular C2
").VerifyDiagnostics();
        }

        [Fact]
        public void Dynamic_Conversion_01()
        {
            var source1 =
@"
public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }

    public static implicit operator int(C0 x)
    {
        System.Console.WriteLine(""implicit C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedImplicitLong(new C0());
        TestCheckedExplicitLong(new C0());
        TestUncheckedImplicitLong(new C0());
        TestUncheckedExplicitLong(new C0());
        TestCheckedImplicitInt(new C0());
        TestCheckedExplicitInt(new C0());
        TestUncheckedImplicitInt(new C0());
        TestUncheckedExplicitInt(new C0());
    }

    public static long TestCheckedImplicitLong(dynamic x)
    {
        checked { return x; }
    }

    public static long TestCheckedExplicitLong(dynamic x)
    {
        checked { return (long)x; }
    }

    public static long TestUncheckedImplicitLong(dynamic x)
    {
        unchecked { return x; }
    }

    public static long TestUncheckedExplicitLong(dynamic x)
    {
        unchecked { return (long)x; }
    }

    public static int TestCheckedImplicitInt(dynamic x)
    {
        checked { return x; }
    }

    public static int TestCheckedExplicitInt(dynamic x)
    {
        checked { return (int)x; }
    }

    public static int TestUncheckedImplicitInt(dynamic x)
    {
        unchecked { return x; }
    }

    public static int TestUncheckedExplicitInt(dynamic x)
    {
        unchecked { return (int)x; }
    }
}
";
            var compilation1 = CreateCompilationWithCSharp(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
implicit C0
regular C0
implicit C0
regular C0
implicit C0
implicit C0
implicit C0
implicit C0
").VerifyDiagnostics();

            compilation1 = CreateCompilationWithCSharp(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular11);
            CompileAndVerify(compilation1, expectedOutput: @"
implicit C0
regular C0
implicit C0
regular C0
implicit C0
implicit C0
implicit C0
implicit C0
").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilationWithCSharp(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular10, references: new[] { compilation2.ToMetadataReference() });
            CompileAndVerify(compilation3, expectedOutput: @"
implicit C0
regular C0
implicit C0
regular C0
implicit C0
implicit C0
implicit C0
implicit C0
").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("-")]
        [InlineData("++")]
        [InlineData("--")]
        public void Matching_UnaryOperators_01(string op)
        {
            var source1 =
@"
#nullable enable

public class C0 
{
    public static C0 operator checked " + op + @"(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0? operator " + op + @"(C0? x)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(" + op + @"x);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview, references: new[] { compilation2.EmitToImageReference() });
            CompileAndVerify(compilation3, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Fact]
        public void Matching_UnaryOperators_02()
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static modopt(System.Object) C0 operator checked -(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            //   
            //     public static C0 operator -(modopt(System.Object) C0 x)
            //     {
            //         System.Console.WriteLine(""regular C0"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 modopt(System.Object) op_CheckedUnaryNegation (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname static 
        class C0 op_UnaryNegation (
            class C0 modopt(System.Object) x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""regular C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(-x);
    }
}
";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void Matching_BinaryOperators_01(string op)
        {
            var source1 =
@"
#nullable enable

public class C0 
{
    public static (C0 a, object b, nint c)  operator checked " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""checked C0"");
        return default;
    }

    public static (C0? d, dynamic e, System.IntPtr f) operator " + op + @"(C0 x, C0 y)
    {
        System.Console.WriteLine(""regular C0"");
        return default;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0(), new C0());
    }

    public static object TestCheckedC0(C0 x, C0 y)
    {
        return checked(x " + op + @" y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview, references: new[] { compilation2.EmitToImageReference() });
            CompileAndVerify(compilation3, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void Matching_BinaryOperators_02(string op)
        {
            var source1 =
@"
#nullable enable

public class C0 
{
    public static C0  operator checked " + op + @"((C0 a, object b, nint c) x, C0 y)
    {
        System.Console.WriteLine(""checked C0"");
        return y;
    }

    public static C0 operator " + op + @"((C0? d, dynamic e, System.IntPtr f) x, C0 y)
    {
        System.Console.WriteLine(""regular C0"");
        return y;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(default, new C0());
    }

    public static object TestCheckedC0((C0, object, nint) x, C0 y)
    {
        return checked(x " + op + @" y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview, references: new[] { compilation2.EmitToImageReference() });
            CompileAndVerify(compilation3, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("*")]
        [InlineData("/")]
        public void Matching_BinaryOperators_03(string op)
        {
            var source1 =
@"
#nullable enable

public class C0 
{
    public static C0  operator checked " + op + @"(C0 x, (C0 a, object b, nint c) y)
    {
        System.Console.WriteLine(""checked C0"");
        return x;
    }

    public static C0 operator " + op + @"(C0 x, (C0? d, dynamic e, System.IntPtr f) y)
    {
        System.Console.WriteLine(""regular C0"");
        return x;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0(), default);
    }

    public static object TestCheckedC0(C0 x, (C0, object, nint) y)
    {
        return checked(x " + op + @" y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview, references: new[] { compilation2.EmitToImageReference() });
            CompileAndVerify(compilation3, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("+", "op_Addition", "op_CheckedAddition")]
        [InlineData("-", "op_Subtraction", "op_CheckedSubtraction")]
        [InlineData("*", "op_Multiply", "op_CheckedMultiply")]
        [InlineData("/", "op_Division", "op_CheckedDivision")]
        public void Matching_BinaryOperators_04(string op, string name, string checkedName)
        {
            // The IL is equivalent to
            //
            // class C0 
            // {
            //     public static modopt(System.Object) C0 operator checked -(C0 x, modopt(System.Object) C0 y)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return x;
            //     }
            //   
            //     public static C0 operator -(modopt(System.Object) C0 x, C0 y)
            //     {
            //         System.Console.WriteLine(""regular C0"");
            //         return x;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        class C0 modopt(System.Object) " + checkedName + @" (
            class C0 x,
            class C0 modopt(System.Object) y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname static 
        class C0 " + name + @" (
            class C0 modopt(System.Object) x,
            class C0 y
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] class C0
        )

        IL_0000: nop
        IL_0001: ldstr ""regular C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldarg.0
        IL_000d: stloc.0
        IL_000e: br.s IL_0010

        IL_0010: ldloc.0
        IL_0011: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedC0(new C0());
    }

    public static C0 TestCheckedC0(C0 x)
    {
        return checked(x " + op + @" x);
    }
}";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Fact]
        public void Matching_Conversion_01()
        {
            var source1 =
@"
#nullable enable

public class C0 
{
    public static explicit operator checked (C0 a, object b, nint c)(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return default;
    }

    public static explicit operator (C0? d, dynamic e, System.IntPtr f)(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return default;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedExplicitLong(new C0());
    }

    public static (C0, object, nint) TestCheckedExplicitLong(C0 x)
    {
        checked { return ((C0, object, nint))x; }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview, references: new[] { compilation2.EmitToImageReference() });
            CompileAndVerify(compilation3, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Fact]
        public void Matching_Conversion_02()
        {
            var source1 =
@"
#nullable enable

public class C0 
{
    public static explicit operator checked C0((C0 a, object b, nint c) x)
    {
        System.Console.WriteLine(""checked C0"");
        return new C0();
    }

    public static explicit operator C0((C0? d, dynamic e, System.IntPtr f) x)
    {
        System.Console.WriteLine(""regular C0"");
        return new C0();
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestCheckedExplicitLong(default);
    }

    public static C0 TestCheckedExplicitLong((C0, object, nint) x)
    {
        checked { return (C0)x; }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugDll, parseOptions: TestOptions.RegularPreview);
            var compilation3 = CreateCompilation(source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview, references: new[] { compilation2.EmitToImageReference() });
            CompileAndVerify(compilation3, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Fact]
        public void Matching_Conversion_03()
        {
            // The IL is equivalent to
            //
            // public class C0 
            // {
            //     public static explicit operator checked modopt(System.Object) long(C0 x)
            //     {
            //         System.Console.WriteLine(""checked C0"");
            //         return 0;
            //     }
            //
            //     public static explicit operator long(modopt(System.Object) C0 x)
            //     {
            //         System.Console.WriteLine(""regular C0"");
            //         return 0;
            //     }
            // }

            var ilSource = @"
.class public auto ansi beforefieldinit C0
    extends System.Object
{
    .method public hidebysig specialname static 
        int64 modopt(System.Object) op_CheckedExplicit (
            class C0 x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] int64
        )

        IL_0000: nop
        IL_0001: ldstr ""checked C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: conv.i8
        IL_000e: stloc.0
        IL_000f: br.s IL_0011

        IL_0011: ldloc.0
        IL_0012: ret
    }

    .method public hidebysig specialname static 
        int64 op_Explicit (
            class C0 modopt(System.Object) x
        ) cil managed 
    {
        .maxstack 1
        .locals init (
            [0] int64
        )

        IL_0000: nop
        IL_0001: ldstr ""regular C0""
        IL_0006: call void [mscorlib]System.Console::WriteLine(string)
        IL_000b: nop
        IL_000c: ldc.i4.0
        IL_000d: conv.i8
        IL_000e: stloc.0
        IL_000f: br.s IL_0011

        IL_0011: ldloc.0
        IL_0012: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    }
}
";

            var source1 =
@"
class Program
{
    static void Main()
    {
        TestCheckedExplicitLong(new C0());
    }

    public static long TestCheckedExplicitLong(C0 x)
    {
        checked { return (long)x; }
    }
}
";

            var compilation1 = CreateCompilationWithIL(source1, ilSource, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"checked C0").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(60419, "https://github.com/dotnet/roslyn/issues/60419")]
        public void ClassifyConversion_01()
        {
            var source1 =
@"
public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestExplicitLong1(new C0());
        TestExplicitLong2(new C0());
    }

    public static long TestExplicitLong1(C0 x)
    {
        checked { return (long)x; }
    }

    public static long TestExplicitLong2(C0 y)
    {
        return checked( (long)y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
checked C0
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);

            var xNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var yNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.GetSymbolInfo(xNode.Parent).Symbol.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.GetSymbolInfo(yNode.Parent).Symbol.ToTestDisplayString());

            var int64 = ((IMethodSymbol)model.GetSymbolInfo(xNode.Parent).Symbol).ReturnType;
            Assert.Equal("System.Int64", int64.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(xNode.SpanStart, xNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(xNode.SpanStart, xNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(yNode.SpanStart, yNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(yNode.SpanStart, yNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(xNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(xNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(yNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_CheckedExplicit(C0 x)", model.ClassifyConversion(yNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());
        }

        [Fact]
        public void ClassifyConversion_02()
        {
            var source1 =
@"
public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestExplicitLong1(new C0());
        TestExplicitLong2(new C0());
    }

    public static long TestExplicitLong1(C0 x)
    {
        unchecked { return (long)x; }
    }

    public static long TestExplicitLong2(C0 y)
    {
        return unchecked( (long)y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);

            var xNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var yNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.GetSymbolInfo(xNode.Parent).Symbol.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.GetSymbolInfo(yNode.Parent).Symbol.ToTestDisplayString());

            var int64 = ((IMethodSymbol)model.GetSymbolInfo(xNode.Parent).Symbol).ReturnType;
            Assert.Equal("System.Int64", int64.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode.SpanStart, xNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode.SpanStart, xNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode.SpanStart, yNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode.SpanStart, yNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(60419, "https://github.com/dotnet/roslyn/issues/60419")]
        public void ClassifyConversion_03()
        {
            var source1 =
@"
public class C0 
{
    public static explicit operator checked long(C0 x)
    {
        System.Console.WriteLine(""checked C0"");
        return 0;
    }

    public static explicit operator long(C0 x)
    {
        System.Console.WriteLine(""regular C0"");
        return 0;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        TestExplicitLong1(new C0());
        TestExplicitLong2(new C0());
    }

    public static long TestExplicitLong1(C0 x)
    {
        checked { unchecked { return (long)x; }}
    }

    public static long TestExplicitLong2(C0 y)
    {
        checked { return unchecked( (long)y); }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);

            var xNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var yNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.GetSymbolInfo(xNode.Parent).Symbol.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.GetSymbolInfo(yNode.Parent).Symbol.ToTestDisplayString());

            var int64 = ((IMethodSymbol)model.GetSymbolInfo(xNode.Parent).Symbol).ReturnType;
            Assert.Equal("System.Int64", int64.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode.SpanStart, xNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode.SpanStart, xNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode.SpanStart, yNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode.SpanStart, yNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(xNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());

            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode, int64, isExplicitInSource: false).Method.ToTestDisplayString());
            Assert.Equal("System.Int64 C0.op_Explicit(C0 x)", model.ClassifyConversion(yNode, int64, isExplicitInSource: true).Method.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(60419, "https://github.com/dotnet/roslyn/issues/60419")]
        public void GetSpeculativeSymbolInfo_01()
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked -(C0 a)
    {
        System.Console.WriteLine(""checked C0"");
        return a;
    }

    public static C0 operator -(C0 a)
    {
        System.Console.WriteLine(""regular C0"");
        return a;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        Test1(new C0());
        Test2(new C0());
    }

    public static C0 Test1(C0 x)
    {
        checked { return -x; }
    }

    public static C0 Test2(C0 y)
    {
        return checked( -y);
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
checked C0
checked C0
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);

            var xNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var yNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("C0 C0.op_CheckedUnaryNegation(C0 a)", model.GetSymbolInfo(xNode.Parent).Symbol.ToTestDisplayString());
            Assert.Equal("C0 C0.op_CheckedUnaryNegation(C0 a)", model.GetSymbolInfo(yNode.Parent).Symbol.ToTestDisplayString());

            var xNodeToSpeculate = SyntaxFactory.ParseExpression("-x");
            var yNodeToSpeculate = SyntaxFactory.ParseExpression("-y");

            Assert.Equal("C0 C0.op_CheckedUnaryNegation(C0 a)", model.GetSpeculativeSymbolInfo(xNode.SpanStart, xNodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());
            Assert.Equal("C0 C0.op_CheckedUnaryNegation(C0 a)", model.GetSpeculativeSymbolInfo(yNode.SpanStart, yNodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(60419, "https://github.com/dotnet/roslyn/issues/60419")]
        public void GetSpeculativeSymbolInfo_02()
        {
            var source1 =
@"
public class C0 
{
    public static C0 operator checked -(C0 a)
    {
        System.Console.WriteLine(""checked C0"");
        return a;
    }

    public static C0 operator -(C0 a)
    {
        System.Console.WriteLine(""regular C0"");
        return a;
    }
}
";
            var source2 =
@"
class Program
{
    static void Main()
    {
        Test1(new C0());
        Test2(new C0());
    }

    public static C0 Test1(C0 x)
    {
        checked { unchecked { return -x; } }
    }

    public static C0 Test2(C0 y)
    {
        checked { return unchecked( -y); }
    }
}
";
            var compilation1 = CreateCompilation(source1 + source2, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: @"
regular C0
regular C0
").VerifyDiagnostics();

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);

            var xNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x").Single();
            var yNode = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").Single();

            Assert.Equal("C0 C0.op_UnaryNegation(C0 a)", model.GetSymbolInfo(xNode.Parent).Symbol.ToTestDisplayString());
            Assert.Equal("C0 C0.op_UnaryNegation(C0 a)", model.GetSymbolInfo(yNode.Parent).Symbol.ToTestDisplayString());

            var xNodeToSpeculate = SyntaxFactory.ParseExpression("-x");
            var yNodeToSpeculate = SyntaxFactory.ParseExpression("-y");

            Assert.Equal("C0 C0.op_UnaryNegation(C0 a)", model.GetSpeculativeSymbolInfo(xNode.SpanStart, xNodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());
            Assert.Equal("C0 C0.op_UnaryNegation(C0 a)", model.GetSpeculativeSymbolInfo(yNode.SpanStart, yNodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());
        }
    }
}
