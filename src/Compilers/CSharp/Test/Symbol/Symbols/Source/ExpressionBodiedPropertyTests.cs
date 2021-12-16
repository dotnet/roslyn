// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public sealed class ExpressionBodiedPropertyTests : CSharpTestBase
    {
        [Fact(Skip = "973907")]
        public void Syntax01()
        {
            // Language feature enabled by default
            var comp = CreateCompilation(@"
class C
{
    public int P => 1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Syntax02()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int P { get; } => 1;
}");
            comp.VerifyDiagnostics(
    // (4,5): error CS8056: Properties cannot combine accessor lists with expression bodies.
    //     public int P { get; } => 1;
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "public int P { get; } => 1;").WithLocation(4, 5)
    );
        }

        [Fact]
        public void Syntax03()
        {
            var comp = CreateCompilation(@"
interface C
{
    int P => 1;
}", parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,14): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     int P => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "1").WithArguments("default interface implementation", "8.0").WithLocation(4, 14)
                );
        }

        [Fact]
        public void Syntax04()
        {
            var comp = CreateCompilationWithMscorlib45(@"
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
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
   public abstract int P => 1;
}");
            comp.VerifyDiagnostics(
    // (4,29): error CS0500: 'C.P.get' cannot declare a body because it is marked abstract
    //    public abstract int P => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "1").WithArguments("C.P.get").WithLocation(4, 29),
    // (4,29): error CS0513: 'C.P.get' is abstract but it is contained in non-abstract type 'C'
    //    public abstract int P => 1;
    Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "1").WithArguments("C.P.get", "C").WithLocation(4, 29));
        }

        [Fact]
        public void Syntax06()
        {
            var comp = CreateCompilationWithMscorlib45(@"
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
            var comp = CreateCompilationWithMscorlib45(@"
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
        public void Syntax08()
        {
            CreateCompilationWithMscorlib45(@"
interface I
{
    int P { get; };
}").VerifyDiagnostics(
    // (4,19): error CS1597: Semicolon after method or accessor block is not valid
    //     int P { get; };
    Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(4, 19));
        }

        [Fact]
        public void Syntax09()
        {
            CreateCompilationWithMscorlib45(@"
class C
{
    int P => 2
}").VerifyDiagnostics(
    // (4,15): error CS1002: ; expected
    //     int P => 2
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 15));
        }

        [Fact]
        public void Syntax10()
        {
            CreateCompilationWithMscorlib45(@"
interface I
{
    int this[int i]
}").VerifyDiagnostics(
    // (4,20): error CS1514: { expected
    //     int this[int i]
    Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 20),
    // (5,2): error CS1513: } expected
    // }
    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2),
    // (4,9): error CS0548: 'I.this[int]': property or indexer must have at least one accessor
    //     int this[int i]
    Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("I.this[int]").WithLocation(4, 9));
        }

        [Fact]
        public void Syntax11()
        {
            CreateCompilationWithMscorlib45(@"
interface I
{
    int this[int i];
}").VerifyDiagnostics(
    // (4,20): error CS1514: { expected
    //     int this[int i];
    Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(4, 20),
    // (4,20): error CS1014: A get, set or init accessor expected
    //     int this[int i];
    Diagnostic(ErrorCode.ERR_GetOrSetExpected, ";").WithLocation(4, 20),
    // (5,2): error CS1513: } expected
    // }
    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2),
    // (4,9): error CS0548: 'I.this[int]': property or indexer must have at least one accessor
    //     int this[int i];
    Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "this").WithArguments("I.this[int]").WithLocation(4, 9));
        }

        [Fact]
        public void Syntax12()
        {
            CreateCompilationWithMscorlib45(@"
interface I
{
    int this[int i] { get; };
}").VerifyDiagnostics(
    // (4,29): error CS1597: Semicolon after method or accessor block is not valid
    //     int this[int i] { get; };
    Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(4, 29));
        }

        [Fact]
        public void Syntax13()
        {
            // End the property declaration at the semicolon after the accessor list
            CreateCompilationWithMscorlib45(@"
class C
{
    int P { get; set; }; => 2;
}").VerifyDiagnostics(
    // (4,24): error CS1597: Semicolon after method or accessor block is not valid
    //     int P { get; set; }; => 2;
    Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(4, 24),
    // (4,26): error CS1519: Invalid token '=>' in class, record, struct, or interface member declaration
    //     int P { get; set; }; => 2;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(4, 26));
        }

        [Fact]
        public void Syntax14()
        {
            CreateCompilationWithMscorlib45(@"
class C
{
    int this[int i] => 2
}").VerifyDiagnostics(
    // (4,25): error CS1002: ; expected
    //     int this[int i] => 2
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 25));
        }

        [Fact]
        public void LambdaTest01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
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
    public int this[int i, int j] => i * j * P;
}";
            var comp = CreateCompilationWithMscorlib45(text);
            comp.VerifyDiagnostics();
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<SourcePropertySymbol>("P");
            Assert.Null(p.SetMethod);
            Assert.NotNull(p.GetMethod);
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.True(p.IsExpressionBodied);

            var indexer = c.GetMember<SourcePropertySymbol>("this[]");
            Assert.Null(indexer.SetMethod);
            Assert.NotNull(indexer.GetMethod);
            Assert.False(indexer.GetMethod.IsImplicitlyDeclared);
            Assert.True(indexer.IsExpressionBodied);
            Assert.True(indexer.IsIndexer);

            Assert.Equal(2, indexer.ParameterCount);
            var i = indexer.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, i.Type.SpecialType);
            Assert.Equal("i", i.Name);
            var j = indexer.Parameters[1];
            Assert.Equal(SpecialType.System_Int32, i.Type.SpecialType);
            Assert.Equal("j", j.Name);
        }

        [Fact]
        public void Override01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
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
        public void Override02()
        {
            CreateCompilationWithMscorlib45(@"
class B
{
    public int P => 10;
    public int this[int i] => i;
}
class C : B
{
    public override int P => 20;
    public override int this[int i] => i * 2;
}").VerifyDiagnostics(
    // (10,25): error CS0506: 'C.this[int]': cannot override inherited member 'B.this[int]' because it is not marked virtual, abstract, or override
    //     public override int this[int i] => i * 2;
    Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "this").WithArguments("C.this[int]", "B.this[int]").WithLocation(10, 25),
    // (9,25): error CS0506: 'C.P': cannot override inherited member 'B.P' because it is not marked virtual, abstract, or override
    //     public override int P => 20;
    Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "P").WithArguments("C.P", "B.P").WithLocation(9, 25));
        }

        [Fact]
        public void Override03()
        {
            CreateCompilationWithMscorlib45(@"
class B
{
    public virtual int P => 10;
    public virtual int this[int i] => i;
}
class C : B
{
    public override int P => 20;
    public override int this[int i] => i * 2;
}").VerifyDiagnostics();
        }

        [Fact]
        public void VoidExpression()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public void P => System.Console.WriteLine(""goo"");
}").VerifyDiagnostics(
    // (4,17): error CS0547: 'C.P': property or indexer cannot have void type
    //     public void P => System.Console.WriteLine("goo");
    Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "P").WithArguments("C.P").WithLocation(4, 17));
        }

        [Fact]
        public void VoidExpression2()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int P => System.Console.WriteLine(""goo"");
}").VerifyDiagnostics(
    // (4,21): error CS0029: Cannot implicitly convert type 'void' to 'int'
    //     public int P => System.Console.WriteLine("goo");
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"System.Console.WriteLine(""goo"")").WithArguments("void", "int").WithLocation(4, 21));
        }

        [Fact]
        public void InterfaceImplementation01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
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
    string I.Q { get { return ""goo""; } }
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

            prop = (SourcePropertySymbol)c.GetProperty("I.Q");
            Assert.True(prop.IsReadOnly);
            Assert.True(prop.IsExplicitInterfaceImplementation);

            prop = (SourcePropertySymbol)c.GetProperty("J.Q");
            Assert.True(prop.IsReadOnly);
            Assert.True(prop.IsExplicitInterfaceImplementation);

            prop = c.GetMember<SourcePropertySymbol>("D");
            Assert.True(prop.IsReadOnly);
        }

        [ClrOnlyFact]
        public void Emit01()
        {
            var comp = CreateCompilationWithMscorlib45(@"
abstract class A
{
    protected abstract string Z { get; }
}
abstract class B : A
{
    protected sealed override string Z => ""goo"";
    protected abstract string Y { get; }
}    
class C : B
{
    public const int X = 2;
    public static int P => C.X * C.X;
    
    public int Q => X;
    private int R => P * Q;
    protected sealed override string Y => Z + R;
    public int this[int i] => R + i;

    public static void Main()
    {
        System.Console.WriteLine(C.X);
        System.Console.WriteLine(C.P);
        var c = new C();
        
        System.Console.WriteLine(c.Q);
        System.Console.WriteLine(c.R);
        System.Console.WriteLine(c.Z);
        System.Console.WriteLine(c.Y);
        System.Console.WriteLine(c[10]);
    }
}", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var verifier = CompileAndVerify(comp, expectedOutput:
@"2
4
2
8
goo
goo8
18");
        }

        [ClrOnlyFact]
        public void AccessorInheritsVisibility()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    private int P => 1;
    private int this[int i] => i;
}", options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            Action<ModuleSymbol> srcValidator = m =>
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var p = c.GetMember<PropertySymbol>("P");
                var indexer = c.Indexers[0];

                Assert.Equal(Accessibility.Private, p.DeclaredAccessibility);
                Assert.Equal(Accessibility.Private, indexer.DeclaredAccessibility);
            };

            var verifier = CompileAndVerify(comp, sourceSymbolValidator: srcValidator);
        }

        [Fact]
        public void StaticIndexer()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    static int this[int i] => i;
}");
            comp.VerifyDiagnostics(
    // (4,16): error CS0106: The modifier 'static' is not valid for this item
    //     static int this[int i] => i;
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(4, 16));
        }

        [Fact]
        public void RefReturningExpressionBodiedProperty()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    int field = 0;
    public ref int P => ref field;
}");
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<SourcePropertySymbol>("P");
            Assert.Null(p.SetMethod);
            Assert.NotNull(p.GetMethod);
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.True(p.IsExpressionBodied);
            Assert.Equal(RefKind.Ref, p.GetMethod.RefKind);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void RefReadonlyReturningExpressionBodiedProperty()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    int field = 0;
    public ref readonly int P => ref field;
}");
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<SourcePropertySymbol>("P");
            Assert.Null(p.SetMethod);
            Assert.NotNull(p.GetMethod);
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.True(p.IsExpressionBodied);
            Assert.Equal(RefKind.RefReadOnly, p.GetMethod.RefKind);
            Assert.False(p.ReturnsByRef);
            Assert.False(p.GetMethod.ReturnsByRef);
            Assert.True(p.ReturnsByRefReadonly);
            Assert.True(p.GetMethod.ReturnsByRefReadonly);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void RefReadonlyReturningExpressionBodiedIndexer()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    int field = 0;
    public ref readonly int this[in int arg] => ref field;
}");
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<SourcePropertySymbol>("this[]");
            Assert.Null(p.SetMethod);
            Assert.NotNull(p.GetMethod);
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.True(p.IsExpressionBodied);
            Assert.Equal(RefKind.RefReadOnly, p.GetMethod.RefKind);
            Assert.Equal(RefKind.In, p.GetMethod.Parameters[0].RefKind);
            Assert.False(p.ReturnsByRef);
            Assert.False(p.GetMethod.ReturnsByRef);
            Assert.True(p.ReturnsByRefReadonly);
            Assert.True(p.GetMethod.ReturnsByRefReadonly);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void RefReadonlyReturningExpressionBodiedIndexer1()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    int field = 0;
    public ref readonly int this[in int arg] => ref field;
}");
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var p = c.GetMember<SourcePropertySymbol>("this[]");
            Assert.Null(p.SetMethod);
            Assert.NotNull(p.GetMethod);
            Assert.False(p.GetMethod.IsImplicitlyDeclared);
            Assert.True(p.IsExpressionBodied);
            Assert.Equal(RefKind.RefReadOnly, p.GetMethod.RefKind);
            Assert.Equal(RefKind.In, p.GetMethod.Parameters[0].RefKind);
            Assert.False(p.ReturnsByRef);
            Assert.False(p.GetMethod.ReturnsByRef);
            Assert.True(p.ReturnsByRefReadonly);
            Assert.True(p.GetMethod.ReturnsByRefReadonly);
        }
    }
}
