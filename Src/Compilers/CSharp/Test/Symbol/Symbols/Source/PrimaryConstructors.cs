// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Cci = Microsoft.Cci;
using Retargeting = Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PrimaryConstructors : CSharpTestBase
    {
        [Fact]
        public void DisabledByDefault()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(int x)
{}
");

            comp.VerifyDiagnostics(
                // (2,11): error CS8058: Feature 'primary constructor' is only available in 'experimental' language version.
                // class Test(int x)
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "(int x)").WithArguments("primary constructor").WithLocation(2, 11));
        }

        [Fact]
        public void Syntax1()
        {
            var comp = CreateCompilationWithMscorlib(@"
enum Test(
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,10): error CS1514: { expected
    // enum Test(
    Diagnostic(ErrorCode.ERR_LbraceExpected, "("),
    // (2,10): error CS1513: } expected
    // enum Test(
    Diagnostic(ErrorCode.ERR_RbraceExpected, "("),
    // (2,10): error CS1022: Type or namespace definition, or end-of-file expected
    // enum Test(
    Diagnostic(ErrorCode.ERR_EOFExpected, "("),
    // (3,2): error CS1022: Type or namespace definition, or end-of-file expected
    // {}
    Diagnostic(ErrorCode.ERR_EOFExpected, "}")
                );
        }

        [Fact]
        public void Syntax2()
        {
            var comp = CreateCompilationWithMscorlib(@"
interface Test(
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,15): error CS1514: { expected
    // interface Test(
    Diagnostic(ErrorCode.ERR_LbraceExpected, "("),
    // (2,15): error CS1513: } expected
    // interface Test(
    Diagnostic(ErrorCode.ERR_RbraceExpected, "("),
    // (2,15): error CS1022: Type or namespace definition, or end-of-file expected
    // interface Test(
    Diagnostic(ErrorCode.ERR_EOFExpected, "("),
    // (3,2): error CS1022: Type or namespace definition, or end-of-file expected
    // {}
    Diagnostic(ErrorCode.ERR_EOFExpected, "}")
                );
        }

        [Fact]
        public void Syntax3()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Test(
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,13): error CS1026: ) expected
    // struct Test(
    Diagnostic(ErrorCode.ERR_CloseParenExpected, ""),
    // (2,12): error CS0568: Structs cannot contain explicit parameterless constructors
    // struct Test(
    Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, @"(
")
                );
        }

        [Fact]
        public void Syntax4()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS1026: ) expected
    // class Test(
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "")
                );
        }

        [Fact]
        public void Syntax5()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

class Test<T>(T x, [In] int y = 2) : object()
{}

abstract class Test2()
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics();

            var test = comp.GetTypeByMetadataName("Test`1");
            var ctor = test.GetMember<MethodSymbol>(".ctor");

            Assert.Equal(MethodKind.Constructor, ctor.MethodKind);
            Assert.Equal(Accessibility.Public, ctor.DeclaredAccessibility);

            var parameters = ctor.Parameters;

            Assert.Same(test.TypeParameters[0], parameters[0].Type);
            Assert.Equal(SpecialType.System_Int32, parameters[1].Type.SpecialType);
            Assert.Equal(2, parameters[1].ExplicitDefaultValue);
            Assert.Equal("System.Runtime.InteropServices.InAttribute", parameters[1].GetAttributes()[0].ToString());

            Assert.True(ctor.IsImplicitlyDeclared);

            Assert.Equal(Accessibility.Protected, comp.GetTypeByMetadataName("Test2").GetMember<MethodSymbol>(".ctor").DeclaredAccessibility);
        }

        [Fact]
        public void Syntax6()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test() : object(
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,23): error CS1001: Identifier expected
    // class Test() : object(
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 23),
    // (3,2): error CS1026: ) expected
    // {}
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(3, 2),
    // (3,2): error CS1514: { expected
    // {}
    Diagnostic(ErrorCode.ERR_LbraceExpected, "}").WithLocation(3, 2),
    // (3,2): error CS1513: } expected
    // {}
    Diagnostic(ErrorCode.ERR_RbraceExpected, "}").WithLocation(3, 2),
    // (3,2): error CS1022: Type or namespace definition, or end-of-file expected
    // {}
    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(3, 2)
                );
        }

        [Fact]
        public void Syntax7()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test : System.Object(
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,27): error CS1003: Syntax error, ',' expected
    // class Test : System.Object(
    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "(")
                );
        }

        [Fact]
        public void Syntax8()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test : object()
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,20): error CS1003: Syntax error, ',' expected
    // class Test : object()
    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "(")
                );
        }

        [Fact]
        public void Syntax9()
        {
            var comp = CreateCompilationWithMscorlib(@"
interface I1 {}

class Test() : object, I1()
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,26): error CS1003: Syntax error, ',' expected
    // class Test() : object, I1()
    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "(")
                );
        }

        [Fact]
        public void Multiple_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
partial class Test()
{}

partial class Test()
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (5,19): error CS8028: Only one part of a partial type can declare primary constructor parameters.
    // partial class Test()
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "()")
                );

            Assert.Equal(2, comp.GetTypeByMetadataName("Test").GetMembers(WellKnownMemberNames.InstanceConstructorName).Length);
        }

        [Fact]
        public void Multiple_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x) {}
    public Base(long x) {}
}

partial class Derived(int x) : Base(x)
{}

partial class Derived(long x) : Base(x)
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (11,22): error CS9001: Only one part of a partial type can declare primary constructor parameters.
    // partial class Derived(long x) : Base(x)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "(long x)")
                );

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel semanticModel = comp.GetSemanticModel(tree);

            var classDecls = (from node in tree.GetRoot().DescendantNodes()
                              where node.CSharpKind() == SyntaxKind.ClassDeclaration && ((ClassDeclarationSyntax)node).Identifier.ValueText == "Derived"
                              select (ClassDeclarationSyntax)node).ToArray();

            Assert.Equal(2, classDecls.Length);

            var declaredCtor = semanticModel.GetDeclaredConstructorSymbol(classDecls[0]);
            Assert.Equal("Derived..ctor(System.Int32 x)", declaredCtor.ToTestDisplayString());
            Assert.Same(declaredCtor, CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, classDecls[0]));

            SymbolInfo symInfo;

            var baseInitializer = (BaseClassWithArgumentsSyntax)classDecls[0].BaseList.Types[0];
            symInfo = semanticModel.GetSymbolInfo(baseInitializer);
            Assert.Equal("Base..ctor(System.Int32 x)", symInfo.Symbol.ToTestDisplayString());
            Assert.Same(symInfo.Symbol, ((CodeAnalysis.SemanticModel)semanticModel).GetSymbolInfo((SyntaxNode)baseInitializer).Symbol);
            Assert.Same(symInfo.Symbol, CSharpExtensions.GetSymbolInfo((CodeAnalysis.SemanticModel)semanticModel, baseInitializer).Symbol);

            declaredCtor = semanticModel.GetDeclaredConstructorSymbol(classDecls[1]);
            Assert.Equal("Derived..ctor(System.Int64 x)", declaredCtor.ToTestDisplayString());
            Assert.Same(declaredCtor, CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, classDecls[1]));

            baseInitializer = (BaseClassWithArgumentsSyntax)classDecls[1].BaseList.Types[0];
            symInfo = semanticModel.GetSymbolInfo(baseInitializer);
            Assert.Equal("Base..ctor(System.Int64 x)", symInfo.Symbol.ToTestDisplayString());
            Assert.Same(symInfo.Symbol, ((CodeAnalysis.SemanticModel)semanticModel).GetSymbolInfo((SyntaxNode)baseInitializer).Symbol);
            Assert.Same(symInfo.Symbol, CSharpExtensions.GetSymbolInfo((CodeAnalysis.SemanticModel)semanticModel, baseInitializer).Symbol);
        }

        [Fact]
        public void Multiple_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x) {}
    public Base(long x) {}
}

partial class Derived(int x) : Base(y)
{
    private int fx = x;
}

partial class Derived(long y) : Base(x)
{
    private long fy = y;
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (13,22): error CS9001: Only one part of a partial type can declare primary constructor parameters.
    // partial class Derived(long y) : Base(x)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "(long y)"),
    // (15,23): error CS0103: The name 'y' does not exist in the current context
    //     private long fy = y;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y"),
    // (13,38): error CS0103: The name 'x' does not exist in the current context
    // partial class Derived(long y) : Base(x)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"),
    // (8,37): error CS0103: The name 'y' does not exist in the current context
    // partial class Derived(int x) : Base(y)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y")
                );
        }

        [Fact]
        public void Multiple_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
partial struct Derived(int x)
{
    private int fx;
}

partial struct Derived(long y)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (7,23): error CS9001: Only one part of a partial type can declare primary constructor parameters.
    // partial struct Derived(long y)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "(long y)"),
    // (2,23): error CS0171: Field 'Derived.fx' must be fully assigned before control is returned to the caller
    // partial struct Derived(int x)
    Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x)").WithArguments("Derived.fx"),
    // (4,17): warning CS0169: The field 'Derived.fx' is never used
    //     private int fx;
    Diagnostic(ErrorCode.WRN_UnreferencedField, "fx").WithArguments("Derived.fx")
                );
        }

        [Fact]
        public void Multiple_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base1
{
    public Base1(int x) {}
}

class Base2
{
    public Base2(long x) {}
}

partial class Derived(int x) : Base1(x)
{
}

