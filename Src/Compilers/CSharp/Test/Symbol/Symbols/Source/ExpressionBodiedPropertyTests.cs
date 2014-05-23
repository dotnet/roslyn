// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public sealed class ExpressionBodiedPropertyTests : CSharpTestBase
    {
        [Fact]
        public void Syntax01()
        {
            // No experimental LanguageVersion
            var comp = CreateCompilationWithMscorlib(@"
class C
{
    public int P => 1;
}");
            comp.VerifyDiagnostics(
    // (4,18): error CS1003: Syntax error, ',' expected
    //     public int P => 1;
    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(4, 18),
    // (4,16): warning CS0649: Field 'C.P' is never assigned to, and will always have its default value 0
    //     public int P => 1;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "P").WithArguments("C.P", "0").WithLocation(4, 16));
        }

        [Fact]
        public void Syntax02()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P { get; } => 1;
}");
            comp.VerifyDiagnostics(
    // (4,16): error CS8055: Properties cannot combine accessor lists with expression bodies.
    //     public int P { get; } => 1;
    Diagnostic(ErrorCode.ERR_AccessorListAndExpressionBody, "P").WithArguments("C.P").WithLocation(4, 16),
    // (4,20): error CS0501: 'C.P.get' must declare a body because it is not marked abstract, extern, or partial
    //     public int P { get; } => 1;
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "get").WithArguments("C.P.get").WithLocation(4, 20));
        }

        [Fact]
        public void Syntax03()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
interface C
{
    int P => 1;
}");
            comp.VerifyDiagnostics(
    // (4,14): error CS0531: 'C.P.get': interface members cannot have a definition
    //     int P => 1;
    Diagnostic(ErrorCode.ERR_InterfaceMemberHasBody, "1").WithArguments("C.P.get").WithLocation(4, 14));
        }

        [Fact]
        public void Syntax04()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
abstract class C
{
  public abstract int P => 1;
}");
            comp.VerifyDiagnostics(
    // (4,28): error CS0500: 'C.P.get' cannot declare a body because it is marked abstract
    //   public abstract int P => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "1").WithArguments("C.P.get").WithLocation(4, 28));
        }

        [Fact]
        public void Syntax05()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
   public abstract int P => 1;
}");
            comp.VerifyDiagnostics(
    // (4,29): error CS0500: 'C.P.get' cannot declare a body because it is marked abstract
    //    public abstract int P => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "1").WithArguments("C.P.get").WithLocation(4, 29),
    // (4,29): error CS0513: 'C.P.get' is abstract but it is contained in non-abstract class 'C'
    //    public abstract int P => 1;
    Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "1").WithArguments("C.P.get", "C").WithLocation(4, 29));
        }

        [Fact]
        public void Syntax06()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
abstract class C
{
   abstract int P => 1;
}");
            comp.VerifyDiagnostics(
    // (4,17): error CS0621: 'C.P': virtual or abstract members cannot be private
    //    abstract int P => 1;
    Diagnostic(ErrorCode.ERR_VirtualPrivate, "P").WithArguments("C.P").WithLocation(4, 17),
    // (4,22): error CS0500: 'C.P.get' cannot declare a body because it is marked abstract
    //    abstract int P => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "1").WithArguments("C.P.get").WithLocation(4, 22));
        }

        [Fact]
        public void Syntax07()
        {
            // The '=' here parses as part of the expression body, not the property
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P => 1 = 2;
}");
            comp.VerifyDiagnostics(
    // (4,21): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
    //     public int P => 1 = 2;
    Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "1").WithLocation(4, 21));
        }

        [Fact]
        public void LambdaTest01()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
using System;
class C
{
    public Func<int, Func<int, int>> P => x => y => x + y;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SimpleTest()
        {
            var text = @"
class C
{
    public int P => 2 * 2;
}";
            var comp = CreateExperimentalCompilationWithMscorlib45(text);
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<SourcePropertySymbol>("P");
            Assert.Null(p.SetMethod);
            Assert.NotNull(p.GetMethod);
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
        }

        [Fact]
        public void Override01()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class B
{
    public virtual int P { get; set; }
}
class C : B
{
    public override int P => 1;
}").VerifyDiagnostics();

        }

        [Fact]
        public void VoidExpression()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public void P => System.Console.WriteLine(""foo"");
}").VerifyDiagnostics(
    // (4,17): error CS0547: 'C.P': property or indexer cannot have void type
    //     public void P => System.Console.WriteLine("foo");
    Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "P").WithArguments("C.P").WithLocation(4, 17));
        }

        [Fact]
        public void VoidExpression2()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
class C
{
    public int P => System.Console.WriteLine(""foo"");
}").VerifyDiagnostics(
    // (4,21): error CS0029: Cannot implicitly convert type 'void' to 'int'
    //     public int P => System.Console.WriteLine("foo");
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"System.Console.WriteLine(""foo"")").WithArguments("void", "int").WithLocation(4, 21));
        }

        [Fact]
        public void InterfaceImplementation01()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
interface I 
{
    int P { get; }
    string Q { get; }
}
internal interface J
{
    string Q { get; }
}
internal interface K
{
    decimal D { get; }
}
class C : I, J, K
{
    public int P => 10;
    string I.Q { get { return ""foo""; } }
    string J.Q { get { return ""bar""; } }
    public decimal D { get { return P; } }
}");
            comp.VerifyDiagnostics();
            var global = comp.GlobalNamespace;
            var i = global.GetTypeMember("I");
            var j = global.GetTypeMember("J");
            var k = global.GetTypeMember("K");
            var c = global.GetTypeMember("C");

            var iP = i.GetMember<SourcePropertySymbol>("P");

            var prop = c.GetMember<SourcePropertySymbol>("P");
            Assert.True(prop.IsReadOnly);
            var implements = prop.ContainingType.FindImplementationForInterfaceMember(iP);
            Assert.Equal(prop, implements);

            prop = c.GetMember<SourcePropertySymbol>("I.Q");
            Assert.True(prop.IsReadOnly);
            Assert.True(prop.IsExplicitInterfaceImplementation);

            prop = c.GetMember<SourcePropertySymbol>("J.Q");
            Assert.True(prop.IsReadOnly);
            Assert.True(prop.IsExplicitInterfaceImplementation);

            prop = c.GetMember<SourcePropertySymbol>("D");
            Assert.True(prop.IsReadOnly);
        } 

        [Fact]
        public void Emit01()
        {
            var comp = CreateExperimentalCompilationWithMscorlib45(@"
abstract class A
{
    protected abstract string Z { get; }
}
abstract class B : A
{
    protected sealed override string Z => ""foo"";
    protected abstract string Y { get; }
}    
class C : B
{
    public const int X = 2;
    public static int P => C.X * C.X;
    
    public int Q => X;
    private int R => P * Q;
    protected sealed override string Y => Z + R;

    public static void Main()
    {
        System.Console.WriteLine(C.X);
        System.Console.WriteLine(C.P);
        var c = new C();
        
        System.Console.WriteLine(c.Q);
        System.Console.WriteLine(c.R);
        System.Console.WriteLine(c.Z);
        System.Console.WriteLine(c.Y);
    }
}", compOptions: TestOptions.ExeAlwaysImportInternals);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"2
4
2
8
foo
foo8");
        }
    }
}
