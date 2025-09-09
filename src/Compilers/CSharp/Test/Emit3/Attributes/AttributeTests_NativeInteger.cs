// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_NativeInteger : CSharpTestBase
    {
        private static readonly SymbolDisplayFormat FormatWithSpecialTypes = SymbolDisplayFormat.TestFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)
            .RemoveCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType);

        [Fact]
        public void EmptyProject()
        {
            var source = @"";
            var comp = CreateCompilation(source);
            var expected =
@"";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void ExplicitAttribute_FromSource()
        {
            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(new[] { NativeIntegerAttributeDefinition, source });
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger] System.UIntPtr[] F2
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NativeIntegerAttribute");
                Assert.NotNull(attributeType);
                AssertNativeIntegerAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_FromMetadata()
        {
            var comp = CreateCompilation(NativeIntegerAttributeDefinition);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            comp = CreateCompilation(source, references: new[] { ref0 }, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger] System.UIntPtr[] F2
";
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NativeIntegerAttribute");
                Assert.Null(attributeType);
                AssertNativeIntegerAttributes(module, expected);
            });
        }

        [Fact]
        public void ExplicitAttribute_MissingEmptyConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NativeIntegerAttribute : Attribute
    {
        public NativeIntegerAttribute(bool[] flags) { }
    }
}";
            var source2 =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nint F1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F1").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(3, 17),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nuint[] F2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(4, 20));
        }

        [Fact]
        public void ExplicitAttribute_MissingConstructor()
        {
            var source1 =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NativeIntegerAttribute : Attribute
    {
    }
}";
            var source2 =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(new[] { source1, source2 }, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (3,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nint F1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F1").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(3, 17),
                // (4,20): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //     public nuint[] F2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "F2").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(4, 20));
        }

        [Fact]
        public void ExplicitAttribute_ReferencedInSource()
        {
            var sourceAttribute =
@"namespace System.Runtime.CompilerServices
{
    internal class NativeIntegerAttribute : Attribute
    {
        internal NativeIntegerAttribute() { }
        internal NativeIntegerAttribute(bool[] flags) { }
    }
}";
            var source =
@"#pragma warning disable 67
#pragma warning disable 169
using System;
using System.Runtime.CompilerServices;
[NativeInteger] class Program
{
    [NativeInteger] IntPtr F;
    [NativeInteger] event EventHandler E;
    [NativeInteger] object P { get; }
    [NativeInteger(new[] { false, true })] static UIntPtr[] M1() => throw null;
    [return: NativeInteger(new[] { false, true })] static UIntPtr[] M2() => throw null;
    static void M3([NativeInteger]object arg) { }
}";

            var comp = CreateCompilation(new[] { sourceAttribute, source }, parseOptions: TestOptions.Regular8);
            verifyDiagnostics(comp);

            comp = CreateCompilation(new[] { sourceAttribute, source }, parseOptions: TestOptions.Regular9);
            verifyDiagnostics(comp);

            static void verifyDiagnostics(CSharpCompilation comp)
            {
                comp.VerifyDiagnostics(
                    // (5,2): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    // [NativeInteger] class Program
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(5, 2),
                    // (7,6): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [NativeInteger] IntPtr F;
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(7, 6),
                    // (8,6): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [NativeInteger] event EventHandler E;
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(8, 6),
                    // (9,6): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [NativeInteger] object P { get; }
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(9, 6),
                    // (11,14): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     [return: NativeInteger(new[] { false, true })] static UIntPtr[] M2() => throw null;
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger(new[] { false, true })").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(11, 14),
                    // (12,21): error CS8335: Do not use 'System.Runtime.CompilerServices.NativeIntegerAttribute'. This is reserved for compiler usage.
                    //     static void M3([NativeInteger]object arg) { }
                    Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NativeInteger").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(12, 21));
            }
        }

        [Fact]
        public void MissingAttributeUsageAttribute()
        {
            var source =
@"public class Program
{
    public nint F1;
    public nuint[] F2;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.MakeTypeMissing(WellKnownType.System_AttributeUsageAttribute);
            comp.VerifyEmitDiagnostics(
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute..ctor'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", ".ctor").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.AllowMultiple'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "AllowMultiple").WithLocation(1, 1),
                // error CS0656: Missing compiler required member 'System.AttributeUsageAttribute.Inherited'
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember).WithArguments("System.AttributeUsageAttribute", "Inherited").WithLocation(1, 1));
        }

        [Fact]
        public void Metadata_ZeroElements()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.NativeIntegerAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig specialname rtspecialname instance void .ctor(bool[] b) cil managed { ret }
}
.class public A<T, U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public B
{
  .method public static void F0(native int x, native uint y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) // new bool[0]
    .param [2]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) // new bool[0]
    ret
  }
  .method public static void F1(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) // new bool[0]
    ret
  }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"class Program
{
    static void F()
    {
        B.F0(default, default);
        B.F1(new A<System.IntPtr, System.UIntPtr>());
    }
}";

            var comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,11): error CS0570: 'B.F0(?, ?)' is not supported by the language
                //         B.F0(default, default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F0").WithArguments("B.F0(?, ?)").WithLocation(5, 11),
                // (6,11): error CS0570: 'B.F1(?)' is not supported by the language
                //         B.F1(new A<System.IntPtr, System.UIntPtr>());
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("B.F1(?)").WithLocation(6, 11));
            verify(comp);

            comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,11): error CS0570: 'B.F0(?, ?)' is not supported by the language
                //         B.F0(default, default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F0").WithArguments("B.F0(?, ?)").WithLocation(5, 11),
                // (6,11): error CS0570: 'B.F1(?)' is not supported by the language
                //         B.F1(new A<System.IntPtr, System.UIntPtr>());
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("B.F1(?)").WithLocation(6, 11));
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var type = comp.GetTypeByMetadataName("B");
                Assert.Equal("void B.F0( x,  y)", type.GetMember("F0").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F1( a)", type.GetMember("F1").ToDisplayString(FormatWithSpecialTypes));

                var expected =
    @"B
    void F0(? x, ? y)
        [NativeInteger({  })] ? x
        [NativeInteger({  })] ? y
    void F1(? a)
        [NativeInteger({  })] ? a
";
                AssertNativeIntegerAttributes(type.ContainingModule, expected);
            }
        }

        [Fact]
        public void Metadata_OneElementFalse()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.NativeIntegerAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig specialname rtspecialname instance void .ctor(bool[] b) cil managed { ret }
}
.class public A<T, U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public B
{
  .method public static void F0(native int x, native uint y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false }
    .param [2]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false }
    ret
  }
  .method public static void F1(class A<int32, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false }
    ret
  }
  .method public static void F2(class A<native int, uint32> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false }
    ret
  }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"class Program
{
    static void F()
    {
        B.F0(default, default);
        B.F1(new A<int, System.UIntPtr>());
        B.F2(new A<System.IntPtr, uint>());
    }
}";

            var comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var type = comp.GetTypeByMetadataName("B");
                Assert.Equal("void B.F0(System.IntPtr x, System.UIntPtr y)", type.GetMember("F0").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F1(A<int, System.UIntPtr> a)", type.GetMember("F1").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F2(A<System.IntPtr, uint> a)", type.GetMember("F2").ToDisplayString(FormatWithSpecialTypes));

                var expected =
    @"B
    void F0(System.IntPtr x, System.UIntPtr y)
        [NativeInteger({ False })] System.IntPtr x
        [NativeInteger({ False })] System.UIntPtr y
    void F1(A<System.Int32, System.UIntPtr> a)
        [NativeInteger({ False })] A<System.Int32, System.UIntPtr> a
    void F2(A<System.IntPtr, System.UInt32> a)
        [NativeInteger({ False })] A<System.IntPtr, System.UInt32> a
