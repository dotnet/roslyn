// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var sourceReduced = sourceMethod.ReduceExtensionMethod(sourceType);
            var retargetingReduced = retargetingMethod.ReduceExtensionMethod(retargetingType);
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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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
            var compilation = CreateCompilationWithMscorlib(source);

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

            var compilation = CreateCompilationWithMscorlib(source);

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

        [WorkItem(542571, "DevDiv")]
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
            var compilation1_v1 = CreateCompilationWithMscorlib(source1, assemblyName: "assembly1");
            var compilation1_v2 = CreateCompilationWithMscorlib(source1, assemblyName: "assembly1");

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
            var compilation2 = CreateCompilationWithMscorlib(source2, new[] { new CSharpCompilationReference(compilation1_v1) }, assemblyName: "assembly2");

            var compilation2Ref = new CSharpCompilationReference(compilation2);

            var compilation3 = CreateCompilationWithMscorlib("", new[] { compilation2Ref, new CSharpCompilationReference(compilation1_v2) }, assemblyName: "assembly3");

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
        [WorkItem(604878, "DevDiv")]
        public void RetargetMissingEnumUnderlyingType_Implicit()
        {
            var source = @"
public enum E
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,13): error CS0518: Predefined type 'System.Enum' is not defined or imported
                // public enum E
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Enum"),
                // (2,13): error CS0518: Predefined type 'System.Int32' is not defined or imported
                // public enum E
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Int32"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, sourceType.Interfaces.Length); // Always returns an empty list for enums.
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_Enum, sourceType.BaseType.SpecialType);
            Assert.Equal(TypeKind.Error, sourceType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int32, sourceType.EnumUnderlyingType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(MissingCorLibrarySymbol.Instance); // Need to do this explicitly since our retargeting assembly wasn't constructed using the real mechanism.
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, retargetingType.Interfaces.Length);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_Enum, retargetingType.BaseType.SpecialType);
            Assert.Equal(TypeKind.Error, retargetingType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int32, retargetingType.EnumUnderlyingType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        public void RetargetMissingEnumUnderlyingType_Explicit()
        {
            var source = @"
public enum E : short
{
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,13): error CS0518: Predefined type 'System.Enum' is not defined or imported
                // public enum E : short
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Enum"),
                // (2,17): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public enum E : short
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, sourceType.Interfaces.Length); // Always returns an empty list for enums.
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_Enum, sourceType.BaseType.SpecialType);
            Assert.Equal(TypeKind.Error, sourceType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.EnumUnderlyingType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(MissingCorLibrarySymbol.Instance); // Need to do this explicitly since our retargeting assembly wasn't constructed using the real mechanism.
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(0, retargetingType.Interfaces.Length);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_Enum, retargetingType.BaseType.SpecialType);
            Assert.Equal(TypeKind.Error, retargetingType.EnumUnderlyingType.TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.EnumUnderlyingType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        public void RetargetInvalidBaseType_Class()
        {
            var source = @"
public class Test : short { }
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (2,14): error CS0509: 'Test': cannot derive from sealed type 'short'
                // public class Test : short { }
                Diagnostic(ErrorCode.ERR_CantDeriveFromSealedType, "Test").WithArguments("Test", "short"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces.Length);
            Assert.Equal(SpecialType.System_Object, sourceType.BaseType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces.Length);
            Assert.Equal(SpecialType.System_Object, retargetingType.BaseType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        public void RetargetMissingBaseType_Class()
        {
            var source = @"
public class Test : short { }
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,21): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public class Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"),
                // (2,21): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public class Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces.Length);
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.BaseType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces.Length);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.BaseType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        public void RetargetInvalidBaseType_Struct()
        {
            var source = @"
public struct Test : short { }
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (2,22): error CS0527: Type 'short' in interface list is not an interface
                // public struct Test : short { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "short").WithArguments("short"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces.Length);
            Assert.Equal(SpecialType.System_ValueType, sourceType.BaseType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces.Length);
            Assert.Equal(SpecialType.System_ValueType, retargetingType.BaseType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        [WorkItem(609515, "DevDiv")]
        public void RetargetMissingBaseType_Struct()
        {
            var source = @"
public struct Test : short { }
";

            var comp = CreateCompilation(source);
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
            Assert.Equal(TypeKind.Error, sourceType.Interfaces.Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.Interfaces.Single().SpecialType);
            Assert.Equal(TypeKind.Error, sourceType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_ValueType, sourceType.BaseType.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, retargetingType.Interfaces.Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.Interfaces.Single().SpecialType);
            Assert.Equal(TypeKind.Error, retargetingType.BaseType.TypeKind);
            Assert.Equal(SpecialType.System_ValueType, retargetingType.BaseType.SpecialType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        public void RetargetInvalidBaseType_Interface()
        {
            var source = @"
public interface Test : short { }
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (2,25): error CS0527: Type 'short' in interface list is not an interface
                // public interface Test : short { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "short").WithArguments("short"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, sourceType.Interfaces.Length);
            Assert.Null(sourceType.BaseType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(0, retargetingType.Interfaces.Length);
            Assert.Null(retargetingType.BaseType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        [WorkItem(609515, "DevDiv")]
        public void RetargetMissingBaseType_Interface()
        {
            var source = @"
public interface Test : short { }
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,25): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public interface Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "short").WithArguments("System.Int16"),
                // (2,18): error CS0518: Predefined type 'System.Int16' is not defined or imported
                // public interface Test : short { }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Test").WithArguments("System.Int16"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, sourceType.Interfaces.Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, sourceType.Interfaces.Single().SpecialType);
            Assert.Null(sourceType.BaseType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");
            Assert.Equal(TypeKind.Error, retargetingType.Interfaces.Single().TypeKind);
            Assert.Equal(SpecialType.System_Int16, retargetingType.Interfaces.Single().SpecialType);
            Assert.Null(retargetingType.BaseType);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        [WorkItem(609519, "DevDiv")]
        public void RetargetInvalidConstraint()
        {
            var source = @"
public class C<T> where T : int
{
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (2,29): error CS0701: 'int' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                // public class C<T> where T : int
                Diagnostic(ErrorCode.ERR_BadBoundType, "int").WithArguments("int"));

            var sourceAssembly = (SourceAssemblySymbol)comp.Assembly;
            var sourceType = sourceAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var sourceTypeParameter = sourceType.TypeParameters.Single();
            Assert.Equal(0, sourceTypeParameter.ConstraintTypes.Length);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var retargetingTypeParameter = retargetingType.TypeParameters.Single();
            Assert.Equal(0, retargetingTypeParameter.ConstraintTypes.Length);
        }

        [Fact]
        [WorkItem(604878, "DevDiv")]
        [WorkItem(609519, "DevDiv")]
        public void RetargetMissingConstraint()
        {
            var source = @"
public class C<T> where T : int
{
}
";

            var comp = CreateCompilation(source);
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
            var sourceTypeParameterConstraint = sourceTypeParameter.ConstraintTypes.Single();
            Assert.Equal(TypeKind.Error, sourceTypeParameterConstraint.TypeKind);
            Assert.Equal(SpecialType.System_Int32, sourceTypeParameterConstraint.SpecialType);

            var retargetingAssembly = new RetargetingAssemblySymbol(sourceAssembly, isLinked: false);
            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var retargetingTypeParameter = retargetingType.TypeParameters.Single();
            var retargetingTypeParameterConstraint = retargetingTypeParameter.ConstraintTypes.Single();
            Assert.Equal(TypeKind.Error, retargetingTypeParameterConstraint.TypeKind);
            Assert.Equal(SpecialType.System_Int32, retargetingTypeParameterConstraint.SpecialType);
        }

        private void CheckTypes(ImmutableArray<TypeSymbol> source, ImmutableArray<TypeSymbol> retargeting)
        {
            Assert.Equal(source.Length, retargeting.Length);
            for (int i = 0; i < source.Length; i++)
            {
                CheckTypes(source[i], retargeting[i]);
            }
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
                var sourceMethod = (SourceMethodSymbol)source;
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

        [Fact, WorkItem(703433, "DevDiv")]
        public void Bug703433()
        {
            var source =
@"
class C1<T>
{
}";
            var comp1 = CreateCompilation(source, new[] { MscorlibRef_v20 }, TestOptions.ReleaseDll);
            comp1.VerifyDiagnostics();

            NamedTypeSymbol c1 = comp1.Assembly.GlobalNamespace.GetTypeMembers("C1").Single();

            var comp2 = CreateCompilation("", new[] { MscorlibRef_v45, new CSharpCompilationReference(comp1) }, TestOptions.ReleaseDll);

            NamedTypeSymbol c1r = comp2.GlobalNamespace.GetTypeMembers("C1").Single();

            Assert.IsType<RetargetingNamedTypeSymbol>(c1r);
            Assert.Equal(c1.Name, c1r.Name);
            Assert.Equal(c1.Arity, c1r.Arity);
            Assert.Equal(c1.MangleName, c1r.MangleName);
            Assert.Equal(c1.MetadataName, c1r.MetadataName);
        }
    }

    internal abstract class SymbolChecker
    {
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
                CheckSymbols((TypeSymbol)a.TryGetSafeArrayElementUserDefinedSubtype(),
                             (TypeSymbol)b.TryGetSafeArrayElementUserDefinedSubtype(),
                             recurse: false);
            }
        }

        public void CheckFields(FieldSymbol a, FieldSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.Type, b.Type, recurse: false);
            CheckSymbols(a.AssociatedSymbol, b.AssociatedSymbol, recurse: false);
            CheckMarshallingInformation(a.MarshallingInformation, b.MarshallingInformation);
        }

        public void CheckMethods(MethodSymbol a, MethodSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.Parameters, b.Parameters, false);
            CheckSymbols(a.ReturnType, b.ReturnType, false);
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
            CheckSymbols(a.Type, b.Type, false);
            CheckMarshallingInformation(a.MarshallingInformation, b.MarshallingInformation);
        }

        public void CheckProperties(PropertySymbol a, PropertySymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.Parameters, b.Parameters, false);
            CheckSymbols(a.Type, b.Type, false);
            CheckSymbols(a.GetMethod, b.GetMethod, true);
            CheckSymbols(a.SetMethod, b.SetMethod, true);
        }

        public void CheckTypes(TypeSymbol a, TypeSymbol b)
        {
            Assert.Equal(a.Name, b.Name);
            CheckSymbols(a.BaseType, b.BaseType, false);
            CheckSymbols(a.Interfaces, b.Interfaces, false);
            CheckSymbols(a.GetMembers(), b.GetMembers(), true);
        }

        public void CheckTypeParameters(TypeParameterSymbol a, TypeParameterSymbol b)
        {
            CheckTypes((TypeSymbol)a, (TypeSymbol)b);
            Assert.Equal(a.HasConstructorConstraint, b.HasConstructorConstraint);
            Assert.Equal(a.HasReferenceTypeConstraint, b.HasReferenceTypeConstraint);
            Assert.Equal(a.HasValueTypeConstraint, b.HasValueTypeConstraint);
            CheckSymbols(a.ConstraintTypes, b.ConstraintTypes, false);
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
                        return (retargeting != null) ? retargeting.UnderlyingNamedType : symbol;
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
