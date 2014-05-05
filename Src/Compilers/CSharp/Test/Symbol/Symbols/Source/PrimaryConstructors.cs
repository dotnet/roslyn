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
    // (2,11): error CS1514: { expected
    // class Test(int x)
    Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 11),
    // (2,11): error CS1513: } expected
    // class Test(int x)
    Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 11),
    // (2,11): error CS1022: Type or namespace definition, or end-of-file expected
    // class Test(int x)
    Diagnostic(ErrorCode.ERR_EOFExpected, "(").WithLocation(2, 11),
    // (2,17): error CS1002: ; expected
    // class Test(int x)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(2, 17),
    // (2,17): error CS1022: Type or namespace definition, or end-of-file expected
    // class Test(int x)
    Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(2, 17),
    // (3,2): error CS1022: Type or namespace definition, or end-of-file expected
    // {}
    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(3, 2)
                );
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
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
    // (3,2): error CS1026: ) expected
    // {}
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "}"),
    // (3,2): error CS1514: { expected
    // {}
    Diagnostic(ErrorCode.ERR_LbraceExpected, "}")
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
", compOptions: TestOptions.Exe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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

            comp.VerifyDiagnostics(
    // (9,10): error CS0102: The type 'Derived2' already contains a definition for 'T'
    //     void T(){}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "T").WithArguments("Derived2", "T")
                );
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

            comp.VerifyDiagnostics(
    // (13,16): error CS0102: The type 'Derived2' already contains a definition for 'Item'
    //     public int this[int x] 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("Derived2", "Item"),
    // (15,9): error CS0102: The type 'Derived2' already contains a definition for 'get_Item'
    //         get { return 0; } 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("Derived2", "get_Item"),
    // (16,9): error CS0102: The type 'Derived2' already contains a definition for 'set_Item'
    //         set {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("Derived2", "set_Item")
                );
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

            comp.VerifyDiagnostics(
    // (4,16): error CS0102: The type 'Derived2' already contains a definition for 'Item'
    //     public int this[int x] 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("Derived2", "Item"),
    // (6,9): error CS0102: The type 'Derived2' already contains a definition for 'get_Item'
    //         get { return 0; } 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("Derived2", "get_Item")
                );
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

            comp.VerifyDiagnostics(
    // (4,16): error CS0102: The type 'Derived2' already contains a definition for 'Item'
    //     public int this[int x] 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "this").WithArguments("Derived2", "Item"),
    // (6,9): error CS0102: The type 'Derived2' already contains a definition for 'set_Item'
    //         set {} 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("Derived2", "set_Item")
                );
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

            comp.VerifyDiagnostics(
    // (13,16): error CS0102: The type 'Derived2' already contains a definition for 'Item1'
    //     public int Item1
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item1").WithArguments("Derived2", "Item1"),
    // (15,9): error CS0102: The type 'Derived2' already contains a definition for 'get_Item1'
    //         get { return 0; } 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("Derived2", "get_Item1"),
    // (16,9): error CS0102: The type 'Derived2' already contains a definition for 'set_Item1'
    //         set {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("Derived2", "set_Item1"),
    // (30,16): error CS0102: The type 'Derived4' already contains a definition for 'Item3'
    //     public int Item3 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item3").WithArguments("Derived4", "Item3"),
    // (32,9): error CS0102: The type 'Derived4' already contains a definition for 'get_Item3'
    //         get { return 0; } 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "get").WithArguments("Derived4", "get_Item3"),
    // (22,16): error CS0102: The type 'Derived3' already contains a definition for 'Item2'
    //     public int Item2
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item2").WithArguments("Derived3", "Item2"),
    // (24,9): error CS0102: The type 'Derived3' already contains a definition for 'set_Item2'
    //         set {}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "set").WithArguments("Derived3", "set_Item2")
                );
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
    // (9,32): error CS0102: The type 'Derived2' already contains a definition for 'Item1'
    //     public event System.Action Item1; 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item1").WithArguments("Derived2", "Item1"),
    // (9,32): error CS0102: The type 'Derived2' already contains a definition for 'add_Item1'
    //     public event System.Action Item1; 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item1").WithArguments("Derived2", "add_Item1"),
    // (9,32): error CS0102: The type 'Derived2' already contains a definition for 'remove_Item1'
    //     public event System.Action Item1; 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item1").WithArguments("Derived2", "remove_Item1"),
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

            comp.VerifyDiagnostics(
    // (13,32): error CS0102: The type 'Derived2' already contains a definition for 'Item1'
    //     public event System.Action Item1 
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item1").WithArguments("Derived2", "Item1"),
    // (15,9): error CS0102: The type 'Derived2' already contains a definition for 'add_Item1'
    //         add{}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "add").WithArguments("Derived2", "add_Item1"),
    // (16,9): error CS0102: The type 'Derived2' already contains a definition for 'remove_Item1'
    //         remove{}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "remove").WithArguments("Derived2", "remove_Item1")
                );
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

            comp.VerifyDiagnostics(
    // (10,11): error CS0102: The type 'Derived2' already contains a definition for 'Item1'
    //     class Item1
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Item1").WithArguments("Derived2", "Item1")
                );
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
    // (61,24): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p12").WithLocation(61, 24),
    // (61,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p12").WithArguments("x").WithLocation(61, 24),
    // (14,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1").WithLocation(14, 34),
    // (19,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p3").WithLocation(19, 34),
    // (17,22): error CS9005: Constructor initializer cannot access the parameters to a primary constructor.
    //     Derived() : this(p2)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterInConstructorInitializer, "p2").WithLocation(17, 22),
    // (26,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p5").WithLocation(26, 22),
    // (29,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p6").WithLocation(29, 38),
    // (35,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p7").WithLocation(35, 22),
    // (38,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p8").WithLocation(38, 38),
    // (46,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p9").WithLocation(46, 38),
    // (50,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p10").WithLocation(50, 38),
    // (67,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p13;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p13").WithLocation(67, 16),
    // (72,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p14;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p14").WithLocation(72, 16)
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
                Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
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
    // (22,17): error CS0573: 'Derived.x': cannot have instance field initializers in structs
    //     private int x = p4;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "x").WithArguments("Derived.x").WithLocation(22, 17),
    // (59,32): error CS0573: 'Derived.E2': cannot have instance field initializers in structs
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "E2").WithArguments("Derived.E2").WithLocation(59, 32),
    // (61,24): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p12").WithLocation(61, 24),
    // (61,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p12").WithArguments("x").WithLocation(61, 24),
    // (14,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1").WithLocation(14, 34),
    // (19,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p3").WithLocation(19, 34),
    // (17,28): error CS9005: Constructor initializer cannot access the parameters to a primary constructor.
    //     Derived(long x) : this(p2)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterInConstructorInitializer, "p2").WithLocation(17, 28),
    // (26,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p5").WithLocation(26, 22),
    // (29,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p6").WithLocation(29, 38),
    // (35,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p7").WithLocation(35, 22),
    // (38,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p8").WithLocation(38, 38),
    // (46,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p9").WithLocation(46, 38),
    // (50,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p10").WithLocation(50, 38),
    // (67,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p13;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p13").WithLocation(67, 16),
    // (72,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p14;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p14").WithLocation(72, 16)
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
                Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
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
        public void ParameterVisibility_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {}

    public const int f0 = 1;
}
class Derived(private int p0, private int p1 = Base.f0, private int p2 = 0, private int p3 = 0, private int p4 = 0, private int p5 = 0, private int p6 = 0, private int p7 = 0, private int p8 = 0, private int p9 = 0, private int p10 = 0, private int p11 = 0, private int p12 = 0, private int p13 = 0, private int p14 = 0)
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
    // (61,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p12").WithArguments("x").WithLocation(61, 24),
    // (17,22): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.p2'
    //     Derived() : this(p2)
    Diagnostic(ErrorCode.ERR_ObjectRequired, "p2").WithArguments("Derived.p2").WithLocation(17, 22),
    // (67,16): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.p13'
    //         return p13;
    Diagnostic(ErrorCode.ERR_ObjectRequired, "p13").WithArguments("Derived.p13").WithLocation(67, 16),
    // (72,16): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.p14'
    //         return p14;
    Diagnostic(ErrorCode.ERR_ObjectRequired, "p14").WithArguments("Derived.p14").WithLocation(72, 16)
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

                switch (symInfo.Symbol.Name)
                {
                    case "p0":
                    case "p4":
                    case "p11":
                        Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
                        break;
                    default:
                        Assert.Equal(SymbolKind.Field, symInfo.Symbol.Kind);
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
        public void ParameterVisibility_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    public Base(int x)
    {}

    public const int f0 = 1;
}