partial class Derived(long y) : Base2(y)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (12,15): error CS0263: Partial declarations of 'Derived' must not specify different base classes
    // partial class Derived(int x) : Base1(x)
    Diagnostic(ErrorCode.ERR_PartialMultipleBases, "Derived").WithArguments("Derived"),
    // (16,22): error CS9001: Only one part of a partial type can declare primary constructor parameters.
    // partial class Derived(long y) : Base2(y)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "(long y)")
                );

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel semanticModel = comp.GetSemanticModel(tree);

            var classDecls = (from node in tree.GetRoot().DescendantNodes()
                              where node.CSharpKind() == SyntaxKind.ClassDeclaration && ((ClassDeclarationSyntax)node).Identifier.ValueText == "Derived"
                              select (ClassDeclarationSyntax)node).ToArray();

            Assert.Equal(2, classDecls.Length);

            var declaredCtor = semanticModel.GetDeclaredConstructorSymbol(classDecls[0]);
            Assert.Equal("Derived..ctor(System.Int32 x)", declaredCtor.ToTestDisplayString());
            Assert.Same(declaredCtor, CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, classDecls[0]));

            SymbolInfo symInfo;

            var baseInitializer = (BaseClassWithArgumentsSyntax)classDecls[0].BaseList.Types[0];
            symInfo = semanticModel.GetSymbolInfo(baseInitializer);
            Assert.Null(symInfo.Symbol);
            Assert.Null(((CodeAnalysis.SemanticModel)semanticModel).GetSymbolInfo((SyntaxNode)baseInitializer).Symbol);
            Assert.Null(CSharpExtensions.GetSymbolInfo((CodeAnalysis.SemanticModel)semanticModel, baseInitializer).Symbol);

            declaredCtor = semanticModel.GetDeclaredConstructorSymbol(classDecls[1]);
            Assert.Equal("Derived..ctor(System.Int64 y)", declaredCtor.ToTestDisplayString());
            Assert.Same(declaredCtor, CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, classDecls[1]));

            baseInitializer = (BaseClassWithArgumentsSyntax)classDecls[1].BaseList.Types[0];
            symInfo = semanticModel.GetSymbolInfo(baseInitializer);
            Assert.Null(symInfo.Symbol);
            Assert.Null(((CodeAnalysis.SemanticModel)semanticModel).GetSymbolInfo((SyntaxNode)baseInitializer).Symbol);
            Assert.Null(CSharpExtensions.GetSymbolInfo((CodeAnalysis.SemanticModel)semanticModel, baseInitializer).Symbol);
        }

        [Fact]
        public void BaseArguments()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {
        System.Console.WriteLine(x);
    }
}
class Derived(int x = 123) : Base(x)
{}

class Program
{
    public static void Main()
    {
        var y = new Derived();
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact]
        public void Struct1()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Struct1(int x)
{
    private int y;

    public Struct1(long x)
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,15): error CS0171: Field 'Struct1.y' must be fully assigned before control is returned to the caller
    // struct Struct1(int x)
    Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x)").WithArguments("Struct1.y"),
    // (6,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Struct1(long x)
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Struct1"),
    // (6,12): error CS0171: Field 'Struct1.y' must be fully assigned before control is returned to the caller
    //     public Struct1(long x)
    Diagnostic(ErrorCode.ERR_UnassignedThis, "Struct1").WithArguments("Struct1.y"),
    // (4,17): warning CS0169: The field 'Struct1.y' is never used
    //     private int y;
    Diagnostic(ErrorCode.WRN_UnreferencedField, "y").WithArguments("Struct1.y")
                );
        }

        [Fact]
        public void Struct2()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Struct1(int x)
{
    public Struct1(long x) : base()
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Struct1(long x) : base()
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Struct1"),
    // (4,12): error CS0522: 'Struct1': structs cannot call base class constructors
    //     public Struct1(long x) : base()
    Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "Struct1").WithArguments("Struct1")
                );
        }

        [Fact]
        public void Struct3()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Struct1(int x)
{
    public Struct1(long x) : this((int)x)
    {}

    static Struct1() {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Struct4()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Struct1(int x)
{
    public Struct1()
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,12): error CS0568: Structs cannot contain explicit parameterless constructors
    //     public Struct1()
    Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "Struct1"),
    // (4,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Struct1()
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Struct1")
                );
        }

        [Fact]
        public void Struct5()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Struct1()
{
    public Struct1()
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,15): error CS0568: Structs cannot contain explicit parameterless constructors
    // struct Struct1()
    Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()"),
    // (4,12): error CS0568: Structs cannot contain explicit parameterless constructors
    //     public Struct1()
    Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "Struct1"),
    // (4,12): error CS0111: Type 'Struct1' already defines a member called '.ctor' with the same parameter types
    //     public Struct1()
    Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Struct1").WithArguments(".ctor", "Struct1"),
    // (4,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Struct1()
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Struct1")
                );
        }

        [Fact()]
        public void Struct6()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Struct1(int x)
{
    public Struct1(long x) : this() 
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,12): error CS9009: Since this struct type has a primary constructor, an instance constructor declaration cannot specify a constructor initializer that invokes default constructor.
    //     public Struct1(long x) : this() // Not an error at the moment, might change our's mind later.
    Diagnostic(ErrorCode.ERR_InstanceCtorCannotHaveDefaultThisInitializer, "Struct1")
                );
        }

        [Fact]
        public void Class1()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x){}
}

class Derived(int x) : Base
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (7,14): error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'Base.Base(int)'
    // class Derived(int x) : Base
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "(int x)").WithArguments("x", "Base.Base(int)")
                );
        }

        [Fact]
        public void Class2()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x){}
}

class Derived1(int x) : Base(x)
{
    public Derived1(long x)
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (9,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Derived1(long x)
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Derived1"),
    // (9,12): error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'Base.Base(int)'
    //     public Derived1(long x)
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "Derived1").WithArguments("x", "Base.Base(int)")
                );
        }

        [Fact]
        public void Class3()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
}

class Derived1(int x) : Base
{
    public Derived1(long x)
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (8,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Derived1(long x)
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Derived1")
                );
        }

        [Fact]
        public void Class4()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int x)
{
    public Derived1(long x)
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (8,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Derived1(long x)
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Derived1")
                );
        }


        [Fact]
        public void Class5()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x) : object
{
    public Derived(long x) : base()
    {}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,12): error CS8029: Since this type has primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     public Derived(long x) : base()
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Derived")
                );
        }

        [Fact]
        public void Class6()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x) : object
{
    public Derived(long x) : this((int)x)
    {}

    static Derived(){}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Class7()
        {
            var comp = CreateCompilationWithMscorlib(@"
static class Derived(int x)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,21): error CS0710: Static classes cannot have instance constructors
    // static class Derived(int x)
    Diagnostic(ErrorCode.ERR_ConstructorInStaticClass, "(int x)")
                );
        }

        [Fact]
        public void Class8_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(int x)
{
}

partial class Derived : object
{}

partial class Derived(int x) : Base(y)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,15): error CS0263: Partial declarations of 'Derived' must not specify different base classes
    // partial class Derived : object
    Diagnostic(ErrorCode.ERR_PartialMultipleBases, "Derived").WithArguments("Derived")
                );
        }

        [Fact]
        public void Class8_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base1(int x)
{
}

class Base2
{}

partial class Derived1(int x) : Base1(y)
{
}

partial class Derived1 : Base2
{}

partial class Derived2 : Base2
{}

partial class Derived2(int x) : Base1(y)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (16,15): error CS0263: Partial declarations of 'Derived2' must not specify different base classes
    // partial class Derived2 : Base2
    Diagnostic(ErrorCode.ERR_PartialMultipleBases, "Derived2").WithArguments("Derived2").WithLocation(16, 15),
    // (9,15): error CS0263: Partial declarations of 'Derived1' must not specify different base classes
    // partial class Derived1(int x) : Base1(y)
    Diagnostic(ErrorCode.ERR_PartialMultipleBases, "Derived1").WithArguments("Derived1").WithLocation(9, 15)
                );
        }

        [Fact]
        public void Class9()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(int x)
{
}

partial class Derived : object
{}

partial class Derived(int x) : Base(x)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,15): error CS0263: Partial declarations of 'Derived' must not specify different base classes
    // partial class Derived : object
    Diagnostic(ErrorCode.ERR_PartialMultipleBases, "Derived").WithArguments("Derived")
                );
        }

        [Fact]
        public void Circular()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x = 123) : Derived(x)
{}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,7): error CS0146: Circular base class dependency involving 'Derived' and 'Derived'
    // class Derived(int x = 123) : Derived(x)
    Diagnostic(ErrorCode.ERR_CircularBase, "Derived").WithArguments("Derived", "Derived")
                );
        }

        [Fact]
        public void NameVisibility_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x = y)
{
    public const int y = 1;
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,23): error CS0103: The name 'y' does not exist in the current context
    // class Derived(int x = y)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y"),
    // (2,19): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    // class Derived(int x = y)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("?", "int")
                );
        }

        [Fact]
        public void NameVisibility_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(int x)
{
}

class Derived() : Base(y)
{
    public const int y = 1;
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,24): error CS0103: The name 'y' does not exist in the current context
    // class Derived() : Base(y)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y")
                );
        }

        [Fact]
        public void NameConflict_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x, int x)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,26): error CS0100: The parameter name 'x' is a duplicate
    // class Derived(int x, int x)
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x")
                );
        }

        [Fact]
        public void NameConflict_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int derived1)
{
}

