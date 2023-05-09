// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class InlineArrayTests : CompilingTestBase
    {
        public const string InlineArrayAttributeDefinition =
@"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
";

        public const string Buffer10Definition =
@"
[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;
}
" + InlineArrayAttributeDefinition;

        [Fact]
        public void InlineArrayType_01_NoAttribute()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Theory]
        [CombinatorialData]
        public void InlineArrayType_02([CombinatorialValues("private", "public", "internal")] string accessibility)
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    " + accessibility + @" int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_03_Generic()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer<T>
{
    private T _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("T", buffer.TryGetInlineArrayElementType().ToTestDisplayString());

                var bufferOfInt = buffer.Construct(m.ContainingAssembly.GetSpecialType(SpecialType.System_Int32));

                Assert.True(bufferOfInt.HasInlineArrayAttribute(out length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, bufferOfInt.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_04_MultipleAttributes()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
[System.Runtime.CompilerServices.InlineArray(100)]
struct Buffer
{
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_05_WrongLength()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(0)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics(
                // (5,17): warning CS0169: The field 'Buffer._element0' is never used
                //     private int _element0;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_element0").WithArguments("Buffer._element0").WithLocation(5, 17)
                );

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Fact]
        public void InlineArrayType_06_WrongLength()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(-1)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics(
                // (5,17): warning CS0169: The field 'Buffer._element0' is never used
                //     private int _element0;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_element0").WithArguments("Buffer._element0").WithLocation(5, 17)
                );

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Fact]
        public void InlineArrayType_07_WrongLength()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(-2)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics(
                // (5,17): warning CS0169: The field 'Buffer._element0' is never used
                //     private int _element0;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "_element0").WithArguments("Buffer._element0").WithLocation(5, 17)
                );

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Fact]
        public void InlineArrayType_08_MoreThanOneField()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
    private int _element1;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_09_NoFields()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_10_WithStaticFields()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    public static long A;
    private int _element0;
    public static short B;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_11_WithSingleStaticField()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private static int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_12_Class()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
class Buffer
{
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
";
            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_13_Enum()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
enum Buffer
{
    _element0
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
";
            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_14_Delegate()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
delegate void Buffer();

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
";
            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_15_Interface()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
interface Buffer
{
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
";
            var comp = CreateCompilation(src, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics(
                // (5,17): error CS0525: Interfaces cannot contain instance fields
                //     private int _element0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "_element0").WithLocation(5, 17)
                );

            var buffer = comp.SourceAssembly.SourceModule.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.False(buffer.TryGetInlineArrayElementType().HasType);
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void InlineArrayType_16_RefField()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
ref struct Buffer
{
    private ref int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition, targetFramework: TargetFramework.NetCoreApp);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void InlineArrayType_17_RefField()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
ref struct Buffer
{
    private ref readonly int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition, targetFramework: TargetFramework.NetCoreApp);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.False(buffer.TryGetInlineArrayElementType().HasType);
            }
        }

        [Fact]
        public void InlineArrayType_18_Retargeting()
        {
            var src1 = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
}
";
            var comp1 = CreateCompilation(src1 + InlineArrayAttributeDefinition, targetFramework: TargetFramework.Net50);
            var comp2 = CreateCompilation("", references: new[] { comp1.ToMetadataReference() }, targetFramework: TargetFramework.Net60);

            var buffer = comp2.GlobalNamespace.GetTypeMember("Buffer");

            Assert.IsType<RetargetingNamedTypeSymbol>(buffer);
            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
        }

        [Fact]
        public void InlineArrayType_19()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray((short)10)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_20()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(Length)]
struct Buffer
{
    [System.Obsolete(""yes"")]
    public const int Length = 10;

    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics(
                // (2,46): warning CS0618: 'Buffer.Length' is obsolete: 'yes'
                // [System.Runtime.CompilerServices.InlineArray(Length)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Length").WithArguments("Buffer.Length", "yes").WithLocation(2, 46)
                );

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_21()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(Length)]
struct Buffer
{
    [System.Obsolete(""yes"", true)]
    public const int Length = 10;

    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition);
            comp.VerifyDiagnostics(
                // (2,46): error CS0619: 'Buffer.Length' is obsolete: 'yes'
                // [System.Runtime.CompilerServices.InlineArray(Length)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Length").WithArguments("Buffer.Length", "yes").WithLocation(2, 46)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
        }

        [Fact]
        public void InlineArrayType_22()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (long length)
        {
        }
    }
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Fact]
        public void InlineArrayType_23()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(Buffer.Length)]
struct Buffer
{
    public const int Length = 10;
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
        }
    }
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_24()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(Buffer<int>.Length)]
struct Buffer<T>
{
    public const int Length = 10;
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
        }
    }
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_25()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
}

[System.Diagnostics.ConditionalAttribute(nameof(C1.Field))]
class C1 : System.Attribute
{
    public Buffer Field = default;
}


namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
        }
    }
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementType().SpecialType);

                Assert.Equal("Field", m.GlobalNamespace.GetTypeMember("C1").GetAppliedConditionalSymbols().Single());
            }
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[0] = 111;
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldc.i4.s   111
  IL_0018:  stind.i4
  IL_0019:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,27): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static int M1(C x) => x.F[0];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F[0]").WithArguments("inline arrays").WithLocation(18, 27),
                // (19,28): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static void M2(C x) => x.F[0] = 111;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F[0]").WithArguments("inline arrays").WithLocation(19, 28)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M1").Single();
            var m1Operation = model.GetOperation(m1);
            VerifyOperationTree(comp, m1Operation,
@"
");

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(C x) => ref x.F[0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F;

    public ref int M2() => ref F[0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(C x) => ref x.F[0];

    static ref int M3(C x)
    {
        ref int y = ref x.F[0];
        return ref y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,35): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static ref int M2(C x) => ref x.F[0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 35),
                // (21,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(21, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public ref int M2() => ref F[0];

    public ref int M3()
    {
        ref int y = ref F[0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref int M2() => ref F[0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[0]").WithLocation(6, 32),
                // (11,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(11, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(ref C x) => ref x.F[0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2() => ref F[0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_08()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(ref C x)
    {
        ref int y = ref x.F[0];
        return ref y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2()
    {
        ref int y = ref F[0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_10()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0][0];
    static void M2(C x) => x.F[0][0] = 111;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_11()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(C x) => ref x.F[0][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_12()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;

    public ref int M2() => ref F[0][0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_13()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(C x) => ref x.F[0][0];

    static ref int M3(C x)
    {
        ref int y = ref x.F[0][0];
        return ref y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,35): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static ref int M2(C x) => ref x.F[0][0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 35),
                // (21,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(21, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_14()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public ref int M2() => ref F[0][0];

    public ref int M3()
    {
        ref int y = ref F[0][0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref int M2() => ref F[0][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[0][0]").WithLocation(6, 32),
                // (11,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(11, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_15()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(ref C x) => ref x.F[0][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_16()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2() => ref F[0][0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_17()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(ref C x)
    {
        ref int y = ref x.F[0][0];
        return ref y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_18()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2()
    {
        ref int y = ref F[0][0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_01()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => x.F[await FromResult(0)];
    static async Task M2(C x) => x.F[await FromResult(0)] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      189 (0xbd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Span<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M1>d__1.x""
    IL_0011:  stfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0016:  ldc.i4.0
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.3
    IL_0022:  ldloca.s   V_3
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.3
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0041:  ldloca.s   V_3
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M1>d__1)""
    IL_0049:  leave.s    IL_00bc
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0051:  stloc.3
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0067:  ldloca.s   V_3
    IL_0069:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006e:  stloc.2
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0075:  ldflda     ""Buffer10<int> C.F""
    IL_007a:  ldc.i4.s   10
    IL_007c:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0081:  stloc.s    V_4
    IL_0083:  ldloca.s   V_4
    IL_0085:  ldloc.2
    IL_0086:  call       ""ref int System.Span<int>.this[int].get""
    IL_008b:  ldind.i4
    IL_008c:  stloc.1
    IL_008d:  leave.s    IL_00a8
  }
  catch System.Exception
  {
    IL_008f:  stloc.s    V_5
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.s   -2
    IL_0094:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_009f:  ldloc.s    V_5
    IL_00a1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a6:  leave.s    IL_00bc
  }
  IL_00a8:  ldarg.0
  IL_00a9:  ldc.i4.s   -2
  IL_00ab:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00b0:  ldarg.0
  IL_00b1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00b6:  ldloc.1
  IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00bc:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      195 (0xc3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Span<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M2>d__2.x""
    IL_0011:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0016:  ldc.i4.0
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.2
    IL_0022:  ldloca.s   V_2
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.2
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_0041:  ldloca.s   V_2
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__2)""
    IL_0049:  leave.s    IL_00c2
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0051:  stloc.2
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0067:  ldloca.s   V_2
    IL_0069:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006e:  stloc.1
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0075:  ldflda     ""Buffer10<int> C.F""
    IL_007a:  ldc.i4.s   10
    IL_007c:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0081:  stloc.3
    IL_0082:  ldloca.s   V_3
    IL_0084:  ldloc.1
    IL_0085:  call       ""ref int System.Span<int>.this[int].get""
    IL_008a:  ldc.i4.s   111
    IL_008c:  stind.i4
    IL_008d:  ldarg.0
    IL_008e:  ldnull
    IL_008f:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0094:  leave.s    IL_00af
  }
  catch System.Exception
  {
    IL_0096:  stloc.s    V_4
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.s   -2
    IL_009b:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00a6:  ldloc.s    V_4
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ad:  leave.s    IL_00c2
  }
  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.s   -2
  IL_00b2:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00b7:  ldarg.0
  IL_00b8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00bd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c2:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_02()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => x.F[await FromResult(^10)];
    static async Task M2(C x) => x.F[await FromResult(^10)] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      204 (0xcc)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Index V_2,
                System.Runtime.CompilerServices.TaskAwaiter<System.Index> V_3,
                System.Span<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0052
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M1>d__1.x""
    IL_0011:  stfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0016:  ldc.i4.s   10
    IL_0018:  ldc.i4.1
    IL_0019:  newobj     ""System.Index..ctor(int, bool)""
    IL_001e:  call       ""System.Threading.Tasks.Task<System.Index> Program.FromResult<System.Index>(System.Index)""
    IL_0023:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> System.Threading.Tasks.Task<System.Index>.GetAwaiter()""
    IL_0028:  stloc.3
    IL_0029:  ldloca.s   V_3
    IL_002b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<System.Index>.IsCompleted.get""
    IL_0030:  brtrue.s   IL_006e
    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  dup
    IL_0035:  stloc.0
    IL_0036:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.3
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M1>d__1.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0048:  ldloca.s   V_3
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Index>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Index>, ref Program.<M1>d__1)""
    IL_0050:  leave.s    IL_00cb
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M1>d__1.<>u__1""
    IL_0058:  stloc.3
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M1>d__1.<>u__1""
    IL_005f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Index>""
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.m1
    IL_0067:  dup
    IL_0068:  stloc.0
    IL_0069:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       ""System.Index System.Runtime.CompilerServices.TaskAwaiter<System.Index>.GetResult()""
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_007c:  ldflda     ""Buffer10<int> C.F""
    IL_0081:  ldc.i4.s   10
    IL_0083:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0088:  stloc.s    V_4
    IL_008a:  ldloca.s   V_4
    IL_008c:  ldloca.s   V_2
    IL_008e:  ldc.i4.s   10
    IL_0090:  call       ""int System.Index.GetOffset(int)""
    IL_0095:  call       ""ref int System.Span<int>.this[int].get""
    IL_009a:  ldind.i4
    IL_009b:  stloc.1
    IL_009c:  leave.s    IL_00b7
  }
  catch System.Exception
  {
    IL_009e:  stloc.s    V_5
    IL_00a0:  ldarg.0
    IL_00a1:  ldc.i4.s   -2
    IL_00a3:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_00a8:  ldarg.0
    IL_00a9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_00ae:  ldloc.s    V_5
    IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00b5:  leave.s    IL_00cb
  }
  IL_00b7:  ldarg.0
  IL_00b8:  ldc.i4.s   -2
  IL_00ba:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00bf:  ldarg.0
  IL_00c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00c5:  ldloc.1
  IL_00c6:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00cb:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      210 (0xd2)
  .maxstack  3
  .locals init (int V_0,
                System.Index V_1,
                System.Runtime.CompilerServices.TaskAwaiter<System.Index> V_2,
                System.Span<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0052
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M2>d__2.x""
    IL_0011:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0016:  ldc.i4.s   10
    IL_0018:  ldc.i4.1
    IL_0019:  newobj     ""System.Index..ctor(int, bool)""
    IL_001e:  call       ""System.Threading.Tasks.Task<System.Index> Program.FromResult<System.Index>(System.Index)""
    IL_0023:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> System.Threading.Tasks.Task<System.Index>.GetAwaiter()""
    IL_0028:  stloc.2
    IL_0029:  ldloca.s   V_2
    IL_002b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<System.Index>.IsCompleted.get""
    IL_0030:  brtrue.s   IL_006e
    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  dup
    IL_0035:  stloc.0
    IL_0036:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.2
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__2.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_0048:  ldloca.s   V_2
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Index>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Index>, ref Program.<M2>d__2)""
    IL_0050:  leave.s    IL_00d1
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__2.<>u__1""
    IL_0058:  stloc.2
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__2.<>u__1""
    IL_005f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Index>""
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.m1
    IL_0067:  dup
    IL_0068:  stloc.0
    IL_0069:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_006e:  ldloca.s   V_2
    IL_0070:  call       ""System.Index System.Runtime.CompilerServices.TaskAwaiter<System.Index>.GetResult()""
    IL_0075:  stloc.1
    IL_0076:  ldarg.0
    IL_0077:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_007c:  ldflda     ""Buffer10<int> C.F""
    IL_0081:  ldc.i4.s   10
    IL_0083:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0088:  stloc.3
    IL_0089:  ldloca.s   V_3
    IL_008b:  ldloca.s   V_1
    IL_008d:  ldc.i4.s   10
    IL_008f:  call       ""int System.Index.GetOffset(int)""
    IL_0094:  call       ""ref int System.Span<int>.this[int].get""
    IL_0099:  ldc.i4.s   111
    IL_009b:  stind.i4
    IL_009c:  ldarg.0
    IL_009d:  ldnull
    IL_009e:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_00a3:  leave.s    IL_00be
  }
  catch System.Exception
  {
    IL_00a5:  stloc.s    V_4
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.s   -2
    IL_00aa:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00af:  ldarg.0
    IL_00b0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00b5:  ldloc.s    V_4
    IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00bc:  leave.s    IL_00d1
  }
  IL_00be:  ldarg.0
  IL_00bf:  ldc.i4.s   -2
  IL_00c1:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00c6:  ldarg.0
  IL_00c7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00cc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00d1:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_03()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => x.F[^await FromResult(10)];
    static async Task M2(C x) => x.F[^await FromResult(10)] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      193 (0xc1)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Span<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M1>d__1.x""
    IL_0011:  stfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0016:  ldc.i4.s   10
    IL_0018:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0022:  stloc.3
    IL_0023:  ldloca.s   V_3
    IL_0025:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002a:  brtrue.s   IL_0068
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.0
    IL_002e:  dup
    IL_002f:  stloc.0
    IL_0030:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0035:  ldarg.0
    IL_0036:  ldloc.3
    IL_0037:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_003c:  ldarg.0
    IL_003d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0042:  ldloca.s   V_3
    IL_0044:  ldarg.0
    IL_0045:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M1>d__1)""
    IL_004a:  leave.s    IL_00c0
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0052:  stloc.3
    IL_0053:  ldarg.0
    IL_0054:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0059:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.0
    IL_0063:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0068:  ldloca.s   V_3
    IL_006a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006f:  stloc.2
    IL_0070:  ldarg.0
    IL_0071:  ldfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0076:  ldflda     ""Buffer10<int> C.F""
    IL_007b:  ldc.i4.s   10
    IL_007d:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0082:  stloc.s    V_4
    IL_0084:  ldloca.s   V_4
    IL_0086:  ldc.i4.s   10
    IL_0088:  ldloc.2
    IL_0089:  sub
    IL_008a:  call       ""ref int System.Span<int>.this[int].get""
    IL_008f:  ldind.i4
    IL_0090:  stloc.1
    IL_0091:  leave.s    IL_00ac
  }
  catch System.Exception
  {
    IL_0093:  stloc.s    V_5
    IL_0095:  ldarg.0
    IL_0096:  ldc.i4.s   -2
    IL_0098:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_009d:  ldarg.0
    IL_009e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_00a3:  ldloc.s    V_5
    IL_00a5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00aa:  leave.s    IL_00c0
  }
  IL_00ac:  ldarg.0
  IL_00ad:  ldc.i4.s   -2
  IL_00af:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00b4:  ldarg.0
  IL_00b5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00ba:  ldloc.1
  IL_00bb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00c0:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      199 (0xc7)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Span<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004c
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M2>d__2.x""
    IL_0011:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0016:  ldc.i4.s   10
    IL_0018:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001d:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0022:  stloc.2
    IL_0023:  ldloca.s   V_2
    IL_0025:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002a:  brtrue.s   IL_0068
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.0
    IL_002e:  dup
    IL_002f:  stloc.0
    IL_0030:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0035:  ldarg.0
    IL_0036:  ldloc.2
    IL_0037:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_003c:  ldarg.0
    IL_003d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_0042:  ldloca.s   V_2
    IL_0044:  ldarg.0
    IL_0045:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__2)""
    IL_004a:  leave.s    IL_00c6
    IL_004c:  ldarg.0
    IL_004d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0052:  stloc.2
    IL_0053:  ldarg.0
    IL_0054:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0059:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.m1
    IL_0061:  dup
    IL_0062:  stloc.0
    IL_0063:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0068:  ldloca.s   V_2
    IL_006a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006f:  stloc.1
    IL_0070:  ldarg.0
    IL_0071:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0076:  ldflda     ""Buffer10<int> C.F""
    IL_007b:  ldc.i4.s   10
    IL_007d:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0082:  stloc.3
    IL_0083:  ldloca.s   V_3
    IL_0085:  ldc.i4.s   10
    IL_0087:  ldloc.1
    IL_0088:  sub
    IL_0089:  call       ""ref int System.Span<int>.this[int].get""
    IL_008e:  ldc.i4.s   111
    IL_0090:  stind.i4
    IL_0091:  ldarg.0
    IL_0092:  ldnull
    IL_0093:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0098:  leave.s    IL_00b3
  }
  catch System.Exception
  {
    IL_009a:  stloc.s    V_4
    IL_009c:  ldarg.0
    IL_009d:  ldc.i4.s   -2
    IL_009f:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00a4:  ldarg.0
    IL_00a5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00aa:  ldloc.s    V_4
    IL_00ac:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00b1:  leave.s    IL_00c6
  }
  IL_00b3:  ldarg.0
  IL_00b4:  ldc.i4.s   -2
  IL_00b6:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00bb:  ldarg.0
  IL_00bc:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00c1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c6:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_04()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.Write(M2().Result);
    }

    static async Task<int> M2() => M3()[await FromResult(0)];

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      183 (0xb7)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.ReadOnlySpan<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004a
    IL_000a:  ldarg.0
    IL_000b:  call       ""Buffer10<int> Program.M3()""
    IL_0010:  stfld      ""Buffer10<int> Program.<M2>d__1.<>7__wrap1""
    IL_0015:  ldc.i4.0
    IL_0016:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001b:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0020:  stloc.3
    IL_0021:  ldloca.s   V_3
    IL_0023:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0028:  brtrue.s   IL_0066
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.0
    IL_002c:  dup
    IL_002d:  stloc.0
    IL_002e:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0033:  ldarg.0
    IL_0034:  ldloc.3
    IL_0035:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_003a:  ldarg.0
    IL_003b:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
    IL_0040:  ldloca.s   V_3
    IL_0042:  ldarg.0
    IL_0043:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__1)""
    IL_0048:  leave.s    IL_00b6
    IL_004a:  ldarg.0
    IL_004b:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_0050:  stloc.3
    IL_0051:  ldarg.0
    IL_0052:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_0057:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.m1
    IL_005f:  dup
    IL_0060:  stloc.0
    IL_0061:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0066:  ldloca.s   V_3
    IL_0068:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006d:  stloc.2
    IL_006e:  ldarg.0
    IL_006f:  ldflda     ""Buffer10<int> Program.<M2>d__1.<>7__wrap1""
    IL_0074:  ldc.i4.s   10
    IL_0076:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_007b:  stloc.s    V_4
    IL_007d:  ldloca.s   V_4
    IL_007f:  ldloc.2
    IL_0080:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0085:  ldind.i4
    IL_0086:  stloc.1
    IL_0087:  leave.s    IL_00a2
  }
  catch System.Exception
  {
    IL_0089:  stloc.s    V_5
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.s   -2
    IL_008e:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
    IL_0099:  ldloc.s    V_5
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a0:  leave.s    IL_00b6
  }
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   -2
  IL_00a5:  stfld      ""int Program.<M2>d__1.<>1__state""
  IL_00aa:  ldarg.0
  IL_00ab:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
  IL_00b0:  ldloc.1
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b6:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_05()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.Write(M2().Result);
    }

    static async Task<int> M2() => M3()[await FromResult(^10)];

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      198 (0xc6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Index V_2,
                System.Runtime.CompilerServices.TaskAwaiter<System.Index> V_3,
                System.ReadOnlySpan<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0051
    IL_000a:  ldarg.0
    IL_000b:  call       ""Buffer10<int> Program.M3()""
    IL_0010:  stfld      ""Buffer10<int> Program.<M2>d__1.<>7__wrap1""
    IL_0015:  ldc.i4.s   10
    IL_0017:  ldc.i4.1
    IL_0018:  newobj     ""System.Index..ctor(int, bool)""
    IL_001d:  call       ""System.Threading.Tasks.Task<System.Index> Program.FromResult<System.Index>(System.Index)""
    IL_0022:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> System.Threading.Tasks.Task<System.Index>.GetAwaiter()""
    IL_0027:  stloc.3
    IL_0028:  ldloca.s   V_3
    IL_002a:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<System.Index>.IsCompleted.get""
    IL_002f:  brtrue.s   IL_006d
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  dup
    IL_0034:  stloc.0
    IL_0035:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_003a:  ldarg.0
    IL_003b:  ldloc.3
    IL_003c:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__1.<>u__1""
    IL_0041:  ldarg.0
    IL_0042:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
    IL_0047:  ldloca.s   V_3
    IL_0049:  ldarg.0
    IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Index>, Program.<M2>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Index>, ref Program.<M2>d__1)""
    IL_004f:  leave.s    IL_00c5
    IL_0051:  ldarg.0
    IL_0052:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__1.<>u__1""
    IL_0057:  stloc.3
    IL_0058:  ldarg.0
    IL_0059:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__1.<>u__1""
    IL_005e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Index>""
    IL_0064:  ldarg.0
    IL_0065:  ldc.i4.m1
    IL_0066:  dup
    IL_0067:  stloc.0
    IL_0068:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_006d:  ldloca.s   V_3
    IL_006f:  call       ""System.Index System.Runtime.CompilerServices.TaskAwaiter<System.Index>.GetResult()""
    IL_0074:  stloc.2
    IL_0075:  ldarg.0
    IL_0076:  ldflda     ""Buffer10<int> Program.<M2>d__1.<>7__wrap1""
    IL_007b:  ldc.i4.s   10
    IL_007d:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_0082:  stloc.s    V_4
    IL_0084:  ldloca.s   V_4
    IL_0086:  ldloca.s   V_2
    IL_0088:  ldc.i4.s   10
    IL_008a:  call       ""int System.Index.GetOffset(int)""
    IL_008f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0094:  ldind.i4
    IL_0095:  stloc.1
    IL_0096:  leave.s    IL_00b1
  }
  catch System.Exception
  {
    IL_0098:  stloc.s    V_5
    IL_009a:  ldarg.0
    IL_009b:  ldc.i4.s   -2
    IL_009d:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
    IL_00a8:  ldloc.s    V_5
    IL_00aa:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00af:  leave.s    IL_00c5
  }
  IL_00b1:  ldarg.0
  IL_00b2:  ldc.i4.s   -2
  IL_00b4:  stfld      ""int Program.<M2>d__1.<>1__state""
  IL_00b9:  ldarg.0
  IL_00ba:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
  IL_00bf:  ldloc.1
  IL_00c0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00c5:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_06()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.Write(M2().Result);
    }

    static async Task<int> M2() => M3()[^await FromResult(10)];

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      187 (0xbb)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.ReadOnlySpan<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldarg.0
    IL_000b:  call       ""Buffer10<int> Program.M3()""
    IL_0010:  stfld      ""Buffer10<int> Program.<M2>d__1.<>7__wrap1""
    IL_0015:  ldc.i4.s   10
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.3
    IL_0022:  ldloca.s   V_3
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.3
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
    IL_0041:  ldloca.s   V_3
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__1)""
    IL_0049:  leave.s    IL_00ba
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_0051:  stloc.3
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0067:  ldloca.s   V_3
    IL_0069:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006e:  stloc.2
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""Buffer10<int> Program.<M2>d__1.<>7__wrap1""
    IL_0075:  ldc.i4.s   10
    IL_0077:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_007c:  stloc.s    V_4
    IL_007e:  ldloca.s   V_4
    IL_0080:  ldc.i4.s   10
    IL_0082:  ldloc.2
    IL_0083:  sub
    IL_0084:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0089:  ldind.i4
    IL_008a:  stloc.1
    IL_008b:  leave.s    IL_00a6
  }
  catch System.Exception
  {
    IL_008d:  stloc.s    V_5
    IL_008f:  ldarg.0
    IL_0090:  ldc.i4.s   -2
    IL_0092:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0097:  ldarg.0
    IL_0098:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
    IL_009d:  ldloc.s    V_5
    IL_009f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a4:  leave.s    IL_00ba
  }
  IL_00a6:  ldarg.0
  IL_00a7:  ldc.i4.s   -2
  IL_00a9:  stfld      ""int Program.<M2>d__1.<>1__state""
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M2>d__1.<>t__builder""
  IL_00b4:  ldloc.1
  IL_00b5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00ba:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_07()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => x.F[await FromResult(0)];

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      189 (0xbd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.ReadOnlySpan<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M1>d__1.x""
    IL_0011:  stfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0016:  ldc.i4.0
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.3
    IL_0022:  ldloca.s   V_3
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.3
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0041:  ldloca.s   V_3
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M1>d__1)""
    IL_0049:  leave.s    IL_00bc
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0051:  stloc.3
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0067:  ldloca.s   V_3
    IL_0069:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006e:  stloc.2
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0075:  ldflda     ""Buffer10<int> C.F""
    IL_007a:  ldc.i4.s   10
    IL_007c:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_0081:  stloc.s    V_4
    IL_0083:  ldloca.s   V_4
    IL_0085:  ldloc.2
    IL_0086:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_008b:  ldind.i4
    IL_008c:  stloc.1
    IL_008d:  leave.s    IL_00a8
  }
  catch System.Exception
  {
    IL_008f:  stloc.s    V_5
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.s   -2
    IL_0094:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_009f:  ldloc.s    V_5
    IL_00a1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a6:  leave.s    IL_00bc
  }
  IL_00a8:  ldarg.0
  IL_00a9:  ldc.i4.s   -2
  IL_00ab:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00b0:  ldarg.0
  IL_00b1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00b6:  ldloc.1
  IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00bc:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_08()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        var x = new Buffer10<int>[] { default };
        System.Console.Write(x[0][0]);
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(x[0][0]);
    }

    static async Task M2(Buffer10<int>[] x) => x[Get01()][Get02()] = await FromResult(111);
    static int Get01() => 0;
    static int Get02() => 0;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      249 (0xf9)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Span<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0077
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""Buffer10<int>[] Program.<M2>d__1.x""
    IL_0011:  stfld      ""Buffer10<int>[] Program.<M2>d__1.<>7__wrap2""
    IL_0016:  ldarg.0
    IL_0017:  call       ""int Program.Get01()""
    IL_001c:  stfld      ""int Program.<M2>d__1.<>7__wrap3""
    IL_0021:  ldarg.0
    IL_0022:  ldfld      ""Buffer10<int>[] Program.<M2>d__1.<>7__wrap2""
    IL_0027:  ldarg.0
    IL_0028:  ldfld      ""int Program.<M2>d__1.<>7__wrap3""
    IL_002d:  ldelema    ""Buffer10<int>""
    IL_0032:  pop
    IL_0033:  ldarg.0
    IL_0034:  call       ""int Program.Get02()""
    IL_0039:  stfld      ""int Program.<M2>d__1.<>7__wrap1""
    IL_003e:  ldc.i4.s   111
    IL_0040:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_0045:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_004a:  stloc.2
    IL_004b:  ldloca.s   V_2
    IL_004d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0052:  brtrue.s   IL_0093
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.0
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_005d:  ldarg.0
    IL_005e:  ldloc.2
    IL_005f:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_0064:  ldarg.0
    IL_0065:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__1.<>t__builder""
    IL_006a:  ldloca.s   V_2
    IL_006c:  ldarg.0
    IL_006d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__1)""
    IL_0072:  leave      IL_00f8
    IL_0077:  ldarg.0
    IL_0078:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_007d:  stloc.2
    IL_007e:  ldarg.0
    IL_007f:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__1.<>u__1""
    IL_0084:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.m1
    IL_008c:  dup
    IL_008d:  stloc.0
    IL_008e:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_0093:  ldloca.s   V_2
    IL_0095:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_009a:  stloc.1
    IL_009b:  ldarg.0
    IL_009c:  ldfld      ""Buffer10<int>[] Program.<M2>d__1.<>7__wrap2""
    IL_00a1:  ldarg.0
    IL_00a2:  ldfld      ""int Program.<M2>d__1.<>7__wrap3""
    IL_00a7:  ldelema    ""Buffer10<int>""
    IL_00ac:  ldc.i4.s   10
    IL_00ae:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_00b3:  stloc.3
    IL_00b4:  ldloca.s   V_3
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      ""int Program.<M2>d__1.<>7__wrap1""
    IL_00bc:  call       ""ref int System.Span<int>.this[int].get""
    IL_00c1:  ldloc.1
    IL_00c2:  stind.i4
    IL_00c3:  ldarg.0
    IL_00c4:  ldnull
    IL_00c5:  stfld      ""Buffer10<int>[] Program.<M2>d__1.<>7__wrap2""
    IL_00ca:  leave.s    IL_00e5
  }
  catch System.Exception
  {
    IL_00cc:  stloc.s    V_4
    IL_00ce:  ldarg.0
    IL_00cf:  ldc.i4.s   -2
    IL_00d1:  stfld      ""int Program.<M2>d__1.<>1__state""
    IL_00d6:  ldarg.0
    IL_00d7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__1.<>t__builder""
    IL_00dc:  ldloc.s    V_4
    IL_00de:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00e3:  leave.s    IL_00f8
  }
  IL_00e5:  ldarg.0
  IL_00e6:  ldc.i4.s   -2
  IL_00e8:  stfld      ""int Program.<M2>d__1.<>1__state""
  IL_00ed:  ldarg.0
  IL_00ee:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__1.<>t__builder""
  IL_00f3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00f8:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_09()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => GetC(x).F[Get01()][await FromResult(Get02(x))];

    static C GetC(C x) => x;
    static int Get01() => 0;
    static int Get02(C c)
    {
        System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[0][0] = 111;
        return 0;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      240 (0xf0)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.ReadOnlySpan<Buffer10<int>> V_4,
                System.ReadOnlySpan<int> V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0068
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M1>d__1.x""
    IL_0011:  call       ""C Program.GetC(C)""
    IL_0016:  stfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_001b:  ldarg.0
    IL_001c:  call       ""int Program.Get01()""
    IL_0021:  stfld      ""int Program.<M1>d__1.<>7__wrap2""
    IL_0026:  ldarg.0
    IL_0027:  ldfld      ""C Program.<M1>d__1.x""
    IL_002c:  call       ""int Program.Get02(C)""
    IL_0031:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_0036:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_003b:  stloc.3
    IL_003c:  ldloca.s   V_3
    IL_003e:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0043:  brtrue.s   IL_0084
    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.0
    IL_0047:  dup
    IL_0048:  stloc.0
    IL_0049:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_004e:  ldarg.0
    IL_004f:  ldloc.3
    IL_0050:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0055:  ldarg.0
    IL_0056:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_005b:  ldloca.s   V_3
    IL_005d:  ldarg.0
    IL_005e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M1>d__1)""
    IL_0063:  leave      IL_00ef
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_006e:  stloc.3
    IL_006f:  ldarg.0
    IL_0070:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0075:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_007b:  ldarg.0
    IL_007c:  ldc.i4.m1
    IL_007d:  dup
    IL_007e:  stloc.0
    IL_007f:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0084:  ldloca.s   V_3
    IL_0086:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_008b:  stloc.2
    IL_008c:  ldarg.0
    IL_008d:  ldfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0092:  ldflda     ""Buffer10<Buffer10<int>> C.F""
    IL_0097:  ldc.i4.s   10
    IL_0099:  call       ""InlineArrayAsReadOnlySpan<Buffer10<Buffer10<int>>, Buffer10<int>>(in Buffer10<Buffer10<int>>, int)""
    IL_009e:  stloc.s    V_4
    IL_00a0:  ldloca.s   V_4
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      ""int Program.<M1>d__1.<>7__wrap2""
    IL_00a8:  call       ""ref readonly Buffer10<int> System.ReadOnlySpan<Buffer10<int>>.this[int].get""
    IL_00ad:  ldc.i4.s   10
    IL_00af:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_00b4:  stloc.s    V_5
    IL_00b6:  ldloca.s   V_5
    IL_00b8:  ldloc.2
    IL_00b9:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_00be:  ldind.i4
    IL_00bf:  stloc.1
    IL_00c0:  leave.s    IL_00db
  }
  catch System.Exception
  {
    IL_00c2:  stloc.s    V_6
    IL_00c4:  ldarg.0
    IL_00c5:  ldc.i4.s   -2
    IL_00c7:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_00cc:  ldarg.0
    IL_00cd:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_00d2:  ldloc.s    V_6
    IL_00d4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00d9:  leave.s    IL_00ef
  }
  IL_00db:  ldarg.0
  IL_00dc:  ldc.i4.s   -2
  IL_00de:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00e3:  ldarg.0
  IL_00e4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00e9:  ldloc.1
  IL_00ea:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00ef:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_10()
        {
            var src = @"
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    public readonly Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x)
        => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(
                        ref Unsafe.AsRef(in GetC(x).F)),
                10)
           [Get01()][await FromResult(Get02(x))];

    static C GetC(C x) => x;
    static int Get01() => 0;
    static int Get02(C c)
    {
        Unsafe.AsRef(in c.F)[0][0] = 111;
        return 0;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (24,22): error CS4007: 'await' cannot be used in an expression containing the type 'System.ReadOnlySpan<Buffer10<int>>'
                //            [Get01()][await FromResult(Get02(x))];
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await FromResult(Get02(x))").WithArguments("System.ReadOnlySpan<Buffer10<int>>").WithLocation(24, 22)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_11()
        {
            var src = @"
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    public readonly Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x)
        => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<Buffer10<int>, int>(
                        ref Unsafe.AsRef(in GetC(x).F[Get01()])),
                10)
           [await FromResult(Get02(x))];

    static C GetC(C x) => x;
    static int Get01() => 0;
    static int Get02(C c)
    {
        Unsafe.AsRef(in c.F)[0][0] = 111;
        return 0;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                    // (24,13): error CS4007: 'await' cannot be used in an expression containing the type 'System.ReadOnlySpan<int>'
                    //            [await FromResult(Get02(x))];
                    Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await FromResult(Get02(x))").WithArguments("System.ReadOnlySpan<int>").WithLocation(24, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_12()
        {
            var src = @"
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    public readonly Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x)
        => GetItem(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(ref Unsafe.AsRef(in GetC(x).F)),10), Get01())[await FromResult(Get02(x))];

    static C GetC(C x) => x;
    static int Get01() => 0;
    static int Get02(C c)
    {
        System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[0][0] = 111;
        return 0;
    }

    static ref readonly Buffer10<int> GetItem(System.ReadOnlySpan<Buffer10<int>> span, int index) => ref span[index];  

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (20,12): error CS8178: 'await' cannot be used in an expression containing a call to 'Program.GetItem(ReadOnlySpan<Buffer10<int>>, int)' because it returns by reference
                //         => GetItem(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(ref Unsafe.AsRef(in GetC(x).F)),10), Get01())[await FromResult(Get02(x))];
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "GetItem(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(ref Unsafe.AsRef(in GetC(x).F)),10), Get01())").WithArguments("Program.GetItem(System.ReadOnlySpan<Buffer10<int>>, int)").WithLocation(20, 12)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Await_13()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.Write(M2().Result);
    }

    static async Task<int> M2() => M3()[await FromResult(0)];

    static Buffer10 M3()
    {
        var b = new Buffer10();
        b[0] = 111;
        return b;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public ref struct Buffer10
{
    private int _element0;
}
";
            var comp = CreateCompilation(src + InlineArrayAttributeDefinition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (11,41): error CS4007: 'await' cannot be used in an expression containing the type 'Buffer10'
                //     static async Task<int> M2() => M3()[await FromResult(0)];
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "await FromResult(0)").WithArguments("Buffer10").WithLocation(11, 41)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Index_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[^10];
    static void M2(C x) => x.F[^10] = 111;
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldc.i4.s   111
  IL_0018:  stind.i4
  IL_0019:  ret
}
");
            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,27): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static int M1(C x) => x.F[^10];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F[^10]").WithArguments("inline arrays").WithLocation(18, 27),
                // (19,28): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static void M2(C x) => x.F[^10] = 111;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F[^10]").WithArguments("inline arrays").WithLocation(19, 28)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Dynamic_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[(dynamic)0] = 111;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            // PROTOTYPE(InlineArrays): Dynamic index is always converted to 'int'. Confirm this is what we want.
            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       95 (0x5f)
  .maxstack  4
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_0015:  brtrue.s   IL_003c
  IL_0017:  ldc.i4.s   32
  IL_0019:  ldtoken    ""int""
  IL_001e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0023:  ldtoken    ""Program""
  IL_0028:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0032:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0037:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_003c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_0041:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0046:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_004b:  ldc.i4.0
  IL_004c:  box        ""int""
  IL_0051:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0056:  call       ""ref int System.Span<int>.this[int].get""
  IL_005b:  ldc.i4.s   111
  IL_005d:  stind.i4
  IL_005e:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..5];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,27): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static int M1(C x) => x.F[0];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F[0]").WithArguments("inline arrays").WithLocation(18, 27),
                // (19,40): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static System.Span<int> M2(C x) => x.F[..5];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F[..5]").WithArguments("inline arrays").WithLocation(19, 40)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x)[0] = 111;
        System.Console.Write(x.F[0]);
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..5];
    static System.Span<int> M3(C x)
    { 
        System.Span<int> y = x.F[..5];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,40): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<int> M2(C x) => x.F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(17, 40),
                // (21,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F[..5];

    public System.Span<int> M3()
    { 
        System.Span<int> y = F[..5];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,37): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<int> M2() => F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(6, 37),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(x.F[0]);
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(ref C x) => x.F[..5];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_08()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(x.F[0]);
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(ref C x)
    {
        System.Span<int> y = x.F[..5];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2()
    {
        System.Span<int> y = F[..5];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_10()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x)[0][0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_12()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;

    public System.Span<Buffer10<int>> M2() => F[..5][..3];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_13()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x)[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
    static System.Span<Buffer10<int>> M3(C x)
    { 
        System.Span<Buffer10<int>> y = x.F[..5][..3];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,50): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(17, 50),
                // (21,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_14()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public System.Span<Buffer10<int>> M2() => F[..5][..3];

    public System.Span<Buffer10<int>> M3()
    { 
        System.Span<Buffer10<int>> y = F[..5][..3];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,47): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<Buffer10<int>> M2() => F[..5][..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(6, 47),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_15()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(ref C x) => x.F[..5][..3];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_16()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<Buffer10<int>> M2() => F[..5][..3];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_17()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(ref C x)
    {
        System.Span<Buffer10<int>> y = x.F[..5][..3];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_18()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<Buffer10<int>> M2()
    {
        System.Span<Buffer10<int>> y = F[..5][..3];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_IsRValue()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[..5] = default;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x.F[..5] = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x.F[..5]").WithLocation(12, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Range_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x, ..5)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x, System.Range y) => GetBuffer(x)[GetRange(y)];
    static ref Buffer10<int> GetBuffer(C x) => ref x.F;
    static System.Range GetRange(System.Range y) => y;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                System.Index V_3,
                System.Span<int> V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref Buffer10<int> Program.GetBuffer(C)""
  IL_0006:  ldarg.1
  IL_0007:  call       ""System.Range Program.GetRange(System.Range)""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""System.Index System.Range.Start.get""
  IL_0014:  stloc.3
  IL_0015:  ldloca.s   V_3
  IL_0017:  ldc.i4.s   10
  IL_0019:  call       ""int System.Index.GetOffset(int)""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""System.Index System.Range.End.get""
  IL_0026:  stloc.3
  IL_0027:  ldloca.s   V_3
  IL_0029:  ldc.i4.s   10
  IL_002b:  call       ""int System.Index.GetOffset(int)""
  IL_0030:  ldloc.1
  IL_0031:  sub
  IL_0032:  stloc.2
  IL_0033:  ldc.i4.s   10
  IL_0035:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_003a:  stloc.s    V_4
  IL_003c:  ldloca.s   V_4
  IL_003e:  ldloc.1
  IL_003f:  ldloc.2
  IL_0040:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0045:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Await_01()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static async Task M2(C x) => x.F[..await FromResult(5)][0] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      208 (0xd0)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Span<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004e
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M2>d__2.x""
    IL_0011:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0016:  ldc.i4.5
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.2
    IL_0022:  ldloca.s   V_2
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_006a
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.2
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_0041:  ldloca.s   V_2
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__2)""
    IL_0049:  leave      IL_00cf
    IL_004e:  ldarg.0
    IL_004f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0054:  stloc.2
    IL_0055:  ldarg.0
    IL_0056:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_005b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.m1
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_006a:  ldloca.s   V_2
    IL_006c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0071:  stloc.1
    IL_0072:  ldarg.0
    IL_0073:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0078:  ldflda     ""Buffer10<int> C.F""
    IL_007d:  ldc.i4.s   10
    IL_007f:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0084:  stloc.3
    IL_0085:  ldloca.s   V_3
    IL_0087:  ldc.i4.0
    IL_0088:  ldloc.1
    IL_0089:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
    IL_008e:  stloc.3
    IL_008f:  ldloca.s   V_3
    IL_0091:  ldc.i4.0
    IL_0092:  call       ""ref int System.Span<int>.this[int].get""
    IL_0097:  ldc.i4.s   111
    IL_0099:  stind.i4
    IL_009a:  ldarg.0
    IL_009b:  ldnull
    IL_009c:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_00a1:  leave.s    IL_00bc
  }
  catch System.Exception
  {
    IL_00a3:  stloc.s    V_4
    IL_00a5:  ldarg.0
    IL_00a6:  ldc.i4.s   -2
    IL_00a8:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00ad:  ldarg.0
    IL_00ae:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00b3:  ldloc.s    V_4
    IL_00b5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ba:  leave.s    IL_00cf
  }
  IL_00bc:  ldarg.0
  IL_00bd:  ldc.i4.s   -2
  IL_00bf:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00c4:  ldarg.0
  IL_00c5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00ca:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00cf:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Await_02()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static async Task M2(C x) => x.F[await FromResult(0)..5][0] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      226 (0xe2)
  .maxstack  4
  .locals init (int V_0,
                C V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Span<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_005c
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""C Program.<M2>d__2.x""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_001e:  ldfld      ""Buffer10<int> C.F""
    IL_0023:  pop
    IL_0024:  ldc.i4.0
    IL_0025:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_002a:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_002f:  stloc.3
    IL_0030:  ldloca.s   V_3
    IL_0032:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0037:  brtrue.s   IL_0078
    IL_0039:  ldarg.0
    IL_003a:  ldc.i4.0
    IL_003b:  dup
    IL_003c:  stloc.0
    IL_003d:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0042:  ldarg.0
    IL_0043:  ldloc.3
    IL_0044:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0049:  ldarg.0
    IL_004a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_004f:  ldloca.s   V_3
    IL_0051:  ldarg.0
    IL_0052:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__2)""
    IL_0057:  leave      IL_00e1
    IL_005c:  ldarg.0
    IL_005d:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0062:  stloc.3
    IL_0063:  ldarg.0
    IL_0064:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0069:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.m1
    IL_0071:  dup
    IL_0072:  stloc.0
    IL_0073:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0078:  ldloca.s   V_3
    IL_007a:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_007f:  stloc.2
    IL_0080:  ldarg.0
    IL_0081:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0086:  ldflda     ""Buffer10<int> C.F""
    IL_008b:  ldc.i4.s   10
    IL_008d:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0092:  stloc.s    V_4
    IL_0094:  ldloca.s   V_4
    IL_0096:  ldloc.2
    IL_0097:  ldc.i4.5
    IL_0098:  ldloc.2
    IL_0099:  sub
    IL_009a:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
    IL_009f:  stloc.s    V_4
    IL_00a1:  ldloca.s   V_4
    IL_00a3:  ldc.i4.0
    IL_00a4:  call       ""ref int System.Span<int>.this[int].get""
    IL_00a9:  ldc.i4.s   111
    IL_00ab:  stind.i4
    IL_00ac:  ldarg.0
    IL_00ad:  ldnull
    IL_00ae:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_00b3:  leave.s    IL_00ce
  }
  catch System.Exception
  {
    IL_00b5:  stloc.s    V_5
    IL_00b7:  ldarg.0
    IL_00b8:  ldc.i4.s   -2
    IL_00ba:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00bf:  ldarg.0
    IL_00c0:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00c5:  ldloc.s    V_5
    IL_00c7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00cc:  leave.s    IL_00e1
  }
  IL_00ce:  ldarg.0
  IL_00cf:  ldc.i4.s   -2
  IL_00d1:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00d6:  ldarg.0
  IL_00d7:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00dc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00e1:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Await_03()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static async Task M2(C x) => x.F[await FromResult(..5)][0] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      279 (0x117)
  .maxstack  3
  .locals init (int V_0,
                C V_1,
                System.Range V_2,
                int V_3,
                int V_4,
                System.Runtime.CompilerServices.TaskAwaiter<System.Range> V_5,
                System.Index V_6,
                System.Span<int> V_7,
                System.Exception V_8)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0068
    IL_000a:  ldarg.0
    IL_000b:  ldfld      ""C Program.<M2>d__2.x""
    IL_0010:  stloc.1
    IL_0011:  ldarg.0
    IL_0012:  ldloc.1
    IL_0013:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_001e:  ldfld      ""Buffer10<int> C.F""
    IL_0023:  pop
    IL_0024:  ldc.i4.5
    IL_0025:  call       ""System.Index System.Index.op_Implicit(int)""
    IL_002a:  call       ""System.Range System.Range.EndAt(System.Index)""
    IL_002f:  call       ""System.Threading.Tasks.Task<System.Range> Program.FromResult<System.Range>(System.Range)""
    IL_0034:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<System.Range> System.Threading.Tasks.Task<System.Range>.GetAwaiter()""
    IL_0039:  stloc.s    V_5
    IL_003b:  ldloca.s   V_5
    IL_003d:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<System.Range>.IsCompleted.get""
    IL_0042:  brtrue.s   IL_0085
    IL_0044:  ldarg.0
    IL_0045:  ldc.i4.0
    IL_0046:  dup
    IL_0047:  stloc.0
    IL_0048:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_004d:  ldarg.0
    IL_004e:  ldloc.s    V_5
    IL_0050:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Range> Program.<M2>d__2.<>u__1""
    IL_0055:  ldarg.0
    IL_0056:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_005b:  ldloca.s   V_5
    IL_005d:  ldarg.0
    IL_005e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Range>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Range>, ref Program.<M2>d__2)""
    IL_0063:  leave      IL_0116
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Range> Program.<M2>d__2.<>u__1""
    IL_006e:  stloc.s    V_5
    IL_0070:  ldarg.0
    IL_0071:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Range> Program.<M2>d__2.<>u__1""
    IL_0076:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Range>""
    IL_007c:  ldarg.0
    IL_007d:  ldc.i4.m1
    IL_007e:  dup
    IL_007f:  stloc.0
    IL_0080:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0085:  ldloca.s   V_5
    IL_0087:  call       ""System.Range System.Runtime.CompilerServices.TaskAwaiter<System.Range>.GetResult()""
    IL_008c:  stloc.2
    IL_008d:  ldloca.s   V_2
    IL_008f:  call       ""System.Index System.Range.Start.get""
    IL_0094:  stloc.s    V_6
    IL_0096:  ldloca.s   V_6
    IL_0098:  ldc.i4.s   10
    IL_009a:  call       ""int System.Index.GetOffset(int)""
    IL_009f:  stloc.3
    IL_00a0:  ldloca.s   V_2
    IL_00a2:  call       ""System.Index System.Range.End.get""
    IL_00a7:  stloc.s    V_6
    IL_00a9:  ldloca.s   V_6
    IL_00ab:  ldc.i4.s   10
    IL_00ad:  call       ""int System.Index.GetOffset(int)""
    IL_00b2:  ldloc.3
    IL_00b3:  sub
    IL_00b4:  stloc.s    V_4
    IL_00b6:  ldarg.0
    IL_00b7:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_00bc:  ldflda     ""Buffer10<int> C.F""
    IL_00c1:  ldc.i4.s   10
    IL_00c3:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_00c8:  stloc.s    V_7
    IL_00ca:  ldloca.s   V_7
    IL_00cc:  ldloc.3
    IL_00cd:  ldloc.s    V_4
    IL_00cf:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
    IL_00d4:  stloc.s    V_7
    IL_00d6:  ldloca.s   V_7
    IL_00d8:  ldc.i4.0
    IL_00d9:  call       ""ref int System.Span<int>.this[int].get""
    IL_00de:  ldc.i4.s   111
    IL_00e0:  stind.i4
    IL_00e1:  ldarg.0
    IL_00e2:  ldnull
    IL_00e3:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_00e8:  leave.s    IL_0103
  }
  catch System.Exception
  {
    IL_00ea:  stloc.s    V_8
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.s   -2
    IL_00ef:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00f4:  ldarg.0
    IL_00f5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00fa:  ldloc.s    V_8
    IL_00fc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0101:  leave.s    IL_0116
  }
  IL_0103:  ldarg.0
  IL_0104:  ldc.i4.s   -2
  IL_0106:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_010b:  ldarg.0
  IL_010c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_0111:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0116:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Await_04()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static async Task M2(C x) => GetC(x).F[Get01()..Get5()][Get02()] = await FromResult(111);
    static C GetC(C x) => x;
    static int Get01() => 0;
    static int Get5() => 5;
    static int Get02() => 0;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      279 (0x117)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Span<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0085
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M2>d__2.x""
    IL_0011:  call       ""C Program.GetC(C)""
    IL_0016:  stfld      ""C Program.<M2>d__2.<>7__wrap4""
    IL_001b:  ldarg.0
    IL_001c:  ldfld      ""C Program.<M2>d__2.<>7__wrap4""
    IL_0021:  ldfld      ""Buffer10<int> C.F""
    IL_0026:  pop
    IL_0027:  call       ""int Program.Get01()""
    IL_002c:  stloc.1
    IL_002d:  ldarg.0
    IL_002e:  ldloc.1
    IL_002f:  stfld      ""int Program.<M2>d__2.<>7__wrap1""
    IL_0034:  ldarg.0
    IL_0035:  call       ""int Program.Get5()""
    IL_003a:  ldloc.1
    IL_003b:  sub
    IL_003c:  stfld      ""int Program.<M2>d__2.<>7__wrap2""
    IL_0041:  ldarg.0
    IL_0042:  call       ""int Program.Get02()""
    IL_0047:  stfld      ""int Program.<M2>d__2.<>7__wrap3""
    IL_004c:  ldc.i4.s   111
    IL_004e:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_0053:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0058:  stloc.3
    IL_0059:  ldloca.s   V_3
    IL_005b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0060:  brtrue.s   IL_00a1
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.0
    IL_0064:  dup
    IL_0065:  stloc.0
    IL_0066:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_006b:  ldarg.0
    IL_006c:  ldloc.3
    IL_006d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0072:  ldarg.0
    IL_0073:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_0078:  ldloca.s   V_3
    IL_007a:  ldarg.0
    IL_007b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__2)""
    IL_0080:  leave      IL_0116
    IL_0085:  ldarg.0
    IL_0086:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_008b:  stloc.3
    IL_008c:  ldarg.0
    IL_008d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0092:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.m1
    IL_009a:  dup
    IL_009b:  stloc.0
    IL_009c:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00a1:  ldloca.s   V_3
    IL_00a3:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00a8:  stloc.2
    IL_00a9:  ldarg.0
    IL_00aa:  ldfld      ""C Program.<M2>d__2.<>7__wrap4""
    IL_00af:  ldflda     ""Buffer10<int> C.F""
    IL_00b4:  ldc.i4.s   10
    IL_00b6:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_00bb:  stloc.s    V_4
    IL_00bd:  ldloca.s   V_4
    IL_00bf:  ldarg.0
    IL_00c0:  ldfld      ""int Program.<M2>d__2.<>7__wrap1""
    IL_00c5:  ldarg.0
    IL_00c6:  ldfld      ""int Program.<M2>d__2.<>7__wrap2""
    IL_00cb:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
    IL_00d0:  stloc.s    V_4
    IL_00d2:  ldloca.s   V_4
    IL_00d4:  ldarg.0
    IL_00d5:  ldfld      ""int Program.<M2>d__2.<>7__wrap3""
    IL_00da:  call       ""ref int System.Span<int>.this[int].get""
    IL_00df:  ldloc.2
    IL_00e0:  stind.i4
    IL_00e1:  ldarg.0
    IL_00e2:  ldnull
    IL_00e3:  stfld      ""C Program.<M2>d__2.<>7__wrap4""
    IL_00e8:  leave.s    IL_0103
  }
  catch System.Exception
  {
    IL_00ea:  stloc.s    V_5
    IL_00ec:  ldarg.0
    IL_00ed:  ldc.i4.s   -2
    IL_00ef:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00f4:  ldarg.0
    IL_00f5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00fa:  ldloc.s    V_5
    IL_00fc:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0101:  leave.s    IL_0116
  }
  IL_0103:  ldarg.0
  IL_0104:  ldc.i4.s   -2
  IL_0106:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_010b:  ldarg.0
  IL_010c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_0111:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0116:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Await_05()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => GetC(x).F[GetRange()][await FromResult(Get01(x))];

    static C GetC(C x) => x;
    static System.Range GetRange() => 0..5;
    static int Get01(C c)
    {
        System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[0] = 111;
        return 0;
    }

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      310 (0x136)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Range V_2,
                int V_3,
                int V_4,
                int V_5,
                System.Index V_6,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_7,
                System.ReadOnlySpan<int> V_8,
                System.Exception V_9)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_00ac
    IL_000d:  ldarg.0
    IL_000e:  ldarg.0
    IL_000f:  ldfld      ""C Program.<M1>d__1.x""
    IL_0014:  call       ""C Program.GetC(C)""
    IL_0019:  stfld      ""C Program.<M1>d__1.<>7__wrap3""
    IL_001e:  ldarg.0
    IL_001f:  ldfld      ""C Program.<M1>d__1.<>7__wrap3""
    IL_0024:  ldfld      ""Buffer10<int> C.F""
    IL_0029:  pop
    IL_002a:  call       ""System.Range Program.GetRange()""
    IL_002f:  stloc.2
    IL_0030:  ldloca.s   V_2
    IL_0032:  call       ""System.Index System.Range.Start.get""
    IL_0037:  stloc.s    V_6
    IL_0039:  ldloca.s   V_6
    IL_003b:  ldc.i4.s   10
    IL_003d:  call       ""int System.Index.GetOffset(int)""
    IL_0042:  stloc.3
    IL_0043:  ldloca.s   V_2
    IL_0045:  call       ""System.Index System.Range.End.get""
    IL_004a:  stloc.s    V_6
    IL_004c:  ldloca.s   V_6
    IL_004e:  ldc.i4.s   10
    IL_0050:  call       ""int System.Index.GetOffset(int)""
    IL_0055:  ldloc.3
    IL_0056:  sub
    IL_0057:  stloc.s    V_4
    IL_0059:  ldarg.0
    IL_005a:  ldloc.3
    IL_005b:  stfld      ""int Program.<M1>d__1.<>7__wrap1""
    IL_0060:  ldarg.0
    IL_0061:  ldloc.s    V_4
    IL_0063:  stfld      ""int Program.<M1>d__1.<>7__wrap2""
    IL_0068:  ldarg.0
    IL_0069:  ldfld      ""C Program.<M1>d__1.x""
    IL_006e:  call       ""int Program.Get01(C)""
    IL_0073:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_0078:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_007d:  stloc.s    V_7
    IL_007f:  ldloca.s   V_7
    IL_0081:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0086:  brtrue.s   IL_00c9
    IL_0088:  ldarg.0
    IL_0089:  ldc.i4.0
    IL_008a:  dup
    IL_008b:  stloc.0
    IL_008c:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0091:  ldarg.0
    IL_0092:  ldloc.s    V_7
    IL_0094:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_009f:  ldloca.s   V_7
    IL_00a1:  ldarg.0
    IL_00a2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M1>d__1)""
    IL_00a7:  leave      IL_0135
    IL_00ac:  ldarg.0
    IL_00ad:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_00b2:  stloc.s    V_7
    IL_00b4:  ldarg.0
    IL_00b5:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_00ba:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.m1
    IL_00c2:  dup
    IL_00c3:  stloc.0
    IL_00c4:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_00c9:  ldloca.s   V_7
    IL_00cb:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00d0:  stloc.s    V_5
    IL_00d2:  ldarg.0
    IL_00d3:  ldfld      ""C Program.<M1>d__1.<>7__wrap3""
    IL_00d8:  ldflda     ""Buffer10<int> C.F""
    IL_00dd:  ldc.i4.s   10
    IL_00df:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_00e4:  stloc.s    V_8
    IL_00e6:  ldloca.s   V_8
    IL_00e8:  ldarg.0
    IL_00e9:  ldfld      ""int Program.<M1>d__1.<>7__wrap1""
    IL_00ee:  ldarg.0
    IL_00ef:  ldfld      ""int Program.<M1>d__1.<>7__wrap2""
    IL_00f4:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
    IL_00f9:  stloc.s    V_8
    IL_00fb:  ldloca.s   V_8
    IL_00fd:  ldloc.s    V_5
    IL_00ff:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_0104:  ldind.i4
    IL_0105:  stloc.1
    IL_0106:  leave.s    IL_0121
  }
  catch System.Exception
  {
    IL_0108:  stloc.s    V_9
    IL_010a:  ldarg.0
    IL_010b:  ldc.i4.s   -2
    IL_010d:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0112:  ldarg.0
    IL_0113:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0118:  ldloc.s    V_9
    IL_011a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_011f:  leave.s    IL_0135
  }
  IL_0121:  ldarg.0
  IL_0122:  ldc.i4.s   -2
  IL_0124:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_0129:  ldarg.0
  IL_012a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_012f:  ldloc.1
  IL_0130:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_0135:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ObjectInitializer_Int_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        System.Console.Write(M2().F[0]);
    }

    static C M2() => new C() { F = {[0] = 111} };
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            // According to the language specification: "an argument_list enclosed in square brackets shall specify arguments for an accessible indexer on the object being initialized"
            // Buffer10<int> doesn't have an indexer.
            comp.VerifyDiagnostics(
                // (14,37): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //     static C M2() => new C() { F = {[0] = 111} };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0]").WithArguments("Buffer10<int>").WithLocation(14, 37)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ObjectInitializer_Int_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        System.Console.Write(M2().F[0]);
    }

    static C M2() => new C() { F = {[0] = 111} };
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;

    public T this[int i]
    {
        get => this[i];
        set => this[i] = value;
    }
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       20 (0x14)
  .maxstack  4
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldflda     ""Buffer10<int> C.F""
  IL_000b:  ldc.i4.0
  IL_000c:  ldc.i4.s   111
  IL_000e:  call       ""void Buffer10<int>.this[int].set""
  IL_0013:  ret
}
");

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ObjectInitializer_Index_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        System.Console.Write(M2().F[0]);
    }

    static C M2() => new C() { F = {[^10] = 111} };
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            // According to the language specification: "an argument_list enclosed in square brackets shall specify arguments for an accessible indexer on the object being initialized"
            // Buffer10<int> doesn't have an indexer.
            comp.VerifyDiagnostics(
                // (14,37): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //     static C M2() => new C() { F = {[^10] = 111} };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[^10]").WithArguments("Buffer10<int>").WithLocation(14, 37)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ObjectInitializer_Index_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        System.Console.Write(M2().F[0]);
    }

    static C M2() => new C() { F = {[^10] = 111} };
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;

    public T this[int i]
    {
        get => this[i];
        set => this[i] = value;
    }

    public int Length => 10;
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            // This scenario fails due to https://github.com/dotnet/roslyn/issues/67533
            comp.VerifyDiagnostics(
                // (14,37): error CS1913: Member '[^10]' cannot be initialized. It is not a field or property.
                //     static C M2() => new C() { F = {[^10] = 111} };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "[^10]").WithArguments("[^10]").WithLocation(14, 37)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ObjectInitializer_Range_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        System.Console.Write(M2().F[0]);
    }

    static C M2() => new C() { F = {[0..1] = 111} };
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            // According to the language specification: "an argument_list enclosed in square brackets shall specify arguments for an accessible indexer on the object being initialized"
            // Buffer10<int> doesn't have an indexer.
            comp.VerifyDiagnostics(
                // (14,37): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //     static C M2() => new C() { F = {[0..1] = 111} };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0..1]").WithArguments("Buffer10<int>").WithLocation(14, 37)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Variable()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[0] = 111;
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c?.F[0];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int? V_0,
                System.Span<int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Buffer10<int> C.F""
  IL_0013:  ldc.i4.s   10
  IL_0015:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldc.i4.0
  IL_001e:  call       ""ref int System.Span<int>.this[int].get""
  IL_0023:  ldind.i4
  IL_0024:  newobj     ""int?..ctor(int)""
  IL_0029:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (12,9): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         c.F[0] = 111;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "c.F[0]").WithArguments("inline arrays").WithLocation(12, 9),
                // (16,30): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static int? M2(C c) => c?.F[0];
                Diagnostic(ErrorCode.ERR_FeatureInPreview, ".F[0]").WithArguments("inline arrays").WithLocation(16, 30)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Variable()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[0] = 111;
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c?.F[..5][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (int? V_0,
                System.Span<int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Buffer10<int> C.F""
  IL_0013:  ldc.i4.s   10
  IL_0015:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_001a:  stloc.1
  IL_001b:  ldloca.s   V_1
  IL_001d:  ldc.i4.0
  IL_001e:  ldc.i4.5
  IL_001f:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0024:  stloc.1
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldc.i4.0
  IL_0028:  call       ""ref int System.Span<int>.this[int].get""
  IL_002d:  ldind.i4
  IL_002e:  newobj     ""int?..ctor(int)""
  IL_0033:  ret
}
");

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_01()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (int? V_0,
                Buffer10<int> V_1,
                System.ReadOnlySpan<int> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int>? C.F""
  IL_0006:  dup
  IL_0007:  call       ""readonly bool Buffer10<int>?.HasValue.get""
  IL_000c:  brtrue.s   IL_0019
  IL_000e:  pop
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""int?""
  IL_0017:  ldloc.0
  IL_0018:  ret
  IL_0019:  call       ""readonly Buffer10<int> Buffer10<int>?.GetValueOrDefault()""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldc.i4.s   10
  IL_0023:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_0028:  stloc.2
  IL_0029:  ldloca.s   V_2
  IL_002b:  ldc.i4.0
  IL_002c:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0031:  ldind.i4
  IL_0032:  newobj     ""int?..ctor(int)""
  IL_0037:  ret
}
");

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_02()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (int? V_0,
                Buffer10<int> V_1,
                System.ReadOnlySpan<int> V_2,
                Buffer10<int> V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int>? C.F""
  IL_0006:  dup
  IL_0007:  call       ""readonly bool Buffer10<int>?.HasValue.get""
  IL_000c:  brtrue.s   IL_0019
  IL_000e:  pop
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""int?""
  IL_0017:  ldloc.0
  IL_0018:  ret
  IL_0019:  call       ""readonly Buffer10<int> Buffer10<int>?.GetValueOrDefault()""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldc.i4.s   10
  IL_0023:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_0028:  stloc.2
  IL_0029:  ldloca.s   V_2
  IL_002b:  ldloca.s   V_3
  IL_002d:  initobj    ""Buffer10<int>""
  IL_0033:  ldloc.3
  IL_0034:  call       ""int Program.M3(Buffer10<int>)""
  IL_0039:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_003e:  ldind.i4
  IL_003f:  newobj     ""int?..ctor(int)""
  IL_0044:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (int? V_0,
                Buffer10<int> V_1,
                System.ReadOnlySpan<int> V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly bool C?.HasValue.get""
  IL_0007:  brtrue.s   IL_0013
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.0
  IL_0012:  ret
  IL_0013:  ldarga.s   V_0
  IL_0015:  call       ""readonly C C?.GetValueOrDefault()""
  IL_001a:  ldfld      ""Buffer10<int> C.F""
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldc.i4.s   10
  IL_0024:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_0029:  stloc.2
  IL_002a:  ldloca.s   V_2
  IL_002c:  ldc.i4.0
  IL_002d:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0032:  ldind.i4
  IL_0033:  newobj     ""int?..ctor(int)""
  IL_0038:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (int? V_0,
                Buffer10<int> V_1,
                System.ReadOnlySpan<int> V_2,
                Buffer10<int> V_3)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly bool C?.HasValue.get""
  IL_0007:  brtrue.s   IL_0013
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.0
  IL_0012:  ret
  IL_0013:  ldarga.s   V_0
  IL_0015:  call       ""readonly C C?.GetValueOrDefault()""
  IL_001a:  ldfld      ""Buffer10<int> C.F""
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldc.i4.s   10
  IL_0024:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_0029:  stloc.2
  IL_002a:  ldloca.s   V_2
  IL_002c:  ldloca.s   V_3
  IL_002e:  initobj    ""Buffer10<int>""
  IL_0034:  ldloc.3
  IL_0035:  call       ""int Program.M3(Buffer10<int>)""
  IL_003a:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_003f:  ldind.i4
  IL_0040:  newobj     ""int?..ctor(int)""
  IL_0045:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_01()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[..5][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C c) => c.F?[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "c.F?").WithLocation(17, 28)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_02()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[M3(default)..][M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C c) => c.F?[M3(default)..][M3(default)];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "c.F?").WithLocation(17, 28)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[..5][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C? c) => c?.F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, ".F").WithLocation(17, 31)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[M3(default)..][M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C? c) => c?.F[M3(default)..][M3(default)];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, ".F").WithLocation(17, 31)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_NotValue()
        {
            var src = @"
class Program
{
    static void Main()
    {
        _ = Buffer10<int>[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0119: 'Buffer10<int>' is a type, which is not valid in the given context
                //         _ = Buffer10<int>[0];
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Buffer10<int>").WithArguments("Buffer10<int>", "type").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_01()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3()[0], default);

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(in int x, Buffer10<int> y)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
                System.ReadOnlySpan<int> V_1,
                int V_2)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.s   10
  IL_000a:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0018:  ldind.i4
  IL_0019:  stloc.2
  IL_001a:  ldloca.s   V_2
  IL_001c:  ldloca.s   V_0
  IL_001e:  initobj    ""Buffer10<int>""
  IL_0024:  ldloc.0
  IL_0025:  call       ""int Program.M4(in int, Buffer10<int>)""
  IL_002a:  ret
}
");

            // PROTOTYPE(InlineArrays)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M3()[M4(default)];

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(Buffer10<int> y)
    {
        return 0;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
                System.ReadOnlySpan<int> V_1,
                Buffer10<int> V_2)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.s   10
  IL_000a:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldloca.s   V_2
  IL_0014:  initobj    ""Buffer10<int>""
  IL_001a:  ldloc.2
  IL_001b:  call       ""int Program.M4(Buffer10<int>)""
  IL_0020:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0025:  ldind.i4
  IL_0026:  ret
}
");

            // PROTOTYPE(InlineArrays)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_03()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        ref readonly int x = M3()[0];
        return x;
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (11,26): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref readonly int x = M3()[0];
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "x = M3()[0]").WithLocation(11, 26),
                // (11,30): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         ref readonly int x = M3()[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()[0]").WithLocation(11, 30)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_04()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        return M4(ref M3()[0]);
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (11,23): error CS1510: A ref or out value must be an assignable variable
                //         return M4(ref M3()[0]);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "M3()[0]").WithLocation(11, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_05()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        return M4(in M3()[0]);
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                    // (11,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                    //         return M4(in M3()[0]);
                    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()[0]").WithLocation(11, 22)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_06()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        return M4(M3()[0]);
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
                System.ReadOnlySpan<int> V_1,
                int V_2)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.s   10
  IL_000a:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0018:  ldind.i4
  IL_0019:  stloc.2
  IL_001a:  ldloca.s   V_2
  IL_001c:  call       ""int Program.M4(in int)""
  IL_0021:  ret
}
");

            // PROTOTYPE(InlineArrays)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_NotValue()
        {
            var src = @"
class Program
{
    static void Main()
    {
        _ = Buffer10<int>[..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0119: 'Buffer10<int>' is a type, which is not valid in the given context
                //         _ = Buffer10<int>[..5];
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Buffer10<int>").WithArguments("Buffer10<int>", "type").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Value_01()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3()[..], default);

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(System.ReadOnlySpan<int> x, Buffer10<int> y)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int M2() => M4(M3()[..], default);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()").WithLocation(9, 27)
                );

            // PROTOTYPE(InlineArrays)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_01()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c) => c.F[0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");

            // PROTOTYPE(InlineArrays)
            //            var tree = comp.SyntaxTrees.First();
            //            var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_02()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        ref readonly int x = ref c.F[0];
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");

            // PROTOTYPE(InlineArrays)
            //            var tree = comp.SyntaxTrees.First();
            //            var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_03()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        return M4(in c.F[0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  call       ""int Program.M4(in int)""
  IL_001b:  ret
}
");

            // PROTOTYPE(InlineArrays)
            //            var tree = comp.SyntaxTrees.First();
            //            var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_04()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        return M4(ref c.F[0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (23,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(23, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_05()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        ref readonly int x = ref c.F[0];
        return ref x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_06()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        return ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_07()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        ref readonly int x = ref c.F[0];
        return ref x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,20): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(24, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_08()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        return ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (23,20): error CS8167: Cannot return by reference a member of parameter 'c' because it is not a ref or out parameter
                //         return ref c.F[0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "c").WithArguments("c").WithLocation(23, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2() => F[0];
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_10()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        ref readonly int x = ref F[0];
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_11()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(in F[0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  call       ""int C.M4(in int)""
  IL_001b:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_12()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(ref F[0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (15,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F[0]").WithArguments("method", "this.get").WithLocation(15, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_13()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly ref readonly int M2()
    {
        ref readonly int x = ref F[0];
        return ref x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,20): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(16, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_14()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly ref readonly int M2()
    {
        ref readonly int x = ref F[0];
        return ref x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_15()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly ref readonly int M2()
    {
        return ref F[0];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (15,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref F[0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[0]").WithLocation(15, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_16()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly ref readonly int M2()
    {
        return ref F[0];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_17()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (18,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_18()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    readonly void Main()
    {
        F[0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (15,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F[0]").WithArguments("method", "this.get").WithLocation(15, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_01()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c) => c.F[..5];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_02()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_04()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        return M4(c.F[..]);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (23,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(c.F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(23, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_07()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(24, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_08()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c)
    {
        return c.F[..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (23,16): error CS8167: Cannot return by reference a member of parameter 'c' because it is not a ref or out parameter
                //         return c.F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "c").WithArguments("c").WithLocation(23, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly System.ReadOnlySpan<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (13,54): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public readonly System.ReadOnlySpan<int> M2() => F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(13, 54)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_10()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly System.ReadOnlySpan<int> M2()
    {
        System.ReadOnlySpan<int> x = F[..5];
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(16, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_12()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(F[..]);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (15,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(15, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_14()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly System.ReadOnlySpan<int> M2()
    {
        System.ReadOnlySpan<int> x = F[..5];
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_16()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly System.ReadOnlySpan<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_17()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[..][0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (18,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[..][0]").WithArguments("property", "this").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_18()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    readonly void Main()
    {
        F[..][0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (15,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F[..][0]").WithArguments("property", "this").WithLocation(15, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_IsRValue()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[..5] = default;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (18,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         c.F[..5] = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c.F[..5]").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c) => c.F[0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        ref readonly int x = ref c.F[0];
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(in c.F[0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  call       ""int Program.M4(in int)""
  IL_001b:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(ref c.F[0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(24, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        ref readonly int x = ref c.F[0];
        return ref x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        return ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        M2(c);
    }

    static void M2(in C c)
    {
        c.F[0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_08()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c) => c.F[0][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_09()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        ref readonly int x = ref c.F[0][0];
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_10()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(in c.F[0][0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_11()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(ref c.F[0][0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0][0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0][0]").WithArguments("method", "this.get").WithLocation(24, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_12()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        ref readonly int x = ref c.F[0][0];
        return ref x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_13()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        return ref c.F[0][0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_14()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        M2(c);
    }

    static void M2(in C c)
    {
        c.F[0][0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0][0]").WithArguments("method", "this.get").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c) => c.F[..5];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(c.F[..]);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(c.F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(24, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        M2(c);
    }

    static void M2(in C c)
    {
        c.F[..][0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[..][0]").WithArguments("property", "this").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ListPattern()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        if (x.F is [0, ..])
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (12,20): error CS8985: List patterns may not be used for a value of type 'Buffer10<int>'. No suitable 'Length' or 'Count' property was found.
                //         if (x.F is [0, ..])
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[0, ..]").WithArguments("Buffer10<int>").WithLocation(12, 20),
                // (12,20): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         if (x.F is [0, ..])
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0, ..]").WithArguments("Buffer10<int>").WithLocation(12, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NoIndex()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (12,17): error CS0443: Syntax error; value expected
                //         _ = x.F[];
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(12, 17)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void TooManyIndexes()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[0, 1];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffer): The wording is somewhat misleading. Adjust?
            comp.VerifyDiagnostics(
                // (12,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         _ = x.F[0, 1];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "x.F[0, 1]").WithArguments("Buffer10<int>").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void WrongIndexType_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[""a""];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffer): The wording is somewhat misleading. Adjust?
            comp.VerifyDiagnostics(
                // (12,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         _ = x.F["a"];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, @"x.F[""a""]").WithArguments("Buffer10<int>").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NamedIndex()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[x: 1];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(InlineArrays): Adjust wording of the message?
            comp.VerifyDiagnostics(
                // (11,13): error CS1742: An array access may not have a named argument specifier
                //         _ = x.F[x: 1];
                Diagnostic(ErrorCode.ERR_NamedArgumentForArray, "x.F[x: 1]").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void RefOutInIndex()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x, int y)
    {
        _ = x.F[ref y];
        _ = x.F[in y];
        _ = x.F[out y];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (11,21): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         _ = x.F[ref y];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "ref").WithLocation(11, 21),
                // (12,20): error CS1615: Argument 1 may not be passed with the 'in' keyword
                //         _ = x.F[in y];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "in").WithLocation(12, 20),
                // (13,21): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         _ = x.F[out y];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "out").WithLocation(13, 21)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        ref int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_03_1()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        ref readonly int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(InlineArrays): Report ErrorCode.WRN_UnassignedInternalField
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_03_2()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        ref readonly int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(InlineArrays): Report ErrorCode.WRN_UnassignedInternalField
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(in C c)
    {
        ref readonly int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(InlineArrays): Report ErrorCode.WRN_UnassignedInternalField
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_05()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.Span<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_06_1()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            // PROTOTYPE(InlineArrays): Report ErrorCode.WRN_UnassignedInternalField?
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_06_2()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,35): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public readonly Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 35)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(in C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_08()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.Span<int> x = c.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_09()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.ReadOnlySpan<int> x = c.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void DefiniteAssignment_01()
        {
            var src = @"
public struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M()
    {
        C c;
        _ = c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void DefiniteAssignment_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M()
    {
        C c;
        c.F[0] = 1;
        _ = c.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,9): error CS0170: Use of possibly unassigned field 'F'
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void DefiniteAssignment_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        F[0] = 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  initobj    ""Buffer10<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer10<int> C.F""
  IL_0012:  ldc.i4.s   10
  IL_0014:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_0019:  stloc.0
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.0
  IL_001d:  call       ""ref int System.Span<int>.this[int].get""
  IL_0022:  ldc.i4.1
  IL_0023:  stind.i4
  IL_0024:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[^10];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[^10];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Index", "GetOffset").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.MemoryMarshal.CreateSpan'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Runtime.InteropServices.MemoryMarshal", "CreateSpan").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Span`1.get_Item'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Span`1", "get_Item").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_04()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_05()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);

            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Index", "GetOffset").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Range.get_Start'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Range", "get_Start").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Range.get_End'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Range", "get_End").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.MemoryMarshal.CreateSpan'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Runtime.InteropServices.MemoryMarshal", "CreateSpan").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Span`1.Slice'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Span`1", "Slice").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_06()
        {
            var src = @"#pragma warning disable CS0649
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        _ = x.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_07()
        {
            var src = @"#pragma warning disable CS0649
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        _ = x.F[^10];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_08()
        {
            var src = @"#pragma warning disable CS0649
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        _ = x.F[^10];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Index", "GetOffset").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.AsRef'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Runtime.CompilerServices.Unsafe", "AsRef").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Runtime.InteropServices.MemoryMarshal", "CreateReadOnlySpan").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.get_Item'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.ReadOnlySpan`1", "get_Item").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_09()
        {
            var src = @"#pragma warning disable CS0649
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_10()
        {
            var src = @"#pragma warning disable CS0649
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);

            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Index", "GetOffset").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Range.get_Start'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Range", "get_Start").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Range.get_End'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Range", "get_End").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.AsRef'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Runtime.CompilerServices.Unsafe", "AsRef").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Runtime.InteropServices.MemoryMarshal", "CreateReadOnlySpan").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.Slice'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.ReadOnlySpan`1", "Slice").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_11()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = (System.Span<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_12()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = (System.ReadOnlySpan<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_13()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = (System.Span<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.MemoryMarshal.CreateSpan'
                //         _ = (System.Span<int>)x.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(System.Span<int>)x.F").WithArguments("System.Runtime.InteropServices.MemoryMarshal", "CreateSpan").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         _ = (System.Span<int>)x.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(System.Span<int>)x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_14()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = (System.ReadOnlySpan<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan'
                //         _ = (System.ReadOnlySpan<int>)x.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(System.ReadOnlySpan<int>)x.F").WithArguments("System.Runtime.InteropServices.MemoryMarshal", "CreateReadOnlySpan").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.AsRef'
                //         _ = (System.ReadOnlySpan<int>)x.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(System.ReadOnlySpan<int>)x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "AsRef").WithLocation(11, 13),
                // (11,13): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         _ = (System.ReadOnlySpan<int>)x.F;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "(System.ReadOnlySpan<int>)x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void PrivateImplementationDetails_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[0];
        _ = x.F[..5];
        _ = (System.Span<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    AssertEx.Equal("System.Span<TElement> <PrivateImplementationDetails>.InlineArrayAsSpan<TBuffer, TElement>(ref TBuffer buffer, System.Int32 length)",
                                   t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName).ToTestDisplayString());
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName));
                }).VerifyDiagnostics();

            verifier.VerifyIL(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName,
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_0006:  ldarg.1
  IL_0007:  call       ""System.Span<TElement> System.Runtime.InteropServices.MemoryMarshal.CreateSpan<TElement>(scoped ref TElement, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void PrivateImplementationDetails_02()
        {
            var src = @"#pragma warning disable CS0649
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        _ = x.F[0];
        _ = x.F[..5];
        _ = (System.ReadOnlySpan<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName));
                    AssertEx.Equal("System.ReadOnlySpan<TElement> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<TBuffer, TElement>(in TBuffer buffer, System.Int32 length)",
                                   t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName).ToTestDisplayString());
                }).VerifyDiagnostics();

            verifier.VerifyIL(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName,
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TBuffer System.Runtime.CompilerServices.Unsafe.AsRef<TBuffer>(scoped in TBuffer)""
  IL_0006:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_000b:  ldarg.1
  IL_000c:  call       ""System.ReadOnlySpan<TElement> System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan<TElement>(scoped ref TElement, int)""
  IL_0011:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullableAnalysis_01()
        {
            var src = @"
#nullable enable

class C<T>
{
    public Buffer10<T> F = default;
}

class Program
{
    static void M2(string s1, string? s2)
    {
        _ = GetC(s1).F[0].Length;
        _ = GetC(s2).F[0].Length;
        _ = GetC(s1).F[..5][0].Length;
        _ = GetC(s2).F[..5][0].Length;
    }

    static C<T> GetC<T>(T x)
    {
        var c = new C<T>();
        c.F[0] = x;
        return c;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = GetC(s2).F[0].Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "GetC(s2).F[0]").WithLocation(14, 13),
                // (16,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = GetC(s2).F[..5][0].Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "GetC(s2).F[..5][0]").WithLocation(16, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullableAnalysis_02()
        {
            var src = @"
#nullable enable

class Program
{
    static void M1(Buffer10<string?> b)
    {
        System.Span<string> x = b;
        System.ReadOnlySpan<string> y = b;
    }

    static void M2(Buffer10<string> b2)
    {
        System.Span<string> x = b2;
        System.ReadOnlySpan<string> y = b2;
    }

    static void M3(Buffer10<string?> b3)
    {
        System.Span<string?> x = b3;
        System.ReadOnlySpan<string?> y = b3;
    }

    static void M4(Buffer10<string> b4)
    {
        System.Span<string?> x = b4;
        System.ReadOnlySpan<string?> y = b4;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (8,33): warning CS8619: Nullability of reference types in value of type 'Buffer10<string?>' doesn't match target type 'Span<string>'.
                //         System.Span<string> x = b;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b").WithArguments("Buffer10<string?>", "System.Span<string>").WithLocation(8, 33),
                // (9,41): warning CS8619: Nullability of reference types in value of type 'Buffer10<string?>' doesn't match target type 'ReadOnlySpan<string>'.
                //         System.ReadOnlySpan<string> y = b;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b").WithArguments("Buffer10<string?>", "System.ReadOnlySpan<string>").WithLocation(9, 41),
                // (26,34): warning CS8619: Nullability of reference types in value of type 'Buffer10<string>' doesn't match target type 'Span<string?>'.
                //         System.Span<string?> x = b4;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b4").WithArguments("Buffer10<string>", "System.Span<string?>").WithLocation(26, 34),
                // (27,42): warning CS8619: Nullability of reference types in value of type 'Buffer10<string>' doesn't match target type 'ReadOnlySpan<string?>'.
                //         System.ReadOnlySpan<string?> y = b4;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b4").WithArguments("Buffer10<string>", "System.ReadOnlySpan<string?>").WithLocation(27, 42)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullableAnalysis_03()
        {
            var src = @"
#nullable enable

class Program
{
    static void M1(Buffer10<string?[]> b)
    {
        System.Span<string[]> x = b;
        System.ReadOnlySpan<string[]> y = b;
    }

    static void M2(Buffer10<string[]> b2)
    {
        System.Span<string[]> x = b2;
        System.ReadOnlySpan<string[]> y = b2;
    }

    static void M3(Buffer10<string?[]> b3)
    {
        System.Span<string?[]> x = b3;
        System.ReadOnlySpan<string?[]> y = b3;
    }

    static void M4(Buffer10<string[]> b4)
    {
        System.Span<string?[]> x = b4;
        System.ReadOnlySpan<string?[]> y = b4;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (8,35): warning CS8619: Nullability of reference types in value of type 'Buffer10<string?[]>' doesn't match target type 'Span<string[]>'.
                //         System.Span<string[]> x = b;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b").WithArguments("Buffer10<string?[]>", "System.Span<string[]>").WithLocation(8, 35),
                // (9,43): warning CS8619: Nullability of reference types in value of type 'Buffer10<string?[]>' doesn't match target type 'ReadOnlySpan<string[]>'.
                //         System.ReadOnlySpan<string[]> y = b;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b").WithArguments("Buffer10<string?[]>", "System.ReadOnlySpan<string[]>").WithLocation(9, 43),
                // (26,36): warning CS8619: Nullability of reference types in value of type 'Buffer10<string[]>' doesn't match target type 'Span<string?[]>'.
                //         System.Span<string?[]> x = b4;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b4").WithArguments("Buffer10<string[]>", "System.Span<string?[]>").WithLocation(26, 36),
                // (27,44): warning CS8619: Nullability of reference types in value of type 'Buffer10<string[]>' doesn't match target type 'ReadOnlySpan<string?[]>'.
                //         System.ReadOnlySpan<string?[]> y = b4;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "b4").WithArguments("Buffer10<string[]>", "System.ReadOnlySpan<string?[]>").WithLocation(27, 44)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void CompoundAssignment_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = -1;
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[0] += 111;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 110", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       29 (0x1d)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  dup
  IL_0017:  ldind.i4
  IL_0018:  ldc.i4.s   111
  IL_001a:  add
  IL_001b:  stind.i4
  IL_001c:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x)[0]);
        M2(x)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x)[0]);
    }

    static System.ReadOnlySpan<int> M1(C x) => x.F;
    static System.Span<int> M2(C x) => x.F;
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,48): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static System.ReadOnlySpan<int> M1(C x) => x.F;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F").WithArguments("inline arrays").WithLocation(18, 48),
                // (19,40): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static System.Span<int> M2(C x) => x.F;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "x.F").WithArguments("inline arrays").WithLocation(19, 40)
                );

#if false // PROTOTYPE(InlineArrays):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_03()
        {
            var src = @"
class C
{
    private Buffer10<int> F;

    public System.ReadOnlySpan<int> M1() => F;
    public System.Span<int> M2() => F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M1()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static System.Span<int> M2(C x) => x.F;
    static System.Span<int> M3(C x)
    { 
        System.Span<int> y = x.F;
        return y;
    }

    static System.ReadOnlySpan<int> M4(C x) => x.F;
    static System.ReadOnlySpan<int> M5(C x)
    { 
        System.ReadOnlySpan<int> y = x.F;
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (9,40): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<int> M2(C x) => x.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(9, 40),
                // (13,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(13, 16),
                // (16,48): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.ReadOnlySpan<int> M4(C x) => x.F;
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 48),
                // (20,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(20, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F;

    public System.Span<int> M3()
    { 
        System.Span<int> y = F;
        return y;
    }

    public System.ReadOnlySpan<int> M4() => F;

    public System.ReadOnlySpan<int> M5()
    { 
        System.ReadOnlySpan<int> y = F;
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,37): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<int> M2() => F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 37),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16),
                // (14,45): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.ReadOnlySpan<int> M4() => F;
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(14, 45),
                // (19,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(19, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(M1(ref x)[0]);
    }

    static System.ReadOnlySpan<int> M1(ref C x) => x.F;
    static System.Span<int> M2(ref C x) => x.F;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_07()
        {
            var src = @"
struct C
{
    private Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.ReadOnlySpan<int> M1() => F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2() => F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M1()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_08()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(M1(ref x)[0]);
    }

    static System.ReadOnlySpan<int> M1(ref C x)
    {
        System.ReadOnlySpan<int> y = x.F;
        return y;
    }

    static System.Span<int> M2(ref C x)
    {
        System.Span<int> y = x.F;
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_09()
        {
            var src = @"
struct C
{
    private Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.ReadOnlySpan<int> M1()
    {
        System.ReadOnlySpan<int> y = F;
        return y;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2()
    {
        System.Span<int> y = F;
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M1()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_10_1()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x)[0]);
        M2(x)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x)[0]);
    }

    static System.ReadOnlySpan<int> M1(C x) => x.F[..5][0];
    static System.Span<int> M2(C x) => x.F[..5][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_10_2()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x)[0][0]);
        M2(x)[0][0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x)[0][0]);
    }

    static System.ReadOnlySpan<Buffer10<int>> M1(C x) => ((System.ReadOnlySpan<Buffer10<int>>)x.F)[..3];
    static System.Span<Buffer10<int>> M2(C x) => ((System.Span<Buffer10<int>>)x.F)[..3];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_12_1()
        {
            var src = @"
class C
{
    private Buffer10<Buffer10<int>> F;

    public System.ReadOnlySpan<int> M1() => F[..5][0];
    public System.Span<int> M2() => F[..5][0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M1()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_12_2()
        {
            var src = @"
class C
{
    private Buffer10<Buffer10<int>> F;

    public System.ReadOnlySpan<Buffer10<int>> M1() => ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];
    public System.Span<Buffer10<int>> M2() => ((System.Span<Buffer10<int>>)F)[..3];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.M1()[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_13_1()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static System.Span<int> M2(C x) => x.F[..5][0];
    static System.Span<int> M3(C x)
    { 
        System.Span<int> y = x.F[..5][0];
        return y;
    }

    static System.ReadOnlySpan<int> M4(C x) => x.F[..5][0];
    static System.ReadOnlySpan<int> M5(C x)
    { 
        System.ReadOnlySpan<int> y = x.F[..5][0];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (9,40): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<int> M2(C x) => x.F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(9, 40),
                // (13,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(13, 16),
                // (16,48): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.ReadOnlySpan<int> M4(C x) => x.F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 48),
                // (20,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(20, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_13_2()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static System.Span<Buffer10<int>> M2(C x) => ((System.Span<Buffer10<int>>)x.F)[..3];
    static System.Span<Buffer10<int>> M3(C x)
    { 
        System.Span<Buffer10<int>> y = ((System.Span<Buffer10<int>>)x.F)[..3];
        return y;
    }

    static System.ReadOnlySpan<Buffer10<int>> M4(C x) => ((System.ReadOnlySpan<Buffer10<int>>)x.F)[..3];
    static System.ReadOnlySpan<Buffer10<int>> M5(C x)
    { 
        System.ReadOnlySpan<Buffer10<int>> y = ((System.ReadOnlySpan<Buffer10<int>>)x.F)[..3];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (9,79): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<Buffer10<int>> M2(C x) => ((System.Span<Buffer10<int>>)x.F)[..3];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(9, 79),
                // (13,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(13, 16),
                // (16,95): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.ReadOnlySpan<Buffer10<int>> M4(C x) => ((System.ReadOnlySpan<Buffer10<int>>)x.F)[..3];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 95),
                // (20,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(20, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_14_1()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public System.Span<int> M2() => F[..5][0];

    public System.Span<int> M3()
    { 
        System.Span<int> y = F[..5][0];
        return y;
    }

    public System.ReadOnlySpan<int> M4() => F[..5][0];

    public System.ReadOnlySpan<int> M5()
    { 
        System.ReadOnlySpan<int> y = F[..5][0];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,37): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<int> M2() => F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(6, 37),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16),
                // (14,45): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.ReadOnlySpan<int> M4() => F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(14, 45),
                // (19,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(19, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_14_2()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public System.Span<Buffer10<int>> M2() => ((System.Span<Buffer10<int>>)F)[..3];

    public System.Span<Buffer10<int>> M3()
    { 
        System.Span<Buffer10<int>> y = ((System.Span<Buffer10<int>>)F)[..3];
        return y;
    }

    public System.ReadOnlySpan<Buffer10<int>> M4() => ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];

    public System.ReadOnlySpan<Buffer10<int>> M5()
    { 
        System.ReadOnlySpan<Buffer10<int>> y = ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,48): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<Buffer10<int>> M2() => ((System.Span<Buffer10<int>>)F)[..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "(System.Span<Buffer10<int>>)F").WithLocation(6, 48),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16),
                // (14,56): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.ReadOnlySpan<Buffer10<int>> M4() => ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "(System.ReadOnlySpan<Buffer10<int>>)F").WithLocation(14, 56),
                // (19,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(19, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_15_1()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(M1(ref x)[0]);
    }

    static System.ReadOnlySpan<int> M1(ref C x) => x.F[..5][0];
    static System.Span<int> M2(ref C x) => x.F[..5][0];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_15_2()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0][0] = 111;
        System.Console.Write(M1(ref x)[0][0]);
    }

    static System.ReadOnlySpan<Buffer10<int>> M1(ref C x) => ((System.ReadOnlySpan<Buffer10<int>>)x.F)[..3];
    static System.Span<Buffer10<int>> M2(ref C x) => ((System.Span<Buffer10<int>>)x.F)[..3];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_16_1()
        {
            var src = @"
struct C
{
    private Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.ReadOnlySpan<int> M1() => F[..5][0];

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2() => F[..5][0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M1()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_16_2()
        {
            var src = @"
struct C
{
    private Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.ReadOnlySpan<Buffer10<int>> M1() => ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<Buffer10<int>> M2() => ((System.Span<Buffer10<int>>)F)[..3];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.M1()[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_17_1()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(M1(ref x)[0]);
    }

    static System.ReadOnlySpan<int> M1(ref C x)
    {
        System.ReadOnlySpan<int> y = x.F[..5][0];
        return y;
    }

    static System.Span<int> M2(ref C x)
    {
        System.Span<int> y = x.F[..5][0];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_17_2()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0][0] = 111;
        System.Console.Write(M1(ref x)[0][0]);
    }

    static System.ReadOnlySpan<Buffer10<int>> M1(ref C x)
    {
        System.ReadOnlySpan<Buffer10<int>> y = ((System.ReadOnlySpan<Buffer10<int>>)x.F)[..3];
        return y;
    }

    static System.Span<Buffer10<int>> M2(ref C x)
    {
        System.Span<Buffer10<int>> y = ((System.Span<Buffer10<int>>)x.F)[..3];
        return y;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_18_1()
        {
            var src = @"
struct C
{
    private Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.ReadOnlySpan<int> M1()
    {
        System.ReadOnlySpan<int> y = F[..5][0];
        return y;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2()
    {
        System.Span<int> y = F[..5][0];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M1()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_18_2()
        {
            var src = @"
struct C
{
    private Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.ReadOnlySpan<Buffer10<int>> M1()
    {
        System.ReadOnlySpan<Buffer10<int>> y = ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];
        return y;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<Buffer10<int>> M2()
    {
        System.Span<Buffer10<int>> y = ((System.Span<Buffer10<int>>)F)[..3];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.M1()[0][0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_IsRValue()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        ((System.Span<int>)x.F) = default;
        ((System.ReadOnlySpan<int>)x.F) = default;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         ((System.Span<int>)x.F) = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "(System.Span<int>)x.F").WithLocation(12, 10),
                // (13,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         ((System.ReadOnlySpan<int>)x.F) = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "(System.ReadOnlySpan<int>)x.F").WithLocation(13, 10)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_Readonly_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c) => c.F;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_Readonly_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c)
    {
        System.ReadOnlySpan<int> x = c.F;
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Variable_Readonly_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(c.F);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,19): error CS9501: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         return M4(c.F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "c.F").WithArguments("System.Span<int>").WithLocation(24, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_ReadonlyContext_01()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        ((System.Span<int>)F)[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c) => c.F;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_ReadonlyContext_04()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;
}

class Program
{
    static int M2(C c)
    {
        return M4(c.F);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (11,19): error CS9501: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         return M4(c.F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "c.F").WithArguments("System.Span<int>").WithLocation(11, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_ReadonlyContext_12()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(F);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyDiagnostics(
                // (15,19): error CS9501: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         return M4(F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "F").WithArguments("System.Span<int>").WithLocation(15, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_ReadonlyContext_14()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly System.ReadOnlySpan<int> M2()
    {
        System.ReadOnlySpan<int> x = F;
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Value_01()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3(), default);

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(System.ReadOnlySpan<int> x, Buffer10<int> y)
    {
        return x[0];
    }

    static int M5() => M6(M3(), default);

    static int M6(System.Span<int> x, Buffer10<int> y)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS9502: Cannot convert expression to 'ReadOnlySpan<int>' because it may not be passed or returned by reference
                //     static int M2() => M4(M3(), default);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported, "M3()").WithArguments("System.ReadOnlySpan<int>").WithLocation(9, 27),
                // (23,27): error CS9501: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //     static int M5() => M6(M3(), default);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "M3()").WithArguments("System.Span<int>").WithLocation(23, 27)
                );

            // PROTOTYPE(InlineArrays)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Value_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3(), default);

    static Buffer10<int>? M3() => null;

    static int M4(System.ReadOnlySpan<int>? x, Buffer10<int> y)
    {
        return 0;
    }

    static int M5() => M6(M3(), default);

    static int M6(System.Span<int>? x, Buffer10<int> y)
    {
        return 0;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>?' to 'System.ReadOnlySpan<int>?'
                //     static int M2() => M4(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>?", "System.ReadOnlySpan<int>?").WithLocation(9, 27),
                // (13,45): error CS0306: The type 'ReadOnlySpan<int>' may not be used as a type argument
                //     static int M4(System.ReadOnlySpan<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "x").WithArguments("System.ReadOnlySpan<int>").WithLocation(13, 45),
                // (18,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>?' to 'System.Span<int>?'
                //     static int M5() => M6(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>?", "System.Span<int>?").WithLocation(18, 27),
                // (20,37): error CS0306: The type 'Span<int>' may not be used as a type argument
                //     static int M6(System.Span<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "x").WithArguments("System.Span<int>").WithLocation(20, 37)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Value_03()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4((System.ReadOnlySpan<int>)M3(), default);

    static Buffer10<int>? M3() => null;

    static int M4(System.ReadOnlySpan<int> x, Buffer10<int> y)
    {
        return x[0];
    }

    static int M5() => M6((System.Span<int>)M3(), default);

    static int M6(System.Span<int> x, Buffer10<int> y)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS0030: Cannot convert type 'Buffer10<int>?' to 'System.ReadOnlySpan<int>'
                //     static int M2() => M4((System.ReadOnlySpan<int>)M3(), default);
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.ReadOnlySpan<int>)M3()").WithArguments("Buffer10<int>?", "System.ReadOnlySpan<int>").WithLocation(9, 27),
                // (18,27): error CS0030: Cannot convert type 'Buffer10<int>?' to 'System.Span<int>'
                //     static int M5() => M6((System.Span<int>)M3(), default);
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Span<int>)M3()").WithArguments("Buffer10<int>?", "System.Span<int>").WithLocation(18, 27)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Value_04()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3(), default);

    static Buffer10<int> M3() => default;

    static int M4(System.ReadOnlySpan<int>? x, Buffer10<int> y)
    {
        return 0;
    }

    static int M5() => M6(M3(), default);

    static int M6(System.Span<int>? x, Buffer10<int> y)
    {
        return 0;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            // Once we stop reporting error CS0306: The type 'ReadOnlySpan<int>' may not be used as a type argument
            // We might decide to allow a conversion from an inline array expression to nullable span types, but this scenario 
            // should still remain an error in that case because the expression is a value.
            comp.VerifyDiagnostics(
                // (9,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>' to 'System.ReadOnlySpan<int>?'
                //     static int M2() => M4(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>", "System.ReadOnlySpan<int>?").WithLocation(9, 27),
                // (13,45): error CS0306: The type 'ReadOnlySpan<int>' may not be used as a type argument
                //     static int M4(System.ReadOnlySpan<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "x").WithArguments("System.ReadOnlySpan<int>").WithLocation(13, 45),
                // (18,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>' to 'System.Span<int>?'
                //     static int M5() => M6(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>", "System.Span<int>?").WithLocation(18, 27),
                // (20,37): error CS0306: The type 'Span<int>' may not be used as a type argument
                //     static int M6(System.Span<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "x").WithArguments("System.Span<int>").WithLocation(20, 37)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Await_01()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
        M2(x).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => ((System.ReadOnlySpan<int>)x.F)[await FromResult(0)];
    static async Task M2(C x) => ((System.Span<int>)x.F)[await FromResult(0)] = 111;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      189 (0xbd)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.ReadOnlySpan<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M1>d__1.x""
    IL_0011:  stfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0016:  ldc.i4.0
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.3
    IL_0022:  ldloca.s   V_3
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.3
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0041:  ldloca.s   V_3
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M1>d__1>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M1>d__1)""
    IL_0049:  leave.s    IL_00bc
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0051:  stloc.3
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M1>d__1.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0067:  ldloca.s   V_3
    IL_0069:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006e:  stloc.2
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""C Program.<M1>d__1.<>7__wrap1""
    IL_0075:  ldflda     ""Buffer10<int> C.F""
    IL_007a:  ldc.i4.s   10
    IL_007c:  call       ""InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_0081:  stloc.s    V_4
    IL_0083:  ldloca.s   V_4
    IL_0085:  ldloc.2
    IL_0086:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
    IL_008b:  ldind.i4
    IL_008c:  stloc.1
    IL_008d:  leave.s    IL_00a8
  }
  catch System.Exception
  {
    IL_008f:  stloc.s    V_5
    IL_0091:  ldarg.0
    IL_0092:  ldc.i4.s   -2
    IL_0094:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0099:  ldarg.0
    IL_009a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_009f:  ldloc.s    V_5
    IL_00a1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a6:  leave.s    IL_00bc
  }
  IL_00a8:  ldarg.0
  IL_00a9:  ldc.i4.s   -2
  IL_00ab:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00b0:  ldarg.0
  IL_00b1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00b6:  ldloc.1
  IL_00b7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00bc:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      195 (0xc3)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Span<int> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C Program.<M2>d__2.x""
    IL_0011:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0016:  ldc.i4.0
    IL_0017:  call       ""System.Threading.Tasks.Task<int> Program.FromResult<int>(int)""
    IL_001c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0021:  stloc.2
    IL_0022:  ldloca.s   V_2
    IL_0024:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0034:  ldarg.0
    IL_0035:  ldloc.2
    IL_0036:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_003b:  ldarg.0
    IL_003c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_0041:  ldloca.s   V_2
    IL_0043:  ldarg.0
    IL_0044:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<M2>d__2>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<M2>d__2)""
    IL_0049:  leave.s    IL_00c2
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0051:  stloc.2
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> Program.<M2>d__2.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0067:  ldloca.s   V_2
    IL_0069:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_006e:  stloc.1
    IL_006f:  ldarg.0
    IL_0070:  ldfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0075:  ldflda     ""Buffer10<int> C.F""
    IL_007a:  ldc.i4.s   10
    IL_007c:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0081:  stloc.3
    IL_0082:  ldloca.s   V_3
    IL_0084:  ldloc.1
    IL_0085:  call       ""ref int System.Span<int>.this[int].get""
    IL_008a:  ldc.i4.s   111
    IL_008c:  stind.i4
    IL_008d:  ldarg.0
    IL_008e:  ldnull
    IL_008f:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0094:  leave.s    IL_00af
  }
  catch System.Exception
  {
    IL_0096:  stloc.s    V_4
    IL_0098:  ldarg.0
    IL_0099:  ldc.i4.s   -2
    IL_009b:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_00a0:  ldarg.0
    IL_00a1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_00a6:  ldloc.s    V_4
    IL_00a8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ad:  leave.s    IL_00c2
  }
  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.s   -2
  IL_00b2:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00b7:  ldarg.0
  IL_00b8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00bd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00c2:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_NotFromType_01()
        {
            var src = @"
class Program
{
    public static implicit operator Buffer10<int>(Program x) => default;

    static void Main()
    {
        var x = new Program();

        _ = (Buffer10<int>)x;
        _ = (System.Span<int>)x;
        _ = (System.ReadOnlySpan<int>)x;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (11,13): error CS0030: Cannot convert type 'Program' to 'System.Span<int>'
                //         _ = (System.Span<int>)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Span<int>)x").WithArguments("Program", "System.Span<int>").WithLocation(11, 13),
                // (12,13): error CS0030: Cannot convert type 'Program' to 'System.ReadOnlySpan<int>'
                //         _ = (System.ReadOnlySpan<int>)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.ReadOnlySpan<int>)x").WithArguments("Program", "System.ReadOnlySpan<int>").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_NotFromType_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Buffer10 b = default;
        b[0] = 111;
        System.Console.Write(((System.ReadOnlySpan<int>)b)[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10
{
    private int _element0;

    public static implicit operator System.ReadOnlySpan<int>(Buffer10 x) => new[] { -111 };
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_01()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.ReadOnlySpan<int> x) => new C() { F = x[0] };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        System.Console.Write(((C)b).F);
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (14,9): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         b[0] = 111;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "b[0]").WithArguments("inline arrays").WithLocation(14, 9),
                // (15,34): error CS8652: The feature 'inline arrays' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "b").WithArguments("inline arrays").WithLocation(15, 34)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_02()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.Span<int> x) => new C() { F = x[0] };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        System.Console.Write(((C)b).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_03()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.ReadOnlySpan<int> x) => new C() { F = x[0] + 1 };
    public static implicit operator C(System.Span<int> x) => new C() { F = x[0] - 1 };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        System.Console.Write(((C)b).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,31): error CS0457: Ambiguous user defined conversions 'C.implicit operator C(ReadOnlySpan<int>)' and 'C.implicit operator C(Span<int>)' when converting from 'Buffer10<int>' to 'C'
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(C)b").WithArguments("C.implicit operator C(System.ReadOnlySpan<int>)", "C.implicit operator C(System.Span<int>)", "Buffer10<int>", "C").WithLocation(16, 31)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_04()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.ReadOnlySpan<int> x) => new C() { F = x[0] + 1 };
    public static implicit operator C(System.Span<int> x) => new C() { F = x[0] - 1 };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        Test(in b);
    }

    static void Test(in Buffer10<int> b)
    {
        System.Console.Write(((C)b).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (21,31): error CS0457: Ambiguous user defined conversions 'C.implicit operator C(ReadOnlySpan<int>)' and 'C.implicit operator C(Span<int>)' when converting from 'Buffer10<int>' to 'C'
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(C)b").WithArguments("C.implicit operator C(System.ReadOnlySpan<int>)", "C.implicit operator C(System.Span<int>)", "Buffer10<int>", "C").WithLocation(21, 31)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_05()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.Span<int> x) => new C() { F = x[0] - 1 };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        Test(in b);
    }

    static void Test(in Buffer10<int> b)
    {
        System.Console.Write(((C)b).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (20,34): error CS9501: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "b").WithArguments("System.Span<int>").WithLocation(20, 34)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_06()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.ReadOnlySpan<int> x) => new C() { F = x[0] };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        Test(in b);
    }

    static void Test(in Buffer10<int> b)
    {
        System.Console.Write(((C)b).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_07()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.ReadOnlySpan<int> x) => new C() { F = x[0] };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        System.Console.Write(((C)new Buffer10<int>()).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (15,34): error CS9502: Cannot convert expression to 'ReadOnlySpan<int>' because it may not be passed or returned by reference
                //         System.Console.Write(((C)new Buffer10<int>()).F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported, "new Buffer10<int>()").WithArguments("System.ReadOnlySpan<int>").WithLocation(15, 34)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_Standard_08()
        {
            var src = @"
class C
{
    public int F;

    public static implicit operator C(System.Span<int> x) => new C() { F = x[0] };
}

class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 111;
        System.Console.Write(((C)new Buffer10<int>()).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (15,34): error CS9501: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         System.Console.Write(((C)new Buffer10<int>()).F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "new Buffer10<int>()").WithArguments("System.Span<int>").WithLocation(15, 34)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Conversion_ElementTypeMismatch()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        _ = (System.Span<long>)b;
        _ = (System.ReadOnlySpan<long>)b;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (7,13): error CS0030: Cannot convert type 'Buffer10<int>' to 'System.Span<long>'
                //         _ = (System.Span<long>)b;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Span<long>)b").WithArguments("Buffer10<int>", "System.Span<long>").WithLocation(7, 13),
                // (8,13): error CS0030: Cannot convert type 'Buffer10<int>' to 'System.ReadOnlySpan<long>'
                //         _ = (System.ReadOnlySpan<long>)b;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.ReadOnlySpan<long>)b").WithArguments("Buffer10<int>", "System.ReadOnlySpan<long>").WithLocation(8, 13)
                );
        }

        [Fact]
        public void AttributeDefaultValueArgument()
        {
            var source =
@"using System;
 
namespace AttributeTest
{
    [A(3, X = 6)]
    public class A : Attribute
    {
        public int X;
        public A(int x, int y = 4, object a = default(A)) { }
    
        static void Main()
        {
            typeof(A).GetCustomAttributes(false);
        }
    }
}
";
            var compilation = CreateCompilation(source);

            var a = compilation.GlobalNamespace.GetMember<MethodSymbol>("AttributeTest.A..ctor");
            // The following was causing a reentrancy into DefaultSyntaxValue on the same thread,
            // effectively blocking the thread indefinitely instead of causing a stack overflow.
            // DefaultSyntaxValue starts binding the syntax, that triggers attribute binding,
            // that asks for a default value for the same parameter and we are back where we started.
            Assert.Null(a.Parameters[2].ExplicitDefaultValue);
            Assert.True(a.Parameters[2].HasExplicitDefaultValue);

            compilation.VerifyEmitDiagnostics();
        }

        [Fact]
        public void DefaultSyntaxValueReentrancy_01()
        {
            var source =
@"
#nullable enable

[A(3, X = 6)]
public struct A
{
    public int X;

    public A(int x, System.Span<int> a = default(A)) { }
}
";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);

            var a = compilation.GlobalNamespace.GetTypeMember("A").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single();

            // The following was causing a reentrancy into DefaultSyntaxValue on the same thread,
            // effectively blocking the thread indefinitely instead of causing a stack overflow.
            // DefaultSyntaxValue starts binding the syntax, that triggers attribute binding,
            // that asks for a default value for the same parameter and we are back where we started.
            Assert.Null(a.Parameters[1].ExplicitDefaultValue);
            Assert.True(a.Parameters[1].HasExplicitDefaultValue);

            compilation.VerifyDiagnostics(
                // (4,2): error CS0616: 'A' is not an attribute class
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "A").WithArguments("A").WithLocation(4, 2),
                // (4,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A(3, X = 6)").WithLocation(4, 2),
                // (9,38): error CS1750: A value of type 'A' cannot be used as a default parameter because there are no standard conversions to type 'Span<int>'
                //     public A(int x, System.Span<int> a = default(A)) { }
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "a").WithArguments("A", "System.Span<int>").WithLocation(9, 38)
                );
        }

        [Fact]
        public void DefaultSyntaxValueReentrancy_02()
        {
            var source =
@"
#nullable enable

[A(3, X = 6)]
public struct A
{
    public int X;

    public A(int x, int a = default(A)[0]) { }
}
";
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);

            var a = compilation.GlobalNamespace.GetTypeMember("A").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single();

            // The following was causing a reentrancy into DefaultSyntaxValue on the same thread,
            // effectively blocking the thread indefinitely instead of causing a stack overflow.
            // DefaultSyntaxValue starts binding the syntax, that triggers attribute binding,
            // that asks for a default value for the same parameter and we are back where we started.
            Assert.Null(a.Parameters[1].ExplicitDefaultValue);
            Assert.True(a.Parameters[1].HasExplicitDefaultValue);

            compilation.VerifyDiagnostics(
                // (4,2): error CS0616: 'A' is not an attribute class
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_NotAnAttributeClass, "A").WithArguments("A").WithLocation(4, 2),
                // (4,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(3, X = 6)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "A(3, X = 6)").WithLocation(4, 2),
                // (9,29): error CS0021: Cannot apply indexing with [] to an expression of type 'A'
                //     public A(int x, int a = default(A)[0]) { }
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "default(A)[0]").WithArguments("A").WithLocation(9, 29)
                );
        }

        [Fact]
        public void CycleThroughAttributes_00()
        {
            var source =
@"
#pragma warning disable CS0169 // The field 'Buffer10._element0' is never used

class C
{
    public static Buffer10 F = default;
}
 
[System.Runtime.CompilerServices.InlineArray(default(Buffer10))]
public struct Buffer10
{
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (System.Span<int> length)
        {
        }
    }
}
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyDiagnostics(
                // (9,46): error CS1503: Argument 1: cannot convert from 'Buffer10' to 'System.Span<int>'
                // [System.Runtime.CompilerServices.InlineArray(default(Buffer10))]
                Diagnostic(ErrorCode.ERR_BadArgType, "default(Buffer10)").WithArguments("1", "Buffer10", "System.Span<int>").WithLocation(9, 46)
                );
        }

        [Fact]
        public void CycleThroughAttributes_01()
        {
            var source =
@"using System;
 
public class A : Attribute
{
    public A(System.Span<int> x) {}
}
 
[A(default(Buffer10))]
[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyDiagnostics(
                // (8,4): error CS1503: Argument 1: cannot convert from 'Buffer10' to 'System.Span<int>'
                // [A(default(Buffer10))]
                Diagnostic(ErrorCode.ERR_BadArgType, "default(Buffer10)").WithArguments("1", "Buffer10", "System.Span<int>").WithLocation(8, 4)
                );
        }

        [Fact]
        public void CycleThroughAttributes_02()
        {
            var source =
@"using System;
 
public class A : Attribute
{
    public A(int x) {}
}
 
[A(default(Buffer10)[0])]
[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyDiagnostics(
                // (8,4): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10'
                // [A(default(Buffer10)[0])]
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "default(Buffer10)[0]").WithArguments("Buffer10").WithLocation(8, 4)
                );
        }

        [Fact]
        public void CycleThroughAttributes_03()
        {
            var source =
@"
#pragma warning disable CS0169 // The field 'Buffer10._element0' is never used

class C
{
    public static Buffer10 F = default;
}
 
[System.Runtime.CompilerServices.InlineArray(((System.Span<int>)C.F)[0])]
public struct Buffer10
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyDiagnostics(
                // (9,47): error CS0030: Cannot convert type 'Buffer10' to 'System.Span<int>'
                // [System.Runtime.CompilerServices.InlineArray(((System.Span<int>)C.F)[0])]
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Span<int>)C.F").WithArguments("Buffer10", "System.Span<int>").WithLocation(9, 47)
                );
        }

        [Fact]
        public void CycleThroughAttributes_04()
        {
            var source =
@"
#pragma warning disable CS0169 // The field 'Buffer10._element0' is never used

class C
{
    public static Buffer10 F = default;
}
 
[System.Runtime.CompilerServices.InlineArray(C.F[0])]
public struct Buffer10
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyDiagnostics(
                // (9,46): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10'
                // [System.Runtime.CompilerServices.InlineArray(C.F[0])]
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "C.F[0]").WithArguments("Buffer10").WithLocation(9, 46)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_IndexerIsIgnored_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[0] = 111;
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;

    public T this[int i]
    {
        get => this[i];
        set => this[i] = value;
    }
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldc.i4.s   111
  IL_0018:  stind.i4
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Index_IndexerIsIgnored_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[^10];
    static void M2(C x) => x.F[^10] = 111;
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;

    public T this[int i]
    {
        get => this[i];
        set => this[i] = value;
    }

    public int Length => 10;
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldc.i4.s   111
  IL_0018:  stind.i4
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_SliceMethodIsIgnored_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..5];
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;

    public T this[int i]
    {
        get => this[i];
        set => this[i] = value;
    }

    public int Length => 10;
    public System.Span<int> Slice(int start, int length) => throw null;
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.5
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }
    }
}