struct Derived(private int p1, private int p2 = Base.f0, private int p3 = 0, private int p4 = 0, private int p5 = 0, private int p6 = 0, private int p7 = 0, private int p8 = 0, private int p9 = 0, private int p10 = 0, private int p11 = 0, private int p12 = 0, private int p13 = 0, private int p14 = 0)
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
    // (22,17): error CS0573: 'Derived.x': cannot have instance field initializers in structs
    //     private int x = p4;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "x").WithArguments("Derived.x").WithLocation(22, 17),
    // (59,32): error CS0573: 'Derived.E2': cannot have instance field initializers in structs
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "E2").WithArguments("Derived.E2").WithLocation(59, 32),
    // (61,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p12").WithArguments("x").WithLocation(61, 24),
    // (17,28): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.p2'
    //     Derived(long x) : this(p2)
    Diagnostic(ErrorCode.ERR_ObjectRequired, "p2").WithArguments("Derived.p2").WithLocation(17, 28),
    // (17,28): error CS0170: Use of possibly unassigned field 'p2'
    //     Derived(long x) : this(p2)
    Diagnostic(ErrorCode.ERR_UseDefViolationField, "p2").WithArguments("p2").WithLocation(17, 28),
    // (67,16): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.p13'
    //         return p13;
    Diagnostic(ErrorCode.ERR_ObjectRequired, "p13").WithArguments("Derived.p13").WithLocation(67, 16),
    // (72,16): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.p14'
    //         return p14;
    Diagnostic(ErrorCode.ERR_ObjectRequired, "p14").WithArguments("Derived.p14").WithLocation(72, 16)
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

                switch (symInfo.Symbol.Name)
                {
                    case "p4":
                    case "p11":
                        Assert.Equal(SymbolKind.Parameter, symInfo.Symbol.Kind);
                        break;
                    default:
                        Assert.Equal(SymbolKind.Field, symInfo.Symbol.Kind);
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
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("Derived1"),
    // (18,25): error CS0514: 'Derived3': static constructor cannot have an explicit 'this' or 'base' constructor call
    //     static Derived3() : this(p3,0)
    Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("Derived3"),
    // (26,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p4);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p4"),
    // (12,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p2);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p2")
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
    // (9,28): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     private static int x = p2;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p2"),
    // (37,54): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     public static event System.Action E2 = GetAction(p7);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p7"),
    // (6,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1"),
    // (13,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p3; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p3"),
    // (16,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p4);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p4"),
    // (24,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p5);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p5"),
    // (28,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p6")
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
    // (9,28): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     private static int x = p2;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p2"),
    // (37,54): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     public static event System.Action E2 = GetAction(p7);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p7"),
    // (6,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1"),
    // (13,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p3; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p3"),
    // (16,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p4);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p4"),
    // (24,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p5);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p5"),
    // (28,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p6")
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
    // (58,24): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p12"),
    // (58,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p12").WithArguments("x"),
    // (56,47): error CS0269: Use of unassigned out parameter 'p11'
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p11").WithArguments("p11"),
    // (6,14): error CS0177: The out parameter 'p9' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p9"),
    // (6,14): error CS0177: The out parameter 'p10' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p10"),
    // (6,14): error CS0177: The out parameter 'p11' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p11"),
    // (6,14): error CS0177: The out parameter 'p12' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p12"),
    // (6,14): error CS0177: The out parameter 'p13' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p13"),
    // (6,14): error CS0177: The out parameter 'p14' must be assigned to before control leaves the current method
    // class Derived(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p0, ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, ref int p7, ref int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p14"),
    // (11,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1"),
    // (16,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p3"),
    // (14,22): error CS9005: Constructor initializer cannot access the parameters to a primary constructor.
    //     Derived() : this(p2)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterInConstructorInitializer, "p2"),
    // (23,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p5"),
    // (26,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p6"),
    // (32,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p7"),
    // (35,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p8"),
    // (43,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p9"),
    // (47,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p10"),
    // (64,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p13;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p13"),
    // (69,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p14;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p14")
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
    // (14,17): error CS0573: 'Derived.x': cannot have instance field initializers in structs
    //     private int x = p4;
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "x").WithArguments("Derived.x"),
    // (51,32): error CS0573: 'Derived.E2': cannot have instance field initializers in structs
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "E2").WithArguments("Derived.E2"),
    // (53,24): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p12"),
    // (53,24): error CS1736: Default parameter value for 'x' must be a compile-time constant
    //     void Test2(int x = p12)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p12").WithArguments("x"),
    // (51,47): error CS0269: Use of unassigned out parameter 'p11'
    //     public event System.Action E2 = GetAction(p11);
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p11").WithArguments("p11"),
    // (2,15): error CS0177: The out parameter 'p7' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p7"),
    // (2,15): error CS0177: The out parameter 'p8' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p8"),
    // (2,15): error CS0177: The out parameter 'p9' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p9"),
    // (2,15): error CS0177: The out parameter 'p10' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p10"),
    // (2,15): error CS0177: The out parameter 'p11' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p11"),
    // (2,15): error CS0177: The out parameter 'p12' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p12"),
    // (2,15): error CS0177: The out parameter 'p13' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p13"),
    // (2,15): error CS0177: The out parameter 'p14' must be assigned to before control leaves the current method
    // struct Derived(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(ref int p1, ref int p2, ref int p3, ref int p4, ref int p5, ref int p6, out int p7, out int p8, out int p9, out int p10, out int p11, out int p12, out int p13, out int p14)").WithArguments("p14"),
    // (6,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p1);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1"),
    // (11,34): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         System.Console.WriteLine(p3);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p3"),
    // (9,28): error CS9005: Constructor initializer cannot access the parameters to a primary constructor.
    //     Derived(long x) : this(p2)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterInConstructorInitializer, "p2"),
    // (59,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p13;
    // (18,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p5; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p5"),
    // (21,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p6);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p6"),
    // (27,22): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         get { return p7; } 
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p7"),
    // (30,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p8);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p8"),
    // (38,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p9);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p9"),
    // (42,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Console.WriteLine(p10);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p10"),
    // (59,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p13;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p13"),
    // (64,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return p14;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p14")
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
    // (15,31): error CS9008: Cannot use primary constructor parameter 'p3' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     System.Func<int> f1 = ()=>p3;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p3").WithArguments("p3"),
    // (16,31): error CS9008: Cannot use primary constructor parameter 'p4' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     System.Func<int> f2 = ()=>p4;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p4").WithArguments("p4"),
    // (17,31): error CS9008: Cannot use primary constructor parameter 'p5' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     System.Func<int> f3 = ()=>p5;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p5").WithArguments("p5"),
    // (19,37): error CS9008: Cannot use primary constructor parameter 'p6' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     event System.Func<int> e1 = ()=>p6;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p6").WithArguments("p6"),
    // (20,37): error CS9008: Cannot use primary constructor parameter 'p7' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     event System.Func<int> e2 = ()=>p7;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p7").WithArguments("p7"),
    // (21,37): error CS9008: Cannot use primary constructor parameter 'p8' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     event System.Func<int> e3 = ()=>p8;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p8").WithArguments("p8"),
    // (13,40): error CS9008: Cannot use primary constructor parameter 'p0' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p0").WithArguments("p0"),
    // (13,48): error CS9008: Cannot use primary constructor parameter 'p1' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p1").WithArguments("p1"),
    // (13,56): error CS9008: Cannot use primary constructor parameter 'p2' inside an anonymous method, lambda expression, or query expression within variable initializers and arguments to the base constructor.
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUsePrimaryConstructorParameter, "p2").WithArguments("p2"),
    // (16,31): error CS0269: Use of unassigned out parameter 'p4'
    //     System.Func<int> f2 = ()=>p4;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p4").WithArguments("p4"),
    // (20,37): error CS0269: Use of unassigned out parameter 'p7'
    //     event System.Func<int> e2 = ()=>p7;
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p7").WithArguments("p7"),
    // (13,48): error CS0269: Use of unassigned out parameter 'p1'
    //     : Base(out p1, out p4, out p7, ()=>p0, ()=>p1, ()=>p2)
    Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p1").WithArguments("p1"),
    // (27,17): error CS1628: Cannot use ref or out parameter 'x1' inside an anonymous method, lambda expression, or query expression
    //         x = ()=>x1;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x1").WithArguments("x1"),
    // (28,17): error CS1628: Cannot use ref or out parameter 'x2' inside an anonymous method, lambda expression, or query expression
    //         x = ()=>x2;
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x2").WithArguments("x2")
                );
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
    // (6,18): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         int x1 = p1;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p1").WithLocation(6, 18),
    // (7,18): error CS0103: The name 'p2' does not exist in the current context
    //         int x2 = p2<int>(0);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p2<int>").WithArguments("p2").WithLocation(7, 18)
                );
        }

        [Fact]
        public void GenericName_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived1(private int p1, private int p2)
{
    void Test()
    {
        int x1 = p1;
        int x2 = p2<int>(0);
    }
}

", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (7,18): error CS0307: The field 'Derived1.p2' cannot be used with type arguments
    //         int x2 = p2<int>(0);
    Diagnostic(ErrorCode.ERR_TypeArgsNotAllowed, "p2<int>").WithArguments("Derived1.p2", "field").WithLocation(7, 18)
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
    // (20,30): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //     static int f2 = Test(ref p4);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p4"),
    // (4,33): error CS9005: Constructor initializer cannot access the parameters to a primary constructor.
    //     Derived1(int p5) : this(ref p1, ref p5, ref p5, ref p5)
    Diagnostic(ErrorCode.ERR_PrimaryCtorParameterInConstructorInitializer, "p1"),
    // (15,18): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         Test(ref p2);
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "p2")
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

class Derived(int x, int y, private int z, int u, private int v, private int w) : Base(x)
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
", compOptions: TestOptions.Exe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Exe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (16,20): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return v;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "v").WithLocation(16, 20),
    // (26,20): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return z;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "z").WithLocation(26, 20),
    // (36,20): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return w;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(36, 20),
    // (40,13): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             w = value;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(40, 13)
                );
        }

        [Fact]
        public void Emit_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
struct Derived(int x, int y, int z, int u, private int v, private int w)
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
        var d = new Derived(1,2,3,4,5,6);

        System.Console.WriteLine(d.V());
        System.Console.WriteLine(d.W);
        d.W=60;
        System.Console.WriteLine(d.W);
    }
}
", compOptions: TestOptions.Exe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Exe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (6,16): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //         return v;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "v").WithLocation(6, 16),
    // (13,20): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return w;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(13, 20),
    // (17,13): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             w = value;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Emit_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