class Derived2(int Derived2)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,20): error CS9004: 'Derived2': a parameter of a primary constructor cannot have the same name as containing type
    // class Derived2(int Derived2)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterSameNameAsContainingType, "Derived2").WithArguments("Derived2")
                );
        }

        [Fact]
        public void NameConflict_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1<T>(int t)
{
}

class Derived2<T>(int T)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,23): error CS9003: 'Derived2<T>': a parameter of a primary constructor cannot have the same name as a type's type parameter 'T'
    // class Derived2<T>(int T)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterSameNameAsTypeParam, "T").WithArguments("Derived2<T>", "T")
                );
        }

        [Fact]
        public void NameConflict_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int t)
{
    void T(){}
}

class Derived2(int T)
{
    void T(){}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void NameConflict_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int item, int get_item, int set_item)
{
    public int this[int x] 
    { 
        get { return 0; } 
        set {}
    }
}

class Derived2(int Item, int get_Item, int set_Item)
{
    public int this[int x] 
    { 
        get { return 0; } 
        set {}
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void NameConflict_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived2(int Item, int get_Item, int set_Item)
{
    public int this[int x] 
    { 
        get { return 0; } 
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void NameConflict_07()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived2(int Item, int get_Item, int set_Item)
{
    public int this[int x] 
    { 
        set {} 
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void NameConflict_08()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int item, int get_item, int set_item)
{
    public int Item 
    { 
        get { return 0; } 
        set {}
    }
}

class Derived2(int Item1, int get_Item1, int set_Item1)
{
    public int Item1
    { 
        get { return 0; } 
        set {}
    }
}

class Derived3(int Item2, int get_Item2, int set_Item2)
{
    public int Item2
    { 
        set {}
    }
}

class Derived4(int Item3, int get_Item3, int set_Item3)
{
    public int Item3 
    { 
        get { return 0; } 
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void NameConflict_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int item, int add_item, int remove_item)
{
    public event System.Action Item; 
}

class Derived2(int Item1, int add_Item1, int remove_Item1)
{
    public event System.Action Item1; 
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (9,32): warning CS0067: The event 'Derived2.Item1' is never used
    //     public event System.Action Item1; 
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Item1").WithArguments("Derived2.Item1"),
    // (4,32): warning CS0067: The event 'Derived1.Item' is never used
    //     public event System.Action Item; 
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Item").WithArguments("Derived1.Item")
                );
        }

        [Fact]
        public void NameConflict_10()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int item, int add_item, int remove_item)
{
    public event System.Action Item
    {
        add{}
        remove{}
    } 
}

class Derived2(int Item1, int add_Item1, int remove_Item1)
{
    public event System.Action Item1 
    {
        add{}
        remove{}
    } 
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void NameConflict_11()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int item)
{
    class Item
    {} 
}

class Derived2(int Item1)
{
    class Item1
    {} 
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp);
        }

        [Fact]
        public void ParameterVisibility_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {}

    public const int f0 = 1;
}
class Derived(int p0, int p1 = Base.f0, int p2 = 0, int p3 = 0, int p4 = 0, int p5 = 0, int p6 = 0, int p7 = 0, int p8 = 0, int p9 = 0, int p10 = 0, int p11 = 0, int p12 = 0, int p13 = 0, int p14 = 0)
    : Base(p0)
{
    void Test1()
    {
        System.Console.WriteLine(p1);
    }

    Derived() : this(p2)
    {
        System.Console.WriteLine(p3);
    }

    private int x = p4;

    public int Item1 
    { 
        get { return p5; } 
        set 
        { 
            System.Console.WriteLine(p6);
        }
    }

    public int this[int x] 
    { 
        get { return p7; } 
        set 
        { 
            System.Console.WriteLine(p8);
        }
    }

    public event System.Action E1 
    { 
        add 
        { 
            System.Console.WriteLine(p9);
        }
        remove 
        { 
            System.Console.WriteLine(p10);
        }
    }

    static System.Action GetAction(int x)
    {
        return null;
    }

    public event System.Action E2 = GetAction(p11);

    void Test2(int x = p12)
    {
    }

    public static int operator + (Derived x)
    {
        return p13;
    }

    public static explicit operator int (Derived x)
    {
        return p14;
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (61,24): error CS0103: The name 'p12' does not exist in the current context
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p12").WithArguments("p12").WithLocation(61, 24),
    // (61,20): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("?", "int").WithLocation(61, 20),
    // (14,34): error CS0103: The name 'p1' does not exist in the current context
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(14, 34),
    // (17,22): error CS0103: The name 'p2' does not exist in the current context
    //     Derived() : this(p2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(17, 22),
    // (19,34): error CS0103: The name 'p3' does not exist in the current context
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p3").WithArguments("p3").WithLocation(19, 34),
    // (26,22): error CS0103: The name 'p5' does not exist in the current context
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p5").WithArguments("p5").WithLocation(26, 22),
    // (29,38): error CS0103: The name 'p6' does not exist in the current context
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p6").WithArguments("p6").WithLocation(29, 38),
    // (35,22): error CS0103: The name 'p7' does not exist in the current context
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p7").WithArguments("p7").WithLocation(35, 22),
    // (38,38): error CS0103: The name 'p8' does not exist in the current context
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p8").WithArguments("p8").WithLocation(38, 38),
    // (46,38): error CS0103: The name 'p9' does not exist in the current context
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p9").WithArguments("p9").WithLocation(46, 38),
    // (50,38): error CS0103: The name 'p10' does not exist in the current context
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p10").WithArguments("p10").WithLocation(50, 38),
    // (67,16): error CS0103: The name 'p13' does not exist in the current context
    //         return p13;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p13").WithArguments("p13").WithLocation(67, 16),
    // (72,16): error CS0103: The name 'p14' does not exist in the current context
    //         return p14;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p14").WithArguments("p14").WithLocation(72, 16)
                );

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel semanticModel = comp.GetSemanticModel(tree);

            var parameterRefs = (from node in tree.GetRoot().DescendantNodes()
                         where node.CSharpKind() == SyntaxKind.IdentifierName
                         select (IdentifierNameSyntax)node).
                         Where(node => node.Identifier.ValueText.StartsWith("p")).ToArray();

            Assert.Equal(15, parameterRefs.Length);

            SymbolInfo symInfo;

            foreach (var n in parameterRefs)
            {
                symInfo = semanticModel.GetSymbolInfo(n);

                switch (n.Identifier.ValueText)
                {
                    case "p0":
                    case "p4":
                    case "p11":
                        Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
                        break;

                    default:
                        Assert.Null(symInfo.Symbol);
                        break;
                }

            }

            var classDecls = (from node in tree.GetRoot().DescendantNodes()
                              where node.CSharpKind() == SyntaxKind.ClassDeclaration
                              select (ClassDeclarationSyntax)node).ToArray();

            Assert.Equal(2, classDecls.Length);

            Assert.Equal("Base", classDecls[0].Identifier.ValueText);
            Assert.Null(semanticModel.GetDeclaredConstructorSymbol(classDecls[0]));
            Assert.Null(CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, classDecls[0]));

            Assert.Equal("Derived", classDecls[1].Identifier.ValueText);
            var declaredCtor = semanticModel.GetDeclaredConstructorSymbol(classDecls[1]);
            Assert.Equal("Derived..ctor(System.Int32 p0, [System.Int32 p1 = 1], [System.Int32 p2 = 0], [System.Int32 p3 = 0], [System.Int32 p4 = 0], [System.Int32 p5 = 0], [System.Int32 p6 = 0], [System.Int32 p7 = 0], [System.Int32 p8 = 0], [System.Int32 p9 = 0], [System.Int32 p10 = 0], [System.Int32 p11 = 0], [System.Int32 p12 = 0], [System.Int32 p13 = 0], [System.Int32 p14 = 0])",
                declaredCtor.ToTestDisplayString());
            Assert.Same(declaredCtor, CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, classDecls[1]));

            var p1 = classDecls[1].ParameterList.Parameters[1];

            Assert.Equal("[System.Int32 p1 = 1]", semanticModel.GetDeclaredSymbol(p1).ToTestDisplayString());

            symInfo = semanticModel.GetSymbolInfo(p1.Default.Value);
            Assert.Equal("System.Int32 Base.f0", symInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void ParameterVisibility_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {}

    public const int f0 = 1;
}

struct Derived(int p1, int p2 = Base.f0, int p3 = 0, int p4 = 0, int p5 = 0, int p6 = 0, int p7 = 0, int p8 = 0, int p9 = 0, int p10 = 0, int p11 = 0, int p12 = 0, int p13 = 0, int p14 = 0)
{
    void Test1()
    {
        System.Console.WriteLine(p1);
    }

    Derived(long x) : this(p2)
    {
        System.Console.WriteLine(p3);
    }

    private int x = p4;

    public int Item1 
    { 
        get { return p5; } 
        set 
        { 
            System.Console.WriteLine(p6);
        }
    }

    public int this[int x] 
    { 
        get { return p7; } 
        set 
        { 
            System.Console.WriteLine(p8);
        }
    }

    public event System.Action E1 
    { 
        add 
        { 
            System.Console.WriteLine(p9);
        }
        remove 
        { 
            System.Console.WriteLine(p10);
        }
    }

    static System.Action GetAction(int x)
    {
        return null;
    }

    public event System.Action E2 = GetAction(p11);

    void Test2(int x = p12)
    {
    }

    public static int operator + (Derived x)
    {
        return p13;
    }

