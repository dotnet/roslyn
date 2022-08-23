// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_ScopedRef : CSharpTestBase
    {
        private const string ScopedRefAttributeDefinition =
@"namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ScopedRefAttribute : Attribute
    {
    }
}";

        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"class Program
{
    public static void F(scoped ref int i) { }
}";
            var comp = CreateCompilation(new[] { ScopedRefAttributeDefinition, source });
            var expected =
@"void Program.F(ref System.Int32 i)
    [ScopedRef] ref System.Int32 i
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Equal("System.Runtime.CompilerServices.ScopedRefAttribute", GetScopedRefType(module).ToTestDisplayString());
                AssertScopedRefAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var comp = CreateCompilation(ScopedRefAttributeDefinition);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source =
@"class Program
{
    public static void F(scoped ref int i) { }
}";
            comp = CreateCompilation(source, references: new[] { ref0 });
            var expected =
@"void Program.F(ref System.Int32 i)
    [ScopedRef] ref System.Int32 i
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Null(GetScopedRefType(module));
                AssertScopedRefAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_MissingConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class ScopedRefAttribute : Attribute
    {
        public ScopedRefAttribute(int i) { }
    }
}";
            var source2 =
@"class Program
{
    public static void F(scoped ref int i) { }
}";
            var comp = CreateCompilation(new[] { source1, source2 });
            comp.VerifyEmitDiagnostics(
                // (3,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.ScopedRefAttribute..ctor'
                //     public static void F(scoped ref int i) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "scoped ref int i").WithArguments("System.Runtime.CompilerServices.ScopedRefAttribute", ".ctor").WithLocation(3, 26));
        }

        [WorkItem(62124, "https://github.com/dotnet/roslyn/issues/62124")]
        [Fact]
        public void ExplicitAttribute_ReferencedInSource_01()
        {
            var source =
@"using System.Runtime.CompilerServices;
delegate void D([ScopedRef] ref int i);
class Program
{
    public static void Main([ScopedRef] string[] args)
    {
        D d = ([ScopedRef] ref int i) => { };
    }
}";
            var comp = CreateCompilation(new[] { ScopedRefAttributeDefinition, source });
            comp.VerifyDiagnostics(
                // (2,18): error CS9065: Do not use 'System.Runtime.CompilerServices.ScopedRefAttribute'. Use the 'scoped' keyword instead.
                // delegate void D([ScopedRef] ref int i);
                Diagnostic(ErrorCode.ERR_ExplicitScopedRef, "ScopedRef").WithLocation(2, 18),
                // (5,30): error CS9065: Do not use 'System.Runtime.CompilerServices.ScopedRefAttribute'. Use the 'scoped' keyword instead.
                //     public static void Main([ScopedRef] string[] args)
                Diagnostic(ErrorCode.ERR_ExplicitScopedRef, "ScopedRef").WithLocation(5, 30),
                // (7,17): error CS9065: Do not use 'System.Runtime.CompilerServices.ScopedRefAttribute'. Use the 'scoped' keyword instead.
                //         D d = ([ScopedRef] ref int i) => { };
                Diagnostic(ErrorCode.ERR_ExplicitScopedRef, "ScopedRef").WithLocation(7, 17)
                );
        }

        [WorkItem(62124, "https://github.com/dotnet/roslyn/issues/62124")]
        [Fact]
        public void ExplicitAttribute_ReferencedInSource_02()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
[module: ScopedRef]
[ScopedRef] class Program
{
    [ScopedRef] object F;
    [ScopedRef] event EventHandler E;
    [ScopedRef] object P { get; }
    [ScopedRef] static object M1() => throw null;
    [return: ScopedRef] static object M2() => throw null;
    static void M3<[ScopedRef] T>() { }
}
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class ScopedRefAttribute : Attribute
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,24): warning CS0169: The field 'Program.F' is never used
                //     [ScopedRef] object F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("Program.F").WithLocation(6, 24),
                // (7,36): warning CS0067: The event 'Program.E' is never used
                //     [ScopedRef] event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Program.E").WithLocation(7, 36));
        }

        [WorkItem(62124, "https://github.com/dotnet/roslyn/issues/62124")]
        [Fact]
        public void ExplicitAttribute_ReferencedInSource_03()
        {
            var source = @"
using System.Runtime.CompilerServices;
record struct R1([ScopedRef] ref int i);
record struct R2([ScopedRef] R i);
ref struct R { }
";
            var comp = CreateCompilation(new[] { ScopedRefAttributeDefinition, source });
            comp.VerifyDiagnostics(
                // (3,19): error CS9065: Do not use 'System.Runtime.CompilerServices.ScopedRefAttribute'. Use the 'scoped' keyword instead.
                // record struct R1([ScopedRef] ref int i);
                Diagnostic(ErrorCode.ERR_ExplicitScopedRef, "ScopedRef").WithLocation(3, 19),
                // (3,30): error CS0631: ref and out are not valid in this context
                // record struct R1([ScopedRef] ref int i);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(3, 30),
                // (4,19): error CS9065: Do not use 'System.Runtime.CompilerServices.ScopedRefAttribute'. Use the 'scoped' keyword instead.
                // record struct R2([ScopedRef] R i);
                Diagnostic(ErrorCode.ERR_ExplicitScopedRef, "ScopedRef").WithLocation(4, 19),
                // (4,30): error CS8345: Field or auto-implemented property cannot be of type 'R' unless it is an instance member of a ref struct.
                // record struct R2([ScopedRef] R i);
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "R").WithArguments("R").WithLocation(4, 30)
                );
        }

        [Fact]
        public void ExplicitAttribute_UnexpectedParameterTargets()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.ScopedRefAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public sealed R extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modreq(int32) F
}
.class public A
{
  .method public static void F1(valuetype R r)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 ) // ScopedRefAttribute()
    ret
  }
  .method public static void F2(int32 y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 ) // ScopedRefAttribute()
    ret
  }
  .method public static void F3(object x, int32& y)
  {
    .param [2]
    .custom instance void System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 ) // ScopedRefAttribute()
    ret
  }
  .method public static void F4(valuetype R& r)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.ScopedRefAttribute::.ctor() = ( 01 00 00 00 ) // ScopedRefAttribute()
    ret
  }
}
";
            var ref0 = CompileIL(source0);

            var source1 =
