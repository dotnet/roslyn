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
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
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
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
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
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
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
        public void ElementAccess_ObjectInitializer_Int()
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

            // PROTOTYPE(InlineArrays): Should work?
            comp.VerifyDiagnostics(
                // (14,37): error CS1913: Member '[0]' cannot be initialized. It is not a field or property.
                //     static C M2() => new C() { F = {[0] = 111} };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "[0]").WithArguments("[0]").WithLocation(14, 37)
                );

#if false // PROTOTYPE(InlineArrays):
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
}
");
#endif

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
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
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
        public void AlwaysDefault_03()
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
        public void AlwaysDefault_06()
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
    }
}