    public static explicit operator int (Derived x)
    {
        return p14;
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (61,24): error CS0103: The name 'p12' does not exist in the current context
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p12").WithArguments("p12").WithLocation(61, 24),
    // (61,20): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("?", "int").WithLocation(61, 20),
    // (14,34): error CS0103: The name 'p1' does not exist in the current context
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(14, 34),
    // (17,28): error CS0103: The name 'p2' does not exist in the current context
    //     Derived(long x) : this(p2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(17, 28),
    // (19,34): error CS0103: The name 'p3' does not exist in the current context
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p3").WithArguments("p3").WithLocation(19, 34),
    // (26,22): error CS0103: The name 'p5' does not exist in the current context
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p5").WithArguments("p5").WithLocation(26, 22),
    // (29,38): error CS0103: The name 'p6' does not exist in the current context
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p6").WithArguments("p6").WithLocation(29, 38),
    // (35,22): error CS0103: The name 'p7' does not exist in the current context
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p7").WithArguments("p7").WithLocation(35, 22),
    // (38,38): error CS0103: The name 'p8' does not exist in the current context
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p8").WithArguments("p8").WithLocation(38, 38),
    // (46,38): error CS0103: The name 'p9' does not exist in the current context
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p9").WithArguments("p9").WithLocation(46, 38),
    // (50,38): error CS0103: The name 'p10' does not exist in the current context
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p10").WithArguments("p10").WithLocation(50, 38),
    // (67,16): error CS0103: The name 'p13' does not exist in the current context
    //         return p13;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p13").WithArguments("p13").WithLocation(67, 16),
    // (72,16): error CS0103: The name 'p14' does not exist in the current context
    //         return p14;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p14").WithArguments("p14").WithLocation(72, 16)
                );

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel semanticModel = comp.GetSemanticModel(tree);

            var parameterRefs = (from node in tree.GetRoot().DescendantNodes()
                                 where node.CSharpKind() == SyntaxKind.IdentifierName
                                 select (IdentifierNameSyntax)node).
                         Where(node => node.Identifier.ValueText.StartsWith("p")).ToArray();

            Assert.Equal(14, parameterRefs.Length);

            SymbolInfo symInfo;

            foreach (var n in parameterRefs)
            {
                symInfo = semanticModel.GetSymbolInfo(n);

                switch (n.Identifier.ValueText)
                {
                    case "p4":
                    case "p11":
                        Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
                        break;

                    default:
                        Assert.Null(symInfo.Symbol);
                        break;
                }
            }

            var structDecl = (from node in tree.GetRoot().DescendantNodes()
                              where node.CSharpKind() == SyntaxKind.StructDeclaration
                              select (StructDeclarationSyntax)node).Single();

            Assert.Equal("Derived", structDecl.Identifier.ValueText);
            var declaredCtor = semanticModel.GetDeclaredConstructorSymbol(structDecl);
            Assert.Equal("Derived..ctor(System.Int32 p1, [System.Int32 p2 = 1], [System.Int32 p3 = 0], [System.Int32 p4 = 0], [System.Int32 p5 = 0], [System.Int32 p6 = 0], [System.Int32 p7 = 0], [System.Int32 p8 = 0], [System.Int32 p9 = 0], [System.Int32 p10 = 0], [System.Int32 p11 = 0], [System.Int32 p12 = 0], [System.Int32 p13 = 0], [System.Int32 p14 = 0])",
                declaredCtor.ToTestDisplayString());
            Assert.Same(declaredCtor, CSharpExtensions.GetDeclaredConstructorSymbol((CodeAnalysis.SemanticModel)semanticModel, structDecl));

            var p1 = structDecl.ParameterList.Parameters[1];

            Assert.Equal("[System.Int32 p2 = 1]", semanticModel.GetDeclaredSymbol(p1).ToTestDisplayString());

            symInfo = semanticModel.GetSymbolInfo(p1.Default.Value);
            Assert.Equal("System.Int32 Base.f0", symInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void StaticConstructors()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int p1)
{
    static Derived1() : this(p1,0)
    {}
}

class Derived2(int p2)
{
    static Derived2()
    {
        System.Console.WriteLine(p2);
    }
}

struct Derived3(int p3)
{
    static Derived3() : this(p3,0)
    {}
}

struct Derived4(int p4)
{
    static Derived4()
    {
        System.Console.WriteLine(p4);
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,25): error CS0514: 'Derived1': static constructor cannot have an explicit 'this' or 'base' constructor call
    //     static Derived1() : this(p1,0)
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("Derived1").WithLocation(4, 25),
    // (18,25): error CS0514: 'Derived3': static constructor cannot have an explicit 'this' or 'base' constructor call
    //     static Derived3() : this(p3,0)
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("Derived3").WithLocation(18, 25),
    // (26,34): error CS0103: The name 'p4' does not exist in the current context
    //         System.Console.WriteLine(p4);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p4").WithArguments("p4").WithLocation(26, 34),
    // (12,34): error CS0103: The name 'p2' does not exist in the current context
    //         System.Console.WriteLine(p2);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(12, 34)
                );
        }

        [Fact]
        public void StaticMembers_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int p1, int p2 = 0, int p3 = 0, int p4 = 0, int p5 = 0, int p6 = 0, int p7 = 0)
{
    static void Test1()
    {
        System.Console.WriteLine(p1);
    }

    private static int x = p2;

    public static int Item1 
    { 
        get { return p3; } 
        set 
        { 
            System.Console.WriteLine(p4);
        }
    }

    public static event System.Action E1 
    { 
        add 
        { 
            System.Console.WriteLine(p5);
        }
        remove 
        { 
            System.Console.WriteLine(p6);
        }
    }

    static System.Action GetAction(int x)
    {
        return null;
    }

    public static event System.Action E2 = GetAction(p7);
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (9,28): error CS0103: The name 'p2' does not exist in the current context
    //     private static int x = p2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(9, 28),
    // (37,54): error CS0103: The name 'p7' does not exist in the current context
    //     public static event System.Action E2 = GetAction(p7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p7").WithArguments("p7").WithLocation(37, 54),
    // (6,34): error CS0103: The name 'p1' does not exist in the current context
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(6, 34),
    // (13,22): error CS0103: The name 'p3' does not exist in the current context
    //         get { return p3; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p3").WithArguments("p3").WithLocation(13, 22),
    // (16,38): error CS0103: The name 'p4' does not exist in the current context
    //             System.Console.WriteLine(p4);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p4").WithArguments("p4").WithLocation(16, 38),
    // (24,38): error CS0103: The name 'p5' does not exist in the current context
    //             System.Console.WriteLine(p5);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p5").WithArguments("p5").WithLocation(24, 38),
    // (28,38): error CS0103: The name 'p6' does not exist in the current context
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p6").WithArguments("p6").WithLocation(28, 38)
                );
        }

        [Fact]
        public void StaticMembers_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Derived(int p1, int p2 = 0, int p3 = 0, int p4 = 0, int p5 = 0, int p6 = 0, int p7 = 0)
{
    static void Test1()
    {
        System.Console.WriteLine(p1);
    }

    private static int x = p2;

    public static int Item1 
    { 
        get { return p3; } 
        set 
        { 
            System.Console.WriteLine(p4);
        }
    }

    public static event System.Action E1 
    { 
        add 
        { 
            System.Console.WriteLine(p5);
        }
        remove 
        { 
            System.Console.WriteLine(p6);
        }
    }

    static System.Action GetAction(int x)
    {
        return null;
    }

    public static event System.Action E2 = GetAction(p7);
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (9,28): error CS0103: The name 'p2' does not exist in the current context
    //     private static int x = p2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(9, 28),
    // (37,54): error CS0103: The name 'p7' does not exist in the current context
    //     public static event System.Action E2 = GetAction(p7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p7").WithArguments("p7").WithLocation(37, 54),
    // (6,34): error CS0103: The name 'p1' does not exist in the current context
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(6, 34),
    // (13,22): error CS0103: The name 'p3' does not exist in the current context
    //         get { return p3; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p3").WithArguments("p3").WithLocation(13, 22),
    // (16,38): error CS0103: The name 'p4' does not exist in the current context
    //             System.Console.WriteLine(p4);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p4").WithArguments("p4").WithLocation(16, 38),
    // (24,38): error CS0103: The name 'p5' does not exist in the current context
    //             System.Console.WriteLine(p5);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p5").WithArguments("p5").WithLocation(24, 38),
    // (28,38): error CS0103: The name 'p6' does not exist in the current context
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p6").WithArguments("p6").WithLocation(28, 38)
                );
        }

        [Fact]
        public void RefOutParameters_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(int x)
{
}

class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    : Base(p0)
{
    void Test1()
    {
        System.Console.WriteLine(p1);
    }
    Derived(ref int x1, ref int x2, ref int x3, ref int x4, ref int x5, ref int x6, ref int x7, ref int x8, out int x9, out int x10, out int x11, out int x12, out int x13, out int x14) 
        : this(ref p2, ref x1, ref x2, ref x3, ref x4, ref x5, ref x6, ref x7, ref x8, out x9, out x10, out x11, out x12, out x13, out x14)
    {
        System.Console.WriteLine(p3);
    }

    private int x = p4;

    public int Item1 
    { 
        get { return p5; } 
        set 
        { 
            System.Console.WriteLine(p6);
        }
    }

    public int this[int x] 
    { 
        get { return p7; } 
        set 
        { 
            System.Console.WriteLine(p8);
        }
    }