@"class Program
{
    static void Main()
    {
        var r = new R();
        A.F1(r);
        A.F2(2);
        object x = 1;
        int y = 2;
        A.F3(x, ref y);
        A.F4(ref r);
    }
}";
            var comp = CreateCompilation(source1, references: new[] { ref0 });
            comp.VerifyDiagnostics(
                // (7,11): error CS0570: 'A.F2(int)' is not supported by the language
                //         A.F2(2);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F2").WithArguments("A.F2(int)").WithLocation(7, 11));

            var method = comp.GetMember<MethodSymbol>("A.F1");
            Assert.Equal("void A.F1(scoped R r)", method.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeScoped)));
            var parameter = method.Parameters[0];
            Assert.Equal(DeclarationScope.ValueScoped, parameter.DeclaredScope);

            method = comp.GetMember<MethodSymbol>("A.F2");
            Assert.Equal("void A.F2(System.Int32 y)", method.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeScoped)));
            parameter = method.Parameters[0];
            Assert.Equal(DeclarationScope.Unscoped, parameter.DeclaredScope);

            method = comp.GetMember<MethodSymbol>("A.F3");
            Assert.Equal("void A.F3(System.Object x, scoped ref System.Int32 y)", method.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeScoped)));
            parameter = method.Parameters[1];
            Assert.Equal(DeclarationScope.RefScoped, parameter.DeclaredScope);

            method = comp.GetMember<MethodSymbol>("A.F4");
            Assert.Equal("void A.F4(ref R r)", method.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeScoped)));
            parameter = method.Parameters[0];
            Assert.Equal(DeclarationScope.RefScoped, parameter.DeclaredScope);
        }

        [Fact]
        public void ExplicitAttribute_UnexpectedAttributeConstructor()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.ScopedRefAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor(bool isRefScoped, bool isValueScoped) cil managed { ret }
}
.class public sealed R extends [mscorlib]System.ValueType
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)
  .field public int32& modreq(int32) F
}
.class public A
{
  .method public static void F1(valuetype R r)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.ScopedRefAttribute::.ctor(bool, bool) = ( 01 00 00 01 00 00 ) // ScopedRefAttribute(isRefScoped: false, isValueScoped: true)
    ret
  }
  .method public static void F2(object x, int32& y)
  {
    .param [2]
    .custom instance void System.Runtime.CompilerServices.ScopedRefAttribute::.ctor(bool, bool) = ( 01 00 01 00 00 00 ) // ScopedRefAttribute(isRefScoped: true, isValueScoped: false)
    ret
  }
}
";
            var ref0 = CompileIL(source0);

            var source1 =