static class Derived(int x, int y, int z, int u, private int v, int w)
{
    public static int V()
    {
        return v;
    }

    public static int W
    {
        get 
        {
            System.Func<int> a = ()=>w;
            return w;
        }
        set
        {
            w = value;
        }
    }
}
", compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (2,62): error CS0708: 'Derived.v': cannot declare instance members in a static class
    // static class Derived(int x, int y, int z, int u, private int v, int w)
    Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "v").WithArguments("Derived.v").WithLocation(2, 62),
    // (2,21): error CS0710: Static classes cannot have instance constructors
    // static class Derived(int x, int y, int z, int u, int v, int w)
    Diagnostic(ErrorCode.ERR_ConstructorInStaticClass, "(int x, int y, int z, int u, private int v, int w)").WithLocation(2, 21),
    // (6,16): error CS0120: An object reference is required for the non-static field, method, or property 'Derived.v'
    //         return v;
    Diagnostic(ErrorCode.ERR_ObjectRequired, "v").WithArguments("Derived.v").WithLocation(6, 16),
    // (13,38): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             System.Func<int> a = ()=>w;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(13, 38),
    // (14,20): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return w;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(14, 20),
    // (18,13): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             w = value;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "w").WithLocation(18, 13)
                );
        }

        [Fact]
        public void Emit_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Derived(private int v)
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
        var d = new Derived(1);

        var g = d.GetV;
        System.Console.WriteLine(g());
        var s = d.SetV;
        s(2);
        System.Console.WriteLine(g());
        System.Console.WriteLine(d.GetV());
        d.SetV(3);
        System.Console.WriteLine(g());
    }
}
", compOptions: TestOptions.Exe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            System.Action<ModuleSymbol> validator = delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                var f = derived.GetMember<FieldSymbol>("v");
                Assert.False(f.IsStatic);
                Assert.False(f.IsReadOnly);
                Assert.Equal(Accessibility.Private, f.DeclaredAccessibility);
            };

            var verifier = CompileAndVerify(comp, expectedOutput:
@"1
2
2
3", sourceSymbolValidator: validator, symbolValidator: validator);
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
", compOptions: TestOptions.Exe.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyEmitDiagnostics(
    // (8,24): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return ()=>v;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "v").WithLocation(8, 24),
    // (16,30): error CS9007: Parameters of a primary constructor can only be accessed in instance variable initializers and arguments to the base constructor.
    //             return (int x)=> v=x;
    Diagnostic(ErrorCode.ERR_InvalidUseOfPrimaryConstructorParameter, "v").WithLocation(16, 30)
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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);
            Assert.Null(parameter.PrimaryConstructorParameterBackingField);

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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());
            Assert.Null(parameter.PrimaryConstructorParameterBackingField);

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");
                Assert.False(derived.GetMembers().OfType<FieldSymbol>().Any());

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("ParameterAttribute", ctor.Parameters[0].GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void FieldAttributes_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([ParameterAttribute] private int v)
{
    public int V { get { return v; } }
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());
            Assert.Same(parameter.PrimaryConstructorParameterBackingField, comp.GetTypeByMetadataName("Derived").GetMember<FieldSymbol>("v"));
            Assert.Same(parameter, parameter.PrimaryConstructorParameterBackingField.AssociatedSymbol);

            System.Action<ModuleSymbol> validator = delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("ParameterAttribute", ctor.Parameters[0].GetAttributes().Single().ToString());

                var v = derived.GetMember<FieldSymbol>("v");
                Assert.Equal(0, v.GetAttributes().Length);
            };

            var verifier = CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        public void FieldAttributes_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([param: ParameterAttribute] private int v)
{
    public int V { get { return v; } }
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());
            Assert.Same(parameter.PrimaryConstructorParameterBackingField, comp.GetTypeByMetadataName("Derived").GetMember<FieldSymbol>("v"));
            Assert.Same(parameter, parameter.PrimaryConstructorParameterBackingField.AssociatedSymbol);

            System.Action<ModuleSymbol> validator = delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("ParameterAttribute", ctor.Parameters[0].GetAttributes().Single().ToString());

                var v = derived.GetMember<FieldSymbol>("v");
                Assert.Equal(0, v.GetAttributes().Length);
            };

            var verifier = CompileAndVerify(comp, sourceSymbolValidator: validator, symbolValidator: validator);
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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);
            Assert.Null(parameter.PrimaryConstructorParameterBackingField);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,16): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
    // class Derived([field: ParameterAttribute] int v)
    Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(7, 16)
                );
        }

        [Fact]
        public void FieldAttributes_05_2()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([field: ParameterAttribute] private int v)
{
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);

            var field = comp.GetTypeByMetadataName("Derived").GetMember<FieldSymbol>("v");
            Assert.Equal("ParameterAttribute", field.GetAttributes().Single().ToString());

            Assert.Same(parameter.PrimaryConstructorParameterBackingField, field);
            Assert.Same(parameter, parameter.PrimaryConstructorParameterBackingField.AssociatedSymbol);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,23): error CS0592: Attribute 'ParameterAttribute' is not valid on this declaration type. It is only valid on 'parameter' declarations.
    // class Derived([field: ParameterAttribute] int v)
    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ParameterAttribute").WithArguments("ParameterAttribute", "parameter")
                );
        }

        [Fact]
        public void FieldAttributes_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

