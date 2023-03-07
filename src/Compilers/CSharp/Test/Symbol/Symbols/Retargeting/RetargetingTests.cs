// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;
using Utils = Microsoft.CodeAnalysis.CSharp.UnitTests.CompilationUtils;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Retargeting
{
    public class RetargetingTests : CSharpTestBase
    {
        [Fact]
        public void RetargetExtensionMethods()
        {
            var source =
@"class C
{
}
static class S1
{
    internal static void E(this object x, object y) { }
}
static class S2
{
    internal static void E<T, U>(this T t, U u) { }
}";
            var compilation = CreateCompilation(source);

            var sourceModule = compilation.SourceModule;
            var sourceAssembly = (SourceAssemblySymbol)sourceModule.ContainingAssembly;
            var sourceNamespace = sourceModule.GlobalNamespace;

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary);
            var retargetingModule = retargetingAssembly.Modules[0];
            var retargetingNamespace = retargetingModule.GlobalNamespace;

            var sourceMethods = new ArrayBuilder<MethodSymbol>();
            sourceNamespace.GetExtensionMethods(sourceMethods, null, 0, LookupOptions.AllMethodsOnArityZero);
            Utils.CheckSymbols(sourceMethods.ToImmutable(),
                "void S1.E(object x, object y)",
                "void S2.E<T, U>(T t, U u)");

            var retargetingMethods = new ArrayBuilder<MethodSymbol>();
            retargetingNamespace.GetExtensionMethods(retargetingMethods, null, 0, LookupOptions.AllMethodsOnArityZero);
            Utils.CheckSymbols(retargetingMethods.ToImmutable(),
                "void S1.E(object x, object y)",
                "void S2.E<T, U>(T t, U u)");

            for (int i = 0; i < sourceMethods.Count; i++)
            {
                CheckMethods(sourceMethods[i], retargetingMethods[i]);
            }

            sourceMethods = new ArrayBuilder<MethodSymbol>();
            sourceNamespace.GetExtensionMethods(sourceMethods, "E", 2, LookupOptions.Default);
            Utils.CheckSymbols(sourceMethods.ToImmutable(),
                "void S2.E<T, U>(T t, U u)");
            var sourceMethod = sourceMethods[0];

            retargetingMethods = new ArrayBuilder<MethodSymbol>();
            retargetingNamespace.GetExtensionMethods(retargetingMethods, "E", 2, LookupOptions.Default);
            Utils.CheckSymbols(retargetingMethods.ToImmutable(),
                "void S2.E<T, U>(T t, U u)");
            var retargetingMethod = retargetingMethods[0];

            var sourceType = sourceNamespace.GetMember<NamedTypeSymbol>("C");
            var retargetingType = retargetingNamespace.GetMember<NamedTypeSymbol>("C");
            CheckTypes(sourceType, retargetingType);

            CheckMethods(sourceMethod, retargetingMethod);
            var sourceReduced = sourceMethod.ReduceExtensionMethod(sourceType, null!);
            var retargetingReduced = retargetingMethod.ReduceExtensionMethod(retargetingType, null!);
            CheckReducedExtensionMethods(sourceReduced, retargetingReduced);
        }

        [Fact]
        public void RetargetProperties()
        {
            var source =
@"interface I
{
    object this[string x, object y] { get; set; }
}
struct S
{
    I P { get { return null; } }
}
class C
{
    internal I Q { get; private set; }
    object this[I index]
    {
        get { return 0; }
        set { }
    }
}";
            var compilation = CreateCompilation(source);

            var sourceModule = compilation.SourceModule;
            var sourceAssembly = (SourceAssemblySymbol)sourceModule.ContainingAssembly;
            var sourceNamespace = sourceModule.GlobalNamespace;

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary);
            var retargetingModule = retargetingAssembly.Modules[0];
            var retargetingNamespace = retargetingModule.GlobalNamespace;

            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("I"), retargetingNamespace.GetMember<NamedTypeSymbol>("I"));
            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("S"), retargetingNamespace.GetMember<NamedTypeSymbol>("S"));
            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("C"), retargetingNamespace.GetMember<NamedTypeSymbol>("C"));
        }

        [Fact]
        public void RetargetFields()
        {
            var source = @"
using System.Runtime.InteropServices;

class D
{
}

class C
{
    internal D F1;

    [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType = typeof(D))]
    internal int F2;
}";
            var compilation = CreateCompilation(source);

            var sourceModule = compilation.SourceModule;
            var sourceAssembly = (SourceAssemblySymbol)sourceModule.ContainingAssembly;
            var sourceNamespace = sourceModule.GlobalNamespace;

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary);
            var retargetingModule = retargetingAssembly.Modules[0];
            var retargetingNamespace = retargetingModule.GlobalNamespace;

            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("C"), retargetingNamespace.GetMember<NamedTypeSymbol>("C"));

            Assert.IsType<RetargetingNamedTypeSymbol>(
                retargetingNamespace.GetMember<NamedTypeSymbol>("C").GetMember<RetargetingFieldSymbol>("F2").MarshallingInformation.TryGetSafeArrayElementUserDefinedSubtype());
        }

        [Fact]
        public void RetargetMethods()
        {
            var source = @"
using System.Runtime.InteropServices;

class C
{
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType = typeof(C))]
    internal int M(
        [MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DISPATCH, SafeArrayUserDefinedSubType = typeof(C))]
        int arg
    ) 
    {
        return 1;
    }
}";
            var compilation = CreateCompilation(source);

            var sourceModule = compilation.SourceModule;
            var sourceAssembly = (SourceAssemblySymbol)sourceModule.ContainingAssembly;
            var sourceNamespace = sourceModule.GlobalNamespace;

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary);
            var retargetingModule = retargetingAssembly.Modules[0];
            var retargetingNamespace = retargetingModule.GlobalNamespace;

            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("C"), retargetingNamespace.GetMember<NamedTypeSymbol>("C"));

            Assert.IsType<RetargetingNamedTypeSymbol>(
                retargetingNamespace.GetMember<NamedTypeSymbol>("C").GetMember<RetargetingMethodSymbol>("M").ReturnValueMarshallingInformation.TryGetSafeArrayElementUserDefinedSubtype());

            Assert.IsType<RetargetingNamedTypeSymbol>(
                ((RetargetingParameterSymbol)retargetingNamespace.GetMember<NamedTypeSymbol>("C").GetMember<RetargetingMethodSymbol>("M").Parameters[0]).
                MarshallingInformation.TryGetSafeArrayElementUserDefinedSubtype());
        }

        [Fact]
        public void RetargetGenericConstraints()
        {
            var source =
@"interface I<T> { }
class C<T> where T : I<T>, new() { }
struct S<T> where T : struct
{
    void M<U, V>()
        where U : class, I<V>
    {
    }
}
delegate T D<T>() where T : I<T>;";

            var compilation = CreateCompilation(source);

            var sourceModule = compilation.SourceModule;
            var sourceAssembly = (SourceAssemblySymbol)sourceModule.ContainingAssembly;
            var sourceNamespace = sourceModule.GlobalNamespace;

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary);
            var retargetingModule = retargetingAssembly.Modules[0];
            var retargetingNamespace = retargetingModule.GlobalNamespace;

            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("I"), retargetingNamespace.GetMember<NamedTypeSymbol>("I"));
            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("C"), retargetingNamespace.GetMember<NamedTypeSymbol>("C"));
            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("S"), retargetingNamespace.GetMember<NamedTypeSymbol>("S"));
            RetargetingSymbolChecker.CheckSymbols(sourceNamespace.GetMember<NamedTypeSymbol>("D"), retargetingNamespace.GetMember<NamedTypeSymbol>("D"));
        }

        [WorkItem(542571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542571")]
        [Fact]
        public void RetargetExplicitImplementationDifferentModule()
        {
            var source1 =
@"public interface I<T>
{
    void M<U>(I<U> o);
    void N(I<T> o);
    I<T> P { get; }
}
public class A
{
}";
            var compilation1_v1 = CreateCompilation(source1, assemblyName: "assembly1");
            var compilation1_v2 = CreateCompilation(source1, assemblyName: "assembly1");

            var source2 =
@"class B : I<A>
{
    void I<A>.M<U>(I<U> o) { }
    void I<A>.N(I<A> o) {}
    I<A> I<A>.P { get { return null; } }
}

class C<CT> : I<CT>
{
    void I<CT>.M<U>(I<U> o) { }
    void I<CT>.N(I<CT> o) { }
    I<CT> I<CT>.P { get { return null; } }
}
";
            var compilation2 = CreateCompilation(source2, new[] { new CSharpCompilationReference(compilation1_v1) }, assemblyName: "assembly2");

            var compilation2Ref = new CSharpCompilationReference(compilation2);

            var compilation3 = CreateCompilation("", new[] { compilation2Ref, new CSharpCompilationReference(compilation1_v2) }, assemblyName: "assembly3");

            var assembly2 = compilation3.GetReferencedAssemblySymbol(compilation2Ref);
            MethodSymbol implemented_m;
            MethodSymbol implemented_n;
            PropertySymbol implemented_p;

            var b = assembly2.GetTypeByMetadataName("B");
            var m = b.GetMethod("I<A>.M");
            implemented_m = m.ExplicitInterfaceImplementations[0];

            Assert.Equal("void I<A>.M<U>(I<U> o)", implemented_m.ToTestDisplayString());

            var a_v2 = compilation1_v2.GetTypeByMetadataName("A");
            var i_a_v2 = compilation1_v2.GetTypeByMetadataName("I`1").Construct(ImmutableArray.Create<TypeSymbol>(a_v2));
            var i_a_m_v2 = i_a_v2.GetMethod("M");
            Assert.Equal(i_a_m_v2, implemented_m);

            var n = b.GetMethod("I<A>.N");
            implemented_n = n.ExplicitInterfaceImplementations[0];

            Assert.Equal("void I<A>.N(I<A> o)", implemented_n.ToTestDisplayString());

            var i_a_n_v2 = i_a_v2.GetMethod("N");
            Assert.Equal(i_a_n_v2, implemented_n);

            var p = b.GetProperty("I<A>.P");
            implemented_p = p.ExplicitInterfaceImplementations[0];

            Assert.Equal("I<A> I<A>.P { get; }", implemented_p.ToTestDisplayString());

            var i_a_p_v2 = i_a_v2.GetProperty("P");
            Assert.Equal(i_a_p_v2, implemented_p);

            var c = assembly2.GetTypeByMetadataName("C`1");
            var i_ct_v2 = compilation1_v2.GetTypeByMetadataName("I`1").Construct(ImmutableArray.Create<TypeSymbol>(c.TypeParameters[0]));

            implemented_m = c.GetMethod("I<CT>.M").ExplicitInterfaceImplementations[0];

            Assert.Equal("void I<CT>.M<U>(I<U> o)", implemented_m.ToTestDisplayString());

            var i_ct_m_v2 = i_ct_v2.GetMethod("M");
            Assert.Equal(i_ct_m_v2, implemented_m);

            implemented_n = c.GetMethod("I<CT>.N").ExplicitInterfaceImplementations[0];

            Assert.Equal("void I<CT>.N(I<CT> o)", implemented_n.ToTestDisplayString());

            var i_ct_n_v2 = i_ct_v2.GetMethod("N");
            Assert.Equal(i_ct_n_v2, implemented_n);

            implemented_p = c.GetProperty("I<CT>.P").ExplicitInterfaceImplementations[0];

            Assert.Equal("I<CT> I<CT>.P { get; }", implemented_p.ToTestDisplayString());

            var i_ct_p_v2 = i_ct_v2.GetProperty("P");
            Assert.Equal(i_ct_p_v2, implemented_p);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        public void RetargetMissingEnumUnderlyingType_Implicit()
        {
            var source = @"
public enum E
{
}
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (2,13): error CS0518: Predefined type 'System.Enum' is not defined or imported
                // public enum E
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Enum"),
                // (2,13): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // public enum E
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Int32"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, sourceType.Interfaces().Length); // Always returns an empty list for enums.
            Assert.Equal(TypeKind.Error, sourceType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_Enum, sourceType.BaseType().SpecialType);
            Assert.Equal(TypeKind.Error, sourceType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int32, sourceType.EnumUnderlyingType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(MissingCorLibrarySymbol.Instance); // Need to do this explicitly since our retargeting assembly wasn't constructed using the real mechanism.
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, retargetingType.Interfaces().Length);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_Enum, retargetingType.BaseType().SpecialType);
            Assert.Equal(TypeKind.Error, retargetingType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int32, retargetingType.EnumUnderlyingType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        public void RetargetMissingEnumUnderlyingType_Explicit()
        {
            var source = @"
public enum E : short
{
}
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (2,13): error CS0518: Predefined type 'System.Enum' is not defined or imported
                // public enum E : short
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Enum"),
                // (2,17): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public enum E : short
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, sourceType.Interfaces().Length); // Always returns an empty list for enums.
            Assert.Equal(TypeKind.Error, sourceType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_Enum, sourceType.BaseType().SpecialType);
            Assert.Equal(TypeKind.Error, sourceType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.EnumUnderlyingType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(MissingCorLibrarySymbol.Instance); // Need to do this explicitly since our retargeting assembly wasn't constructed using the real mechanism.
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, retargetingType.Interfaces().Length);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_Enum, retargetingType.BaseType().SpecialType);
            Assert.Equal(TypeKind.Error, retargetingType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.EnumUnderlyingType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        public void RetargetInvalidBaseType_Class()
        {
            var source = @"
public class Test : short { }
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,21): error CS0509: 'Test': cannot derive from sealed type 'short'
                // public class Test : short { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "short").WithArguments("Test", "short"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces().Length);
            Assert.Equal(SpecialType.System_Object, sourceType.BaseType().SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces().Length);
            Assert.Equal(SpecialType.System_Object, retargetingType.BaseType().SpecialType);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        public void RetargetMissingBaseType_Class()
        {
            var source = @"
public class Test : short { }
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (2,21): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public class Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"),
                // (2,21): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public class Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces().Length);
            Assert.Equal(TypeKind.Error, sourceType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.BaseType().SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces().Length);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.BaseType().SpecialType);
        }

        [Fact]
        [WorkItem(3898, "https://github.com/dotnet/roslyn/issues/3898")]
        public void Retarget_IsSerializable()
        {
            var source = @"
public class Test { }
[System.Serializable]
public class TestS { }
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var retargetingAssembly = new RetargetingAssemblySymbol((SourceAssemblySymbol)comp.Assembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.IsType<RetargetingNamedTypeSymbol>(retargetingType);
            Assert.False(retargetingType.IsSerializable);

            var retargetingTypeS = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("TestS");
            Assert.IsType<RetargetingNamedTypeSymbol>(retargetingTypeS);
            Assert.True(retargetingTypeS.IsSerializable);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        public void RetargetInvalidBaseType_Struct()
        {
            var source = @"
public struct Test : short { }
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,22): error CS0527: Type 'short' in interface list is not an interface
                // public struct Test : short { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "short").WithArguments("short"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces().Length);
            Assert.Equal(SpecialType.System_ValueType, sourceType.BaseType().SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces().Length);
            Assert.Equal(SpecialType.System_ValueType, retargetingType.BaseType().SpecialType);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        [WorkItem(609515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609515")]
        public void RetargetMissingBaseType_Struct()
        {
            var source = @"
public struct Test : short { }
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (2,22): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public struct Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"),
                // (2,15): error CS0518: Predefined type 'System.ValueType' is not defined or imported
                // public struct Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.ValueType"),
                // (2,15): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public struct Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, sourceType.Interfaces().Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.Interfaces().Single().SpecialType);
            Assert.Equal(TypeKind.Error, sourceType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_ValueType, sourceType.BaseType().SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, retargetingType.Interfaces().Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.Interfaces().Single().SpecialType);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType().TypeKind);
            Assert.Equal(SpecialType.System_ValueType, retargetingType.BaseType().SpecialType);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        public void RetargetInvalidBaseType_Interface()
        {
            var source = @"
public interface Test : short { }
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,25): error CS0527: Type 'short' in interface list is not an interface
                // public interface Test : short { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "short").WithArguments("short"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces().Length);
            Assert.Null(sourceType.BaseType());

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces().Length);
            Assert.Null(retargetingType.BaseType());
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        [WorkItem(609515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609515")]
        public void RetargetMissingBaseType_Interface()
        {
            var source = @"
public interface Test : short { }
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (2,25): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public interface Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"),
                // (2,18): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public interface Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, sourceType.Interfaces().Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.Interfaces().Single().SpecialType);
            Assert.Null(sourceType.BaseType());

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, retargetingType.Interfaces().Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.Interfaces().Single().SpecialType);
            Assert.Null(retargetingType.BaseType());
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        [WorkItem(609519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609519")]
        public void RetargetInvalidConstraint()
        {
            var source = @"
public class C<T> where T : int
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,29): error CS0701: 'int' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // public class C<T> where T : int
                Diagnostic(ErrorCode.ERR_BadBoundType, "int").WithArguments("int"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var sourceTypeParameter = sourceType.TypeParameters.Single();
            Assert.Equal(0, sourceTypeParameter.ConstraintTypes().Length);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var retargetingTypeParameter = retargetingType.TypeParameters.Single();
            Assert.Equal(0, retargetingTypeParameter.ConstraintTypes().Length);
        }

        [Fact]
        [WorkItem(604878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/604878")]
        [WorkItem(609519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609519")]
        public void RetargetMissingConstraint()
        {
            var source = @"
public class C<T> where T : int
{
}
";

            var comp = CreateEmptyCompilation(source);
            comp.VerifyDiagnostics(
                // (2,14): error CS0518: Predefined type 'System.Object' is not defined or imported
                // public class C<T> where T : int
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Object"),
                // (2,29): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // public class C<T> where T : int
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32"),
                // (2,16): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // public class C<T> where T : int
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "T").WithArguments("System.Int32"),
                // (2,14): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // public class C<T> where T : int
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("object", "0"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var sourceTypeParameter = sourceType.TypeParameters.Single();
            var sourceTypeParameterConstraint = sourceTypeParameter.ConstraintTypes().Single();
            Assert.Equal(TypeKind.Error, sourceTypeParameterConstraint.TypeKind);
            Assert.Equal(SpecialType.System_Int32, sourceTypeParameterConstraint.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var retargetingTypeParameter = retargetingType.TypeParameters.Single();
            var retargetingTypeParameterConstraint = retargetingTypeParameter.ConstraintTypes().Single();
            Assert.Equal(TypeKind.Error, retargetingTypeParameterConstraint.TypeKind);
            Assert.Equal(SpecialType.System_Int32, retargetingTypeParameterConstraint.SpecialType);
        }

        [Theory]
        [InlineData("class Test<T> where T : unmanaged { }", true)]
        [InlineData("class Test<T> { }", false)]
        public void RetargetingUnmanagedTypeParameters(string code, bool isUnmanaged)
        {
            var compilation = CreateCompilation(code).VerifyDiagnostics();
            var sourceAssembly = (SourceAssemblySymbol)compilation.Assembly;

            SourceTypeParameterSymbol sourceTypeParameter = (SourceTypeParameterSymbol)sourceAssembly.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
            Assert.Equal(isUnmanaged, sourceTypeParameter.HasUnmanagedTypeConstraint);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(sourceAssembly.CorLibrary);

            RetargetingTypeParameterSymbol retargetingTypeParameter = (RetargetingTypeParameterSymbol)retargetingAssembly.GlobalNamespace.GetTypeMember("Test").TypeParameters.Single();
            Assert.Equal(isUnmanaged, retargetingTypeParameter.HasUnmanagedTypeConstraint);
        }

        private void CheckTypes(Symbol source, Symbol retargeting)
        {
            CheckUnderlyingMember(source.OriginalDefinition, ((RetargetingNamedTypeSymbol)retargeting.OriginalDefinition).UnderlyingNamedType);
        }

        private void CheckMethods(Symbol source, Symbol retargeting)
        {
            Assert.Equal(source == null, retargeting == null);
            if (source != null)
            {
                var sourceMethod = (SourceMemberMethodSymbol)source;
                var retargetingMethod = (RetargetingMethodSymbol)retargeting;
                CheckUnderlyingMember(sourceMethod, retargetingMethod.UnderlyingMethod);
                CheckParameters(sourceMethod.Parameters, retargetingMethod.Parameters);
            }
        }

        private void CheckParameters(ImmutableArray<ParameterSymbol> source, ImmutableArray<ParameterSymbol> retargeting)
        {
            Assert.Equal(source.Length, retargeting.Length);
            for (int i = 0; i < source.Length; i++)
            {
                CheckParameters(source[i], retargeting[i]);
            }
        }

        private void CheckParameters(Symbol source, Symbol retargeting)
        {
            CheckUnderlyingMember(source, ((RetargetingParameterSymbol)retargeting).UnderlyingParameter);
        }

        private void CheckUnderlyingMember(Symbol source, Symbol underlying)
        {
            Assert.NotNull(source);
            Assert.NotNull(underlying);
            Assert.Same(underlying, source);
        }

        private void CheckReducedExtensionMethods(MethodSymbol source, MethodSymbol retargeting)
        {
            CheckMethods(source.ReducedFrom, retargeting.ReducedFrom);
        }

        [Fact, WorkItem(703433, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703433")]
        public void Bug703433()
        {
            var source =
@"
class C1<T>
{
}";
            var comp1 = CreateEmptyCompilation(source, new[] { MscorlibRef_v20 }, TestOptions.ReleaseDll);
            comp1.VerifyDiagnostics();

            NamedTypeSymbol c1 = comp1.Assembly.GlobalNamespace.GetTypeMembers("C1").Single();

            var comp2 = CreateEmptyCompilation("", new[] { MscorlibRef_v4_0_30316_17626, new CSharpCompilationReference(comp1) }, TestOptions.ReleaseDll);

            NamedTypeSymbol c1r = comp2.GlobalNamespace.GetTypeMembers("C1").Single();

            Assert.IsType<RetargetingNamedTypeSymbol>(c1r);
            Assert.Equal(c1.Name, c1r.Name);
            Assert.Equal(c1.Arity, c1r.Arity);
            Assert.Equal(c1.MangleName, c1r.MangleName);
            Assert.Equal(c1.MetadataName, c1r.MetadataName);
        }

        [Fact]
        public void FunctionPointerRetargeting_FullyConsistent()
        {
            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));
        }

        [Fact]
        public void FunctionPointerRetargeting_Return()
        {
            TestFunctionPointerRetargetingSignature(
                "method class [Ret]R modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref R>",
                returnConsistent: (typeConsistent: false, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));

            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Ret]R) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: false, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));

            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Ret]R) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: false),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));
        }

        [Fact]
        public void FunctionPointerRetargeting_Param1()
        {
            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Ret]R modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref R, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: false, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));

            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Ret]R), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: false, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));

            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Ret]R) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: false),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true));
        }

        [Fact]
        public void FunctionPointerRetargeting_Param2()
        {
            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Ret]R modopt([Con]C) & modopt([Con]C))",
                "delegate*<ref C, ref R, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: false, refModConsistent: true, typeModConsistent: true));

            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Con]C) & modopt([Ret]R))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: false, typeModConsistent: true));

            TestFunctionPointerRetargetingSignature(
                "method class [Con]C modopt([Con]C) & modopt([Con]C) *(class [Con]C modopt([Con]C) & modopt([Con]C), class [Con]C modopt([Ret]R) & modopt([Con]C))",
                "delegate*<ref C, ref C, ref C>",
                returnConsistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param1Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: true),
                param2Consistent: (typeConsistent: true, refModConsistent: true, typeModConsistent: false));
        }

        private void TestFunctionPointerRetargetingSignature(
            string ilSignature,
            string overriddenSignature,
            (bool typeConsistent, bool refModConsistent, bool typeModConsistent) returnConsistent,
            (bool typeConsistent, bool refModConsistent, bool typeModConsistent) param1Consistent,
            (bool typeConsistent, bool refModConsistent, bool typeModConsistent) param2Consistent)
        {
            var (retargetedAssembly1, retargetedAssembly2, consistentAssembly, originalComp, retargetedComp) = getFunctionPointerRetargetingDefinitions(ilSignature, overriddenSignature);

            var mOriginal = getMethodSymbol(originalComp);
            var mRetargeted = getMethodSymbol(retargetedComp);

            Assert.IsType<RetargetingAssemblySymbol>(mRetargeted.ContainingAssembly);
            Assert.NotSame(originalComp.Assembly, mRetargeted.ContainingAssembly);
            Assert.NotSame(retargetedAssembly1, retargetedAssembly2);
            Assert.Same(originalComp.Assembly, ((RetargetingAssemblySymbol)mRetargeted.ContainingAssembly).UnderlyingAssembly);

            var ptrOriginal = (FunctionPointerTypeSymbol)mOriginal.ReturnType;
            var ptrRetargeted = (FunctionPointerTypeSymbol)mRetargeted.ReturnType;

            FunctionPointerUtilities.CommonVerifyFunctionPointer(ptrOriginal);
            FunctionPointerUtilities.CommonVerifyFunctionPointer(ptrRetargeted);

            if ((true, true, true) == returnConsistent &&
                (true, true, true) == param1Consistent &&
                (true, true, true) == param2Consistent)
            {
                Assert.Same(ptrOriginal, ptrRetargeted);
            }
            else
            {
                Assert.NotSame(ptrOriginal, ptrRetargeted);
            }

            assert(returnConsistent.typeConsistent,
                   ptrOriginal.Signature.ReturnType,
                   ptrRetargeted.Signature.ReturnType);
            assert(returnConsistent.refModConsistent,
                   getModifierTypeSymbol(ptrOriginal.Signature.RefCustomModifiers),
                   getModifierTypeSymbol(ptrRetargeted.Signature.RefCustomModifiers));
            assert(returnConsistent.typeModConsistent,
                   getModifierTypeSymbol(ptrOriginal.Signature.ReturnTypeWithAnnotations.CustomModifiers),
                   getModifierTypeSymbol(ptrRetargeted.Signature.ReturnTypeWithAnnotations.CustomModifiers));

            Assert.Equal(2, ptrOriginal.Signature.ParameterCount);
            Assert.Equal(2, ptrRetargeted.Signature.ParameterCount);

            var param1Original = ptrOriginal.Signature.Parameters[0];
            var param2Original = ptrOriginal.Signature.Parameters[1];
            var param1Retargeted = ptrRetargeted.Signature.Parameters[0];
            var param2Retargeted = ptrRetargeted.Signature.Parameters[1];

            assert(param1Consistent.typeConsistent,
                   param1Original.Type,
                   param1Retargeted.Type);
            assert(param1Consistent.refModConsistent,
                   getModifierTypeSymbol(param1Original.RefCustomModifiers),
                   getModifierTypeSymbol(param1Retargeted.RefCustomModifiers));
            assert(param1Consistent.typeModConsistent,
                   getModifierTypeSymbol(param1Original.TypeWithAnnotations.CustomModifiers),
                   getModifierTypeSymbol(param1Retargeted.TypeWithAnnotations.CustomModifiers));

            assert(param2Consistent.typeConsistent,
                   param2Original.Type,
                   param2Retargeted.Type);
            assert(param2Consistent.refModConsistent,
                   getModifierTypeSymbol(param2Original.RefCustomModifiers),
                   getModifierTypeSymbol(param2Retargeted.RefCustomModifiers));
            assert(param2Consistent.typeModConsistent,
                   getModifierTypeSymbol(param2Original.TypeWithAnnotations.CustomModifiers),
                   getModifierTypeSymbol(param2Retargeted.TypeWithAnnotations.CustomModifiers));

            static MethodSymbol getMethodSymbol(CSharpCompilation compilation)
            {
                var c = compilation.GetTypeByMetadataName("Source");
                return c.GetMethod("M");
            }

            static TypeSymbol getModifierTypeSymbol(ImmutableArray<CustomModifier> modifiers)
                => ((CSharpCustomModifier)modifiers.Single()).ModifierSymbol;

            void assert(bool consistent, TypeSymbol originalType, TypeSymbol retargetedType)
            {
                Assert.False(originalType.IsErrorType());
                Assert.False(retargetedType.IsErrorType());
                if (consistent)
                {
                    Assert.Same(consistentAssembly, originalType.ContainingAssembly);
                    Assert.Same(consistentAssembly, retargetedType.ContainingAssembly);
                }
                else
                {
                    Assert.Same(retargetedAssembly1, originalType.ContainingAssembly);
                    Assert.Same(retargetedAssembly2, retargetedType.ContainingAssembly);
                }
            }

            static (AssemblySymbol retargetedAssembly1, AssemblySymbol retargetedAssembly2, AssemblySymbol consistentAssembly, CSharpCompilation originalComp, CSharpCompilation retargetedComp)
                getFunctionPointerRetargetingDefinitions(string mIlSignature, string mOverriddenSignature)
            {
                var retargetedSource = @"public class R {{}}";
                var retargetedIdentity = new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true);
                var standardReference = TargetFrameworkUtil.StandardReferences.ToArray();
                var retargeted1 = CreateCompilation(retargetedIdentity, new[] { retargetedSource }, references: standardReference);
                var retargeted1Ref = retargeted1.ToMetadataReference();
                var retargeted2 = CreateCompilation(retargetedIdentity.WithVersion(new Version(2, 0, 0, 0)), new[] { retargetedSource }, references: standardReference);
                var retargeted2Ref = retargeted2.ToMetadataReference();

                var consistent = CreateCompilation("public class C {}", assemblyName: "Con", targetFramework: TargetFramework.Standard);
                var consistentRef = consistent.ToMetadataReference();

                var ilSource = $@"
{buildAssemblyExternClause(retargeted1)}
{buildAssemblyExternClause(consistent)}
.class public auto ansi beforefieldinit Il
       extends [mscorlib]System.Object
{{
    .method public hidebysig newslot virtual 
        instance {mIlSignature} 'M' ()
    {{
        .maxstack 8

        ldnull
        throw
    }}

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {{
        .maxstack  8
        ldarg.0
        call       instance void [mscorlib]System.Object::.ctor()
        ret
    }}
}}
";

                var ilRef = CompileIL(ilSource);

                var originalComp = CreateCompilation($@"
unsafe class Source : Il
{{
    public override {mOverriddenSignature} M() => throw null;
}}", new[] { retargeted1Ref, consistentRef, ilRef }, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.Standard);

                originalComp.VerifyDiagnostics();

                var retargetedComp = CreateCompilation("", references: new[] { originalComp.ToMetadataReference(), retargeted2Ref, consistentRef, ilRef },
                                                       options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9,
                                                       targetFramework: TargetFramework.Standard);

                retargetedComp.VerifyDiagnostics();

                return (retargeted1.Assembly, retargeted2.Assembly, consistent.Assembly, originalComp, retargetedComp);

                static string buildAssemblyExternClause(CSharpCompilation comp)
                {
                    AssemblyIdentity assemblyIdentity = comp.Assembly.Identity;
                    System.Version version = assemblyIdentity.Version;

                    return $@"
.assembly extern {assemblyIdentity.Name}
{{
  .ver {version.Major}:{version.Minor}:{version.Build}:{version.Revision}
}}
";
                }
            }
        }

        [Fact]
        public void RetargetedUnmanagedCallersOnlyData()
        {
            var originalIdentity = new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true);
            // Custom corlib is necessary as the CallConv type must be defined in corlib, and we need to make
            // sure that it's retargeted correctly.
            string corlibSource = @"
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public abstract partial class Enum : ValueType {}
    public class String { }
    public struct Boolean { }
    public struct Int32 { }
    public class Type { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) {}
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { Method = 0x0040, }
    namespace Runtime
    {
        namespace InteropServices
        {
            [AttributeUsage(AttributeTargets.Method, Inherited = false)]
            public sealed class UnmanagedCallersOnlyAttribute : Attribute
            {
                public UnmanagedCallersOnlyAttribute()
                {
                }

                public Type[] CallConvs;
                public string EntryPoint;
            }
        }
        namespace CompilerServices
        {
            public sealed class CallConvCdecl {}
        }
    }
}
";
            var beforeRetargeting = CreateCompilation(originalIdentity, new[] { corlibSource }, new MetadataReference[0]);
            beforeRetargeting.VerifyDiagnostics();

            var unmanagedCallersOnlyAssembly = CreateEmptyCompilation(@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) } )]
    public static void M(int s) {}
}
", new[] { beforeRetargeting.ToMetadataReference() });
            unmanagedCallersOnlyAssembly.VerifyDiagnostics();

            var afterRetargeting = CreateCompilation(originalIdentity.WithVersion(new Version(2, 0, 0, 0)), new[] { corlibSource }, new MetadataReference[0]);
            afterRetargeting.VerifyDiagnostics();

            var finalComp = CreateEmptyCompilation(@"C.M(1);", options: TestOptions.ReleaseExe, references: new[] { afterRetargeting.ToMetadataReference(), unmanagedCallersOnlyAssembly.ToMetadataReference() });
            finalComp.VerifyDiagnostics(
                // (1,1): error CS8901: 'C.M(int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                // C.M(1);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "C.M(1)").WithArguments("C.M(int)").WithLocation(1, 1)
            );

            var m = finalComp.GetTypeByMetadataName("C").GetMethod("M");
            var unmanagedCallersOnlyData = m.GetUnmanagedCallersOnlyAttributeData(forceComplete: true);
            Assert.IsType<RetargetingMethodSymbol>(m);
            var containingAssembly = unmanagedCallersOnlyData.CallingConventionTypes.Single().ContainingAssembly;
            Assert.NotSame(containingAssembly, beforeRetargeting.Assembly);
            Assert.Same(containingAssembly, afterRetargeting.Assembly);
        }

        [Fact]
        public void RetargetedUnmanagedCallersOnlyEmptyData()
        {
            var originalIdentity = new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true);
            // Custom corlib is necessary as the CallConv type must be defined in corlib, and we need to make
            // sure that it's retargeted correctly.
            string corlibSource = @"
namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public abstract partial class Enum : ValueType {}
    public class String { }
    public struct Boolean { }
    public struct Int32 { }
    public class Type { }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets validOn) {}
        public bool Inherited { get; set; }
    }
    public enum AttributeTargets { Method = 0x0040, }
    namespace Runtime
    {
        namespace InteropServices
        {
            [AttributeUsage(AttributeTargets.Method, Inherited = false)]
            public sealed class UnmanagedCallersOnlyAttribute : Attribute
            {
                public UnmanagedCallersOnlyAttribute()
                {
                }

                public Type[] CallConvs;
                public string EntryPoint;
            }
        }
        namespace CompilerServices
        {
            public sealed class CallConvCdecl {}
        }
    }
}
";
            var beforeRetargeting = CreateCompilation(originalIdentity, new[] { corlibSource }, new MetadataReference[0]);
            beforeRetargeting.VerifyDiagnostics();

            var unmanagedCallersOnlyAssembly = CreateEmptyCompilation(@"
using System.Runtime.InteropServices;
public class C
{
    [UnmanagedCallersOnly]
    public static void M(int s) {}
}
", new[] { beforeRetargeting.ToMetadataReference() });
            unmanagedCallersOnlyAssembly.VerifyDiagnostics();

            var afterRetargeting = CreateCompilation(originalIdentity.WithVersion(new Version(2, 0, 0, 0)), new[] { corlibSource }, new MetadataReference[0]);
            afterRetargeting.VerifyDiagnostics();

            var finalComp = CreateEmptyCompilation(@"C.M(1);", options: TestOptions.ReleaseExe, references: new[] { afterRetargeting.ToMetadataReference(), unmanagedCallersOnlyAssembly.ToMetadataReference() });
            finalComp.VerifyDiagnostics(
                // (1,1): error CS8901: 'C.M(int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                // C.M(1);
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, "C.M(1)").WithArguments("C.M(int)").WithLocation(1, 1)
            );
        }
    }

    internal abstract class SymbolChecker
    {
        public void CheckSymbols(TypeWithAnnotations a, TypeWithAnnotations b, bool recurse)
        {
            CheckSymbols(a.Type, b.Type, recurse);
        }

        public void CheckSymbols(Symbol a, Symbol b, bool recurse)
        {
            Assert.Equal(a == null, b == null);
            if (a != null)
            {
                Assert.Equal(a.Kind, b.Kind);
                CheckSymbolsInternal(a, b);

                if (!recurse)
                {
                    return;
                }

                switch (a.Kind)
                {
                    case SymbolKind.Field:
                        CheckFields((FieldSymbol)a, (FieldSymbol)b);
                        break;
                    case SymbolKind.NamedType:
                        CheckNamedTypes((NamedTypeSymbol)a, (NamedTypeSymbol)b);
                        break;
                    case SymbolKind.Method:
                        CheckMethods((MethodSymbol)a, (MethodSymbol)b);
                        break;
                    case SymbolKind.Parameter:
                        CheckParameters((ParameterSymbol)a, (ParameterSymbol)b);
                        break;
                    case SymbolKind.Property:
                        CheckProperties((PropertySymbol)a, (PropertySymbol)b);
                        break;
                    case SymbolKind.TypeParameter:
                        CheckTypeParameters((TypeParameterSymbol)a, (TypeParameterSymbol)b);
                        break;
                    default:
                        Assert.True(false, "Unexpected symbol kind: " + a.Kind);
                        break;
                }
            }
        }

        protected abstract void CheckSymbolsInternal(Symbol a, Symbol b);

        public void CheckSymbols<T>(ImmutableArray<T> a, ImmutableArray<T> b, bool recurse)
            where T : Symbol
        {
            Assert.Equal(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                CheckSymbols(a[i], b[i], recurse);
            }
        }

        public void CheckMarshallingInformation(MarshalPseudoCustomAttributeData a, MarshalPseudoCustomAttributeData b)
        {
            Assert.Equal(a == null, b == null);
            if (a != null)
            {
                CheckSymbols((Symbol)a.TryGetSafeArrayElementUserDefinedSubtype(),
                             (Symbol)b.TryGetSafeArrayElementUserDefinedSubtype(),
                             recurse: false);
            }
        }

        public void CheckFields(FieldSymbol a, FieldSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.TypeWithAnnotations, b.TypeWithAnnotations, recurse: false);
            CheckSymbols(a.AssociatedSymbol, b.AssociatedSymbol, recurse: false);
            CheckMarshallingInformation(a.MarshallingInformation, b.MarshallingInformation);
        }

        public void CheckMethods(MethodSymbol a, MethodSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.Parameters, b.Parameters, false);
            CheckSymbols(a.ReturnTypeWithAnnotations, b.ReturnTypeWithAnnotations, false);
            CheckSymbols(a.TypeParameters, b.TypeParameters, true);
            CheckMarshallingInformation(a.ReturnValueMarshallingInformation, b.ReturnValueMarshallingInformation);
        }

        public void CheckNamedTypes(NamedTypeSymbol a, NamedTypeSymbol b)
        {
            CheckTypes((TypeSymbol)a, (TypeSymbol)b);
            CheckSymbols(a.TypeParameters, b.TypeParameters, true);
        }

        public void CheckParameters(ParameterSymbol a, ParameterSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            Assert.Equal(a.Ordinal, b.Ordinal);
            CheckSymbols(a.TypeWithAnnotations, b.TypeWithAnnotations, false);
            CheckMarshallingInformation(a.MarshallingInformation, b.MarshallingInformation);
        }

        public void CheckProperties(PropertySymbol a, PropertySymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.Parameters, b.Parameters, false);
            CheckSymbols(a.TypeWithAnnotations, b.TypeWithAnnotations, false);
            CheckSymbols(a.GetMethod, b.GetMethod, true);
            CheckSymbols(a.SetMethod, b.SetMethod, true);
        }

        public void CheckTypes(TypeSymbol a, TypeSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.BaseType(), b.BaseType(), false);
            CheckSymbols(a.Interfaces(), b.Interfaces(), false);
            CheckSymbols(a.GetMembers(), b.GetMembers(), true);
        }

        public void CheckTypeParameters(TypeParameterSymbol a, TypeParameterSymbol b)
        {
            CheckTypes((TypeSymbol)a, (TypeSymbol)b);
            Assert.Equal(a.HasConstructorConstraint, b.HasConstructorConstraint);
            Assert.Equal(a.HasReferenceTypeConstraint, b.HasReferenceTypeConstraint);
            Assert.Equal(a.HasValueTypeConstraint, b.HasValueTypeConstraint);
            CheckSymbols(a.ConstraintTypes(), b.ConstraintTypes(), false);
        }
    }

    internal sealed class RetargetingSymbolChecker : SymbolChecker
    {
        public static void CheckSymbols(Symbol a, Symbol b)
        {
            var checker = new RetargetingSymbolChecker();
            checker.CheckSymbols(a, b, true);
        }

        protected override void CheckSymbolsInternal(Symbol a, Symbol b)
        {
            a = a.OriginalDefinition;
            b = b.OriginalDefinition;
            var underlying = GetUnderlyingSymbol(b);
            Assert.NotNull(underlying);
            Assert.Same(underlying, a);
        }

        private static Symbol GetUnderlyingSymbol(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    {
                        var retargeting = symbol as RetargetingNamedTypeSymbol;
                        return ((object)retargeting != null) ? retargeting.UnderlyingNamedType : symbol;
                    }
                case SymbolKind.Field:
                    return ((RetargetingFieldSymbol)symbol).UnderlyingField;
                case SymbolKind.Method:
                    return ((RetargetingMethodSymbol)symbol).UnderlyingMethod;
                case SymbolKind.Parameter:
                    return ((RetargetingParameterSymbol)symbol).UnderlyingParameter;
                case SymbolKind.Property:
                    return ((RetargetingPropertySymbol)symbol).UnderlyingProperty;
                case SymbolKind.TypeParameter:
                    return ((RetargetingTypeParameterSymbol)symbol).UnderlyingTypeParameter;
            }
            return null;
        }
    }
}