    public event System.Action E1 
    { 
        add 
        { 
            System.Console.WriteLine(p9);
        }
        remove 
        { 
            System.Console.WriteLine(p10);
        }
    }

    static System.Action GetAction(int x)
    {
        return null;
    }

    public event System.Action E2 = GetAction(p11);

    void Test2(int x = p12)
    {
    }

    public static int operator + (Derived x)
    {
        return p13;
    }

    public static explicit operator int (Derived x)
    {
        return p14;
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (58,24): error CS0103: The name 'p12' does not exist in the current context
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p12").WithArguments("p12").WithLocation(58, 24),
    // (58,20): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("?", "int").WithLocation(58, 20),
    // (56,47): error CS0269: Use of unassigned out parameter 'p11'
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p11").WithArguments("p11").WithLocation(56, 47),
    // (6,14): error CS0177: The out parameter 'p9' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p9").WithLocation(6, 14),
    // (6,14): error CS0177: The out parameter 'p10' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p10").WithLocation(6, 14),
    // (6,14): error CS0177: The out parameter 'p11' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p11").WithLocation(6, 14),
    // (6,14): error CS0177: The out parameter 'p12' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p12").WithLocation(6, 14),
    // (6,14): error CS0177: The out parameter 'p13' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p13").WithLocation(6, 14),
    // (6,14): error CS0177: The out parameter 'p14' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p14").WithLocation(6, 14),
    // (11,34): error CS0103: The name 'p1' does not exist in the current context
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(11, 34),
    // (14,20): error CS0103: The name 'p2' does not exist in the current context
    //         : this(ref p2, ref x1, ref x2, ref x3, ref x4, ref x5, ref x6, ref x7, ref x8, out x9, out x10, out x11, out x12, out x13, out x14)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(14, 20),
    // (16,34): error CS0103: The name 'p3' does not exist in the current context
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p3").WithArguments("p3").WithLocation(16, 34),
    // (23,22): error CS0103: The name 'p5' does not exist in the current context
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p5").WithArguments("p5").WithLocation(23, 22),
    // (26,38): error CS0103: The name 'p6' does not exist in the current context
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p6").WithArguments("p6").WithLocation(26, 38),
    // (32,22): error CS0103: The name 'p7' does not exist in the current context
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p7").WithArguments("p7").WithLocation(32, 22),
    // (35,38): error CS0103: The name 'p8' does not exist in the current context
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p8").WithArguments("p8").WithLocation(35, 38),
    // (43,38): error CS0103: The name 'p9' does not exist in the current context
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p9").WithArguments("p9").WithLocation(43, 38),
    // (47,38): error CS0103: The name 'p10' does not exist in the current context
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p10").WithArguments("p10").WithLocation(47, 38),
    // (64,16): error CS0103: The name 'p13' does not exist in the current context
    //         return p13;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p13").WithArguments("p13").WithLocation(64, 16),
    // (69,16): error CS0103: The name 'p14' does not exist in the current context
    //         return p14;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p14").WithArguments("p14").WithLocation(69, 16)
                );
        }

        [Fact]
        public void RefOutParameters_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
{
    void Test1()
    {
        System.Console.WriteLine(p1);
    }
    Derived(ref int x1, ref int x2, ref int x3, ref int x4, ref int x5, ref int x6, out int x7, out int x8, out int x9, out int x10, out int x11, out int x12, out int x13)
     : this(ref p2, ref x1, ref x2, ref x3, ref x4, ref x5, out x6, out x7, out x8, out x9, out x10, out x11, out x12, out x13)
    {
        System.Console.WriteLine(p3);
    }

    private int x = p4;

    public int Item1 
    { 
        get { return p5; } 
        set 
        { 
            System.Console.WriteLine(p6);
        }
    }

    public int this[int x] 
    { 
        get { return p7; } 
        set 
        { 
            System.Console.WriteLine(p8);
        }
    }

    public event System.Action E1 
    { 
        add 
        { 
            System.Console.WriteLine(p9);
        }
        remove 
        { 
            System.Console.WriteLine(p10);
        }
    }

    static System.Action GetAction(int x)
    {
        return null;
    }

    public event System.Action E2 = GetAction(p11);

    void Test2(int x = p12)
    {
    }

    public static int operator + (Derived x)
    {
        return p13;
    }

    public static explicit operator int (Derived x)
    {
        return p14;
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (53,24): error CS0103: The name 'p12' does not exist in the current context
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p12").WithArguments("p12").WithLocation(53, 24),
    // (53,20): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'int'
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("?", "int").WithLocation(53, 20),
    // (51,47): error CS0269: Use of unassigned out parameter 'p11'
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p11").WithArguments("p11").WithLocation(51, 47),
    // (2,15): error CS0177: The out parameter 'p7' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p7").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p8' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p8").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p9' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p9").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p10' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p10").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p11' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p11").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p12' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p12").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p13' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p13").WithLocation(2, 15),
    // (2,15): error CS0177: The out parameter 'p14' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p14").WithLocation(2, 15),
    // (6,34): error CS0103: The name 'p1' does not exist in the current context
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(6, 34),
    // (9,17): error CS0103: The name 'p2' does not exist in the current context
    //      : this(ref p2, ref x1, ref x2, ref x3, ref x4, ref x5, out x6, out x7, out x8, out x9, out x10, out x11, out x12, out x13)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(9, 17),
    // (11,34): error CS0103: The name 'p3' does not exist in the current context
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p3").WithArguments("p3").WithLocation(11, 34),
    // (18,22): error CS0103: The name 'p5' does not exist in the current context
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p5").WithArguments("p5").WithLocation(18, 22),
    // (21,38): error CS0103: The name 'p6' does not exist in the current context
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p6").WithArguments("p6").WithLocation(21, 38),
    // (27,22): error CS0103: The name 'p7' does not exist in the current context
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p7").WithArguments("p7").WithLocation(27, 22),
    // (30,38): error CS0103: The name 'p8' does not exist in the current context
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p8").WithArguments("p8").WithLocation(30, 38),
    // (38,38): error CS0103: The name 'p9' does not exist in the current context
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p9").WithArguments("p9").WithLocation(38, 38),
    // (42,38): error CS0103: The name 'p10' does not exist in the current context
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p10").WithArguments("p10").WithLocation(42, 38),
    // (59,16): error CS0103: The name 'p13' does not exist in the current context
    //         return p13;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p13").WithArguments("p13").WithLocation(59, 16),
    // (64,16): error CS0103: The name 'p14' does not exist in the current context
    //         return p14;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p14").WithArguments("p14").WithLocation(64, 16)
                );
        }

        [Fact]
        public void RefOutParameters_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(out int x)
    {
        x =0;
    }
}

class Derived(out int p0, out int p1)
    : Base(out p0)
{
    private int x = (p1 = 2);
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefOutParameters_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {
        x =0;
    }
}

class Derived(out int p0)
    : Base(p0)
{
    private int x = (p0 = 2);
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefOutParameters_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(out int x)
    {
        x = 0;
    }

    public Base() {}
}

class Derived(out int p0)
    : Base(out p0)
{
    private int x = p0;

    Derived(out int p1, int a) : this(out p1)
    {}

    Derived(out int p2, int a, int b)
    {
        p2 = 0;
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (13,21): error CS0269: Use of unassigned out parameter 'p0'
    //     private int x = p0;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p0").WithArguments("p0"),
    // (20,5): error CS9002: Since this type has a primary constructor, all instance constructor declarations must specify a constructor initializer of the form this([argument-list]).
    //     Derived(out int p2, int a, int b)
    Diagnostic(ErrorCode.ERR_InstanceCtorMustHaveThisInitializer, "Derived")
                );
        }

        [Fact]
        public void RefOutParameters_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(out int p0, out int p1)
{
    private int x = (p0 = 1);
    private int y = p0 + p1;
    private int z = (p1 = 1);
    private int u = p0 + p1;
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (5,26): error CS0269: Use of unassigned out parameter 'p1'
    //     private int y = p0 + p1;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p1").WithArguments("p1").WithLocation(5, 26)
                );
        }


        [Fact]
        public void CapturingOfParameters_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(out int x, out int y, out int z, params System.Func<int> [] u)
    {
        x = 0;
        y = 0;
        z = 0;
    }
}

