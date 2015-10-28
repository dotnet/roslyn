// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            var comp = CreateCompilationWithMscorlib(text);
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
            foreach (var options in new[] { TestOptions.Script, TestOptions.Interactive })
            {
                var comp = CreateCompilationWithMscorlib45(text, parseOptions: options);
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
                    // (4,16): warning CS0628: 'M3()': new protected member declared in sealed class
                    Diagnostic(ErrorCode.WRN_ProtectedInSealed, "M3").WithArguments("M3()"),
                    // (6,25): warning CS0628: 'M5()': new protected member declared in sealed class
                    Diagnostic(ErrorCode.WRN_ProtectedInSealed, "M5").WithArguments("M5()"),
                    // (8,13): warning CS0626: Method, operator, or accessor 'M7()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                    Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "M7").WithArguments("M7()")
                );
            }
        }

        [Fact]
        public void TypeMap()
        {
            var source = @"
struct S<T> where T : struct
{
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var intType = comp.GetSpecialType(SpecialType.System_Int32);
            var customModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateOptional(intType));

            var structType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S");
            var typeParamType = structType.TypeParameters.Single();

            var pointerType = new PointerTypeSymbol(TypeSymbolWithAnnotations.Create(typeParamType, customModifiers)); // NOTE: We're constructing this manually, since it's illegal.
            var arrayType = ArrayTypeSymbol.CreateCSharpArray(comp.Assembly, TypeSymbolWithAnnotations.Create(typeParamType, customModifiers)); // This is legal, but we're already manually constructing types.

            var typeMap = new TypeMap(ImmutableArray.Create(typeParamType), ImmutableArray.Create(TypeSymbolWithAnnotations.Create(intType)));

            var substitutedPointerType = (PointerTypeSymbol)typeMap.SubstituteType(pointerType).AsTypeSymbolOnly();
            var substitutedArrayType = (ArrayTypeSymbol)typeMap.SubstituteType(arrayType).AsTypeSymbolOnly();

            // The map changed the types.
            Assert.Equal(intType, substitutedPointerType.PointedAtType.TypeSymbol);
            Assert.Equal(intType, substitutedArrayType.ElementType.TypeSymbol);

            // The map preserved the custom modifiers.
            Assert.Equal(customModifiers, substitutedPointerType.PointedAtType.CustomModifiers);
            Assert.Equal(customModifiers, substitutedArrayType.ElementType.CustomModifiers);
        }
    }
}