";
                AssertNativeIntegerAttributes(type.ContainingModule, expected);
            }
        }

        [Fact]
        public void Metadata_OneElementTrue()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.NativeIntegerAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig specialname rtspecialname instance void .ctor(bool[] b) cil managed { ret }
}
.class public A<T, U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public B
{
  .method public static void F0(native int x, native uint y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 01 00 00 ) // new[] { true }
    .param [2]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 01 00 00 ) // new[] { true }
    ret
  }
  .method public static void F1(class A<int32, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 01 00 00 ) // new[] { true }
    ret
  }
  .method public static void F2(class A<native int, uint32> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 01 00 00 ) // new[] { true }
    ret
  }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"class Program
{
    static void F()
    {
        B.F0(default, default);
        B.F1(new A<int, nuint>());
        B.F2(new A<nint, uint>());
    }
}";

            var comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (6,25): error CS8400: Feature 'native-sized integers' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         B.F1(new A<int, nuint>());
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "nuint").WithArguments("native-sized integers", "9.0").WithLocation(6, 25),
                // (7,20): error CS8400: Feature 'native-sized integers' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         B.F2(new A<nint, uint>());
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "nint").WithArguments("native-sized integers", "9.0").WithLocation(7, 20));
            verify(comp);

            comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var type = comp.GetTypeByMetadataName("B");
                Assert.Equal("void B.F0(nint x, nuint y)", type.GetMember("F0").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F1(A<int, nuint> a)", type.GetMember("F1").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F2(A<nint, uint> a)", type.GetMember("F2").ToDisplayString(FormatWithSpecialTypes));

                var expected =
    @"B
    void F0(System.IntPtr x, System.UIntPtr y)
        [NativeInteger({ True })] System.IntPtr x
        [NativeInteger({ True })] System.UIntPtr y
    void F1(A<System.Int32, System.UIntPtr> a)
        [NativeInteger({ True })] A<System.Int32, System.UIntPtr> a
    void F2(A<System.IntPtr, System.UInt32> a)
        [NativeInteger({ True })] A<System.IntPtr, System.UInt32> a
";
                AssertNativeIntegerAttributes(type.ContainingModule, expected);
            }
        }

        [Fact]
        public void Metadata_AllFalse()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.NativeIntegerAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig specialname rtspecialname instance void .ctor(bool[] b) cil managed { ret }
}
.class public A<T, U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}
.class public B
{
  .method public static void F0(native int x, native uint y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false } 
    .param [2]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false } 
    ret
  }
  .method public static void F1(class A<int32, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 00 00 00 ) // new[] { false } 
    ret
  }
  .method public static void F2(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 00 00 00 ) // new[] { false, false }
    ret
  }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"class Program
{
    static void F()
    {
        B.F0(default, default);
        B.F1(new A<int, System.UIntPtr>());
        B.F2(new A<System.IntPtr, System.UIntPtr>());
    }
}";

            var comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var type = comp.GetTypeByMetadataName("B");
                Assert.Equal("void B.F0(System.IntPtr x, System.UIntPtr y)", type.GetMember("F0").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F1(A<int, System.UIntPtr> a)", type.GetMember("F1").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F2(A<System.IntPtr, System.UIntPtr> a)", type.GetMember("F2").ToDisplayString(FormatWithSpecialTypes));

                var expected =
    @"B
    void F0(System.IntPtr x, System.UIntPtr y)
        [NativeInteger({ False })] System.IntPtr x
        [NativeInteger({ False })] System.UIntPtr y
    void F1(A<System.Int32, System.UIntPtr> a)
        [NativeInteger({ False })] A<System.Int32, System.UIntPtr> a
    void F2(A<System.IntPtr, System.UIntPtr> a)
        [NativeInteger({ False, False })] A<System.IntPtr, System.UIntPtr> a
