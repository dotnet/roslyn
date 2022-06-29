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
    public class AttributeTests_LifetimeAnnotation : CSharpTestBase
    {
        private const string LifetimeAnnotationAttributeDefinition =
@"namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class LifetimeAnnotationAttribute : Attribute
    {
        public LifetimeAnnotationAttribute(bool isRefScoped, bool isValueScoped)
        {
            IsRefScoped = isRefScoped;
            IsValueScoped = isValueScoped;
        }
        public bool IsRefScoped { get; }
        public bool IsValueScoped { get; }
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
            var comp = CreateCompilation(new[] { LifetimeAnnotationAttributeDefinition, source });
            var expected =
@" void Program.F(ref System.Int32 i)
    [LifetimeAnnotation(True, False)] ref System.Int32 i
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                AssertLifetimeAnnotationAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var comp = CreateCompilation(LifetimeAnnotationAttributeDefinition);
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
    [LifetimeAnnotation(True, False)] ref System.Int32 i
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Null(GetLifetimeAnnotationType(module));
                AssertLifetimeAnnotationAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_MissingConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class LifetimeAnnotationAttribute : Attribute
    {
        public LifetimeAnnotationAttribute() { }
        public bool IsRefScoped { get; }
        public bool IsValueScoped { get; }
    }
}";
            var source2 =
@"class Program
{
    public static void F(scoped ref int i) { }
}";
            var comp = CreateCompilation(new[] { source1, source2 });
            comp.VerifyEmitDiagnostics(
                // (3,26): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.LifetimeAnnotationAttribute..ctor'
                //     public static void F(scoped ref int i) { }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "scoped ref int i").WithArguments("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", ".ctor").WithLocation(3, 26));
        }

        [WorkItem(62124, "https://github.com/dotnet/roslyn/issues/62124")]
        [Fact]
        public void ExplicitAttribute_ReferencedInSource_01()
        {
            var source =
@"using System.Runtime.CompilerServices;
delegate void D([LifetimeAnnotation(true, false)] ref int i);
class Program
{
    public static void Main([LifetimeAnnotation(false, true)] string[] args)
    {
        D d = ([LifetimeAnnotation(true, false)] ref int i) => { };
    }
}";
            var comp = CreateCompilation(new[] { LifetimeAnnotationAttributeDefinition, source });
            // https://github.com/dotnet/roslyn/issues/62124: Re-enable check for LifetimeAnnotationAttribute in ReportExplicitUseOfReservedAttributes.
            comp.VerifyDiagnostics();
        }

        [WorkItem(62124, "https://github.com/dotnet/roslyn/issues/62124")]
        [Fact]
        public void ExplicitAttribute_ReferencedInSource_02()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
[module: LifetimeAnnotation(false, true)]
[LifetimeAnnotation(false, true)] class Program
{
    [LifetimeAnnotation(false, true)] object F;
    [LifetimeAnnotation(false, true)] event EventHandler E;
    [LifetimeAnnotation(false, true)] object P { get; }
    [LifetimeAnnotation(false, true)] static object M1() => throw null;
    [return: LifetimeAnnotation(false, true)] static object M2() => throw null;
    static void M3<[LifetimeAnnotation(false, true)] T>() { }
}";
            var comp = CreateCompilation(new[] { LifetimeAnnotationAttributeDefinition, source });
            // https://github.com/dotnet/roslyn/issues/62124: Re-enable check for LifetimeAnnotationAttribute in ReportExplicitUseOfReservedAttributes.
            comp.VerifyDiagnostics(
                // (6,46): warning CS0169: The field 'Program.F' is never used
                //     [LifetimeAnnotation(false, true)] object F;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("Program.F").WithLocation(6, 46),
                // (7,58): warning CS0067: The event 'Program.E' is never used
                //     [LifetimeAnnotation(false, true)] event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Program.E").WithLocation(7, 58));
        }

        [Fact]
        public void ExplicitAttribute_UnexpectedParameterTargets()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.LifetimeAnnotationAttribute extends [mscorlib]System.Attribute
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
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 01 00 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: true, isValueScoped: false)
    ret
  }
  .method public static void F2(int32 y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 00 01 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: false, isValueScoped: true)
    ret
  }
  .method public static void F3(object x, int32& y)
  {
    .param [2]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 00 01 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: false, isValueScoped: true)
    ret
  }
  .method public static void F4(valuetype R& r)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor(bool, bool) = ( 01 00 01 01 00 00 ) // LifetimeAnnotationAttribute(isRefScoped: true, isValueScoped: true)
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
                // (6,11): error CS0570: 'A.F1(R)' is not supported by the language
                //         A.F1(r);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("A.F1(R)").WithLocation(6, 11),
                // (7,11): error CS0570: 'A.F2(int)' is not supported by the language
                //         A.F2(2);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F2").WithArguments("A.F2(int)").WithLocation(7, 11),
                // (10,11): error CS0570: 'A.F3(object, ref int)' is not supported by the language
                //         A.F3(x, ref y);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F3").WithArguments("A.F3(object, ref int)").WithLocation(10, 11));

            var method = comp.GetMember<MethodSymbol>("A.F4");
            Assert.Equal("void A.F4(ref scoped R r)", method.ToDisplayString(SymbolDisplayFormat.TestFormat.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.IncludeScoped)));
            var parameter = method.Parameters[0];
            Assert.Equal(DeclarationScope.ValueScoped, parameter.Scope);
        }

        [Fact]
        public void ExplicitAttribute_UnexpectedAttributeConstructor()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.LifetimeAnnotationAttribute extends [mscorlib]System.Attribute
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
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor() = ( 01 00 00 00 ) // LifetimeAnnotationAttribute()
    ret
  }
  .method public static void F2(object x, int32& y)
  {
    .param [2]
    .custom instance void System.Runtime.CompilerServices.LifetimeAnnotationAttribute::.ctor() = ( 01 00 00 00 ) // LifetimeAnnotationAttribute()
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
            // https://github.com/dotnet/roslyn/issues/61647: If the [LifetimeAnnotation] scoped value is an int
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
    public static S operator+(S a, in scoped R b) => a;
}";
            var comp = CreateCompilation(source);
            var expected =