[System.AttributeUsage(System.AttributeTargets.Field)]
public class FieldAttribute : System.Attribute
{
}

[StructLayout(LayoutKind.Explicit)]
struct Derived([field: FieldAttribute] private int v)
{
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (10,44): error CS0625: 'Derived.v': instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
    // struct Derived([field: FieldAttribute] int v)
    Diagnostic(ErrorCode.ERR_MissingStructOffset, "v").WithArguments("Derived.v")
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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);
            Assert.Null(parameter.PrimaryConstructorParameterBackingField);

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
        public void FieldAttributes_07_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Field)]
public class FieldAttribute : System.Attribute
{
}

class Derived([field: FieldAttribute] private int v)
{
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);

            var field = comp.GetTypeByMetadataName("Derived").GetMember<FieldSymbol>(parameter.Name);
            Assert.Equal("FieldAttribute", field.GetAttributes().Single().ToString());
            Assert.Same(parameter.PrimaryConstructorParameterBackingField,  field);
            Assert.Same(parameter, parameter.PrimaryConstructorParameterBackingField.AssociatedSymbol);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
                );

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal(0, ctor.Parameters[0].GetAttributes().Length);

                var v = derived.GetMember<FieldSymbol>("v");
                Assert.Equal("FieldAttribute", v.GetAttributes().Single().ToString());
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
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);
            Assert.Equal(0, comp.GetTypeByMetadataName("Derived").GetMembers(parameter.Name).Length);
            Assert.Null(parameter.PrimaryConstructorParameterBackingField);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (7,16): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
    // class Derived([field: FieldAttribute] int v)
    Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(7, 16)
                );
        }

        [Fact]
        public void FieldAttributes_08_2()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Field)]