class Derived(int p0, out int p1, ref int p2, int p3, out int p4, ref int p5, int p6, out int p7, ref int p8)
    : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
{
    System.Func<int> f1 = ()=>p3;
    System.Func<int> f2 = ()=>p4;
    System.Func<int> f3 = ()=>p5;

    event System.Func<int> e1 = ()=>p6;
    event System.Func<int> e2 = ()=>p7;
    event System.Func<int> e3 = ()=>p8;

    void Test(int x0, out int x1, ref int x2)
    {
        x1 = 0;
        System.Func<int> x = ()=>x0;
        x = ()=>x1;
        x = ()=>x2;
    }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (16,31): error CS1628: Cannot use ref or out parameter 'p4' inside an anonymous method, lambda expression, or query expression
    //     System.Func<int> f2 = ()=>p4;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p4").WithArguments("p4").WithLocation(16, 31),
    // (17,31): error CS1628: Cannot use ref or out parameter 'p5' inside an anonymous method, lambda expression, or query expression
    //     System.Func<int> f3 = ()=>p5;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p5").WithArguments("p5").WithLocation(17, 31),
    // (20,37): error CS1628: Cannot use ref or out parameter 'p7' inside an anonymous method, lambda expression, or query expression
    //     event System.Func<int> e2 = ()=>p7;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p7").WithArguments("p7").WithLocation(20, 37),
    // (21,37): error CS1628: Cannot use ref or out parameter 'p8' inside an anonymous method, lambda expression, or query expression
    //     event System.Func<int> e3 = ()=>p8;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p8").WithArguments("p8").WithLocation(21, 37),
    // (13,48): error CS1628: Cannot use ref or out parameter 'p1' inside an anonymous method, lambda expression, or query expression
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p1").WithArguments("p1").WithLocation(13, 48),
    // (13,56): error CS1628: Cannot use ref or out parameter 'p2' inside an anonymous method, lambda expression, or query expression
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p2").WithArguments("p2").WithLocation(13, 56),
    // (16,31): error CS0269: Use of unassigned out parameter 'p4'
    //     System.Func<int> f2 = ()=>p4;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p4").WithArguments("p4").WithLocation(16, 31),
    // (20,37): error CS0269: Use of unassigned out parameter 'p7'
    //     event System.Func<int> e2 = ()=>p7;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p7").WithArguments("p7").WithLocation(20, 37),
    // (13,48): error CS0269: Use of unassigned out parameter 'p1'
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p1").WithArguments("p1").WithLocation(13, 48),
    // (27,17): error CS1628: Cannot use ref or out parameter 'x1' inside an anonymous method, lambda expression, or query expression
    //         x = ()=>x1;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x1").WithArguments("x1").WithLocation(27, 17),
    // (28,17): error CS1628: Cannot use ref or out parameter 'x2' inside an anonymous method, lambda expression, or query expression
    //         x = ()=>x2;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x2").WithArguments("x2").WithLocation(28, 17)
                );
        }

        [Fact]
        public void CapturingOfParameters_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(System.Func<int> u)
    {
        System.Console.WriteLine(u());
    }
}

class Derived(int p0)
    : Base(()=>p0)
{

    public static void Main()
    {
        var d = new Derived(156);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput:"156");
        }

        [Fact]
        public void CapturingOfParameters_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(System.Func<int> u)
{
    public System.Func<int> x0 = u;
}

class Derived(int p0)
    : Base(()=>p0)
{
    int x1 = p0++;
    
    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(""{0} {1}"", d.x0(), d.x1);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "157 156");
        }

        [Fact]
        public void CapturingOfParameters_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(System.Func<int> u, int v)
{
    public System.Func<int> x0 = u;
    public int x1 = v;
}

class Derived(int p0)
    : Base(()=>p0, p0++)
{
    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(""{0} {1}"", d.x0(), d.x1);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "157 156");
        }

        [Fact]
        public void CapturingOfParameters_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int p0)
{
    System.Func<int> f1 = ()=>p0;

    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(d.f1());
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "156");
        }

        [Fact]
        public void CapturingOfParameters_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int p0)
{
    System.Func<int> f1 = ()=>p0;
    int f2 = p0++;

    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(""{0} {1}"", d.f1(), d.f2);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "157 156");
        }

        [Fact]
        public void CapturingOfParameters_07()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(int u)
{
    public int x0 = u;
}

class Derived(int p0) : Base(p0++)
{
    System.Func<int> f1 = ()=>p0;

    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(""{0} {1}"", d.f1(), d.x0);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "157 156");
        }

        [Fact]
        public void CapturingOfParameters_08()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int p0)
{
    System.Func<int> f1 = ()=>p0++;
    event System.Func<int> e1 = ()=>p0++;

    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(""{0} {1} {2}"", d.f1(), d.e1(), d.f1());
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "156 157 158");
        }

        [Fact]
        public void CapturingOfParameters_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base(System.Func<int> u)
{
    public System.Func<int> x0 = u;
}

class Derived(int p0) : Base(()=>p0++)
{
    System.Func<int> f1 = ()=>p0++;

    public static void Main()
    {
        var d = new Derived(156);
        System.Console.WriteLine(""{0} {1} {2}"", d.f1(), d.x0(), d.f1());
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "156 157 158");
        }

        [Fact]
        public void GenericName_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int p1, int p2)
{
    void Test()
    {
        int x1 = p1;
        int x2 = p2<int>(0);
    }
}

", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,18): error CS0103: The name 'p1' does not exist in the current context
    //         int x1 = p1;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(6, 18),
    // (7,18): error CS0103: The name 'p2' does not exist in the current context
    //         int x2 = p2<int>(0);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2<int>").WithArguments("p2").WithLocation(7, 18)
                );
        }

        [Fact]
        public void GenericName_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(int p1, int p2)
{
    int x1 = p1;
    int x2 = p2<int>(0);
}

", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (5,14): error CS0103: The name 'p2' does not exist in the current context
    //     int x2 = p2<int>(0);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2<int>").WithArguments("p2").WithLocation(5, 14)
                );
        }

        [Fact]
        public void FlowAnalysis()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(ref int p1, ref int p2, ref int p3, ref int p4)
{
    Derived1(int p5) : this(ref p1, ref p5, ref p5, ref p5)
    {
    }

    static int Test(ref int p6)
    {
        return 1;
    }

    void Test()
    {
        Test(ref p2);
    }

    int f1 = Test(ref p3);

    static int f2 = Test(ref p4);
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (20,30): error CS0103: The name 'p4' does not exist in the current context
    //     static int f2 = Test(ref p4);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p4").WithArguments("p4").WithLocation(20, 30),
    // (4,33): error CS0103: The name 'p1' does not exist in the current context
    //     Derived1(int p5) : this(ref p1, ref p5, ref p5, ref p5)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(4, 33),
    // (15,18): error CS0103: The name 'p2' does not exist in the current context
    //         Test(ref p2);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(15, 18)
                );
        }

        [Fact]
        public void Emit_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {
        System.Console.WriteLine(x);
    }
}

class Derived(int x, int y, int z, int u, int v, int w) : Base(x)
{
    private int z = z;
    private int v = v;
    private int w = w;

    public int V
    {
        get 
        {
            return v;
        }
    }

    public int Fy = y;

    public int Z
    {
        get 
        {
            return z;
        }
    }

    public int Fu = u;

    public int W
    {
        get 
        {
            return w;
        }
        set
        {
            w = value;
        }
    }
}

class Program
{
    public static void Main()
    {
        var d = new Derived(1,2,3,4,5,6);

        System.Console.WriteLine(d.Fy);
        System.Console.WriteLine(d.Z);
        System.Console.WriteLine(d.Fu);
        System.Console.WriteLine(d.V);
        System.Console.WriteLine(d.W);
        d.W=60;
        System.Console.WriteLine(d.W);
    }
}
", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            System.Action<ModuleSymbol> validator = delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                foreach (var name in new[] { "x", "y", "u" })
                {
                    Assert.Equal(0, derived.GetMembers(name).Length);
                }

                foreach (var name in new[] { "z", "v", "w" })
                {
                    var f = derived.GetMember<FieldSymbol>(name);
                    Assert.False(f.IsStatic);
                    Assert.False(f.IsReadOnly);
                    Assert.Equal(Accessibility.Private, f.DeclaredAccessibility);
                }
            };

            var verifier = CompileAndVerify(comp, expectedOutput: 
@"1
2
3
4
5
6
60", sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void Emit_01_Err()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {
        System.Console.WriteLine(x);
    }
}

class Derived(int x, int y, int z, int u, int v, int w) : Base(x)
{
    public int V
    {
        get 
        {
            return v;
        }
    }

    public int Fy = y;

    public int Z
    {
        get 
        {
            return z;
        }
    }

    public int Fu = u;

    public int W
    {
        get 
        {
            return w;
        }
        set
        {
            w = value;
        }
    }
}

class Program
{
    public static void Main()
    {
    }
}
", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (16,20): error CS0103: The name 'v' does not exist in the current context
    //             return v;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(16, 20),
    // (26,20): error CS0103: The name 'z' does not exist in the current context
    //             return z;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(26, 20),
    // (36,20): error CS0103: The name 'w' does not exist in the current context
    //             return w;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(36, 20),
    // (40,13): error CS0103: The name 'w' does not exist in the current context
    //             w = value;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(40, 13)

                );
        }

        [Fact]
        public void Emit_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Derived(int x, int y, int z, int u, int v, int w)
{
    private int v = v;
    private int w = w;

    public int V()
    {
        return v;
    }

    public int W
    {
        get 
        {
            return w;
        }
        set
        {
            w = value;
        }
    }
}

class Program
{
    public static void Main()
    {
        var d = new Derived(1,2,3,4,5,6);

        System.Console.WriteLine(d.V());
        System.Console.WriteLine(d.W);
        d.W=60;
        System.Console.WriteLine(d.W);
    }
}
", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            System.Action<ModuleSymbol> validator = delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                foreach (var name in new[] { "x", "y", "z", "u" })
                {
                    Assert.Equal(0, derived.GetMembers(name).Length);
                }

                foreach (var name in new[] { "v", "w" })
                {
                    var f = derived.GetMember<FieldSymbol>(name);
                    Assert.False(f.IsStatic);
                    Assert.False(f.IsReadOnly);
                    Assert.Equal(Accessibility.Private, f.DeclaredAccessibility);
                }
            };

            var verifier = CompileAndVerify(comp, expectedOutput:
@"5
6
60", sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void Emit_02_Err()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Derived(int x, int y, int z, int u, int v, int w)
{
    public int V()
    {
        return v;
    }

    public int W
    {
        get 
        {
            return w;
        }
        set
        {
            w = value;
        }
    }
}

class Program
{
    public static void Main()
    {
    }
}
", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,16): error CS0103: The name 'v' does not exist in the current context
    //         return v;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(6, 16),
    // (13,20): error CS0103: The name 'w' does not exist in the current context
    //             return w;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(13, 20),
    // (17,13): error CS0103: The name 'w' does not exist in the current context
    //             w = value;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(17, 13)

                );
        }

        [Fact]
        public void Emit_04_Err()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int v)
{
    public  System.Func<int> GetV
    {
        get 
        {
            return ()=>v;
        }
    }

    public  System.Action<int> SetV
    {
        get 
        {
            return (int x)=> v=x;
        }
    }
}