@"class Program
{
    static void Main()
    {
        A.F1(new R());
        object x = 1;
        int y = 2;
        A.F2(x, ref y);
    }
}";
            var comp = CreateCompilation(source1, references: new[] { ref0 });
            // https://github.com/dotnet/roslyn/issues/61647: If the [ScopedRef] scoped value is an int
            // rather than a pair of bools, the compiler should reject attribute values that it does not recognize.
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void EmitAttribute_MethodParameters()
        {
            var source =
@"ref struct R { }
struct S
{
    public S(scoped ref int i) { }
    public static void F(scoped R r) { }
    public object this[scoped in int i] => null;
    public static S operator+(S a, scoped in R b) => a;
}";
            var comp = CreateCompilation(source);
            var expected =
@"S..ctor(ref System.Int32 i)
    [ScopedRef] ref System.Int32 i
void S.F(R r)
    [ScopedRef] R r
System.Object S.this[in System.Int32 i].get
    [ScopedRef] in System.Int32 i
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Equal("System.Runtime.CompilerServices.ScopedRefAttribute", GetScopedRefType(module).ToTestDisplayString());
                AssertScopedRefAttributes(module, expected);
            });
        }

        [Fact]
        public void EmitAttribute_OutParameters_01()
        {
            var source =
@"ref struct R { }
class Program
{
    public static void F(out int x, out R y)
    {
        x = default;
        y = default;
    }
}";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Null(GetScopedRefType(module));
                AssertScopedRefAttributes(module, "");
            });
        }

        [Fact]
        public void EmitAttribute_OutParameters_02()
        {
            var source =
@"ref struct R { }
class Program
{
    public static void F(scoped out int x, scoped out R y)
    {
        x = default;
        y = default;
    }
}";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Null(GetScopedRefType(module));
                AssertScopedRefAttributes(module, "");
            });
        }

        [Fact]
        public void EmitAttribute_RefToRefStructParameters()
        {
            var source =
@"ref struct R { }
class Program
{
    public static void F0(R r) { }
    public static void F1(ref R r) { }
    public static void F2(in R r) { }
    public static void F3(out R r) { r = default; }
    public static void F4(scoped ref R r) { }
    public static void F5(scoped in R r) { }
    public static void F6(scoped out R r) { r = default; }
}";
            var comp = CreateCompilation(source);
            var expected =
@"";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Null(GetScopedRefType(module));
                AssertScopedRefAttributes(module, expected);
            });

            // https://github.com/dotnet/roslyn/issues/62780: Test additional cases with [UnscopedRef].
        }

        [Fact]
        public void EmitAttribute_DelegateParameters()
        {
            var source =
@"ref struct R { }
delegate void D(scoped in int x, scoped R y);
";
            var comp = CreateCompilation(source);
            var expected =
@"void D.Invoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x, R y)
    [ScopedRef] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x
    [ScopedRef] R y
System.IAsyncResult D.BeginInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x, R y, System.AsyncCallback callback, System.Object @object)
    [ScopedRef] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x
    [ScopedRef] R y
    System.AsyncCallback callback
    System.Object @object
void D.EndInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x, System.IAsyncResult result)
    [ScopedRef] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x
    System.IAsyncResult result
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.ScopedRefAttribute", GetScopedRefType(module).ToTestDisplayString());
                    AssertScopedRefAttributes(module, expected);
                });
        }

        [Fact]
        public void EmitAttribute_LambdaParameters()
        {
            var source =
@"delegate void D(scoped in int i);
class Program
{
    static void Main()
    {
        D d = (scoped in int i) => { };
        d(0);
    }
}";
            var comp = CreateCompilation(source);
            var expected =
@"void D.Invoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i)
    [ScopedRef] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i
System.IAsyncResult D.BeginInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i, System.AsyncCallback callback, System.Object @object)
    [ScopedRef] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i
    System.AsyncCallback callback
    System.Object @object
void D.EndInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i, System.IAsyncResult result)
    [ScopedRef] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i
    System.IAsyncResult result
void Program.<>c.<Main>b__0_0(in System.Int32 i)
    [ScopedRef] in System.Int32 i
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.ScopedRefAttribute", GetScopedRefType(module).ToTestDisplayString());
                    AssertScopedRefAttributes(module, expected);
                });
        }

        [Fact]
        public void EmitAttribute_LocalFunctionParameters()
        {
            var source =
@"class Program
{
    static void M()
    {
        void L(scoped in int i) { }
        L(0);
    }
}";
            var comp = CreateCompilation(source);
            var expected =
@"void Program.<M>g__L|0_0(in System.Int32 i)
    [ScopedRef] in System.Int32 i
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.ScopedRefAttribute", GetScopedRefType(module).ToTestDisplayString());
                    AssertScopedRefAttributes(module, expected);
                });
        }

        [Fact]
        public void EmitAttribute_InferredDelegateParameters()
        {
            var source =
@"ref struct R { }
class Program
{
    static void Main()
    {
        var d1 = (scoped in int i) => { };
        d1(0);
        var d2 = (scoped R r) => new R();
        d2(new R());
    }
}";
            var comp = CreateCompilation(source);
            var expected =
@"void <>f__AnonymousDelegate0.Invoke(in System.Int32 value)
    [ScopedRef] in System.Int32 value
R <>f__AnonymousDelegate1.Invoke(R value)
    [ScopedRef] R value
void Program.<>c.<Main>b__0_0(in System.Int32 i)
    [ScopedRef] in System.Int32 i
R Program.<>c.<Main>b__0_1(R r)
    [ScopedRef] R r
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                verify: Verification.Skipped,
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.ScopedRefAttribute", GetScopedRefType(module).ToTestDisplayString());
                    AssertScopedRefAttributes(module, expected);
                });
        }

        private static void AssertScopedRefAttributes(ModuleSymbol module, string expected)
        {
            var actual = ScopedRefAttributesVisitor.GetString((PEModuleSymbol)module);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
        }

        private static NamedTypeSymbol GetScopedRefType(ModuleSymbol module)
        {
            return module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.ScopedRefAttribute");
        }
    }
}