";
                AssertNativeIntegerAttributes(type.ContainingModule, expected);
            }
        }

        [Fact]
        public void Metadata_TooFewAndTooManyTransformFlags()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.NativeIntegerAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig specialname rtspecialname instance void .ctor(bool[] b) cil managed { ret }
}
.class public A<T, U>
{
}
.class public B
{
  .method public static void F(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor() = ( 01 00 00 00 ) // no array, too few
    ret
  }
  .method public static void F0(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 00 00 00 00 00 00 ) // new bool[0], too few
    ret
  }
  .method public static void F1(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 01 00 00 00 01 00 00 ) // new[] { true }, too few
    ret
  }
  .method public static void F2(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) // new[] { false, true }, valid
    ret
  }
  .method public static void F3(class A<native int, native uint> a)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 03 00 00 00 00 01 01 00 00 ) // new[] { false, true, true }, too many
    ret
  }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"class Program
{
    static void F(A<nint, nuint> a)
    {
        B.F(a);
        B.F0(a);
        B.F1(a);
        B.F2(a);
        B.F3(a);
    }
}";

            var comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,21): error CS8400: Feature 'native-sized integers' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     static void F(A<nint, nuint> a)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "nint").WithArguments("native-sized integers", "9.0").WithLocation(3, 21),
                // (3,27): error CS8400: Feature 'native-sized integers' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     static void F(A<nint, nuint> a)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "nuint").WithArguments("native-sized integers", "9.0").WithLocation(3, 27),
                // (5,11): error CS0570: 'B.F(?)' is not supported by the language
                //         B.F(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F").WithArguments("B.F(?)").WithLocation(5, 11),
                // (6,11): error CS0570: 'B.F0(?)' is not supported by the language
                //         B.F0(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F0").WithArguments("B.F0(?)").WithLocation(6, 11),
                // (7,11): error CS0570: 'B.F1(?)' is not supported by the language
                //         B.F1(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("B.F1(?)").WithLocation(7, 11),
                // (9,11): error CS0570: 'B.F3(?)' is not supported by the language
                //         B.F3(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F3").WithArguments("B.F3(?)").WithLocation(9, 11));
            verify(comp);

            comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,11): error CS0570: 'B.F(?)' is not supported by the language
                //         B.F(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F").WithArguments("B.F(?)").WithLocation(5, 11),
                // (6,11): error CS0570: 'B.F0(?)' is not supported by the language
                //         B.F0(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F0").WithArguments("B.F0(?)").WithLocation(6, 11),
                // (7,11): error CS0570: 'B.F1(?)' is not supported by the language
                //         B.F1(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("B.F1(?)").WithLocation(7, 11),
                // (9,11): error CS0570: 'B.F3(?)' is not supported by the language
                //         B.F3(a);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F3").WithArguments("B.F3(?)").WithLocation(9, 11));
            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var type = comp.GetTypeByMetadataName("B");
                Assert.Equal("void B.F( a)", type.GetMember("F").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F0( a)", type.GetMember("F0").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F1( a)", type.GetMember("F1").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F2(A<System.IntPtr, nuint> a)", type.GetMember("F2").ToDisplayString(FormatWithSpecialTypes));
                Assert.Equal("void B.F3( a)", type.GetMember("F3").ToDisplayString(FormatWithSpecialTypes));

                var expected =
    @"B
    void F(? a)
        [NativeInteger] ? a
    void F0(? a)
        [NativeInteger({  })] ? a
    void F1(? a)
        [NativeInteger({ True })] ? a
    void F2(A<System.IntPtr, System.UIntPtr> a)
        [NativeInteger({ False, True })] A<System.IntPtr, System.UIntPtr> a
    void F3(? a)
        [NativeInteger({ False, True, True })] ? a
";
                AssertNativeIntegerAttributes(type.ContainingModule, expected);
            }
        }

        [Fact]
        public void Metadata_UnexpectedTarget()
        {
            var source0 =
@".class private System.Runtime.CompilerServices.NativeIntegerAttribute extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
  .method public hidebysig specialname rtspecialname instance void .ctor(bool[] b) cil managed { ret }
}
.class A<T>
{
}
.class public B
{
  .method public static void F1(int32 w)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor() = ( 01 00 00 00 ) 
    ret
  }
  .method public static void F2(object[] x)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) // new[] { false, true }
    ret
  }
  .method public static void F3(class A<class B> y)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) // new[] { false, true } 
    ret
  }
  .method public static void F4(native int[] z)
  {
    .param [1]
    .custom instance void System.Runtime.CompilerServices.NativeIntegerAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 01 01 00 00 ) // new[] { true, true }
    ret
  }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"class Program
{
    static void F()
    {
        B.F1(default);
        B.F2(default);
        B.F3(default);
        B.F4(default);
    }
}";

            var comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (5,11): error CS0570: 'B.F1(?)' is not supported by the language
                //         B.F1(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("B.F1(?)").WithLocation(5, 11),
                // (6,11): error CS0570: 'B.F2(?)' is not supported by the language
                //         B.F2(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F2").WithArguments("B.F2(?)").WithLocation(6, 11),
                // (7,11): error CS0570: 'B.F3(?)' is not supported by the language
                //         B.F3(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F3").WithArguments("B.F3(?)").WithLocation(7, 11),
                // (8,11): error CS0570: 'B.F4(?)' is not supported by the language
                //         B.F4(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F4").WithArguments("B.F4(?)").WithLocation(8, 11)
            );

            comp = CreateCompilation(source1, new[] { ref0 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,11): error CS0570: 'B.F1(?)' is not supported by the language
                //         B.F1(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F1").WithArguments("B.F1(?)").WithLocation(5, 11),
                // (6,11): error CS0570: 'B.F2(?)' is not supported by the language
                //         B.F2(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F2").WithArguments("B.F2(?)").WithLocation(6, 11),
                // (7,11): error CS0570: 'B.F3(?)' is not supported by the language
                //         B.F3(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F3").WithArguments("B.F3(?)").WithLocation(7, 11),
                // (8,11): error CS0570: 'B.F4(?)' is not supported by the language
                //         B.F4(default);
                Diagnostic(ErrorCode.ERR_BindToBogus, "F4").WithArguments("B.F4(?)").WithLocation(8, 11)
            );

            var type = comp.GetTypeByMetadataName("B");
            Assert.Equal("void B.F1( w)", type.GetMember("F1").ToDisplayString(FormatWithSpecialTypes));
            Assert.Equal("void B.F2( x)", type.GetMember("F2").ToDisplayString(FormatWithSpecialTypes));
            Assert.Equal("void B.F3( y)", type.GetMember("F3").ToDisplayString(FormatWithSpecialTypes));
            Assert.Equal("void B.F4( z)", type.GetMember("F4").ToDisplayString(FormatWithSpecialTypes));

            var expected =