public class FieldAttribute : System.Attribute
{
}

class Derived([field: FieldAttribute] private int v)
{
    public int V { get { return v; } }
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal(0, parameter.GetAttributes().Length);

            var field = comp.GetTypeByMetadataName("Derived").GetMember<FieldSymbol>(parameter.Name);
            Assert.Equal("FieldAttribute", field.GetAttributes().Single().ToString());

            Assert.Same(parameter.PrimaryConstructorParameterBackingField, field);
            Assert.Same(parameter, parameter.PrimaryConstructorParameterBackingField.AssociatedSymbol);

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
                );

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal(0, ctor.Parameters[0].GetAttributes().Length);

                var v = derived.GetMember<FieldSymbol>("v");
                Assert.Equal("FieldAttribute", v.GetAttributes().Single().ToString());
            });
        }

        [Fact]
        public void FieldAttributes_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Field)]
public class FieldAttribute : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

class Derived([field: FieldAttribute][ParameterAttribute] private int v, int w)
{
    public int V { get { return v; } }
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var parameter = comp.GetTypeByMetadataName("Derived").GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Equal("ParameterAttribute", parameter.GetAttributes().Single().ToString());
            Assert.Equal("FieldAttribute", comp.GetTypeByMetadataName("Derived").GetMember<FieldSymbol>(parameter.Name).GetAttributes().Single().ToString());

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
                );

            var verifier = CompileAndVerify(comp, symbolValidator: delegate (ModuleSymbol m)
            {
                var derived = m.GlobalNamespace.GetTypeMember("Derived");

                var ctor = derived.GetMember<MethodSymbol>(".ctor");
                Assert.Equal("ParameterAttribute", ctor.Parameters[0].GetAttributes().Single().ToString());

                var v = derived.GetMember<FieldSymbol>("v");
                Assert.Equal("FieldAttribute", v.GetAttributes().Single().ToString());

                Assert.Equal(1, derived.GetMembers().OfType<FieldSymbol>().Count());
            });
        }

        [Fact]
        public void FieldAttributes_10()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
