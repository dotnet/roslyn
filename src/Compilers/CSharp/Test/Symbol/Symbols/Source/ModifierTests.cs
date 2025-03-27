// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ModifierTests : CSharpTestBase
    {
        [Fact]
        public void Simple1()
        {
            var text =
@"abstract class A : Base
{
    void M1() { }
    public void M2() { }
    protected void M3() { }
    internal void M4() { }
    protected internal void M5() { }
    internal abstract protected void M5_1() { }
    abstract protected internal void M5_2() { }
    private void M6() { }
    extern void M7();
    override sealed internal void M8() { }
    override internal sealed void M8_1() { }
    internal override sealed void M8_2() { }
    abstract internal void M9();
    override internal void M10() { }
    virtual internal void M11() { }
    static void M12() { }
}
abstract class Base
{
    virtual internal void M8() { }
    abstract internal void M10();
}";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var m1 = a.GetMembers("M1").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility);
            Assert.True(m1.ReturnsVoid);
            var m2 = a.GetMembers("M2").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
            var m3 = a.GetMembers("M3").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Protected, m3.DeclaredAccessibility);
            var m4 = a.GetMembers("M4").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Internal, m4.DeclaredAccessibility);
            var m5 = a.GetMembers("M5").Single() as MethodSymbol;
            Assert.Equal(Accessibility.ProtectedOrInternal, m5.DeclaredAccessibility);
            var m5_1 = a.GetMembers("M5_1").Single() as MethodSymbol;
            Assert.Equal(Accessibility.ProtectedOrInternal, m5_1.DeclaredAccessibility);
            Assert.True(m5_1.IsAbstract);
            var m5_2 = a.GetMembers("M5_2").Single() as MethodSymbol;
            Assert.Equal(Accessibility.ProtectedOrInternal, m5_2.DeclaredAccessibility);
            Assert.True(m5_2.IsAbstract);
            var m6 = a.GetMembers("M6").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Private, m6.DeclaredAccessibility);
            var m7 = a.GetMembers("M7").Single() as MethodSymbol;
            Assert.True(m7.IsExtern);
            var m8 = a.GetMembers("M8").Single() as MethodSymbol;
            Assert.True(m8.IsOverride);
            Assert.True(m8.IsSealed);
            Assert.Equal(Accessibility.Internal, m8.DeclaredAccessibility);
            var m8_1 = a.GetMembers("M8_1").Single() as MethodSymbol;
            Assert.True(m8_1.IsOverride);
            Assert.True(m8_1.IsSealed);
            Assert.Equal(Accessibility.Internal, m8_1.DeclaredAccessibility);
            var m8_2 = a.GetMembers("M8_2").Single() as MethodSymbol;
            Assert.True(m8_2.IsOverride);
            Assert.True(m8_2.IsSealed);
            Assert.Equal(Accessibility.Internal, m8_2.DeclaredAccessibility);
            var m9 = a.GetMembers("M9").Single() as MethodSymbol;
            Assert.True(m9.IsAbstract);
            Assert.Equal(Accessibility.Internal, m9.DeclaredAccessibility);
            var m10 = a.GetMembers("M10").Single() as MethodSymbol;
            Assert.True(m10.IsOverride);
            Assert.Equal(Accessibility.Internal, m10.DeclaredAccessibility);
            var m11 = a.GetMembers("M11").Single() as MethodSymbol;
            Assert.True(m11.IsVirtual);
            Assert.Equal(Accessibility.Internal, m11.DeclaredAccessibility);
            var m12 = a.GetMembers("M12").Single() as MethodSymbol;
            Assert.True(m12.IsStatic);
        }

        [Fact]
        public void InScript()
        {
            var text =
@"
void M1() { }
public void M2() { }
protected void M3() { }
internal void M4() { }
protected internal void M5() { }
private void M6() { }
extern void M7();
static void M12() { }
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Script);
            var script = comp.ScriptClass;
            var m1 = script.GetMembers("M1").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Private, m1.DeclaredAccessibility);
            var m2 = script.GetMembers("M2").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Public, m2.DeclaredAccessibility);
            var m3 = script.GetMembers("M3").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Protected, m3.DeclaredAccessibility);
            var m4 = script.GetMembers("M4").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Internal, m4.DeclaredAccessibility);
            var m5 = script.GetMembers("M5").Single() as MethodSymbol;
            Assert.Equal(Accessibility.ProtectedOrInternal, m5.DeclaredAccessibility);
            var m6 = script.GetMembers("M6").Single() as MethodSymbol;
            Assert.Equal(Accessibility.Private, m6.DeclaredAccessibility);
            var m7 = script.GetMembers("M7").Single() as MethodSymbol;
            Assert.True(m7.IsExtern);
            var m12 = script.GetMembers("M12").Single() as MethodSymbol;
            Assert.True(m12.IsStatic);

            comp.VerifyDiagnostics(
                // (4,16): warning CS0628: 'M3()': new protected member declared in sealed type
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "M3").WithArguments("M3()"),
                // (6,25): warning CS0628: 'M5()': new protected member declared in sealed type
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "M5").WithArguments("M5()"),
                // (8,13): warning CS0626: Method, operator, or accessor 'M7()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M7").WithArguments("M7()")
            );
        }

        [Fact]
        public void TypeMap()
        {
            var source = @"
struct S<T> where T : struct
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var intType = comp.GetSpecialType(SpecialType.System_Int32);
            var customModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateOptional(intType));

            var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
            var typeParamType = structType.TypeParameters.Single();

            var pointerType = new PointerTypeSymbol(TypeWithAnnotations.Create(typeParamType, customModifiers: customModifiers)); // NOTE: We're constructing this manually, since it's illegal.
            var arrayType = ArrayTypeSymbol.CreateCSharpArray(comp.Assembly, TypeWithAnnotations.Create(typeParamType, customModifiers: customModifiers)); // This is legal, but we're already manually constructing types.

            var typeMap = new TypeMap(ImmutableArray.Create(typeParamType), ImmutableArray.Create(TypeWithAnnotations.Create(intType)));

            var substitutedPointerType = (PointerTypeSymbol)typeMap.SubstituteType(pointerType).AsTypeSymbolOnly();
            var substitutedArrayType = (ArrayTypeSymbol)typeMap.SubstituteType(arrayType).AsTypeSymbolOnly();

            // The map changed the types.
            Assert.Equal(intType, substitutedPointerType.PointedAtType);
            Assert.Equal(intType, substitutedArrayType.ElementType);

            // The map preserved the custom modifiers.
            Assert.Equal(customModifiers, substitutedPointerType.PointedAtTypeWithAnnotations.CustomModifiers);
            Assert.Equal(customModifiers, substitutedArrayType.ElementTypeWithAnnotations.CustomModifiers);
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter1()
        {
            CreateCompilation(@"
public class Base {
    public void M(ref readonly int X) {
    }
}").VerifyDiagnostics();
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter2()
        {
            CreateCompilation(@"
public class Base {
    public void M(readonly ref int X) {
    }
}").VerifyDiagnostics(
            // (3,19): error CS9190: 'readonly' modifier must be specified after 'ref'.
            //     public void M(readonly ref int X) {
            Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 19));
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter3()
        {
            CreateCompilation(@"
public class Base {
    public void M(readonly int X) {
    }
}").VerifyDiagnostics(
                // (3,19): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //     public void M(readonly int X) {
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(3, 19));
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter4()
        {
            CreateCompilation(@"
public class Base {
    void M()
    {
        var v = (readonly int i) => { };
    }
}").VerifyDiagnostics(
                // (5,18): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //         var v = (readonly int i) => { };
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(5, 18));
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter5()
        {
            CreateCompilation(@"
public class Base {
    void M()
    {
        var v = (ref readonly int i) => { };
    }
}").VerifyDiagnostics();
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter6()
        {
            CreateCompilation(@"
public class Base {
    void M()
    {
        var v = (readonly ref int i) => { };
    }
}").VerifyDiagnostics(
                // (5,18): error CS9190: 'readonly' modifier must be specified after 'ref'.
                //         var v = (readonly ref int i) => { };
                Diagnostic(ErrorCode.ERR_RefReadOnlyWrongOrdering, "readonly").WithLocation(5, 18));
        }

        [Fact]
        public void RefExtensionMethodsNotSupportedBefore7_2_InSyntax()
        {
            var code = @"
public static class Extensions
{
    public static void Print(in this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int p = 5;
        p.Print();
    }
}";

            CreateCompilation(code, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (4,30): error CS8302: Feature 'readonly references' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public static void Print(in this int p)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "in").WithArguments("readonly references", "7.2").WithLocation(4, 30),
                // (4,33): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public static void Print(in this int p)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "this").WithArguments("ref extension methods", "7.2").WithLocation(4, 33),
                // (14,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(14, 9));

            CompileAndVerify(code, expectedOutput: "5");
        }

        [Fact]
        public void RefExtensionMethodsNotSupportedBefore7_2_RefSyntax()
        {
            var code = @"
public static class Extensions
{
    public static void Print(ref this int p)
    {
        System.Console.WriteLine(p);
    }
}
public static class Program
{
    public static void Main()
    {
        int p = 5;
        p.Print();
    }
}";

            CreateCompilation(code, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (4,34): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //     public static void Print(ref this int p)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "this").WithArguments("ref extension methods", "7.2").WithLocation(4, 34),
                // (14,9): error CS8302: Feature 'ref extension methods' is not available in C# 7.1. Please use language version 7.2 or greater.
                //         p.Print();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_1, "p").WithArguments("ref extension methods", "7.2").WithLocation(14, 9));

            CompileAndVerify(code, expectedOutput: "5");
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void InParametersWouldErrorOutInEarlierCSharpVersions()
        {
            var code = @"
public class Test
{
    public void DoSomething(in int x) { }
}";

            CreateCompilation(code, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7)).VerifyDiagnostics(
                // (4,29): error CS8107: Feature 'readonly references' is not available in C# 7.0. Please use language version 7.2 or greater.
                //     public void DoSomething(in int x) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "in").WithArguments("readonly references", "7.2").WithLocation(4, 29));
        }
    }
}