@"
B
    void F1(? w)
        [NativeInteger] ? w
    void F2(? x)
        [NativeInteger({ False, True })] ? x
    void F3(? y)
        [NativeInteger({ False, True })] ? y
    void F4(? z)
        [NativeInteger({ True, True })] ? z
";

            AssertNativeIntegerAttributes(type.ContainingModule, expected);
        }

        [Fact]
        public void EmitAttribute_BaseClass()
        {
            var source =
@"public class A<T, U>
{
}
public class B : A<nint, nuint[]>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"[NativeInteger({ True, True })] B
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Interface()
        {
            var source =
@"public interface I<T>
{
}
public class A : I<(nint, nuint[])>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "A");
                var interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().Single());
                AssertAttributes(reader, interfaceImpl.GetCustomAttributes(), "MethodDefinition:Void System.Runtime.CompilerServices.NativeIntegerAttribute..ctor(Boolean[])");
            });
        }

        [Fact]
        public void EmitAttribute_AllTypes()
        {
            var source =
@"public enum E { }
public class C<T>
{
    public delegate void D<T>();
    public enum F { }
    public struct S<U> { }
    public interface I<U> { }
    public C<T>.S<nint> F1;
    public C<nuint>.I<T> F2;
    public C<E>.D<nint> F3;
    public C<nuint>.D<dynamic> F4;
    public C<C<nuint>.D<System.IntPtr>>.F F5;
    public C<C<System.UIntPtr>.F>.D<nint> F6;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"C<T>
    [NativeInteger] C<T>.S<System.IntPtr> F1
    [NativeInteger] C<System.UIntPtr>.I<T> F2
    [NativeInteger] C<E>.D<System.IntPtr> F3
    [NativeInteger] C<System.UIntPtr>.D<dynamic> F4
    [NativeInteger({ True, False })] C<C<System.UIntPtr>.D<System.IntPtr>>.F F5
    [NativeInteger({ False, True })] C<C<System.UIntPtr>.F>.D<System.IntPtr> F6
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_ErrorType()
        {
            var source1 =
@"public class A { }
public class B<T> { }";
            var comp = CreateCompilation(source1, assemblyName: "95d36b13-f2e1-495d-9ab6-62e8cc63ac22");
            var ref1 = comp.EmitToImageReference();

            var source2 =
@"public class C<T, U> { }
public class D
{
    public B<nint> F1;
    public C<nint, A> F2;
}";
            comp = CreateCompilation(source2, references: new[] { ref1 }, parseOptions: TestOptions.Regular9);
            var ref2 = comp.EmitToImageReference();

            var source3 =
@"class Program
{
    static void Main()
    {
        var d = new D();
        _ = d.F1;
        _ = d.F2;
    }
}";
            comp = CreateCompilation(source3, references: new[] { ref2 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,15): error CS0012: The type 'B<>' is defined in an assembly that is not referenced. You must add a reference to assembly '95d36b13-f2e1-495d-9ab6-62e8cc63ac22, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         _ = d.F1;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "F1").WithArguments("B<>", "95d36b13-f2e1-495d-9ab6-62e8cc63ac22, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(6, 15),
                // (7,15): error CS0012: The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly '95d36b13-f2e1-495d-9ab6-62e8cc63ac22, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                //         _ = d.F2;
                Diagnostic(ErrorCode.ERR_NoTypeDef, "F2").WithArguments("A", "95d36b13-f2e1-495d-9ab6-62e8cc63ac22, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(7, 15));
        }

        [Fact]
        public void EmitAttribute_Fields()
        {
            var source =
@"public class Program
{
    public nint F1;
    public (System.IntPtr, nuint[]) F2;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger({ False, True })] (System.IntPtr, System.UIntPtr[]) F2
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_MethodReturnType()
        {
            var source =
@"public class Program
{
    public (System.IntPtr, nuint[]) F() => default;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    [NativeInteger({ False, True })] (System.IntPtr, System.UIntPtr[]) F()
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_MethodParameters()
        {
            var source =
@"public class Program
{
    public void F(nint x, nuint y) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    void F(System.IntPtr x, System.UIntPtr y)
        [NativeInteger] System.IntPtr x
        [NativeInteger] System.UIntPtr y
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_PropertyType()
        {
            var source =
@"public class Program
{
    public (System.IntPtr, nuint[]) P => default;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    [NativeInteger({ False, True })] (System.IntPtr, System.UIntPtr[]) P { get; }
        [NativeInteger({ False, True })] (System.IntPtr, System.UIntPtr[]) P.get
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_PropertyParameters()
        {
            var source =
@"public class Program
{
    public object this[nint x, (nuint[], System.IntPtr) y] => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    System.Object this[System.IntPtr x, (System.UIntPtr[], System.IntPtr) y] { get; }
        [NativeInteger] System.IntPtr x
        [NativeInteger({ True, False })] (System.UIntPtr[], System.IntPtr) y
        System.Object this[System.IntPtr x, (System.UIntPtr[], System.IntPtr) y].get
            [NativeInteger] System.IntPtr x
            [NativeInteger({ True, False })] (System.UIntPtr[], System.IntPtr) y
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_EventType()
        {
            var source =
@"using System;
public class Program
{
    public event EventHandler<nuint[]> E;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    [NativeInteger] event System.EventHandler<System.UIntPtr[]> E
        void E.add
            [NativeInteger] System.EventHandler<System.UIntPtr[]> value
        void E.remove
            [NativeInteger] System.EventHandler<System.UIntPtr[]> value
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_OperatorReturnType()
        {
            var source =
@"public class C
{
    public static nint operator+(C a, C b) => 0;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"C
    [NativeInteger] System.IntPtr operator +(C a, C b)
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_OperatorParameters()
        {
            var source =
@"public class C
{
    public static C operator+(C a, nuint[] b) => a;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"C
    C operator +(C a, System.UIntPtr[] b)
        [NativeInteger] System.UIntPtr[] b
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_DelegateReturnType()
        {
            var source =
@"public delegate nint D();";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"D
    [NativeInteger] System.IntPtr Invoke()
    [NativeInteger] System.IntPtr EndInvoke(System.IAsyncResult result)
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_DelegateParameters()
        {
            var source =
@"public delegate void D(nint x, nuint[] y);";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var expected =
@"D
    void Invoke(System.IntPtr x, System.UIntPtr[] y)
        [NativeInteger] System.IntPtr x
        [NativeInteger] System.UIntPtr[] y
    System.IAsyncResult BeginInvoke(System.IntPtr x, System.UIntPtr[] y, System.AsyncCallback callback, System.Object @object)
        [NativeInteger] System.IntPtr x
        [NativeInteger] System.UIntPtr[] y
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Constraint()
        {
            var source =
@"public class A<T>
{
}
public class B<T> where T : A<nint>
{
}
public class C<T> where T : A<nuint[]>
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            var type = comp.GetMember<NamedTypeSymbol>("B");
            Assert.Equal("A<nint>", getConstraintType(type).ToDisplayString(FormatWithSpecialTypes));
            type = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal("A<nuint[]>", getConstraintType(type).ToDisplayString(FormatWithSpecialTypes));

            static TypeWithAnnotations getConstraintType(NamedTypeSymbol type) => type.TypeParameters[0].ConstraintTypesNoUseSiteDiagnostics[0];
        }

        [Fact]
        public void EmitAttribute_LambdaReturnType()
        {
            var source =
@"using System;
class Program
{
    static object M()
    {
        Func<nint> f = () => (nint)2;
        return f();
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular9,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("Program+<>c").GetMethod("<M>b__0_0");
                    AssertNativeIntegerAttribute(method.GetReturnTypeAttributes());
                });
        }

        [Fact]
        public void EmitAttribute_LambdaParameters()
        {
            var source =
@"using System;
class Program
{
    static void M()
    {
        Action<nuint[]> a = (nuint[] n) => { };
        a(null);
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular9,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("Program+<>c").GetMethod("<M>b__0_0");
                    AssertNativeIntegerAttribute(method.Parameters[0].GetAttributes());
                });
        }

        [Fact]
        public void EmitAttribute_LocalFunctionReturnType()
        {
            var source =
@"class Program
{
    static object M()
    {
        nint L() => (nint)2;
        return L();
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular9,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("Program").GetMethod("<M>g__L|0_0");
                    AssertNativeIntegerAttribute(method.GetReturnTypeAttributes());
                    AssertAttributes(method.GetAttributes(), "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
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
        void L(nuint[] n) { }
        L(null);
    }
}";
            CompileAndVerify(
                source,
                parseOptions: TestOptions.Regular9,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                symbolValidator: module =>
                {
                    var method = module.ContainingAssembly.GetTypeByMetadataName("Program").GetMethod("<M>g__L|0_0");
                    AssertNativeIntegerAttribute(method.Parameters[0].GetAttributes());
                });
        }

        [Fact]
        public void EmitAttribute_Lambda_NetModule()
        {
            var source =
@"class Program
{
    static void Main()
    {
        var a1 = (nint n) => { };
        a1(1);
        var a2 = nuint[] () => null;
        a2();
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseModule);
            comp.VerifyDiagnostics(
                // (5,19): error CS0518: Predefined type 'System.Runtime.CompilerServices.NativeIntegerAttribute' is not defined or imported
                //         var a1 = (nint n) => { };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nint n").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(5, 19),
                // (7,29): error CS0518: Predefined type 'System.Runtime.CompilerServices.NativeIntegerAttribute' is not defined or imported
                //         var a2 = nuint[] () => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "=>").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(7, 29));
        }

        [Fact]
        public void EmitAttribute_LocalFunction_NetModule()
        {
            var source =
@"class Program
{
    static void Main()
    {
        void L1(nint n) { };
        L1(1);
        nuint[] L2() => null;
        L2();
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseModule);
            comp.VerifyDiagnostics(
                // (5,17): error CS0518: Predefined type 'System.Runtime.CompilerServices.NativeIntegerAttribute' is not defined or imported
                //         void L1(nint n) { };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nint n").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(5, 17),
                // (7,9): error CS0518: Predefined type 'System.Runtime.CompilerServices.NativeIntegerAttribute' is not defined or imported
                //         nuint[] L2() => null;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "nuint[]").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute").WithLocation(7, 9));
        }

        [Fact]
        public void EmitAttribute_Lambda_MissingAttributeConstructor()
        {
            var sourceA =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NativeIntegerAttribute : Attribute
    {
        private NativeIntegerAttribute() { }
    }
}";
            var sourceB =
@"class Program
{
    static void Main()
    {
        var a1 = (nint n) => { };
        a1(1);
        var a2 = nuint[] () => null;
        a2();
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (5,19): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //         var a1 = (nint n) => { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "nint n").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(5, 19),
                // (7,29): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //         var a2 = nuint[] () => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "=>").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(7, 29));
        }

        [Fact]
        public void EmitAttribute_LocalFunction_MissingAttributeConstructor()
        {
            var sourceA =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NativeIntegerAttribute : Attribute
    {
        private NativeIntegerAttribute() { }
    }
}";
            var sourceB =
@"class Program
{
    static void Main()
    {
        void L1(nint n) { };
        L1(1);
        nuint[] L2() => null;
        L2();
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (5,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //         void L1(nint n) { };
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "nint n").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(5, 17),
                // (7,9): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NativeIntegerAttribute..ctor'
                //         nuint[] L2() => null;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "nuint[]").WithArguments("System.Runtime.CompilerServices.NativeIntegerAttribute", ".ctor").WithLocation(7, 9));
        }

        [Fact]
        public void EmitAttribute_LocalFunctionConstraints()
        {
            var source =
@"interface I<T>
{
}
class Program
{
    static void M()
    {
        void L<T>() where T : I<nint> { }
        L<I<nint>>();
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly.GetTypeByMetadataName("System.Runtime.CompilerServices.NativeIntegerAttribute"));
            });
        }

        [Fact]
        public void EmitAttribute_InferredDelegate()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        var f = (in nint i) => { };
        f(1);
    }
}";
            var comp = CreateCompilation(source);
            var expected =
@"Program
    Program.<>c
        [NativeInteger] <>A{00000003}<System.IntPtr> <>9__0_0
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_Nested()
        {
            var source =
@"public class A<T>
{
    public class B<U> { }
}
unsafe public class Program
{
    public nint F1;
    public nuint[] F2;
    public nint* F3;
    public A<nint>.B<nuint> F4;
    public (nint, nuint) F5;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);
            var expected =
@"Program
    [NativeInteger] System.IntPtr F1
    [NativeInteger] System.UIntPtr[] F2
    [NativeInteger] System.IntPtr* F3
    [NativeInteger({ True, True })] A<System.IntPtr>.B<System.UIntPtr> F4
    [NativeInteger({ True, True })] (System.IntPtr, System.UIntPtr) F5
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_LongTuples_01()
        {
            var source =
@"public class A<T>
{
}
unsafe public class B
{
    public A<(object, (nint, nuint, nint[], nuint, nint, nuint*[], nint, System.UIntPtr))> F1;
    public A<(nint, object, nuint[], object, nint, object, (System.IntPtr, nuint), object, nuint)> F2;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.UnsafeReleaseDll);
            var expected =
@"B
    [NativeInteger({ True, True, True, True, True, True, True, False })] A<(System.Object, (System.IntPtr, System.UIntPtr, System.IntPtr[], System.UIntPtr, System.IntPtr, System.UIntPtr*[], System.IntPtr, System.UIntPtr))> F1
    [NativeInteger({ True, True, True, False, True, True })] A<(System.IntPtr, System.Object, System.UIntPtr[], System.Object, System.IntPtr, System.Object, (System.IntPtr, System.UIntPtr), System.Object, System.UIntPtr)> F2
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void EmitAttribute_LongTuples_02()
        {
            var source1 =
@"public interface IA { }
public interface IB<T> { }
public class C : IA, IB<(nint, object, nuint[], object, nint, object, (System.IntPtr, nuint), object, nuint)>
{
}";
            var comp = CreateCompilation(source1, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, validator: assembly =>
            {
                var reader = assembly.GetMetadataReader();
                var typeDef = GetTypeDefinitionByName(reader, "C");
                var interfaceImpl = reader.GetInterfaceImplementation(typeDef.GetInterfaceImplementations().ElementAt(1));
                var customAttributes = interfaceImpl.GetCustomAttributes();
                AssertAttributes(reader, customAttributes, "MethodDefinition:Void System.Runtime.CompilerServices.NativeIntegerAttribute..ctor(Boolean[])");
                var customAttribute = GetAttributeByConstructorName(reader, customAttributes, "MethodDefinition:Void System.Runtime.CompilerServices.NativeIntegerAttribute..ctor(Boolean[])");
                AssertEx.Equal(ImmutableArray.Create(true, true, true, false, true, true), reader.ReadBoolArray(customAttribute.Value));
            });
            var ref1 = comp.EmitToImageReference();

            var source2 =
@"class Program
{
    static void Main()
    {
        IA a = new C();
        _ = a;
    }
}";
            comp = CreateCompilation(source2, references: new[] { ref1 }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(45519, "https://github.com/dotnet/roslyn/issues/45519")]
        public void EmitAttribute_PartialMethods()
        {
            var source =
@"public partial class Program
{
    static partial void F1(System.IntPtr x);
    static partial void F2(System.UIntPtr x) { }
    static partial void F1(nint x) { }
    static partial void F2(nuint x);
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular9);
            var expected =
@"Program
    void F2(System.UIntPtr x)
        [NativeInteger] System.UIntPtr x
";
            AssertNativeIntegerAttributes(comp, expected);

            comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(6), parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (4,25): warning CS8826: Partial method declarations 'void Program.F2(nuint x)' and 'void Program.F2(UIntPtr x)' have signature differences.
                //     static partial void F2(System.UIntPtr x) { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F2").WithArguments("void Program.F2(nuint x)", "void Program.F2(UIntPtr x)").WithLocation(4, 25),
                // (5,25): warning CS8826: Partial method declarations 'void Program.F1(IntPtr x)' and 'void Program.F1(nint x)' have signature differences.
                //     static partial void F1(nint x) { }
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "F1").WithArguments("void Program.F1(IntPtr x)", "void Program.F1(nint x)").WithLocation(5, 25));
        }

        // Shouldn't depend on [NullablePublicOnly].
        [Fact]
        public void NoPublicMembers()
        {
            var source =
@"class A<T, U>
{
}
class B : A<System.UIntPtr, nint>
{
}";
            var comp = CreateCompilation(
                source,
                options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All),
                parseOptions: TestOptions.Regular9.WithNullablePublicOnly());
            var expected =
@"[NativeInteger({ False, True })] B
";
            AssertNativeIntegerAttributes(comp, expected);
        }

        [Fact]
        public void AttributeUsage()
        {
            var source =
@"public class Program
{
    public nint F;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var attributeType = module.GlobalNamespace.GetMember<NamedTypeSymbol>("System.Runtime.CompilerServices.NativeIntegerAttribute");
                AttributeUsageInfo attributeUsage = attributeType.GetAttributeUsageInfo();
                Assert.False(attributeUsage.Inherited);
                Assert.False(attributeUsage.AllowMultiple);
                Assert.True(attributeUsage.HasValidAttributeTargets);
                var expectedTargets =
                    AttributeTargets.Class |
                    AttributeTargets.Event |
                    AttributeTargets.Field |
                    AttributeTargets.GenericParameter |
                    AttributeTargets.Parameter |
                    AttributeTargets.Property |
                    AttributeTargets.ReturnValue;
                Assert.Equal(expectedTargets, attributeUsage.ValidTargets);
            });
        }

        [Fact]
        public void AttributeFieldExists()
        {
            var source =
@"public class Program
{
    public nint F;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            CompileAndVerify(comp, symbolValidator: module =>
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("Program");
                var member = type.GetMembers("F").Single();
                var attributes = member.GetAttributes();
                AssertNativeIntegerAttribute(attributes);
                var attribute = GetNativeIntegerAttribute(attributes);
                var field = attribute.AttributeClass.GetField("TransformFlags");
                Assert.Equal("System.Boolean[]", field.TypeWithAnnotations.ToTestDisplayString());
            });
        }

        [Fact]
        public void NestedNativeIntegerWithPrecedingType()
        {
            var comp = CompileAndVerify(@"
class C<T, U, V>
{
    public C<dynamic, T, nint> F0;
    public C<dynamic, nint, System.IntPtr> F1;
    public C<dynamic, nuint, System.UIntPtr> F2;
    public C<T, nint, System.IntPtr> F3;
    public C<T, nuint, System.UIntPtr> F4;
}
", options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular9, symbolValidator: symbolValidator);

            static void symbolValidator(ModuleSymbol module)
            {
                var expectedAttributes = @"
C<T, U, V>
    [NativeInteger] C<dynamic, T, System.IntPtr> F0
    [NativeInteger({ True, False })] C<dynamic, System.IntPtr, System.IntPtr> F1
    [NativeInteger({ True, False })] C<dynamic, System.UIntPtr, System.UIntPtr> F2
    [NativeInteger({ True, False })] C<T, System.IntPtr, System.IntPtr> F3
    [NativeInteger({ True, False })] C<T, System.UIntPtr, System.UIntPtr> F4
";

                AssertNativeIntegerAttributes(module, expectedAttributes);
                var c = module.GlobalNamespace.GetTypeMember("C");

                assert("C<dynamic, T, System.IntPtr>", "F0");
                assert("C<dynamic, System.IntPtr, System.IntPtr>", "F1");
                assert("C<dynamic, System.UIntPtr, System.UIntPtr>", "F2");
                assert("C<T, System.IntPtr, System.IntPtr>", "F3");
                assert("C<T, System.UIntPtr, System.UIntPtr>", "F4");

                void assert(string expectedType, string fieldName)
                {
                    Assert.Equal(expectedType, c.GetField(fieldName).Type.ToTestDisplayString());
                }
            }
        }

        [Fact]
        public void FunctionPointersWithNativeIntegerTypes()
        {
            var comp = CompileAndVerify(@"
unsafe class C
{
    public delegate*<nint, object, object> F0;
    public delegate*<nint, nint, nint> F1;
    public delegate*<System.IntPtr, System.IntPtr, nint> F2;
    public delegate*<nint, System.IntPtr, System.IntPtr> F3;
    public delegate*<System.IntPtr, nint, System.IntPtr> F4;
    public delegate*<delegate*<System.IntPtr, System.IntPtr, System.IntPtr>, nint> F5;
    public delegate*<nint, delegate*<System.IntPtr, System.IntPtr, System.IntPtr>> F6;
    public delegate*<delegate*<System.IntPtr, System.IntPtr, nint>, System.IntPtr> F7;
    public delegate*<System.IntPtr, delegate*<System.IntPtr, nint, System.IntPtr>> F8;
}
", options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular9, symbolValidator: symbolValidator);

            static void symbolValidator(ModuleSymbol module)
            {
                var expectedAttributes = @"
C
    [NativeInteger] delegate*<System.IntPtr, System.Object, System.Object> F0
    [NativeInteger({ True, True, True })] delegate*<System.IntPtr, System.IntPtr, System.IntPtr> F1
    [NativeInteger({ True, False, False })] delegate*<System.IntPtr, System.IntPtr, System.IntPtr> F2
    [NativeInteger({ False, True, False })] delegate*<System.IntPtr, System.IntPtr, System.IntPtr> F3
    [NativeInteger({ False, False, True })] delegate*<System.IntPtr, System.IntPtr, System.IntPtr> F4
    [NativeInteger({ True, False, False, False })] delegate*<delegate*<System.IntPtr, System.IntPtr, System.IntPtr>, System.IntPtr> F5
    [NativeInteger({ False, False, False, True })] delegate*<System.IntPtr, delegate*<System.IntPtr, System.IntPtr, System.IntPtr>> F6
    [NativeInteger({ False, True, False, False })] delegate*<delegate*<System.IntPtr, System.IntPtr, System.IntPtr>, System.IntPtr> F7
    [NativeInteger({ False, False, True, False })] delegate*<System.IntPtr, delegate*<System.IntPtr, System.IntPtr, System.IntPtr>> F8
";

                AssertNativeIntegerAttributes(module, expectedAttributes);
                var c = module.GlobalNamespace.GetTypeMember("C");

                assert("delegate*<System.IntPtr, System.Object, System.Object>", "F0");
                assert("delegate*<System.IntPtr, System.IntPtr, System.IntPtr>", "F1");
                assert("delegate*<System.IntPtr, System.IntPtr, System.IntPtr>", "F2");
                assert("delegate*<System.IntPtr, System.IntPtr, System.IntPtr>", "F3");
                assert("delegate*<System.IntPtr, System.IntPtr, System.IntPtr>", "F4");
                assert("delegate*<delegate*<System.IntPtr, System.IntPtr, System.IntPtr>, System.IntPtr>", "F5");
                assert("delegate*<System.IntPtr, delegate*<System.IntPtr, System.IntPtr, System.IntPtr>>", "F6");
                assert("delegate*<delegate*<System.IntPtr, System.IntPtr, System.IntPtr>, System.IntPtr>", "F7");
                assert("delegate*<System.IntPtr, delegate*<System.IntPtr, System.IntPtr, System.IntPtr>>", "F8");

                void assert(string expectedType, string fieldName)
                {
                    var field = c.GetField(fieldName);
                    FunctionPointerUtilities.CommonVerifyFunctionPointer((FunctionPointerTypeSymbol)field.Type);
                    Assert.Equal(expectedType, c.GetField(fieldName).Type.ToTestDisplayString());
                }
            }
        }

        private static TypeDefinition GetTypeDefinitionByName(MetadataReader reader, string name)
        {
            return reader.GetTypeDefinition(reader.TypeDefinitions.Single(h => reader.StringComparer.Equals(reader.GetTypeDefinition(h).Name, name)));
        }

        private static string GetAttributeConstructorName(MetadataReader reader, CustomAttributeHandle handle)
        {
            return reader.Dump(reader.GetCustomAttribute(handle).Constructor);
        }

        private static CustomAttribute GetAttributeByConstructorName(MetadataReader reader, CustomAttributeHandleCollection handles, string name)
        {
            return reader.GetCustomAttribute(handles.FirstOrDefault(h => GetAttributeConstructorName(reader, h) == name));
        }

        private static void AssertAttributes(MetadataReader reader, CustomAttributeHandleCollection handles, params string[] expectedNames)
        {
            var actualNames = handles.Select(h => GetAttributeConstructorName(reader, h)).ToArray();
            AssertEx.Equal(expectedNames, actualNames);
        }

        private static void AssertNativeIntegerAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            AssertAttributes(attributes, "System.Runtime.CompilerServices.NativeIntegerAttribute");
        }

        private static void AssertAttributes(ImmutableArray<CSharpAttributeData> attributes, params string[] expectedNames)
        {
            var actualNames = attributes.Select(a => a.AttributeClass.ToTestDisplayString()).ToArray();
            AssertEx.Equal(expectedNames, actualNames);
        }

        private void AssertNativeIntegerAttributes(CSharpCompilation comp, string expected)
        {
            CompileAndVerify(comp, symbolValidator: module => AssertNativeIntegerAttributes(module, expected));
        }

        private static void AssertNativeIntegerAttributes(ModuleSymbol module, string expected)
        {
            var actual = NativeIntegerAttributesVisitor.GetString((PEModuleSymbol)module);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, actual);
        }

        private static CSharpAttributeData GetNativeIntegerAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Single(a => a.AttributeClass.ToTestDisplayString() == "System.Runtime.CompilerServices.NativeIntegerAttribute");
        }
    }
}