@"S..ctor(ref System.Int32 i)
    [LifetimeAnnotation(True, False)] ref System.Int32 i
void S.F(R r)
    [LifetimeAnnotation(False, True)] R r
S S.op_Addition(S a, in R b)
    S a
    [LifetimeAnnotation(False, True)] in R b
System.Object S.this[in System.Int32 i].get
    [LifetimeAnnotation(True, False)] in System.Int32 i
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                AssertLifetimeAnnotationAttributes(module, expected);
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
                Assert.Null(GetLifetimeAnnotationType(module));
                AssertLifetimeAnnotationAttributes(module, "");
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
                Assert.Null(GetLifetimeAnnotationType(module));
                AssertLifetimeAnnotationAttributes(module, "");
            });
        }

        [Fact]
        public void EmitAttribute_OutParameters_03()
        {
            var source =
@"ref struct R { }
class Program
{
    public static void F(out scoped R r) { r = default; }
}";
            var comp = CreateCompilation(source);
            var expected =
@" void Program.F(out R r)
    [LifetimeAnnotation(False, True)] out R r
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                AssertLifetimeAnnotationAttributes(module, expected);
            });
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
    [LifetimeAnnotation(True, False)] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x
    [LifetimeAnnotation(False, True)] R y
System.IAsyncResult D.BeginInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x, R y, System.AsyncCallback callback, System.Object @object)
    [LifetimeAnnotation(True, False)] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x
    [LifetimeAnnotation(False, True)] R y
    System.AsyncCallback callback
    System.Object @object
void D.EndInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x, System.IAsyncResult result)
    [LifetimeAnnotation(True, False)] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 x
    System.IAsyncResult result
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                    AssertLifetimeAnnotationAttributes(module, expected);
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
    [LifetimeAnnotation(True, False)] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i
System.IAsyncResult D.BeginInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i, System.AsyncCallback callback, System.Object @object)
    [LifetimeAnnotation(True, False)] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i
    System.AsyncCallback callback
    System.Object @object
void D.EndInvoke(in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i, System.IAsyncResult result)
    [LifetimeAnnotation(True, False)] in modreq(System.Runtime.InteropServices.InAttribute) System.Int32 i
    System.IAsyncResult result
void Program.<>c.<Main>b__0_0(in System.Int32 i)
    [LifetimeAnnotation(True, False)] in System.Int32 i

";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                    AssertLifetimeAnnotationAttributes(module, expected);
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
    [LifetimeAnnotation(True, False)] in System.Int32 i
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                    AssertLifetimeAnnotationAttributes(module, expected);
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
    [LifetimeAnnotation(True, False)] in System.Int32 value
R <>f__AnonymousDelegate1.Invoke(R value)
    [LifetimeAnnotation(False, True)] R value
void Program.<>c.<Main>b__0_0(in System.Int32 i)
    [LifetimeAnnotation(True, False)] in System.Int32 i
R Program.<>c.<Main>b__0_1(R r)
    [LifetimeAnnotation(False, True)] R r
";
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                verify: Verification.Skipped,
                symbolValidator: module =>
                {
                    Assert.Equal("System.Runtime.CompilerServices.LifetimeAnnotationAttribute", GetLifetimeAnnotationType(module).ToTestDisplayString());
                    AssertLifetimeAnnotationAttributes(module, expected);
                });
        }

        private static void AssertLifetimeAnnotationAttributes(ModuleSymbol module, string expected)
        {
            var actual = LifetimeAnnotationAttributesVisitor.GetString((PEModuleSymbol)module);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
        }

        private static NamedTypeSymbol GetLifetimeAnnotationType(ModuleSymbol module)
        {
            return module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.LifetimeAnnotationAttribute");
        }
    }
}
