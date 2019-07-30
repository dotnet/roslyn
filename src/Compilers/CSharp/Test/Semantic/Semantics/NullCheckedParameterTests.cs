// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) null-checked variables and lambdas.
    /// </summary>
    public class NullCheckedParameterTests : CompilingTestBase
    {
        [Fact]
        public void NullCheckedDelegateDeclaration()
        {
            var source = @"
delegate void Del(string x!, int y);
class C
{
    Del d = delegate(string k!, int j) { /* ... */ };
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (2,26): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(2, 26));
        }

        [Fact]
        public void NullCheckedAbstractMethod()
        {
            var source = @"
abstract class C
{
    abstract public int M(string x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,34): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     abstract public int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 34));
        }

        [Fact]
        public void NullCheckedInterfaceMethod()
        {
            var source = @"
interface C
{
    public int M(string x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,25): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 25));
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullCheckedInterfaceMethod2()
        {
            var source = @"
interface C
{
    public void M(string x!) { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandardLatest,
                                                         parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedAutoProperty()
        {
            var source = @"
abstract class C
{
    string FirstName! { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,12): warning CS0169: The field 'C.FirstName' is never used
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "FirstName").WithArguments("C.FirstName").WithLocation(4, 12),
                    // (4,21): error CS1003: Syntax error, ',' expected
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(4, 21),
                    // (4,25): error CS1002: ; expected
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "get").WithLocation(4, 25),
                    // (4,28): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                    // (4,28): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                    // (4,33): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 33),
                    // (4,33): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 33),
                    // (5,1): error CS1022: Type or namespace definition, or end-of-file expected
                    // }
                    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(5, 1));
        }

        [Fact]
        public void NullCheckedPartialMethod()
        {
            var source = @"
partial class C
{
    partial void M(string x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,27): error CS8717: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     partial void M(string x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 27));
        }

        [Fact]
        public void NullCheckedInterfaceProperty()
        {
            var source = @"
interface C
{
    public string this[string index!] { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,31): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public string this[int index!] { get; set; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 31));
        }

        [Fact]
        public void NullCheckedAbstractProperty()
        {
            var source = @"
abstract class C
{
    public abstract string this[string index!] { get; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,40): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public abstract string this[int index!] { get; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 40));
        }

        [Fact]
        public void NullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[string index!] => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourcePropertySymbol>("this[]");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedExternMethod()
        {
            var source = @"
using System.Runtime.InteropServices;
class C
{
    [DllImport(""User32.dll"")]
    public static extern int M(string x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (6,39): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public static extern int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(6, 39));
        }

        [Fact]
        public void NullCheckedMethodDeclaration()
        {
            var source = @"
class C
{
    void M(string name!) { }
    void M2(string x) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);

            var m2 = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M2");
            Assert.False(((SourceParameterSymbol)m2.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void FailingNullCheckedArgList()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1, 'M'));
    }
    void M2(__arglist!)
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics( 
                    // (8,22): error CS1003: Syntax error, ',' expected
                    //     void M2(__arglist!)
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(8, 22));
        }

        [Fact]
        public void NullCheckedArgList()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1!, 'M'));
    }
    void M2(__arglist)
    {
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalNullParameter()
        {
            var source = @"
class C
{
    void M(string name! = null) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (4,19): warning CS8719: Parameter 'name' is null-checked but is null by default.
                    //     void M(string name! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("name").WithLocation(4, 19));
                                var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalStringLiteralParameter()
        {
            var source = @"
class C
{
    void M(string name! = ""rose"") { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalParameterSplitBySpace()
        {
            var source = @"
class C
{
    void M(string name! =null) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (4,19): warning CS8719: Parameter 'name' is null-checked but is null by default.
                    //     void M(string name! =null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("name").WithLocation(4, 19));
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedConstructor()
        {
            var source = @"
class C
{
    public C(string name!) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>(".ctor");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedOperator()
        {
            var source = @"
class Box 
{ 
    public static int operator+ (Box b!, Box c)  
    { 
        return 2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("Box").GetMember<SourceUserDefinedOperatorSymbol>("op_Addition");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)m.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x! => x + ""1"";
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            SimpleLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                    .GetMember<SourceMethodSymbol>("M")
                                                                    .GetNonNullSyntaxNode()
                                                                    .DescendantNodes()
                                                                    .OfType<SimpleLambdaExpressionSyntax>()
                                                                    .Single();
            var lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaWithMultipleParameters()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, bool> func1 = (x!, y) => x == y;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            var lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)lambdaSymbol.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaSingleParameterInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = (x!) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            Syntax.ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                                  .GetMember<SourceMethodSymbol>("M")
                                                                                  .GetNonNullSyntaxNode()
                                                                                  .DescendantNodes()
                                                                                  .OfType<Syntax.ParenthesizedLambdaExpressionSyntax>()
                                                                                  .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaSingleParameterNoSpaces()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x!=> x;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (7,39): error CS8713: Space required between '!' and '=' here.
                    //         Func<int, int> func1 = x!=> x;
                    Diagnostic(ErrorCode.ERR_NeedSpaceBetweenExclamationAndEquals, "!=").WithLocation(7, 39));
        }

        [Fact]
        public void NullCheckedLambdaSingleTypedParameterInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = (string x!) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaManyTypedParametersInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (string x!, string y) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)lambdaSymbol.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void LambdaManyNullCheckedParametersInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (string x!, string y!) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                           .GetMember<SourceMethodSymbol>("M")
                                                           .GetNonNullSyntaxNode()
                                                           .DescendantNodes()
                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedDiscard()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, int> func1 = (_!) => 42;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void TestNullCheckedOutString()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(out string x!)
    {
        x = ""hello world"";
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (5,31): error CS8720: By-reference parameter 'x' cannot be null-checked.
                    //     public void M(out string x!)
                    Diagnostic(ErrorCode.ERR_NullCheckingOnByRefParameter, "!").WithArguments("x").WithLocation(5, 31));
        }

        [Fact]
        public void TestNullCheckedRefString()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(ref string x!)
    {
        x = ""hello world"";
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (5,31): error CS8720: By-reference parameter 'x' cannot be null-checked.
                    //     public void M(ref string x!)
                    Diagnostic(ErrorCode.ERR_NullCheckingOnByRefParameter, "!").WithArguments("x").WithLocation(5, 31));
        }

        [Fact]
        public void TestNullCheckedInString()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(in string x!) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (5,30): error CS8720: By-reference parameter 'x' cannot be null-checked.
                    //     public void M(in string x!) { }
                    Diagnostic(ErrorCode.ERR_NullCheckingOnByRefParameter, "!").WithArguments("x").WithLocation(5, 30));
        }

        [Fact]
        public void TestNullCheckedGenericWithDefault()
        {
            var source = @"
class C
{
    static void M1<T>(T t! = default) { }
    static void M2<T>(T? t! = default) where T : struct { }
    static void M3<T>(T t! = default) where T : class { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (5,26): warning CS8721: Nullable value type 'T?' is null-checked and will throw if null.
                    //     static void M2<T>(T? t! = default) where T : struct { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableValueType, "t").WithArguments("T?").WithLocation(5, 26),
                    // (6,25): warning CS8719: Parameter 't' is null-checked but is null by default.
                    //     static void M3<T>(T t! = default) where T : class { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "t").WithArguments("t").WithLocation(6, 25));
        }

        [Fact]
        public void TestNullableInteraction()
        {
            var source = @"
class C
{
    static void M(int? i!) { }
    public static void Main() { }
}";
            // Release
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (4,24): error CS8721: Nullable value type 'int?' is null-checked and will throw if null.
                    //     static void M(int? i!) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableValueType, "i").WithArguments("int?").WithLocation(4, 24));
        }

        [Fact]
        public void TestNullableGenericsImplementingGenericAbstractClass()
        {
            var source = @"
abstract class A<T>
{
    internal abstract void F<U>(U u) where U : T;
}
class B1 : A<object>
{
    internal override void F<U>(U u! = default) { }
}
class B2 : A<string>
{
    internal override void F<U>(U u! = default) { }
}
class B3 : A<int?>
{
    internal override void F<U>(U u! = default) { }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (12,35): warning CS8719: Parameter 'u' is null-checked but is null by default.
                    //     internal override void F<U>(U u! = default) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "u").WithArguments("u").WithLocation(12, 35),
                    // (16,35): warning CS8721: Nullable value type 'U' is null-checked and will throw if null.
                    //     internal override void F<U>(U u! = default) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableValueType, "u").WithArguments("U").WithLocation(16, 35));
        }

        [Fact]
        public void NoGeneratedNullCheckIfNonNullableTest()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = x! => x;
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                    // (7,32): error CS8718: Parameter 'int' is a non-nullable value type and therefore cannot be null-checked.
                    //         Func<int, int> func1 = x! => x;
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "x").WithArguments("int").WithLocation(7, 32));
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalNullParameter()
        {
            var source = @"
class C
{
    public static void Main() { }
    void M(string name! = null) { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (5,19): warning CS8719: Parameter 'name' is null-checked but is null by default.
                    //     void M(string name! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("name").WithLocation(5, 19));
        }

        [Fact]
        public void TestManyNullCheckedArgs()
        {
            var source = @"
class C
{
    public void M(int x!, string y!) { }
    public static void Main() { }
}";

            CreateCompilation(source).VerifyDiagnostics(
                    // (4,23): error CS8718: Parameter 'int' is a non-nullable value type and therefore cannot be null-checked.
                    //     public void M(int x!, string y!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "x").WithArguments("int").WithLocation(4, 23));
        }

        [Fact]
        public void TestNullCheckedSubstitutionWithDiagnostic1()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B2<T> : A<T> where T : struct
{
    internal override void M<U>(U u!) { }
}";

            CreateCompilation(source).VerifyDiagnostics(
                    // (8,35): error CS8718: Parameter 'U' is a non-nullable value type and therefore cannot be null-checked.
                    //     internal override void M<U>(U u!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "u").WithArguments("U").WithLocation(8, 35));
        }

        [Fact]
        public void TestNullCheckedSubstitutionWithDiagnostic2()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!) where U : T { }
}
class B4 : A<int>
{
    internal override void M<U>(U u!) { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (8,35): error CS8718: Parameter 'U' is a non-nullable value type and therefore cannot be null-checked.
                    //     internal override void M<U>(U u!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "u").WithArguments("U").WithLocation(8, 35));
        }

        [Fact]
        public void TestNullCheckedSubstitutionWithDiagnostic3()
        {
            var source = @"
class C
{
    void M<T>(T value!) where T : unmanaged { }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,17): error CS8718: Parameter 'T' is a non-nullable value type and cannot be null-checked.
                    //     void M<T>(T value!) where T : unmanaged { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "value").WithArguments("T").WithLocation(4, 17));
        }

        [Fact]
        public void TestNullCheckedNullableValueTypeInLocalFunction()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M((int?)5);
        void M(int? x!) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (7,21): warning CS8721: Nullable value type 'int?' is null-checked and will throw if null.
                    //         void M(int? x!) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableValueType, "x").WithArguments("int?").WithLocation(7, 21));
        }

        [Fact]
        public void TestNullCheckedParameterWithDefaultNullValueInLocalFunction()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(""ok"");
        void M(string x! = null) { }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (7,23): warning CS8719: Parameter 'x' is null-checked but is null by default.
                    //         void M(string x! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "x").WithArguments("x").WithLocation(7, 23));
        }

        [Fact]
        public void TestNullCheckedWithMissingType()
        {
            var source =
@"
class Program
{
    static void Main(string[] args!) { }
}";
            var comp = CreateCompilation(source);
            comp.MakeMemberMissing(WellKnownMember.System_ArgumentNullException__ctorString);
            comp.MakeTypeMissing(WellKnownType.System_ArgumentNullException);
            comp.VerifyDiagnostics(
                    // (4,31): error CS0656: Missing compiler required member 'System.ArgumentNullException..ctor'
                    //     static void Main(string[] args!) { }
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "args").WithArguments("System.ArgumentNullException", ".ctor").WithLocation(4, 31));
        }

        [Fact]
        public void TestNullCheckedMethodParameterWithWrongLanguageVersion()
        {
            var source =
@"
class Program
{
    void M(string x!) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                    // (4,20): error CS8059: Feature '!' is not available in C# 6. Please use language version CSharp8 or greater.
                    //     static void Main(string x!) { }
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "!").WithArguments("!", "CSharp8").WithLocation(4, 20));
        }

        [Fact]
        public void TestNullCheckedLambdaParameterWithWrongLanguageVersion()
        {
            var source =
@"
using System;
class Program
{
    void M()
    {
        Func<string, string> func = x! => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                    // (7,38): error CS8059: Feature '!' is not available in C# 6. Please use language version CSharp8 or greater.
                    //         Func<string, string> func = x! => x;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "!").WithArguments("!", "CSharp8").WithLocation(7, 38));
        }

        [Fact]
        public void TestNullCheckedLambdaParametersWithWrongLanguageVersion()
        {
            var source =
@"
using System;
class Program
{
    void M()
    {
        Func<string, string, string> func = (x!, y) => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                    // (7,47): error CS8059: Feature '!' is not available in C# 6. Please use language version CSharp8 or greater.
                    //         Func<string, string, string> func = (x!, y) => x;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "!").WithArguments("!", "CSharp8").WithLocation(7, 47));
        }
    }
}