class Program
{
    public static void Main()
    {
    }
}
", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (8,24): error CS0103: The name 'v' does not exist in the current context
    //             return ()=>v;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(8, 24),
    // (16,30): error CS0103: The name 'v' does not exist in the current context
    //             return (int x)=> v=x;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(16, 30)
                );
        }

        [Fact]
        public void BaseClassInitializerSemanticModel_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {}

    public Base(long x)
    {}
}

//class Derived : Base
//{
//    Derived(byte x) : base(x) {}
//}

class Derived(byte x)
    : Base(x)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel semanticModel = comp.GetSemanticModel(tree);

            var initializer = (from node in tree.GetRoot().DescendantNodes()
                                 where node.CSharpKind() == SyntaxKind.BaseClassWithArguments 
                                 select (BaseClassWithArgumentsSyntax)node).Single();

            SymbolInfo symInfo;

            symInfo = semanticModel.GetSymbolInfo(initializer);
            Assert.Equal("Base..ctor(System.Int32 x)", symInfo.Symbol.ToTestDisplayString());
            Assert.Same(symInfo.Symbol, ((CodeAnalysis.SemanticModel)semanticModel).GetSymbolInfo((SyntaxNode)initializer).Symbol);
            Assert.Same(symInfo.Symbol, CSharpExtensions.GetSymbolInfo((CodeAnalysis.SemanticModel)semanticModel, initializer).Symbol);

            TypeInfo typeInfo;
            typeInfo = semanticModel.GetTypeInfo(initializer);
            Assert.Equal("System.Void", typeInfo.Type.ToTestDisplayString());
            Assert.Same(typeInfo.Type, ((CodeAnalysis.SemanticModel)semanticModel).GetTypeInfo((SyntaxNode)initializer).Type);
            Assert.Same(typeInfo.Type, CSharpExtensions.GetTypeInfo((CodeAnalysis.SemanticModel)semanticModel, initializer).Type);

            ImmutableArray<ISymbol> memberGroup;
            memberGroup = semanticModel.GetMemberGroup(initializer);
            Assert.Equal(0, memberGroup.Length);
            Assert.Equal(0, ((CodeAnalysis.SemanticModel)semanticModel).GetMemberGroup((SyntaxNode)initializer).Length);
            Assert.Equal(0, CSharpExtensions.GetMemberGroup((CodeAnalysis.SemanticModel)semanticModel, initializer).Length);
        }

        [Fact]
        public void CtorAttributes_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Class)]
public class TypeAttribute : System.Attribute
{
}

[TypeAttribute]
class Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetAttributes().Single().ToString());
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").GetAttributes().Length);

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal("TypeAttribute", derived.GetAttributes().Single().ToString());

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal(0, ctor.GetAttributes().Length);
            });
        }

        [Fact]
        public void CtorAttributes_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Class)]
public class TypeAttribute : System.Attribute
{
}

[type: TypeAttribute]
class Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetAttributes().Single().ToString());
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").GetAttributes().Length);

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal("TypeAttribute", derived.GetAttributes().Single().ToString());

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal(0, ctor.GetAttributes().Length);
            });
        }

        [Fact]
        public void CtorAttributes_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Struct)]
public class TypeAttribute : System.Attribute
{
}

[TypeAttribute]
struct Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetAttributes().Single().ToString());
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(".ctor").
                OfType<MethodSymbol>().Where(m => m.ParameterCount == 1).Single().GetAttributes().Length);

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal("TypeAttribute", derived.GetAttributes().Single().ToString());

                var ctor = derived.GetMembers(".ctor").OfType<MethodSymbol>().Where(c => c.ParameterCount == 1).Single();
                Assert.Equal(0, ctor.GetAttributes().Length);
            });
        }

        [Fact]
        public void CtorAttributes_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Struct)]
public class TypeAttribute : System.Attribute
{
}

[type: TypeAttribute]
struct Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetAttributes().Single().ToString());
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(".ctor").
                OfType<MethodSymbol>().Where(m => m.ParameterCount == 1).Single().GetAttributes().Length);

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal("TypeAttribute", derived.GetAttributes().Single().ToString());

                var ctor = derived.GetMembers(".ctor").OfType<MethodSymbol>().Where(c => c.ParameterCount == 1).Single();
                Assert.Equal(0, ctor.GetAttributes().Length);
            });
        }

        [Fact]
        public void CtorAttributes_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Class)]
public class TypeAttribute : System.Attribute
{
}

[method: TypeAttribute]
class Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetAttributes().Length);
            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").GetAttributes().Single().ToString());

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,10): error CS0592: Attribute 'TypeAttribute' is not valid on this declaration type. It is only valid on 'class' declarations.
    // [method: TypeAttribute]
    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TypeAttribute").WithArguments("TypeAttribute", "class")
                );
        }

        [Fact]
        public void CtorAttributes_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Struct)]
public class TypeAttribute : System.Attribute
{
}

[method: TypeAttribute]
struct Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetAttributes().Length);
            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetMembers(".ctor").
                OfType<MethodSymbol>().Where(m => m.ParameterCount == 1).Single().GetAttributes().Single().ToString());

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,10): error CS0592: Attribute 'TypeAttribute' is not valid on this declaration type. It is only valid on 'struct' declarations.
    // [method: TypeAttribute]
    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TypeAttribute").WithArguments("TypeAttribute", "struct")
                );
        }

        [Fact]
        public void CtorAttributes_07()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Constructor)]
public class CtorAttribute : System.Attribute
{
}

[method: CtorAttribute]
class Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetAttributes().Length);
            Assert.Equal("CtorAttribute", comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").GetAttributes().Single().ToString());

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal(0, derived.GetAttributes().Length);

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("CtorAttribute", ctor.GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void CtorAttributes_08()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Constructor)]
public class CtorAttribute : System.Attribute
{
}

[method: CtorAttribute]
struct Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetAttributes().Length);
            Assert.Equal("CtorAttribute", comp.GetTypeByMetadataName("Derived").GetMembers(".ctor").
                OfType<MethodSymbol>().Where(m => m.ParameterCount == 1).Single().GetAttributes().Single().ToString());

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal(0, derived.GetAttributes().Length);

                var ctor = derived.GetMembers(".ctor").OfType<MethodSymbol>().Where(c => c.ParameterCount == 1).Single();
                Assert.Equal("CtorAttribute", ctor.GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void CtorAttributes_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Constructor)]
public class CtorAttribute : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class TypeAttribute : System.Attribute
{
}

[method: CtorAttribute][TypeAttribute]
class Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetAttributes().Single().ToString());
            Assert.Equal("CtorAttribute", comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").GetAttributes().Single().ToString());

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal("TypeAttribute", derived.GetAttributes().Single().ToString());

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("CtorAttribute", ctor.GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void CtorAttributes_10()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Constructor)]
public class CtorAttribute : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Struct)]
public class TypeAttribute : System.Attribute
{
}

[method: CtorAttribute][TypeAttribute]
struct Derived(int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            Assert.Equal("TypeAttribute", comp.GetTypeByMetadataName("Derived").GetAttributes().Single().ToString());
            Assert.Equal("CtorAttribute", comp.GetTypeByMetadataName("Derived").GetMembers(".ctor").
                OfType<MethodSymbol>().Where(m => m.ParameterCount == 1).Single().GetAttributes().Single().ToString());

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.Equal("TypeAttribute", derived.GetAttributes().Single().ToString());

                var ctor = derived.GetMembers(".ctor").OfType<MethodSymbol>().Where(c => c.ParameterCount == 1).Single();
                Assert.Equal("CtorAttribute", ctor.GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void FieldAttributes_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([ParameterAttribute] int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.False(derived.GetMembers().OfType<FieldSymbol>().Any());

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("ParameterAttribute", ctor.Parameters[0].GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void FieldAttributes_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([param: ParameterAttribute] int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.False(derived.GetMembers().OfType<FieldSymbol>().Any());

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("ParameterAttribute", ctor.Parameters[0].GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void FieldAttributes_05_1()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([field: ParameterAttribute] int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,16): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
    // class Derived([field: ParameterAttribute] int v)
    Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(7, 16)
                );
        }

        [Fact]
        public void FieldAttributes_07_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Field)]
public class FieldAttribute : System.Attribute
{
}

class Derived([field: FieldAttribute] int v)
{
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,16): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
    // class Derived([field: FieldAttribute] int v)
    Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(7, 16)
                );

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal(0, ctor.Parameters[0].GetAttributes().Length);

                Assert.Equal(0, derived.GetMembers("v").Length);
            });
        }

        [Fact]
        public void FieldAttributes_08_1()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Field)]
public class FieldAttribute : System.Attribute
{
}