struct Derived(private int v)
{
    public int V { get { return v; } }
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (5,28): error CS0625: 'Derived.v': instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
    // struct Derived(private int v)
    Diagnostic(ErrorCode.ERR_MissingStructOffset, "v").WithArguments("Derived.v").WithLocation(5, 28)
                );
       }

        [Fact]
        public void FieldAttributes_11()
        {
            var comp = CreateCompilationWithMscorlib(@"
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public class ParameterAttribute : System.Attribute
{
}

partial class Derived(string x)
{
}

partial class Derived([field: ParameterAttribute] private int v)
{
}
", compOptions: TestOptions.Dll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.GetDiagnostics(CompilationStage.Declare, true, default(CancellationToken)).Verify(
    // (11,22): error CS9001: Only one part of a partial type can declare primary constructor parameters.
    // partial class Derived([field: ParameterAttribute] private int v)
    Diagnostic(ErrorCode.ERR_SeveralPartialsDeclarePrimaryCtor, "([field: ParameterAttribute] private int v)").WithLocation(11, 22),
    // (11,31): error CS0592: Attribute 'ParameterAttribute' is not valid on this declaration type. It is only valid on 'parameter' declarations.
    // partial class Derived([field: ParameterAttribute] private int v)
    Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "ParameterAttribute").WithArguments("ParameterAttribute", "parameter").WithLocation(11, 31)
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
                );

            var field = comp.GetTypeByMetadataName("Test").GetMember<SourceFieldSymbol>("v");

            Assert.True(field.IsImplicitlyDeclared);
            Assert.False(field.IsStatic);
            Assert.Equal(Accessibility.Private, field.DeclaredAccessibility);
            Assert.False(field.IsVolatile);
            Assert.False(field.IsNew);
            Assert.True(field.CustomModifiers.IsEmpty);
        }

        [Fact]
        public void ParameterWithFields_02()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(static static int v)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS9010: A parameter cannot have static modifier.
    // class Test(static static int v)
    Diagnostic(ErrorCode.ERR_StaticParamMod, "static").WithLocation(2, 12)
                );

            var field = comp.GetTypeByMetadataName("Test").GetMember<FieldSymbol>("v");
            Assert.True(field.IsStatic);
            Assert.Equal(Accessibility.Private, field.DeclaredAccessibility);
        }

        [Fact]
        public void ParameterWithFields_03()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(readonly int v)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS9011: A parameter must either have an accessibility modifier or must not have any field modifiers.
    // class Test(readonly int v)
    Diagnostic(ErrorCode.ERR_ParamMissingAccessMod, "readonly").WithLocation(2, 12)
                );

            Assert.True(comp.GetTypeByMetadataName("Test").GetMember<FieldSymbol>("v").IsReadOnly);
        }

        [Fact]
        public void ParameterWithFields_04()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(static readonly new int v)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS9010: A parameter cannot have static modifier.
    // class Test(static readonly new int v)
    Diagnostic(ErrorCode.ERR_StaticParamMod, "static").WithLocation(2, 12),
    // (2,19): error CS9011: A parameter must either have an accessibility modifier or must not have any field modifiers.
    // class Test(static readonly new int v)
    Diagnostic(ErrorCode.ERR_ParamMissingAccessMod, "readonly").WithLocation(2, 19),
    // (2,36): warning CS0109: The member 'Test.v' does not hide an inherited member. The new keyword is not required.
    // class Test(static readonly new int v)
    Diagnostic(ErrorCode.WRN_NewNotRequired, "v").WithArguments("Test.v").WithLocation(2, 36)
                );
        }

        [Fact]
        public void ParameterWithFields_05()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(private int v)
{
    void v(){}
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (4,10): error CS0102: The type 'Test' already contains a definition for 'v'
    //     void v(){}
    Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "v").WithArguments("Test", "v").WithLocation(4, 10)
                );
        }


        [Fact]
        public void ParameterWithFields_06()
        {
            var comp = CreateCompilationWithMscorlib(@"
static class Test(public int v)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,30): error CS0708: 'Test.v': cannot declare instance members in a static class
    // static class Test(public int v)
    Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "v").WithArguments("Test.v").WithLocation(2, 30),
    // (2,18): error CS0710: Static classes cannot have instance constructors
    // static class Test(public int v)
    Diagnostic(ErrorCode.ERR_ConstructorInStaticClass, "(public int v)").WithLocation(2, 18)
                );

            var field = comp.GetTypeByMetadataName("Test").GetMember<FieldSymbol>("v");
            Assert.False(field.IsStatic);
            Assert.Equal(Accessibility.Public, field.DeclaredAccessibility);
        }

        [Fact]
        public void ParameterWithFields_07()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(protected volatile int v)
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
                );

            var field = comp.GetTypeByMetadataName("Test").GetMember<FieldSymbol>("v");

            Assert.Equal(Accessibility.Protected, field.DeclaredAccessibility);
            Assert.True(field.IsVolatile);
            Assert.Equal(1, field.CustomModifiers.Length);
            Assert.False(field.CustomModifiers[0].IsOptional);
            Assert.Equal("System.Runtime.CompilerServices.IsVolatile", field.CustomModifiers[0].Modifier.ToTestDisplayString());
        }

        [Fact]
        public void ParameterWithFields_08()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Base
{
    protected int v = 0;
}

class Test(protected new int v) : Base
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
                );

            var field = comp.GetTypeByMetadataName("Test").GetMember<SourceFieldSymbol>("v");
            Assert.True(field.IsNew);
        }

        [Fact]
        public void ParameterWithFields_09()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(protected ref int v) 
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS9012: A ref or out parameter cannot have any field modifiers.
    // class Test(protected ref int v) 
    Diagnostic(ErrorCode.ERR_RefOutParameterWithFieldModifier, "protected").WithLocation(2, 12)
                );
        }

        [Fact]
        public void ParameterWithFields_10()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(protected out int v) 
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS9012: A ref or out parameter cannot have any field modifiers.
    // class Test(protected out int v) 
    Diagnostic(ErrorCode.ERR_RefOutParameterWithFieldModifier, "protected").WithLocation(2, 12),
    // (2,11): error CS0177: The out parameter 'v' must be assigned to before control leaves the current method
    // class Test(protected out int v) 
    Diagnostic(ErrorCode.ERR_ParamUnassigned, "(protected out int v)").WithArguments("v").WithLocation(2, 11)
                );
        }

        [Fact]
        public void ParameterWithFields_11()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Test(readonly ref int v) 
{
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            comp.VerifyDiagnostics(
    // (2,12): error CS9012: A ref or out parameter cannot have any field modifiers.
    // class Test(readonly ref int v) 
    Diagnostic(ErrorCode.ERR_RefOutParameterWithFieldModifier, "readonly").WithLocation(2, 12)
                );
        }

        [Fact]
        public void ParameterWithFields_12()
        {
            var comp1 = CreateCompilation(new[] { Parse(@"
class Derived(private int v)
{
}
", options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental)) }, new[] { MscorlibRef_v20 }, compOptions: TestOptions.Dll);

            var derived = comp1.GetTypeByMetadataName("Derived");
            var parameter = derived.GetMember<MethodSymbol>(".ctor").Parameters[0];
            var field = derived.GetMember<FieldSymbol>(parameter.Name);

            Assert.Same(parameter.PrimaryConstructorParameterBackingField, field);
            Assert.Same(parameter, field.AssociatedSymbol);

            var comp2 = CreateCompilation(@"", new[] { MscorlibRef, new CSharpCompilationReference(comp1) }, compOptions: TestOptions.Dll);

            derived = comp2.GetTypeByMetadataName("Derived");
            parameter = derived.GetMember<MethodSymbol>(".ctor").Parameters[0];
            field = derived.GetMember<FieldSymbol>(parameter.Name);
            Assert.Same(parameter.PrimaryConstructorParameterBackingField, field);
            Assert.Same(parameter, field.AssociatedSymbol);

            var comp3 = CreateCompilation(@"", new[] { MscorlibRef, new CSharpCompilationReference(comp1) }, compOptions: TestOptions.Dll);

            derived = comp3.GetTypeByMetadataName("Derived");
            field = derived.GetMember<FieldSymbol>(parameter.Name);
            parameter = derived.GetMember<MethodSymbol>(".ctor").Parameters[0];
            Assert.Same(parameter, field.AssociatedSymbol);
            Assert.Same(parameter.PrimaryConstructorParameterBackingField, field);
        }

        [Fact]
        public void ParameterWithFields_13()
        {
            var comp1 = CreateCompilationWithMscorlib(@"
class Derived<T>(private int v)
{
}
", compOptions: TestOptions.Dll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Experimental));

            var derived = comp1.GetTypeByMetadataName("Derived`1").Construct(comp1.GetSpecialType(SpecialType.System_Int32));
            var parameter = derived.GetMember<MethodSymbol>(".ctor").Parameters[0];
            var field = derived.GetMember<FieldSymbol>(parameter.Name);

            Assert.Equal(parameter.PrimaryConstructorParameterBackingField, field);
            Assert.Equal(parameter, field.AssociatedSymbol);
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

    }
}