class Derived([field: FieldAttribute] int v)
{
    public int V { get { return v; } }
}
", options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,16): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
    // class Derived([field: FieldAttribute] int v)
    Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(7, 16)
                );
        }

        [Fact]
        public void ParameterWithFields_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(private int v)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS1026: ) expected
    // class Test(private int v)
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "private").WithLocation(2, 12),
    // (2,12): error CS1514: { expected
    // class Test(private int v)
    Diagnostic(ErrorCode.ERR_LbraceExpected, "private").WithLocation(2, 12),
    // (2,12): error CS1513: } expected
    // class Test(private int v)
    Diagnostic(ErrorCode.ERR_RbraceExpected, "private").WithLocation(2, 12),
    // (2,25): error CS1002: ; expected
    // class Test(private int v)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(2, 25),
    // (2,25): error CS1022: Type or namespace definition, or end-of-file expected
    // class Test(private int v)
    Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(2, 25),
    // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
    // }
    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1)
                );
        }

        [Fact, WorkItem(4)]
        public void ArgumentsOnImplementedInterface_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System;

class C1() : IDisposable()
{
    public void Dispose() { }
}

class C2() : IDisposable(2)
{
    public void Dispose() { }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,25): error CS9013: Implemented interface cannot have arguments.
    // class C1() : IDisposable()
    Diagnostic(ErrorCode.ERR_ImplementedInterfaceWithArguments, "()").WithLocation(4, 25),
    // (9,25): error CS9013: Implemented interface cannot have arguments.
    // class C2() : IDisposable(2)
    Diagnostic(ErrorCode.ERR_ImplementedInterfaceWithArguments, "(2)").WithLocation(9, 25)
                );
        }

        [Fact, WorkItem(4)]
        public void ArgumentsOnImplementedInterface_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System;

class Base
{
    public Base(int x){}
}

partial class Derived : Base
{
}

partial class Derived() : IDisposable(2)
{
    public void Dispose() { }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (13,38): error CS9013: Implemented interface cannot have arguments.
    // partial class Derived() : IDisposable(2)
    Diagnostic(ErrorCode.ERR_ImplementedInterfaceWithArguments, "(2)").WithLocation(13, 38),
    // (13,22): error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'Base.Base(int)'
    // partial class Derived() : IDisposable(2)
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "()").WithArguments("x", "Base.Base(int)").WithLocation(13, 22)
                );
        }

        [Fact]
        public void Body_DisabledByDefault()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived()
{
    public readonly int X;

    { X = 5; }
}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived()).X);
    }
}
", options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (2,14): error CS8058: Feature 'primary constructor' is only available in 'experimental' language version.
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "()").WithArguments("primary constructor"),
                // (6,5): error CS8058: Feature 'primary constructor' is only available in 'experimental' language version.
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "{ X = 5; }").WithArguments("primary constructor"));
        }

        [Fact]
        public void Body_00()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived
{
    public readonly int X;

    {
        X = 5;
    }
}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived()).X);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,5): error CS1519: Invalid token '{' in class, struct, or interface member declaration
    //     {
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(6, 5),
    // (7,11): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //         X = 5;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(7, 11),
    // (7,11): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //         X = 5;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(7, 11),
    // (9,1): error CS1022: Type or namespace definition, or end-of-file expected
    // }
    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(9, 1),
    // (4,25): warning CS0649: Field 'Derived.X' is never assigned to, and will always have its default value 0
    //     public readonly int X;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "X").WithArguments("Derived.X", "0").WithLocation(4, 25)
                );
        }

        [Fact, WorkItem(1003200)]
        public void Body_01()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived()
{
    public readonly int X;

    {
        X = 5;
    }
}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived()).X);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp, expectedOutput: "5").VerifyDiagnostics();
        }

        [Fact, WorkItem(1003200)]
        public void Body_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x)
{
    {
        X = x;
    }

    public readonly int X;
}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived(6)).X);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            CompileAndVerify(comp, expectedOutput: "6").VerifyDiagnostics();
        }

        [Fact, WorkItem(1003200)]
        public void Body_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Derived(int x)
{
    {
        X = x;
    }

    public readonly int X;
}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived(6)).X);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: "6").VerifyDiagnostics();
        }

        [Fact]
        public void Body_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived()
{
    {
        X = 5;
    };

    public readonly int X;
}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived()).X);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,6): error CS1597: Semicolon after method or accessor block is not valid
    //     };
    Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(6, 6)
                );
        }

        [Fact]
        public void Body_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived()
{
    {
    }

    public readonly int X;

    { // duplicate #1
        X = 5;
    }

    {} // duplicate #2

}

class Program
{
    public static void Main()
    {
        System.Console.WriteLine((new Derived()).X);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (9,5): error CS8040: Primary constructor already has a body.
    //     { // duplicate #1
    Diagnostic(ErrorCode.ERR_DuplicatePrimaryCtorBody, @"{ // duplicate #1
        X = 5;
    }").WithLocation(9, 5),
    // (13,5): error CS8040: Primary constructor already has a body.
    //     {} // duplicate #2
    Diagnostic(ErrorCode.ERR_DuplicatePrimaryCtorBody, "{}").WithLocation(13, 5),
    // (7,25): warning CS0649: Field 'Derived.X' is never assigned to, and will always have its default value 0
    //     public readonly int X;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "X").WithArguments("Derived.X", "0").WithLocation(7, 25)
                );
        }

        [Fact]
        public void Body_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
partial class Derived(int x)
{
    {
        int x = 5;
        System.Console.WriteLine(x);
        int y = 6;
        System.Console.WriteLine(y);
    }
}

partial class Derived(short y)
{
    { 
        int y = 7;
        System.Console.WriteLine(y);
        int x = 8;
        System.Console.WriteLine(x);
    }
}

class Program
{
    public static void Main()
    {
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (10,22): error CS8036: Only one part of a partial type can declare primary constructor parameters.
    // partial class Derived(short y)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "(short y)").WithLocation(12, 22),
    // (5,13): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         int x = 5;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(5, 13),
    // (13,13): error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         int y = 7;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(15, 13)
                );
        }

        [Fact]
        public void Body_07()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(int x, int y)
{
    {
        int x = 5; // first
        System.Console.WriteLine(x);
    }

    {
        int x = 5;
        System.Console.WriteLine(x);
    }
}

class Program
{
    public static void Main()
    {
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (9,5): error CS8040: Primary constructor already has a body.
    //     {
    Diagnostic(ErrorCode.ERR_DuplicatePrimaryCtorBody, @"{
        int x = 5;
        System.Console.WriteLine(x);
    }").WithLocation(9, 5),
    // (5,13): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         int x = 5; // first
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(5, 13)
                );
        }

        [Fact, WorkItem(1003200)]
        public void Body_08()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int a)
    {
        System.Console.WriteLine(a);
    }
}

class Derived(int x, int y) : Base(y)
{
    {
        System.Console.WriteLine(x);
    }
}

class Program
{
    public static void Main()
    {
        var x = new Derived(5, 6);
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: @"6
5").VerifyDiagnostics();
        }

        [Fact, WorkItem(1003200)]
        public void Body_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived()
{
    {
        System.Console.WriteLine(5);
    }

    private int X = GetX();

    private static int GetX()
    {
        System.Console.WriteLine(7);
        return 0;
    }
}

class Program
{
    public static void Main()
    {
        var x = new Derived();
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: @"7
5").VerifyDiagnostics();
        }

        [Fact, WorkItem(1003200)]
        public void Body_10()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int a)
    {
        System.Console.WriteLine(a);
    }
}

class Derived() : Base(6)
{
    {
        System.Console.WriteLine(5);
    }

    private int X = GetX();

    private static int GetX()
    {
        System.Console.WriteLine(7);
        return 0;
    }
}

class Program
{
    public static void Main()
    {
        var x = new Derived();
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var verifier = CompileAndVerify(comp, expectedOutput: @"7
6
5").VerifyDiagnostics();
        }

        [Fact]
        public void Body_11()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int a)
    {
    }
}

partial class Derived(int x, int y) : Base(v)
{
    {
        System.Console.WriteLine(u);
    }
}

partial class Derived(int u, int v) : Base(y)
{
    { 
        System.Console.WriteLine(x);
    }
}

class Program
{
    public static void Main()
    {
    }
}
", options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (16,22): error CS8036: Only one part of a partial type can declare primary constructor parameters.
    // partial class Derived(int u, int v) : Base(y)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "(int u, int v)").WithLocation(16, 22),
    // (9,44): error CS0103: The name 'v' does not exist in the current context
    // partial class Derived(int x, int y) : Base(v)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(9, 44),
    // (12,34): error CS0103: The name 'u' does not exist in the current context
    //         System.Console.WriteLine(u);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u").WithArguments("u").WithLocation(12, 34),
    // (16,44): error CS0103: The name 'y' does not exist in the current context
    // partial class Derived(int u, int v) : Base(y)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(16, 44),
    // (19,34): error CS0103: The name 'x' does not exist in the current context
    //         System.Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(19, 34)
                );
        }

        [Fact, WorkItem(965894, "DevDiv")]
        public void Bug965894()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System;

namespace CSharpVNext
{
    class Book(string title)
    {
        public string Title{ get; } = title;
    }

    class Magazine(string title) : Book(title), IDisposable
    {}
}
", options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (11,49): error CS0535: 'CSharpVNext.Magazine' does not implement interface member 'System.IDisposable.Dispose()'
    //     class Magazine(string title) : Book(title), IDisposable
    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IDisposable").WithArguments("CSharpVNext.Magazine", "System.IDisposable.Dispose()").WithLocation(11, 49)
                );
        }

    }
}