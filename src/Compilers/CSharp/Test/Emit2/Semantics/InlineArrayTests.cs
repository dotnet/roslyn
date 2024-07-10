// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    [CompilerTrait(CompilerFeature.RefLifetime)]
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
";

        public const string Buffer4Definition =
@"
[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer4<T>
{
    private T _element0;
}
";

        private static Verification VerifyOnMonoOrCoreClr
        {
            get
            {
                return ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped;
            }
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_00_LayoutAtRuntime()
        {
            var src = @"
var c = new C();
c.F = new Enclosing.Buffer[2];
c.F[0][0] = 111;
c.F[0][1] = 42;
c.F[0][2] = 43;
c.F[0][3] = 44;
c.F[1][0] = 45;
c.F[1][1] = 46;
c.F[1][2] = 47;
c.F[1][3] = 48;
System.Console.WriteLine(c.F[0][0]);
System.Console.WriteLine(c.F[0][1]);
System.Console.WriteLine(c.F[0][2]);
System.Console.WriteLine(c.F[0][3]);
System.Console.WriteLine(c.F[1][0]);
System.Console.WriteLine(c.F[1][1]);
System.Console.WriteLine(c.F[1][2]);
System.Console.WriteLine(c.F[1][3]);

class C
{
    public Enclosing.Buffer[] F;
}

public class Enclosing
{
    [System.Runtime.CompilerServices.InlineArray(4)]
    public struct Buffer
    {
        private int _element0;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var output = @"
111
42
43
44
45
46
47
48
";
            CompileAndVerify(comp, expectedOutput: output).VerifyDiagnostics();

            var t = comp.GetSpecialType(SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute);
            Assert.Equal(SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute, t.SpecialType);
            Assert.Equal(t.IsValueType, SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute.IsValueType());

            Assert.True(comp.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes));

            var vbComp = CreateVisualBasicCompilation("", referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Net80, null));
            Assert.True(vbComp.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes));
        }

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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("T", buffer.TryGetInlineArrayElementField().Type.ToTestDisplayString());

                var bufferOfInt = buffer.Construct(m.ContainingAssembly.GetSpecialType(SpecialType.System_Int32));

                Assert.True(bufferOfInt.HasInlineArrayAttribute(out length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, bufferOfInt.TryGetInlineArrayElementField().Type.SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_04_MultipleAttributes()
        {
            var src = @"
#pragma warning disable CS0436 // The type 'InlineArrayAttribute' conflicts with the imported type

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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
            }
        }

        [Fact]
        public void InlineArrayType_05_WrongLength()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(0)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (4,46): error CS9167: Inline array length must be greater than 0.
                // [System.Runtime.CompilerServices.InlineArray(0)]
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayLength, "0").WithLocation(4, 46)
                );

            verify(comp);

            var ilSource = @"
.class private sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 00 00 00 00 00 00
    )

    .field private int32 _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Fact]
        public void InlineArrayType_06_WrongLength()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(-1)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (4,46): error CS9167: Inline array length must be greater than 0.
                // [System.Runtime.CompilerServices.InlineArray(-1)]
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayLength, "-1").WithLocation(4, 46)
                );

            verify(comp);

            var ilSource = @"
.class private sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 ff ff ff ff 00 00
    )

    .field private int32 _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.False(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(0, length);
            }
        }

        [Fact]
        public void InlineArrayType_07_WrongLength()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray(-2)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (4,46): error CS9167: Inline array length must be greater than 0.
                // [System.Runtime.CompilerServices.InlineArray(-2)]
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayLength, "-2").WithLocation(4, 46)
                );

            verify(comp);

            var ilSource = @"
.class private sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 fe ff ff ff 00 00
    )

    .field private int32 _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,8): error CS9169: Inline array struct must declare one and only one instance field.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayFields, "Buffer").WithLocation(3, 8)
                );

            verify(comp);

            var ilSource = @"
.class private sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )
    // Fields
    .field private int32 _element0
    .field private int32 _element1
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,8): error CS9169: Inline array struct must declare one and only one instance field.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayFields, "Buffer").WithLocation(3, 8)
                );

            verify(comp);

            var ilSource = @"
.class private sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )
    .pack 0
    .size 1
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,8): error CS9169: Inline array struct must declare one and only one instance field.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayFields, "Buffer").WithLocation(3, 8)
                );

            verify(comp);

            var ilSource = @"
.class private sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )
    .pack 0
    .size 1

    .field private static int32 _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
            comp.VerifyDiagnostics(
                // (2,2): error CS0592: Attribute 'System.Runtime.CompilerServices.InlineArray' is not valid on this declaration type. It is only valid on 'struct' declarations.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Runtime.CompilerServices.InlineArray").WithArguments("System.Runtime.CompilerServices.InlineArray", "struct").WithLocation(2, 2)
                );

            verify(comp);

            var ilSource = @"
.class private auto ansi beforefieldinit Buffer
    extends [mscorlib]System.Object
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )

    .field private int32 _element0

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.IsClassType());
                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
            comp.VerifyDiagnostics(
                // (2,2): error CS0592: Attribute 'System.Runtime.CompilerServices.InlineArray' is not valid on this declaration type. It is only valid on 'struct' declarations.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Runtime.CompilerServices.InlineArray").WithArguments("System.Runtime.CompilerServices.InlineArray", "struct").WithLocation(2, 2)
                );

            verify(comp);

            var ilSource = @"
.class private auto ansi sealed Buffer
    extends [mscorlib]System.Enum
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )
    // Fields
    .field public specialname rtspecialname int32 value__
    .field public static literal valuetype Buffer _element0 = int32(0)

}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.IsEnumType());
                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
            comp.VerifyDiagnostics(
                // (2,2): error CS0592: Attribute 'System.Runtime.CompilerServices.InlineArray' is not valid on this declaration type. It is only valid on 'struct' declarations.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Runtime.CompilerServices.InlineArray").WithArguments("System.Runtime.CompilerServices.InlineArray", "struct").WithLocation(2, 2)
                );

            verify(comp);

            var ilSource = @"
.class private auto ansi sealed Buffer
    extends [mscorlib]System.MulticastDelegate
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            object 'object',
            native int 'method'
        ) runtime managed 
    {
    }

    .method public hidebysig newslot virtual 
        instance void Invoke () runtime managed 
    {
    }

    .method public hidebysig newslot virtual 
        instance class [mscorlib]System.IAsyncResult BeginInvoke (
            class [mscorlib]System.AsyncCallback callback,
            object 'object'
        ) runtime managed 
    {
    }

    .method public hidebysig newslot virtual 
        instance void EndInvoke (
            class [mscorlib]System.IAsyncResult result
        ) runtime managed 
    {
    }
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL("", ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.IsDelegateType());
                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
                // (2,2): error CS0592: Attribute 'System.Runtime.CompilerServices.InlineArray' is not valid on this declaration type. It is only valid on 'struct' declarations.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Runtime.CompilerServices.InlineArray").WithArguments("System.Runtime.CompilerServices.InlineArray", "struct").WithLocation(2, 2),
                // (5,17): error CS0525: Interfaces cannot contain instance fields
                //     private int _element0;
                Diagnostic(ErrorCode.ERR_InterfacesCantContainFields, "_element0").WithLocation(5, 17)
                );

            var buffer = comp.SourceAssembly.SourceModule.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_16_RefField()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
ref struct Buffer
{
    private ref int _element0;
}
";

            string consumer = @"
class C
{
    void Test(Buffer b)
    {
        _ = b[0];
    }
}
";

            var comp = CreateCompilation(consumer + src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (6,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer'
                //         _ = b[0];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "b[0]").WithArguments("Buffer").WithLocation(6, 13),
                // (13,21): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private ref int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(13, 21)
                );

            verify(comp);

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )

    .field private int32& _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL(consumer, ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics(
                // (6,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer'
                //         _ = b[0];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "b[0]").WithArguments("Buffer").WithLocation(6, 13)
                );

            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
            }
        }

        [Fact]
        public void InlineArrayType_17_RefField()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
ref struct Buffer
{
    private ref readonly int _element0;
}
";

            string consumer = @"
class C
{
    void Test(Buffer b)
    {
        _ = b[0];
    }
}
";
            var comp = CreateCompilation(consumer + src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (6,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer'
                //         _ = b[0];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "b[0]").WithArguments("Buffer").WithLocation(6, 13),
                // (13,30): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private ref readonly int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(13, 30)
                );

            verify(comp);

            var ilSource = @"
.class public sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )

    .field private int32& _element0
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
        01 00 00 00
    )
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            comp = CreateCompilationWithIL(consumer, ilSource, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics(
                // (6,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer'
                //         _ = b[0];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "b[0]").WithArguments("Buffer").WithLocation(6, 13)
                );

            verify(comp);

            void verify(CSharpCompilation comp)
            {
                var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Null(buffer.TryGetInlineArrayElementField());
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
            Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (2,46): warning CS0618: 'Buffer.Length' is obsolete: 'yes'
                // [System.Runtime.CompilerServices.InlineArray(Length)]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Length").WithArguments("Buffer.Length", "yes").WithLocation(2, 46)
                );

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (2,46): error CS0619: 'Buffer.Length' is obsolete: 'yes'
                // [System.Runtime.CompilerServices.InlineArray(Length)]
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Length").WithArguments("Buffer.Length", "yes").WithLocation(2, 46)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
        }

        [Fact]
        public void InlineArrayType_22_WrongSignature()
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
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
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
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetTypeMember("Buffer");

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);

                Assert.Equal("Field", m.GlobalNamespace.GetTypeMember("C1").GetAppliedConditionalSymbols().Single());
            }
        }

        [Fact]
        public void InlineArrayType_26()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
    public event System.Action E;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,8): error CS9169: Inline array struct must declare one and only one instance field.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayFields, "Buffer").WithLocation(3, 8)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_27()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
    int E { get; set; }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,8): error CS9169: Inline array struct must declare one and only one instance field.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayFields, "Buffer").WithLocation(3, 8)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_28_Record()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
record struct Buffer(int p)
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (2,2): error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_InlineArrayAttributeOnRecord, "System.Runtime.CompilerServices.InlineArray").WithLocation(2, 2)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_29_UseSiteError()
        {
            // [System.Runtime.CompilerServices.InlineArray(10)]
            // public struct Buffer
            // {
            //     private modreq(int32) int _element0;
            // }
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )

    .field private int32 modreq(int32) _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 08 00 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var src = @"
var x = new Buffer();
x[0] = 111;
_ = (System.Span<int>)x;
";

            var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net80);
            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);

            comp.VerifyDiagnostics(
                // (3,1): error CS0570: 'Buffer._element0' is not supported by the language
                // x[0] = 111;
                Diagnostic(ErrorCode.ERR_BindToBogus, "x[0]").WithArguments("Buffer._element0").WithLocation(3, 1),
                // (4,5): error CS0570: 'Buffer._element0' is not supported by the language
                // _ = (System.Span<int>)x;
                Diagnostic(ErrorCode.ERR_BindToBogus, "(System.Span<int>)x").WithArguments("Buffer._element0").WithLocation(4, 5)
                );
        }

        [Fact]
        public void InlineArrayType_30_UnsupportedElementType()
        {
            var src = @"
unsafe class Program
{
    static void Test()
    {
        var x = new Buffer();
        x[0] = null;
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
unsafe struct Buffer
{
    private void* _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Equal("System.Void*", buffer.TryGetInlineArrayElementField().Type.ToTestDisplayString());

            comp.VerifyDiagnostics(
                // (7,9): error CS0306: The type 'void*' may not be used as a type argument
                //         x[0] = null;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "x[0]").WithArguments("void*").WithLocation(7, 9),
                // (14,19): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private void* _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(14, 19)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_31_Nested()
        {
            var src = @"
var c = new C();
c.F[0] = 111;
System.Console.WriteLine(c.F[0]);

class C
{
    public Enclosing.Buffer F;
}

public class Enclosing
{
    [System.Runtime.CompilerServices.InlineArray(10)]
    public struct Buffer
    {
        private int _element0;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "111").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetMember<FieldSymbol>("C.F").Type;

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("System.Int32 Enclosing.Buffer._element0", buffer.TryGetInlineArrayElementField().ToTestDisplayString());
            }
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_32_Generic()
        {
            var src = @"
var c = new C();
c.F[0] = 111;
System.Console.WriteLine(c.F[0]);

class C
{
    public Enclosing<int>.Buffer F;
}

public class Enclosing<T>
{
    [System.Runtime.CompilerServices.InlineArray(10)]
    public struct Buffer
    {
        private T _element0;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "111").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetMember<FieldSymbol>("C.F").Type;

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("System.Int32 Enclosing<System.Int32>.Buffer._element0", buffer.TryGetInlineArrayElementField().ToTestDisplayString());
            }
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_33_Generic()
        {
            var src = @"
var c = new C();
c.F[0] = 111;
System.Console.WriteLine(c.F[0]);

class C
{
    public Enclosing.Buffer<int> F;
}

public class Enclosing
{
    [System.Runtime.CompilerServices.InlineArray(10)]
    public struct Buffer<T>
    {
        private T _element0;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "111").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetMember<FieldSymbol>("C.F").Type;

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("System.Int32 Enclosing.Buffer<System.Int32>._element0", buffer.TryGetInlineArrayElementField().ToTestDisplayString());
            }
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_34_Generic()
        {
            var src = @"
var c = new C();
c.F[0] = 111;
System.Console.WriteLine(c.F[0]);

class C
{
    public Enclosing<int>.Buffer<string> F;
}

public class Enclosing<T>
{
    [System.Runtime.CompilerServices.InlineArray(10)]
    public struct Buffer<U>
    {
        private T _element0;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "111").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetMember<FieldSymbol>("C.F").Type;

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("System.Int32 Enclosing<System.Int32>.Buffer<System.String>._element0", buffer.TryGetInlineArrayElementField().ToTestDisplayString());
            }
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_35_Generic()
        {
            var src = @"
var c = new C();
c.F[0] = ""111"";
System.Console.WriteLine(c.F[0]);

class C
{
    public Enclosing<int>.Buffer<string> F;
}

public class Enclosing<T>
{
    [System.Runtime.CompilerServices.InlineArray(10)]
    public struct Buffer<U>
    {
        private U _element0;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, symbolValidator: verify, sourceSymbolValidator: verify, expectedOutput: "111").VerifyDiagnostics();

            void verify(ModuleSymbol m)
            {
                var buffer = m.GlobalNamespace.GetMember<FieldSymbol>("C.F").Type;

                Assert.True(buffer.HasInlineArrayAttribute(out int length));
                Assert.Equal(10, length);
                Assert.Equal("System.String Enclosing<System.Int32>.Buffer<System.String>._element0", buffer.TryGetInlineArrayElementField().ToTestDisplayString());
            }
        }

        [Fact]
        public void InlineArrayType_36_CapturingPrimaryConstructor()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer(int p)
{
    private int _element0;

    int M() => p;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,8): error CS9169: Inline array struct must declare one and only one instance field.
                // struct Buffer(int p)
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayFields, "Buffer").WithLocation(3, 8)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_37_StructLayout_Auto()
        {
            var src = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.NotNull(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_38_StructLayout_Sequential()
        {
            var src = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics();

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.NotNull(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_39_StructLayout_Explicit()
        {
            var src = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    [FieldOffset(10)]
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (6,8): error CS9168: Inline array struct must not have explicit layout.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_InvalidInlineArrayLayout, "Buffer").WithLocation(6, 8)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.NotNull(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void InlineArrayType_40_WrongSignature()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray]
struct Buffer
{
    private int _element0;
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute ()
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
        public void InlineArrayType_41_MissingArgument()
        {
            var src = @"
#pragma warning disable CS0169 // The field 'Buffer._element0' is never used

[System.Runtime.CompilerServices.InlineArray]
struct Buffer
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (4,2): error CS7036: There is no argument given that corresponds to the required parameter 'length' of 'InlineArrayAttribute.InlineArrayAttribute(int)'
                // [System.Runtime.CompilerServices.InlineArray]
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "System.Runtime.CompilerServices.InlineArray").WithArguments("length", "System.Runtime.CompilerServices.InlineArrayAttribute.InlineArrayAttribute(int)").WithLocation(4, 2)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.False(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(0, length);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void InlineArrayType_42()
        {
            var src = @"
var b = new Buffer();
System.Console.WriteLine(b[0]);

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer
{
    private int _element0 = 111;

    public Buffer()
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            // No warning CS0414: The field 'Buffer._element0' is assigned but its value is never used
            CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [Fact]
        public void InlineArrayType_43_RequiredMember()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer
{
    required public int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (5,25): error CS9180: Inline array element field cannot be declared as required, readonly, volatile, or as a fixed size buffer.
                //     required public int _element0;
                Diagnostic(ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier, "_element0").WithLocation(5, 25)
                );
        }

        [Fact]
        public void InlineArrayType_44_RequiredMember()
        {
            // [System.Runtime.CompilerServices.InlineArray(4)]
            // public struct Buffer
            // {
            //     required public int _element0;
            // }
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 04 00 00 00 00 00
    )
    // Fields
    .field public int32 _element0
    .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredMemberAttribute::.ctor() = (
        01 00 00 00
    )
}
";

            var src = @"
#pragma warning disable CS0219 // The variable 'a' is assigned but its value is never used
var a = new Buffer();
var b = new Buffer() { _element0 = 1 };
";
            var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (3,13): error CS9035: Required member 'Buffer._element0' must be set in the object initializer or attribute constructor.
                // var a = new Buffer();
                Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "Buffer").WithArguments("Buffer._element0").WithLocation(3, 13)
                );
        }

        [Fact]
        public void InlineArrayType_45_Readonly()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer1
{
    public readonly int _element1;
}

[System.Runtime.CompilerServices.InlineArray(4)]
public readonly struct Buffer2
{
    public readonly int _element2;
}

[System.Runtime.CompilerServices.InlineArray(4)]
public readonly struct Buffer3
{
    public int _element3;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (5,25): error CS9180: Inline array element field cannot be declared as required, readonly, volatile, or as a fixed size buffer.
                //     public readonly int _element1;
                Diagnostic(ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier, "_element1").WithLocation(5, 25),
                // (11,25): error CS9180: Inline array element field cannot be declared as required, readonly, volatile, or as a fixed size buffer.
                //     public readonly int _element2;
                Diagnostic(ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier, "_element2").WithLocation(11, 25),
                // (17,16): error CS8340: Instance fields of readonly structs must be readonly.
                //     public int _element3;
                Diagnostic(ErrorCode.ERR_FieldsInRoStruct, "_element3").WithLocation(17, 16)
                );
        }

        [Fact]
        public void InlineArrayType_46_Volatile()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer1
{
    public volatile int _element1;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (5,25): error CS9180: Inline array element field cannot be declared as required, readonly, volatile, or as a fixed size buffer.
                //     public volatile int _element1;
                Diagnostic(ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier, "_element1").WithLocation(5, 25)
                );
        }

        [Fact]
        public void InlineArrayType_47_FixedSizeBuffer()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
public unsafe struct Buffer
{
    public fixed int x[5];
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (5,22): error CS9180: Inline array element field cannot be declared as required, readonly, volatile, or as a fixed size buffer.
                //     public fixed int x[5];
                Diagnostic(ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier, "x").WithLocation(5, 22)
                );
        }

        [Fact]
        public void InlineArrayType_48_FixedSizeBuffer()
        {
            // [System.Runtime.CompilerServices.InlineArray(4)]
            // public unsafe struct Buffer
            // {
            //     public fixed int x[5];
            // }
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit Buffer
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 04 00 00 00 00 00
    )

    // Nested Types
    .class nested public sequential ansi sealed beforefieldinit '<x>e__FixedBuffer'
        extends [mscorlib]System.ValueType
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [mscorlib]System.Runtime.CompilerServices.UnsafeValueTypeAttribute::.ctor() = (
            01 00 00 00
        )
        .pack 0
        .size 20

        // Fields
        .field public int32 FixedElementField

    } // end of class <x>e__FixedBuffer


    // Fields
    .field public valuetype Buffer/'<x>e__FixedBuffer' x
    .custom instance void [mscorlib]System.Runtime.CompilerServices.FixedBufferAttribute::.ctor(class [mscorlib]System.Type, int32) = (
        01 00 59 53 79 73 74 65 6D 2E 49 6E 74 33 32 2C   // ..YSystem.Int32,
        20 6D 73 63 6F 72 6C 69 62 2C 20 56 65 72 73 69   //  mscorlib, Versi
        6F 6E 3D 34 2E 30 2E 30 2E 30 2C 20 43 75 6C 74   // on=4.0.0.0, Cult
        75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75 62   // ure=neutral, Pub
        6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62 37 37 61   // licKeyToken=b77a
        35 63 35 36 31 39 33 34 65 30 38 39 05 00 00 00   // 5c561934e089....
        00 00
    )
}
";

            var src = @"
unsafe class Program
{
    static void Main()
    {
        var a = new Buffer();
        int* x = a[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (7,18): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer'
                //         int* x = a[0];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "a[0]").WithArguments("Buffer").WithLocation(7, 18)
                );
        }

        [Fact]
        public void InlineArrayType_49_UnsupportedElementType()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
unsafe struct Buffer
{
    private delegate*<void> _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (5,29): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private delegate*<void> _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(5, 29)
                );
        }

        [Fact]
        public void InlineArrayType_50_UnsupportedElementType()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
struct Buffer
{
    private System.ArgIterator _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (5,13): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     private System.ArgIterator _element0;
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(5, 13),
                // (5,32): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private System.ArgIterator _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71058")]
        public void InlineArrayType_51_RefStruct()
        {
            var src1 = @"
[System.Runtime.CompilerServices.InlineArray(10)]
public ref struct Buffer
{
    private char _element0;
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll);
            CompileAndVerify(comp1, verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped).VerifyDiagnostics(
                // (3,19): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                // public ref struct Buffer
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "Buffer").WithLocation(3, 19)
                );

            var src2 = @"
class Program
{
    static void Main()
    {
        var a = new Buffer();
        var x = a[0];
        var y1 = (System.Span<char>)a;
        var y2 = (System.ReadOnlySpan<char>)a;

        foreach (var z in a)
        {}
    }
}
";
            var comp2 = CreateCompilation(src2, references: new[] { comp1.ToMetadataReference() }, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            comp2.VerifyDiagnostics(
                // (7,17): error CS0306: The type 'Buffer' may not be used as a type argument
                //         var x = a[0];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "a[0]").WithArguments("Buffer").WithLocation(7, 17),
                // (8,18): error CS0306: The type 'Buffer' may not be used as a type argument
                //         var y1 = (System.Span<char>)a;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "(System.Span<char>)a").WithArguments("Buffer").WithLocation(8, 18),
                // (9,18): error CS0306: The type 'Buffer' may not be used as a type argument
                //         var y2 = (System.ReadOnlySpan<char>)a;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "(System.ReadOnlySpan<char>)a").WithArguments("Buffer").WithLocation(9, 18),
                // (11,27): error CS0306: The type 'Buffer' may not be used as a type argument
                //         foreach (var z in a)
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "a").WithArguments("Buffer").WithLocation(11, 27)
                );
        }

        [Fact]
        public void InlineArrayType_52_Record()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
record struct Buffer(int p1, int p2)
{
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (2,2): error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_InlineArrayAttributeOnRecord, "System.Runtime.CompilerServices.InlineArray").WithLocation(2, 2)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void InlineArrayType_53_Record(int size)
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(" + size + @")]
record struct Buffer()
{
    private int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (2,2): error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
                // [System.Runtime.CompilerServices.InlineArray(1)]
                Diagnostic(ErrorCode.ERR_InlineArrayAttributeOnRecord, "System.Runtime.CompilerServices.InlineArray").WithLocation(2, 2)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(size, length);
            Assert.Equal(SpecialType.System_Int32, buffer.TryGetInlineArrayElementField().Type.SpecialType);
        }

        [Fact]
        public void InlineArrayType_54_Record()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(10)]
record struct Buffer()
{
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (2,2): error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
                // [System.Runtime.CompilerServices.InlineArray(10)]
                Diagnostic(ErrorCode.ERR_InlineArrayAttributeOnRecord, "System.Runtime.CompilerServices.InlineArray").WithLocation(2, 2)
                );

            var buffer = comp.GlobalNamespace.GetTypeMember("Buffer");

            Assert.True(buffer.HasInlineArrayAttribute(out int length));
            Assert.Equal(10, length);
            Assert.Null(buffer.TryGetInlineArrayElementField());
        }

        [Fact]
        public void Access_ArgumentType_01()
        {
            var src = @"
class C
{
    public static implicit operator int(C x) => 0;
    public static implicit operator System.Index(C x) => 0;
    public static implicit operator System.Range(C x) => 0..;
}


class Program
{
    static void Main(Buffer10<int> b, C c)
    {
        _ = b[c];
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var c = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "c").Single();
            Assert.Equal("b[c]", c.Parent.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(c);

            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void Access_ArgumentType_02()
        {
            var src = @"
class C
{
    public static implicit operator System.Index(C x) => 0;
    public static implicit operator System.Range(C x) => 0..;
}


class Program
{
    static void Main(Buffer10<int> b, C c)
    {
        _ = b[c];
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var c = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "c").Single();
            Assert.Equal("b[c]", c.Parent.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(c);

            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Index", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void Access_ArgumentType_03()
        {
            var src = @"
class C
{
    public static implicit operator System.Range(C x) => 0..;
}


class Program
{
    static void Main(Buffer10<int> b, C c)
    {
        _ = b[c];
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var c = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "c").Single();
            Assert.Equal("b[c]", c.Parent.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(c);

            Assert.Equal("C", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Range", typeInfo.ConvertedType.ToTestDisplayString());
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldc.i4.s   111
  IL_000d:  stind.i4
  IL_000e:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,27): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static int M1(C x) => x.F[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F[0]").WithArguments("inline arrays", "12.0").WithLocation(18, 27),
                // (19,28): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static void M2(C x) => x.F[0] = 111;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F[0]").WithArguments("inline arrays", "12.0").WithLocation(19, 28)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "F").First();
            Assert.Equal("x.F[0]", f.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(f);

            Assert.Equal("Buffer10<System.Int32> C.F", symbolInfo.Symbol.ToTestDisplayString());

            var access = f.Parent.Parent;
            typeInfo = model.GetTypeInfo(access);

            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(access);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,35): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static ref int M2(C x) => ref x.F[0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 35),
                // (21,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(21, 20)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref int M2() => ref F[0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 32),
                // (11,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(11, 20)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,35): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static ref int M2(C x) => ref x.F[0][0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 35),
                // (21,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(21, 20)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref int M2() => ref F[0][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 32),
                // (11,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(11, 20)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Variable_19()
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
        System.Console.Write(' ');
        System.Console.Write(M3(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
        System.Console.Write(' ');
        System.Console.Write(M3(x));
    }

    static int M1(C x) => x.F[1];
    static void M2(C x) => x.F[1] = 111;
    static int M3(C x) { var i = 1; return x.F[i]; }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 0 111 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ldind.i4
  IL_000d:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ldc.i4.s   111
  IL_000e:  stind.i4
  IL_000f:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Variable_20()
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
        System.Console.Write(' ');
        System.Console.Write(M3(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
        System.Console.Write(' ');
        System.Console.Write(M3(x));
    }

    static int M1(C x) => x.F[9];
    static void M2(C x) => x.F[9] = 111;
    static int M3(C x) { var i = 9; return x.F[i]; }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 0 111 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   9
  IL_0008:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ldind.i4
  IL_000e:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   9
  IL_0008:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ldc.i4.s   111
  IL_000f:  stind.i4
  IL_0010:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Variable_21()
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
        System.Console.Write(M1(x, 0));
        System.Console.Write(' ');
        System.Console.Write(M3(x));
        M2(x, 0);
        System.Console.Write(' ');
        System.Console.Write(M1(x, 0));
        System.Console.Write(' ');
        System.Console.Write(M3(x));
    }

    static int M1(C x, int i) => x.F[i];
    static void M2(C x, int i) => x.F[i] = 111;
    static int M3(C x) => x.F[0];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 0 111 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldarg.1
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldarg.1
  IL_0011:  call       ""ref int System.Span<int>.this[int].get""
  IL_0016:  ldc.i4.s   111
  IL_0018:  stind.i4
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_007c:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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
    IL_007c:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_0083:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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
    IL_0083:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_007d:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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
    IL_007d:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_0076:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_007d:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_0077:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_007c:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_00ae:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_0099:  call       ""System.ReadOnlySpan<Buffer10<int>> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<Buffer10<int>>, Buffer10<int>>(in Buffer10<Buffer10<int>>, int)""
    IL_009e:  stloc.s    V_4
    IL_00a0:  ldloca.s   V_4
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      ""int Program.<M1>d__1.<>7__wrap2""
    IL_00a8:  call       ""ref readonly Buffer10<int> System.ReadOnlySpan<Buffer10<int>>.this[int].get""
    IL_00ad:  ldc.i4.s   10
    IL_00af:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (20,12): error CS4007: Instance of type 'System.ReadOnlySpan<Buffer10<int>>' cannot be preserved across 'await' or 'yield' boundary.
                //         => MemoryMarshal.CreateReadOnlySpan(
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, @"MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(
                        ref Unsafe.AsRef(in GetC(x).F)),
                10)").WithArguments("System.ReadOnlySpan<Buffer10<int>>").WithLocation(20, 12)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (20,12): error CS4007: Instance of type 'System.ReadOnlySpan<int>' cannot be preserved across 'await' or 'yield' boundary.
                //         => MemoryMarshal.CreateReadOnlySpan(
                Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, @"MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<Buffer10<int>, int>(
                        ref Unsafe.AsRef(in GetC(x).F[Get01()])),
                10)").WithArguments("System.ReadOnlySpan<int>").WithLocation(20, 12)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (20,12): error CS8178: A reference returned by a call to 'Program.GetItem(ReadOnlySpan<Buffer10<int>>, int)' cannot be preserved across 'await' or 'yield' boundary.
                //         => GetItem(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(ref Unsafe.AsRef(in GetC(x).F)),10), Get01())[await FromResult(Get02(x))];
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "GetItem(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<Buffer10<Buffer10<int>>, Buffer10<int>>(ref Unsafe.AsRef(in GetC(x).F)),10), Get01())").WithArguments("Program.GetItem(System.ReadOnlySpan<Buffer10<int>>, int)").WithLocation(20, 12)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (11,36): error CS0306: The type 'Buffer10' may not be used as a type argument
                //     static async Task<int> M2() => M3()[await FromResult(0)];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M3()[await FromResult(0)]").WithArguments("Buffer10").WithLocation(11, 36),
                // (16,9): error CS0306: The type 'Buffer10' may not be used as a type argument
                //         b[0] = 111;
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "b[0]").WithArguments("Buffer10").WithLocation(16, 9),
                // (29,19): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                // public ref struct Buffer10
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "Buffer10").WithLocation(29, 19)
                );
        }

        [Fact]
        public void ElementAccess_ExpressionTree_01()
        {
            var src = @"
using System.Linq.Expressions;

class Program
{
    static Expression<System.Func<int>> M1(Buffer10<int> x) =>
        () => x[0];

    static Expression<System.Action> M2(Buffer10<int> x) =>
        () => x[0] = 111;
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (7,15): error CS9170: An expression tree may not contain an inline array access or conversion
                //         () => x[0];
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation, "x[0]").WithLocation(7, 15),
                // (10,15): error CS0832: An expression tree may not contain an assignment operator
                //         () => x[0] = 111;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "x[0] = 111").WithLocation(10, 15),
                // (10,15): error CS9170: An expression tree may not contain an inline array access or conversion
                //         () => x[0] = 111;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation, "x[0]").WithLocation(10, 15)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Await_14_GenericMethod()
        {
            var src = @"
using System.Threading.Tasks;

class C<T>
{
    public Buffer10<T> F;
}

class Program
{
    static void Main()
    {
        var x = new C<int>();
        System.Console.Write(M1(x).Result);
        M2(x, 111).Wait();
        System.Console.Write(' ');
        System.Console.Write(M1(x).Result);
    }

    static async Task<T> M1<T>(C<T> x) => x.F[await FromResult(^10)];
    static async Task M2<T>(C<T> x, T y) => x.F[await FromResult(^10)] = y;

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      208 (0xd0)
  .maxstack  3
  .locals init (int V_0,
                T V_1,
                System.Index V_2,
                System.Runtime.CompilerServices.TaskAwaiter<System.Index> V_3,
                System.Span<T> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M1>d__1<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0052
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C<T> Program.<M1>d__1<T>.x""
    IL_0011:  stfld      ""C<T> Program.<M1>d__1<T>.<>7__wrap1""
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
    IL_0036:  stfld      ""int Program.<M1>d__1<T>.<>1__state""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.3
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M1>d__1<T>.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> Program.<M1>d__1<T>.<>t__builder""
    IL_0048:  ldloca.s   V_3
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Index>, Program.<M1>d__1<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Index>, ref Program.<M1>d__1<T>)""
    IL_0050:  leave.s    IL_00cf
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M1>d__1<T>.<>u__1""
    IL_0058:  stloc.3
    IL_0059:  ldarg.0
    IL_005a:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M1>d__1<T>.<>u__1""
    IL_005f:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Index>""
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.m1
    IL_0067:  dup
    IL_0068:  stloc.0
    IL_0069:  stfld      ""int Program.<M1>d__1<T>.<>1__state""
    IL_006e:  ldloca.s   V_3
    IL_0070:  call       ""System.Index System.Runtime.CompilerServices.TaskAwaiter<System.Index>.GetResult()""
    IL_0075:  stloc.2
    IL_0076:  ldarg.0
    IL_0077:  ldfld      ""C<T> Program.<M1>d__1<T>.<>7__wrap1""
    IL_007c:  ldflda     ""Buffer10<T> C<T>.F""
    IL_0081:  ldc.i4.s   10
    IL_0083:  call       ""System.Span<T> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<T>, T>(ref Buffer10<T>, int)""
    IL_0088:  stloc.s    V_4
    IL_008a:  ldloca.s   V_4
    IL_008c:  ldloca.s   V_2
    IL_008e:  ldc.i4.s   10
    IL_0090:  call       ""int System.Index.GetOffset(int)""
    IL_0095:  call       ""ref T System.Span<T>.this[int].get""
    IL_009a:  ldobj      ""T""
    IL_009f:  stloc.1
    IL_00a0:  leave.s    IL_00bb
  }
  catch System.Exception
  {
    IL_00a2:  stloc.s    V_5
    IL_00a4:  ldarg.0
    IL_00a5:  ldc.i4.s   -2
    IL_00a7:  stfld      ""int Program.<M1>d__1<T>.<>1__state""
    IL_00ac:  ldarg.0
    IL_00ad:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> Program.<M1>d__1<T>.<>t__builder""
    IL_00b2:  ldloc.s    V_5
    IL_00b4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetException(System.Exception)""
    IL_00b9:  leave.s    IL_00cf
  }
  IL_00bb:  ldarg.0
  IL_00bc:  ldc.i4.s   -2
  IL_00be:  stfld      ""int Program.<M1>d__1<T>.<>1__state""
  IL_00c3:  ldarg.0
  IL_00c4:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T> Program.<M1>d__1<T>.<>t__builder""
  IL_00c9:  ldloc.1
  IL_00ca:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<T>.SetResult(T)""
  IL_00cf:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2<T>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      221 (0xdd)
  .maxstack  3
  .locals init (int V_0,
                System.Index V_1,
                System.Runtime.CompilerServices.TaskAwaiter<System.Index> V_2,
                System.Span<T> V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<M2>d__2<T>.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0055
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      ""C<T> Program.<M2>d__2<T>.x""
    IL_0011:  stfld      ""C<T> Program.<M2>d__2<T>.<>7__wrap1""
    IL_0016:  ldc.i4.s   10
    IL_0018:  ldc.i4.1
    IL_0019:  newobj     ""System.Index..ctor(int, bool)""
    IL_001e:  call       ""System.Threading.Tasks.Task<System.Index> Program.FromResult<System.Index>(System.Index)""
    IL_0023:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> System.Threading.Tasks.Task<System.Index>.GetAwaiter()""
    IL_0028:  stloc.2
    IL_0029:  ldloca.s   V_2
    IL_002b:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<System.Index>.IsCompleted.get""
    IL_0030:  brtrue.s   IL_0071
    IL_0032:  ldarg.0
    IL_0033:  ldc.i4.0
    IL_0034:  dup
    IL_0035:  stloc.0
    IL_0036:  stfld      ""int Program.<M2>d__2<T>.<>1__state""
    IL_003b:  ldarg.0
    IL_003c:  ldloc.2
    IL_003d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__2<T>.<>u__1""
    IL_0042:  ldarg.0
    IL_0043:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2<T>.<>t__builder""
    IL_0048:  ldloca.s   V_2
    IL_004a:  ldarg.0
    IL_004b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<System.Index>, Program.<M2>d__2<T>>(ref System.Runtime.CompilerServices.TaskAwaiter<System.Index>, ref Program.<M2>d__2<T>)""
    IL_0050:  leave      IL_00dc
    IL_0055:  ldarg.0
    IL_0056:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__2<T>.<>u__1""
    IL_005b:  stloc.2
    IL_005c:  ldarg.0
    IL_005d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<System.Index> Program.<M2>d__2<T>.<>u__1""
    IL_0062:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<System.Index>""
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.0
    IL_006c:  stfld      ""int Program.<M2>d__2<T>.<>1__state""
    IL_0071:  ldloca.s   V_2
    IL_0073:  call       ""System.Index System.Runtime.CompilerServices.TaskAwaiter<System.Index>.GetResult()""
    IL_0078:  stloc.1
    IL_0079:  ldarg.0
    IL_007a:  ldfld      ""C<T> Program.<M2>d__2<T>.<>7__wrap1""
    IL_007f:  ldflda     ""Buffer10<T> C<T>.F""
    IL_0084:  ldc.i4.s   10
    IL_0086:  call       ""System.Span<T> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<T>, T>(ref Buffer10<T>, int)""
    IL_008b:  stloc.3
    IL_008c:  ldloca.s   V_3
    IL_008e:  ldloca.s   V_1
    IL_0090:  ldc.i4.s   10
    IL_0092:  call       ""int System.Index.GetOffset(int)""
    IL_0097:  call       ""ref T System.Span<T>.this[int].get""
    IL_009c:  ldarg.0
    IL_009d:  ldfld      ""T Program.<M2>d__2<T>.y""
    IL_00a2:  stobj      ""T""
    IL_00a7:  ldarg.0
    IL_00a8:  ldnull
    IL_00a9:  stfld      ""C<T> Program.<M2>d__2<T>.<>7__wrap1""
    IL_00ae:  leave.s    IL_00c9
  }
  catch System.Exception
  {
    IL_00b0:  stloc.s    V_4
    IL_00b2:  ldarg.0
    IL_00b3:  ldc.i4.s   -2
    IL_00b5:  stfld      ""int Program.<M2>d__2<T>.<>1__state""
    IL_00ba:  ldarg.0
    IL_00bb:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2<T>.<>t__builder""
    IL_00c0:  ldloc.s    V_4
    IL_00c2:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00c7:  leave.s    IL_00dc
  }
  IL_00c9:  ldarg.0
  IL_00ca:  ldc.i4.s   -2
  IL_00cc:  stfld      ""int Program.<M2>d__2<T>.<>1__state""
  IL_00d1:  ldarg.0
  IL_00d2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2<T>.<>t__builder""
  IL_00d7:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00dc:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Await_15()
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

    static async Task<int> M1(C x) => GetInt(ref x.F[1], await FromResult(0));
    static async Task M2(C x) => x.F[1] = await FromResult(111);

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }

    static int GetInt(ref int x, int y) => x;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      183 (0xb7)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
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
    IL_0049:  leave.s    IL_00b6
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
    IL_007a:  ldc.i4.1
    IL_007b:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0080:  ldloc.2
    IL_0081:  call       ""int Program.GetInt(ref int, int)""
    IL_0086:  stloc.1
    IL_0087:  leave.s    IL_00a2
  }
  catch System.Exception
  {
    IL_0089:  stloc.s    V_4
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.s   -2
    IL_008e:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0099:  ldloc.s    V_4
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a0:  leave.s    IL_00b6
  }
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   -2
  IL_00a5:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00aa:  ldarg.0
  IL_00ab:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00b0:  ldloc.1
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b6:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      183 (0xb7)
  .maxstack  3
  .locals init (int V_0,
            int V_1,
            System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
            System.Exception V_3)
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
    IL_0016:  ldc.i4.s   111
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
    IL_004a:  leave.s    IL_00b6
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
    IL_007b:  ldc.i4.1
    IL_007c:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer10<int>, int>(ref Buffer10<int>, int)""
    IL_0081:  ldloc.1
    IL_0082:  stind.i4
    IL_0083:  ldarg.0
    IL_0084:  ldnull
    IL_0085:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_008a:  leave.s    IL_00a3
  }
  catch System.Exception
  {
    IL_008c:  stloc.3
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.s   -2
    IL_0090:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0095:  ldarg.0
    IL_0096:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_009b:  ldloc.3
    IL_009c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a1:  leave.s    IL_00b6
  }
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.s   -2
  IL_00a6:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00ab:  ldarg.0
  IL_00ac:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b6:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Await_16()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F[1] = 111;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x).Result);
    }

    static async Task<int> M1(C x) => GetInt(in x.F[1], await FromResult(0));

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }

    static int GetInt(in int x, int y) => x;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      183 (0xb7)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
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
    IL_0049:  leave.s    IL_00b6
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
    IL_007a:  ldc.i4.1
    IL_007b:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>, int)""
    IL_0080:  ldloc.2
    IL_0081:  call       ""int Program.GetInt(in int, int)""
    IL_0086:  stloc.1
    IL_0087:  leave.s    IL_00a2
  }
  catch System.Exception
  {
    IL_0089:  stloc.s    V_4
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.s   -2
    IL_008e:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0093:  ldarg.0
    IL_0094:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0099:  ldloc.s    V_4
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_00a0:  leave.s    IL_00b6
  }
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   -2
  IL_00a5:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00aa:  ldarg.0
  IL_00ab:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00b0:  ldloc.1
  IL_00b1:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b6:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Await_17()
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

    static async Task<int> M1(C x) => GetInt(ref x.F[0], await FromResult(0));
    static async Task M2(C x) => x.F[0] = await FromResult(111);

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }

    static int GetInt(ref int x, int y) => x;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
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
    IL_0049:  leave.s    IL_00b5
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
    IL_007a:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
    IL_007f:  ldloc.2
    IL_0080:  call       ""int Program.GetInt(ref int, int)""
    IL_0085:  stloc.1
    IL_0086:  leave.s    IL_00a1
  }
  catch System.Exception
  {
    IL_0088:  stloc.s    V_4
    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.s   -2
    IL_008d:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0092:  ldarg.0
    IL_0093:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0098:  ldloc.s    V_4
    IL_009a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_009f:  leave.s    IL_00b5
  }
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00af:  ldloc.1
  IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b5:  ret
}
");

            verifier.VerifyIL("Program.<M2>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
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
    IL_0016:  ldc.i4.s   111
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
    IL_004a:  leave.s    IL_00b5
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
    IL_007b:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
    IL_0080:  ldloc.1
    IL_0081:  stind.i4
    IL_0082:  ldarg.0
    IL_0083:  ldnull
    IL_0084:  stfld      ""C Program.<M2>d__2.<>7__wrap1""
    IL_0089:  leave.s    IL_00a2
  }
  catch System.Exception
  {
    IL_008b:  stloc.3
    IL_008c:  ldarg.0
    IL_008d:  ldc.i4.s   -2
    IL_008f:  stfld      ""int Program.<M2>d__2.<>1__state""
    IL_0094:  ldarg.0
    IL_0095:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
    IL_009a:  ldloc.3
    IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a0:  leave.s    IL_00b5
  }
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.s   -2
  IL_00a5:  stfld      ""int Program.<M2>d__2.<>1__state""
  IL_00aa:  ldarg.0
  IL_00ab:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M2>d__2.<>t__builder""
  IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b5:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Await_18()
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

    static async Task<int> M1(C x) => GetInt(in x.F[0], await FromResult(0));

    static async Task<T> FromResult<T>(T r)
    {
        await Task.Yield();
        await Task.Delay(2);
        return await Task.FromResult(r);
    }

    static int GetInt(in int x, int y) => x;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.<M1>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      182 (0xb6)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                int V_2,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_3,
                System.Exception V_4)
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
    IL_0049:  leave.s    IL_00b5
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
    IL_007a:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
    IL_007f:  ldloc.2
    IL_0080:  call       ""int Program.GetInt(in int, int)""
    IL_0085:  stloc.1
    IL_0086:  leave.s    IL_00a1
  }
  catch System.Exception
  {
    IL_0088:  stloc.s    V_4
    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.s   -2
    IL_008d:  stfld      ""int Program.<M1>d__1.<>1__state""
    IL_0092:  ldarg.0
    IL_0093:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
    IL_0098:  ldloc.s    V_4
    IL_009a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetException(System.Exception)""
    IL_009f:  leave.s    IL_00b5
  }
  IL_00a1:  ldarg.0
  IL_00a2:  ldc.i4.s   -2
  IL_00a4:  stfld      ""int Program.<M1>d__1.<>1__state""
  IL_00a9:  ldarg.0
  IL_00aa:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int> Program.<M1>d__1.<>t__builder""
  IL_00af:  ldloc.1
  IL_00b0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<int>.SetResult(int)""
  IL_00b5:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldc.i4.s   111
  IL_000d:  stind.i4
  IL_000e:  ret
}
");
            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,27): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static int M1(C x) => x.F[^10];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F[^10]").WithArguments("inline arrays", "12.0").WithLocation(18, 27),
                // (19,28): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static void M2(C x) => x.F[^10] = 111;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F[^10]").WithArguments("inline arrays", "12.0").WithLocation(19, 28)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "F").First();
            Assert.Equal("x.F[^10]", f.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(f);

            Assert.Equal("Buffer10<System.Int32> C.F", symbolInfo.Symbol.ToTestDisplayString());
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
    static void M3(int[] x) => x[(dynamic)0] = 111;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            // Dynamic index is always converted to 'int'. This behavior is consistent with specification and
            // with behavior around regular arrays (see IL for Program.M3 below).
            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       95 (0x5f)
  .maxstack  4
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

            verifier.VerifyIL("Program.M3",
@"
{
  // Code size       75 (0x4b)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__3.<>p__0""
  IL_0006:  brtrue.s   IL_002d
  IL_0008:  ldc.i4.s   32
  IL_000a:  ldtoken    ""int""
  IL_000f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0014:  ldtoken    ""Program""
  IL_0019:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0023:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0028:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__3.<>p__0""
  IL_002d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__3.<>p__0""
  IL_0032:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0037:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__3.<>p__0""
  IL_003c:  ldc.i4.0
  IL_003d:  box        ""int""
  IL_0042:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0047:  ldc.i4.s   111
  IL_0049:  stelem.i4
  IL_004a:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
        System.Console.Write(M2(x).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..5];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 5 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (20,27): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static int M1(C x) => x.F[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F[0]").WithArguments("inline arrays", "12.0").WithLocation(20, 27),
                // (21,40): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static System.Span<int> M2(C x) => x.F[..5];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F[..5]").WithArguments("inline arrays", "12.0").WithLocation(21, 40)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "F").Last();
            Assert.Equal("x.F[..5]", f.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(f);

            Assert.Equal("Buffer10<System.Int32> C.F", symbolInfo.Symbol.ToTestDisplayString());

            var access = f.Parent.Parent;
            typeInfo = model.GetTypeInfo(access);

            Assert.Equal("System.Span<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(access);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Slice_Variable_03([CombinatorialValues("..10", "0..", "..^0", "^10..")] string range)
        {
            var src = @"
class C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F[" + range + @"];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.M2().Length);
        System.Console.Write(' ');
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "10 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,40): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<int> M2(C x) => x.F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(17, 40),
                // (21,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,37): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<int> M2() => F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 37),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,50): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(17, 50),
                // (21,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,47): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<Buffer10<int>> M2() => F[..5][..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 47),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void Slice_Variable_19()
        {
            var src = @"
class C
{
    void M(Buffer10<char> a1, System.Range i1, System.Span<char> result1)
    {
        result1 = a1[i1];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            // Ref safety error is unexpected. Likely an artifact of https://github.com/dotnet/roslyn/issues/68372
            comp.VerifyDiagnostics(
                // (6,19): error CS8166: Cannot return a parameter by reference 'a1' because it is not a ref parameter
                //         result1 = a1[i1];
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "a1").WithArguments("a1").WithLocation(6, 19)
                );
        }

        [Fact]
        public void Slice_Variable_20()
        {
            var src = @"
class C
{
    void M(Buffer10<char> a1, System.Range i1)
    {
        System.Span<char> result1;
        result1 = a1[i1];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            // Ref safety error is unexpected. Likely an artifact of https://github.com/dotnet/roslyn/issues/68372
            comp.VerifyDiagnostics(
                // (7,19): error CS8166: Cannot return a parameter by reference 'a1' because it is not a ref parameter
                //         result1 = a1[i1];
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "a1").WithArguments("a1").WithLocation(7, 19)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_21()
        {
            var src = @"
class C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F[..0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(x.M2().Length);
        System.Console.Write(' ');
        var r = ..0;
        System.Console.Write(x.F[r].Length);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_22()
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
        System.Console.Write(M2(x).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[1];
    static System.Span<int> M2(C x) => x.F[1..5];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 4 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.4
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_23()
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
        M2(x, 5)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M2(x, 5).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x, int y) => x.F[0..y];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 5 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldarg.1
  IL_0012:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_24()
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
        M2(x, 0)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M2(x, 0).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x, int y) => x.F[y..5];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 5 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       28 (0x1c)
  .maxstack  4
  .locals init (int V_0,
                System.Span<int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldarg.1
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.s   10
  IL_000a:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.5
  IL_0014:  ldloc.0
  IL_0015:  sub
  IL_0016:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_001b:  ret
}
");
        }

        [ConditionalTheory(typeof(CoreClrOnly))]
        [CombinatorialData]
        public void Slice_Variable_25([CombinatorialValues("1..", "^9..")] string range)
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
        System.Console.Write(M2(x).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[1];
    static System.Span<int> M2(C x) => x.F[" + range + @"];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 9 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.s   9
  IL_0013:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0018:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_26()
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
        M2(x, 0)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M2(x, 0).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x, int y) => x.F[y..];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 10 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       29 (0x1d)
  .maxstack  4
  .locals init (int V_0,
                System.Span<int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldarg.1
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.s   10
  IL_000a:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.s   10
  IL_0015:  ldloc.0
  IL_0016:  sub
  IL_0017:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_001c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_27()
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
        System.Console.Write(M2(x).Length);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..^1];
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 9 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
      // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   9
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x.F[..5] = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x.F[..5]").WithLocation(12, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
  IL_0035:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_003a:  stloc.s    V_4
  IL_003c:  ldloca.s   V_4
  IL_003e:  ldloc.1
  IL_003f:  ldloc.2
  IL_0040:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0045:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Range_Variable_Readonly_01()
        {
            var src = @"
class C
{
    readonly public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Runtime.CompilerServices.Unsafe.AsRef(in M2(x, ..5)[0]) = 111;
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.ReadOnlySpan<int> M2(C x, System.Range y) => GetBuffer(x)[GetRange(y)];
    static ref readonly Buffer10<int> GetBuffer(C x) => ref x.F;
    static System.Range GetRange(System.Range y) => y;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                System.Index V_3,
                System.ReadOnlySpan<int> V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref readonly Buffer10<int> Program.GetBuffer(C)""
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
  IL_0035:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_003a:  stloc.s    V_4
  IL_003c:  ldloca.s   V_4
  IL_003e:  ldloc.1
  IL_003f:  ldloc.2
  IL_0040:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0045:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_007f:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_008d:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_00c3:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_00b6:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_00df:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // According to the language specification: "an argument_list enclosed in square brackets shall specify arguments for an accessible indexer on the object being initialized"
            // Buffer10<int> doesn't have an indexer.
            comp.VerifyDiagnostics(
                // (14,37): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //     static C M2() => new C() { F = {[0] = 111} };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0]").WithArguments("Buffer10<int>").WithLocation(14, 37)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Last().Left;
            Assert.Equal("[0]", f.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.True(typeInfo.Type.IsErrorType());

            var symbolInfo = model.GetSymbolInfo(f);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics(
                // (22,14): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public T this[int i]
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(22, 14)
                );

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
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // According to the language specification: "an argument_list enclosed in square brackets shall specify arguments for an accessible indexer on the object being initialized"
            // Buffer10<int> doesn't have an indexer.
            comp.VerifyDiagnostics(
                // (14,37): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //     static C M2() => new C() { F = {[^10] = 111} };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[^10]").WithArguments("Buffer10<int>").WithLocation(14, 37)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (22,14): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public T this[int i]
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(22, 14)
                );

            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Skipped);
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // According to the language specification: "an argument_list enclosed in square brackets shall specify arguments for an accessible indexer on the object being initialized"
            // Buffer10<int> doesn't have an indexer.
            comp.VerifyDiagnostics(
                // (14,37): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //     static C M2() => new C() { F = {[0..1] = 111} };
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0..1]").WithArguments("Buffer10<int>").WithLocation(14, 37)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Buffer10<int> C.F""
  IL_0013:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_0018:  ldind.i4
  IL_0019:  newobj     ""int?..ctor(int)""
  IL_001e:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (12,9): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         c.F[0] = 111;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "c.F[0]").WithArguments("inline arrays", "12.0").WithLocation(12, 9),
                // (16,30): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static int? M2(C c) => c?.F[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, ".F[0]").WithArguments("inline arrays", "12.0").WithLocation(16, 30)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       41 (0x29)
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
  IL_0013:  ldc.i4.5
  IL_0014:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_0019:  stloc.1
  IL_001a:  ldloca.s   V_1
  IL_001c:  ldc.i4.0
  IL_001d:  call       ""ref int System.Span<int>.this[int].get""
  IL_0022:  ldind.i4
  IL_0023:  newobj     ""int?..ctor(int)""
  IL_0028:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (int? V_0,
                Buffer10<int> V_1)
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
  IL_0021:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_0026:  ldind.i4
  IL_0027:  newobj     ""int?..ctor(int)""
  IL_002c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
  IL_0023:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (int? V_0,
                Buffer10<int> V_1)
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
  IL_0022:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_0027:  ldind.i4
  IL_0028:  newobj     ""int?..ctor(int)""
  IL_002d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
  IL_0024:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C c) => c.F?[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "c.F?").WithLocation(17, 28)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C c) => c.F?[M3(default)..][M3(default)];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "c.F?").WithLocation(17, 28)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C? c) => c?.F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, ".F").WithLocation(17, 31)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C? c) => c?.F[M3(default)..][M3(default)];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, ".F").WithLocation(17, 31)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0119: 'Buffer10<int>' is a type, which is not valid in the given context
                //         _ = Buffer10<int>[0];
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Buffer10<int>").WithArguments("Buffer10<int>", "type").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
                int V_1)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000d:  ldind.i4
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  ldloca.s   V_0
  IL_0013:  initobj    ""Buffer10<int>""
  IL_0019:  ldloc.0
  IL_001a:  call       ""int Program.M4(in int, Buffer10<int>)""
  IL_001f:  ret
}
");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m3 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "M3").Single().Parent;
            Assert.Equal("M3()[0]", m3.Parent.ToString());

            var typeInfo = model.GetTypeInfo(m3);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(m3);

            Assert.Equal("Buffer10<System.Int32> Program.M3()", symbolInfo.Symbol.ToTestDisplayString());

            var access = m3.Parent;
            typeInfo = model.GetTypeInfo(access);

            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(access);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
  IL_000a:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (11,26): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref readonly int x = M3()[0];
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "x = M3()[0]").WithLocation(11, 26),
                // (11,30): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         ref readonly int x = M3()[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()[0]").WithLocation(11, 30)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (11,23): error CS1510: A ref or out value must be an assignable variable
                //         return M4(ref M3()[0]);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "M3()[0]").WithLocation(11, 23)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                    // (11,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                    //         return M4(in M3()[0]);
                    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()[0]").WithLocation(11, 22)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (Buffer10<int> V_0,
                int V_1)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000d:  ldind.i4
  IL_000e:  stloc.1
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       ""int Program.M4(in int)""
  IL_0016:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Value_07()
        {
            var src = @"
class Program
{
    static System.Span<int> M1()
    {
        System.Span<int> x = stackalloc int[2];
        return M3(x)[0];
    }

    static System.Span<int> M2()
    {
        System.Span<int> x = stackalloc int[2];
        var y = M3(x)[0];
        return y;
    }

    static Buffer10 M3(System.Span<int> x)
    {
        throw null;
    }

    static System.Span<int> M1(System.Span<int> xx)
    {
        return M3(xx)[0];
    }

    static System.Span<int> M2(System.Span<int> xx)
    {
        var yy = M3(xx)[0];
        return yy;
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public ref struct Buffer10
{
    private System.Span<int> _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (7,16): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         return M3(x)[0];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M3(x)[0]").WithArguments("System.Span<int>").WithLocation(7, 16),
                // (7,16): error CS8347: Cannot use a result of 'Program.M3(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return M3(x)[0];
                Diagnostic(ErrorCode.ERR_EscapeCall, "M3(x)").WithArguments("Program.M3(System.Span<int>)", "x").WithLocation(7, 16),
                // (7,19): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return M3(x)[0];
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(7, 19),
                // (13,17): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var y = M3(x)[0];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M3(x)[0]").WithArguments("System.Span<int>").WithLocation(13, 17),
                // (14,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(14, 16),
                // (24,16): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         return M3(xx)[0];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M3(xx)[0]").WithArguments("System.Span<int>").WithLocation(24, 16),
                // (29,18): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         var yy = M3(xx)[0];
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "M3(xx)[0]").WithArguments("System.Span<int>").WithLocation(29, 18),
                // (37,30): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private System.Span<int> _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(37, 30)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0119: 'Buffer10<int>' is a type, which is not valid in the given context
                //         _ = Buffer10<int>[..5];
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Buffer10<int>").WithArguments("Buffer10<int>", "type").WithLocation(6, 13)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int M2() => M4(M3()[..], default);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()").WithLocation(9, 27)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m3 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "M3").Single();
            Assert.Equal("M3()[..]", m3.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(m3.Parent);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(m3.Parent);

            Assert.Equal("Buffer10<System.Int32> Program.M3()", symbolInfo.Symbol.ToTestDisplayString());
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  call       ""int Program.M4(in int)""
  IL_0010:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (23,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(23, 23)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,20): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(24, 20)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (23,20): error CS8167: Cannot return by reference a member of parameter 'c' because it is not a ref or out parameter
                //         return ref c.F[0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "c").WithArguments("c").WithLocation(23, 20)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  call       ""int C.M4(in int)""
  IL_0010:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (15,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F[0]").WithArguments("method", "this.get").WithLocation(15, 23)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,20): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(16, 20)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (15,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref F[0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(15, 20)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (18,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(18, 9)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (15,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F[0]").WithArguments("method", "this.get").WithLocation(15, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (23,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(c.F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(23, 19)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(24, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (23,16): error CS8167: Cannot return by reference a member of parameter 'c' because it is not a ref or out parameter
                //         return c.F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "c").WithArguments("c").WithLocation(23, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (13,54): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public readonly System.ReadOnlySpan<int> M2() => F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(13, 54)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(16, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (15,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(15, 19)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (18,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[..][0]").WithArguments("property", "this").WithLocation(18, 9)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (15,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F[..][0]").WithArguments("property", "this").WithLocation(15, 9)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (18,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         c.F[..5] = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c.F[..5]").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "F").Last();
            Assert.Equal("c.F[0]", f.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(f);

            Assert.Equal("Buffer10<System.Int32> C.F", symbolInfo.Symbol.ToTestDisplayString());

            var access = f.Parent.Parent;
            typeInfo = model.GetTypeInfo(access);

            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(access);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  call       ""int Program.M4(in int)""
  IL_0010:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(24, 23)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>)""
  IL_000b:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0][0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0][0]").WithArguments("method", "this.get").WithLocation(24, 23)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0][0]").WithArguments("method", "this.get").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Variable_Readonly_19()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        var i = 1;
        b[i] = 111;
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

    static int M2(in C c) => c.F[1];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ldind.i4
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Variable_Readonly_20()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        var i = 9;
        b[i] = 111;
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

    static int M2(in C c) => c.F[9];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   9
  IL_0008:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ldind.i4
  IL_000e:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Variable_Readonly_21()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        var i = 0;
        b[i] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c, 0));
    }

    static int M2(in C c, int i) => c.F[i];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldarg.1
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ret
}
");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "F").Last();
            Assert.Equal("c.F[..5]", f.Parent.Parent.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer10<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(f);

            Assert.Equal("Buffer10<System.Int32> C.F", symbolInfo.Symbol.ToTestDisplayString());

            var access = f.Parent.Parent;
            typeInfo = model.GetTypeInfo(access);

            Assert.Equal("System.ReadOnlySpan<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            symbolInfo = model.GetSymbolInfo(access);

            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(c.F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(24, 19)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[..][0]").WithArguments("property", "this").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Variable_Readonly_08()
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
        System.Console.Write(M2(c, 5)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c, int y) => c.F[..y];
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.0
  IL_0011:  ldarg.1
  IL_0012:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0017:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (12,20): error CS8985: List patterns may not be used for a value of type 'Buffer10<int>'. No suitable 'Length' or 'Count' property was found.
                //         if (x.F is [0, ..])
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[0, ..]").WithArguments("Buffer10<int>").WithLocation(12, 20),
                // (12,20): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         if (x.F is [0, ..])
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0, ..]").WithArguments("Buffer10<int>").WithLocation(12, 20)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (12,17): error CS0443: Syntax error; value expected
                //         _ = x.F[];
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(12, 17)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,13): error CS9172: Elements of an inline array type can be accessed only with a single argument implicitly convertible to 'int', 'System.Index', or 'System.Range'.
                //         _ = x.F[0, 1];
                Diagnostic(ErrorCode.ERR_InlineArrayBadIndex, "x.F[0, 1]").WithLocation(12, 13)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,13): error CS9172: Elements of an inline array type can be accessed only with a single argument implicitly convertible to 'int', 'System.Index', or 'System.Range'.
                //         _ = x.F["a"];
                Diagnostic(ErrorCode.ERR_InlineArrayBadIndex, @"x.F[""a""]").WithLocation(12, 13)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (11,13): error CS9173: An inline array access may not have a named argument specifier
                //         _ = x.F[x: 1];
                Diagnostic(ErrorCode.ERR_NamedArgumentForInlineArray, "x.F[x: 1]").WithLocation(11, 13)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
        public void AlwaysDefault_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AlwaysDefault_03_1()
        {
            var src = @"
class C
{
    public Buffer10<int> F1;
    public int F2;
}

class Program
{
    static void M(C c)
    {
        ref readonly int x = ref c.F1[0];
        ref readonly int y = ref c.F2;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AlwaysDefault_03_2()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F1;
    public readonly int F2;
}

class Program
{
    static void M(C c)
    {
        ref readonly int x = ref c.F1[0];
        ref readonly int y = ref c.F2;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AlwaysDefault_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F1;
    public int F2;
}

class Program
{
    static void M(in C c)
    {
        ref readonly int x = ref c.F1[0];
        ref readonly int y = ref c.F2;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,35): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public readonly Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 35)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 13)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (13,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(13, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[0]);
        System.Console.Write(' ');
        System.Console.Write(c.F[9]);

        System.Console.Write(' ');
        c.F[9] = 2; 
        c = new C();
        System.Console.Write(c.F[0]);
        System.Console.Write(' ');
        System.Console.Write(c.F[9]);
    }
}

";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 0 1 0").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  initobj    ""Buffer10<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer10<int> C.F""
  IL_0012:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_0017:  ldc.i4.1
  IL_0018:  stind.i4
  IL_0019:  ret
}
");
        }

        [Fact]
        public void DefiniteAssignment_04()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;
}

class Program
{
    static void M()
    {
        C c;
        c.F._element0 = 1;
        _ = c.F;

        Buffer2Ref b;
        b._element0 = ref (new [] { 1 })[0];
        _ = b;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}

[System.Runtime.CompilerServices.InlineArray(2)]
public ref struct Buffer2Ref
{
    public ref int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (13,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(13, 13),
                // (17,13): error CS0165: Use of unassigned local variable 'b'
                //         _ = b;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b").WithArguments("b").WithLocation(17, 13),
                // (30,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(30, 20)
                );
        }

        [Fact]
        public void DefiniteAssignment_05()
        {
            var src = @"
struct C
{
    public Buffer1<int> F;
}

class Program
{
    static void M()
    {
        C c;
        c.F._element0 = 1;
        _ = c.F;

        Buffer2Ref b;
        b._element0 = ref (new [] { 1 })[0];
        _ = b;
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1<T>
{
    public T _element0;
}

[System.Runtime.CompilerServices.InlineArray(1)]
public ref struct Buffer2Ref
{
    public ref int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (30,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(30, 20)
                );
        }

        [Fact]
        public void DefiniteAssignment_06()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;
}

class Program
{
    static void M()
    {
        C c;
        c.F = default;
        _ = c.F;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_07()
        {
            var src = @"
public struct C
{
    public Buffer2<int> F;
}

class Program
{
    static void M()
    {
        C c;
        c = default;
        _ = c.F;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_08()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;

    public C()
    {
        F._element0 = 0;
        F[0] = 1;
        F[1] = 2;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer2<int> C.F""
  IL_0006:  initobj    ""Buffer2<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer2<int> C.F""
  IL_0012:  ldc.i4.0
  IL_0013:  stfld      ""int Buffer2<int>._element0""
  IL_0018:  ldarg.0
  IL_0019:  ldflda     ""Buffer2<int> C.F""
  IL_001e:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer2<int>, int>(ref Buffer2<int>)""
  IL_0023:  ldc.i4.1
  IL_0024:  stind.i4
  IL_0025:  ldarg.0
  IL_0026:  ldflda     ""Buffer2<int> C.F""
  IL_002b:  ldc.i4.1
  IL_002c:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer2<int>, int>(ref Buffer2<int>, int)""
  IL_0031:  ldc.i4.2
  IL_0032:  stind.i4
  IL_0033:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_09()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;

    public C()
    {
        F._element0 = 1;
        F[1] = 2;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[0]);
        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 2").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer2<int> C.F""
  IL_0006:  initobj    ""Buffer2<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer2<int> C.F""
  IL_0012:  ldc.i4.1
  IL_0013:  stfld      ""int Buffer2<int>._element0""
  IL_0018:  ldarg.0
  IL_0019:  ldflda     ""Buffer2<int> C.F""
  IL_001e:  ldc.i4.1
  IL_001f:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer2<int>, int>(ref Buffer2<int>, int)""
  IL_0024:  ldc.i4.2
  IL_0025:  stind.i4
  IL_0026:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_10()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;

    public C()
    {
        F._element0 = 1;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[0]);
        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
        c.F[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
        System.Console.Write(' ');

        c = new C();
        System.Console.Write(c.F[0]);
        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 0 2 1 0").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer2<int> C.F""
  IL_0006:  initobj    ""Buffer2<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer2<int> C.F""
  IL_0012:  ldc.i4.1
  IL_0013:  stfld      ""int Buffer2<int>._element0""
  IL_0018:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_11()
        {
            var src = @"
struct C
{
    public Buffer1<int> F;

    public C()
    {
        F._element0 = 1;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer1<int> C.F""
  IL_0006:  ldc.i4.1
  IL_0007:  stfld      ""int Buffer1<int>._element0""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void DefiniteAssignment_12()
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
        _ = c.F[2..5];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F[2..5];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 13)
                );
        }

        [Fact]
        public void DefiniteAssignment_13()
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
        c.F[2..5][0] = 1;
        _ = c.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,9): error CS0170: Use of possibly unassigned field 'F'
                //         c.F[2..5][0] = 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 9)
                );
        }

        [Fact]
        public void DefiniteAssignment_14()
        {
            var src = @"
struct C
{
    public Buffer1 F;
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

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_15()
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
        ref int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,25): error CS0170: Use of possibly unassigned field 'F'
                //         ref int x = ref c.F[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 25)
                );
        }

        [Fact]
        public void DefiniteAssignment_16()
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
        ref readonly int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,34): error CS0170: Use of possibly unassigned field 'F'
                //         ref readonly int x = ref c.F[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 34)
                );
        }

        [Fact]
        public void DefiniteAssignment_17()
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
        c.F[0] = 1;
        _ = c.F[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (13,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(13, 13)
                );
        }

        [Fact]
        public void DefiniteAssignment_18()
        {
            var src = @"
public struct C
{
    public Buffer1 F;
}

class Program
{
    static void M()
    {
        C c;
        c.F[0] = 1;
        _ = c.F[0];
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    int _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_19()
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
        _ = (System.Span<int>)c.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,31): error CS0170: Use of possibly unassigned field 'F'
                //         _ = (System.Span<int>)c.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 31)
                );
        }

        [Fact]
        public void DefiniteAssignment_20()
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
        _ = (System.ReadOnlySpan<int>)c.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,39): error CS0170: Use of possibly unassigned field 'F'
                //         _ = (System.ReadOnlySpan<int>)c.F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 39)
                );
        }

        [Fact]
        public void DefiniteAssignment_21()
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
        c.F[0] += 1;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,9): error CS0170: Use of possibly unassigned field 'F'
                //         c.F[0] += 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 9)
                );
        }

        [Fact]
        public void DefiniteAssignment_22()
        {
            var src = @"
public struct C
{
    public Buffer10<S> FF;
}

public struct S
{
    public int F;
}

class Program
{
    static void M1()
    {
        C c;
        c.FF[0].F += 1;
    }

    static void M2()
    {
        C c;
        _ = c.FF[0].F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (17,9): error CS0170: Use of possibly unassigned field 'F'
                //         c.FF[0].F += 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.FF[0].F").WithArguments("F").WithLocation(17, 9),
                // (23,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.FF[0].F;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.FF[0].F").WithArguments("F").WithLocation(23, 13)
                );
        }

        [Fact]
        public void DefiniteAssignment_31()
        {
            var src = @"
class Program
{
    static void M()
    {
        Buffer10<int> f;
        _ = f[0];
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (7,13): error CS0165: Use of unassigned local variable 'f'
                //         _ = f[0];
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f").WithLocation(7, 13)
                );
        }

        [Fact]
        public void DefiniteAssignment_32()
        {
            var src = @"
class Program
{
    static void M()
    {
        Buffer10<int> f;
        f[0] = 1;
        _ = f;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (8,13): error CS0165: Use of unassigned local variable 'f'
                //         _ = f;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f").WithLocation(8, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_33()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer10();
        System.Console.Write(f[0]);
        System.Console.Write(' ');
        System.Console.Write(f[9]);

        System.Console.Write(' ');
        f[9] = 2; 
        f = new Buffer10();
        System.Console.Write(f[0]);
        System.Console.Write(' ');
        System.Console.Write(f[9]);
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10
{
    private int _element0;

    public Buffer10()
    {
        this[0] = 1;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 0 1 0").VerifyDiagnostics();

            verifier.VerifyIL("Buffer10..ctor",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer10""
  IL_0007:  ldarg.0
  IL_0008:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10, int>(ref Buffer10)""
  IL_000d:  ldc.i4.1
  IL_000e:  stind.i4
  IL_000f:  ret
}
");
        }

        [Fact]
        public void DefiniteAssignment_34()
        {
            var src = @"
class Program
{
    static void M()
    {
        Buffer2<int> f;
        f._element0 = 1;
        _ = f;

        Buffer2<int> f2;
        f2._element0 = 1;
        f2[0] = 1;
        f2[1] = 2;
        _ = f2;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (8,13): error CS0165: Use of unassigned local variable 'f'
                //         _ = f;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f").WithArguments("f").WithLocation(8, 13),
                // (14,13): error CS0165: Use of unassigned local variable 'f2'
                //         _ = f2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "f2").WithArguments("f2").WithLocation(14, 13)
                );
        }

        [Fact]
        public void DefiniteAssignment_35()
        {
            var src = @"
class Program
{
    static void M()
    {
        Buffer1<int> f;
        f._element0 = 1;
        _ = f;
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_36()
        {
            var src = @"
class Program
{
    static void M()
    {
        Buffer2<int> f;
        f = default;
        _ = f;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    public T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_38()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _element0 = 0;
        this[0] = 1;
        this[1] = 2;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.0
  IL_0009:  stfld      ""int Buffer2._element0""
  IL_000e:  ldarg.0
  IL_000f:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer2, int>(ref Buffer2)""
  IL_0014:  ldc.i4.1
  IL_0015:  stind.i4
  IL_0016:  ldarg.0
  IL_0017:  ldc.i4.1
  IL_0018:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer2, int>(ref Buffer2, int)""
  IL_001d:  ldc.i4.2
  IL_001e:  stind.i4
  IL_001f:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_39()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer2();
        System.Console.Write(f[0]);
        System.Console.Write(' ');
        System.Console.Write(f[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _element0 = 1;
        this[1] = 2;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 2").VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int Buffer2._element0""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.1
  IL_0010:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer2, int>(ref Buffer2, int)""
  IL_0015:  ldc.i4.2
  IL_0016:  stind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_40()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer2();
        System.Console.Write(f[0]);
        System.Console.Write(' ');
        System.Console.Write(f[1]);
        f[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(f[1]);
        System.Console.Write(' ');

        f = new Buffer2();
        System.Console.Write(f[0]);
        System.Console.Write(' ');
        System.Console.Write(f[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _element0 = 1;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1 0 2 1 0").VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int Buffer2._element0""
  IL_000e:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_41()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    public int _element0;

    public Buffer1()
    {
        _element0 = 1;
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public ref struct Buffer1Ref
{
    public ref int _element2;

    public Buffer1Ref()
    {
        _element2 = ref (new [] { 1 })[0];
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics(
                // (25,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element2;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element2").WithLocation(25, 20)
                );

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int Buffer1._element0""
  IL_0007:  ret
}
");

            verifier.VerifyIL("Buffer1Ref..ctor",
@"
{
  // Code size       23 (0x17)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""int""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  ldc.i4.0
  IL_000c:  ldelema    ""int""
  IL_0011:  stfld      ""ref int Buffer1Ref._element2""
  IL_0016:  ret
}
");
            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,30): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         System.Console.Write(f[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "f[0]").WithArguments("inline arrays", "12.0").WithLocation(7, 30),
                // (25,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref int _element2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref int").WithArguments("ref fields", "11.0").WithLocation(25, 12),
                // (25,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element2;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element2").WithLocation(25, 20)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_42()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    int _element0 = 1;

    public Buffer1()
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int Buffer1._element0""
  IL_0007:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_43()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);
        System.Console.Write(' ');
        f[0] = 1;
        System.Console.Write(f[0]);
        System.Console.Write(' ');

        f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    public int _element0;

    public Buffer1()
    {
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public ref struct Buffer1Ref
{
    public ref int _element0;

    public Buffer1Ref()
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 1 0", verify: Verification.Fails).VerifyDiagnostics(
                // (31,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(31, 20)
                );

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int Buffer1._element0""
  IL_0007:  ret
}
");

            verifier.VerifyIL("Buffer1Ref..ctor",
@"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  conv.u
  IL_0003:  stfld      ""ref int Buffer1Ref._element0""
  IL_0008:  ret
}
");
        }

        [Fact]
        public void DefiniteAssignment_44()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _element0 = 1;
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public ref struct Buffer2Ref
{
    public ref int _element2;

    public Buffer2Ref()
    {
        _element2 = ref (new [] { 1 })[0];
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics(
                // (16,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element2;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element2").WithLocation(16, 20)
                );

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int Buffer2._element0""
  IL_000e:  ret
}
");

            verifier.VerifyIL("Buffer2Ref..ctor",
@"
{
  // Code size       30 (0x1e)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2Ref""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  newarr     ""int""
  IL_000e:  dup
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.1
  IL_0011:  stelem.i4
  IL_0012:  ldc.i4.0
  IL_0013:  ldelema    ""int""
  IL_0018:  stfld      ""ref int Buffer2Ref._element2""
  IL_001d:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,12): error CS0177: The out parameter 'this' must be assigned to before control leaves the current method
                //     public Buffer2()
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "Buffer2").WithArguments("this").WithLocation(7, 12),
                // (16,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref int _element2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref int").WithArguments("ref fields", "11.0").WithLocation(16, 12),
                // (16,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element2;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element2").WithLocation(16, 20),
                // (18,12): error CS0177: The out parameter 'this' must be assigned to before control leaves the current method
                //     public Buffer2Ref()
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "Buffer2Ref").WithArguments("this").WithLocation(18, 12)
                );
        }

        [Fact]
        public void DefiniteAssignment_45()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    int _element0 = 1;

    public Buffer2()
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int Buffer2._element0""
  IL_000e:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,12): error CS0177: The out parameter 'this' must be assigned to before control leaves the current method
                //     public Buffer2()
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "Buffer2").WithArguments("this").WithLocation(7, 12)
                );
        }

        [Fact]
        public void DefiniteAssignment_46()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        this = default;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr).VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_47()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _element0 = 1;
        _ = this[0];
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public ref struct Buffer2Ref
{
    public ref int _element2;

    public Buffer2Ref()
    {
        _element2 = ref (new [] { 1 })[0];
        _ = this;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp).VerifyDiagnostics(
                // (17,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element2;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element2").WithLocation(17, 20)
                );

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""int Buffer2._element0""
  IL_000e:  ldarg.0
  IL_000f:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer2, int>(ref Buffer2)""
  IL_0014:  pop
  IL_0015:  ret
}
");

            verifier.VerifyIL("Buffer2Ref..ctor",
@"
{
  // Code size       30 (0x1e)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2Ref""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  newarr     ""int""
  IL_000e:  dup
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.1
  IL_0011:  stelem.i4
  IL_0012:  ldc.i4.0
  IL_0013:  ldelema    ""int""
  IL_0018:  stfld      ""ref int Buffer2Ref._element2""
  IL_001d:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,12): error CS0177: The out parameter 'this' must be assigned to before control leaves the current method
                //     public Buffer2()
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "Buffer2").WithArguments("this").WithLocation(7, 12),
                // (10,13): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         _ = this[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "this[0]").WithArguments("inline arrays", "12.0").WithLocation(10, 13),
                // (10,13): error CS0188: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version '11.0' to auto-default the unassigned fields.
                //         _ = this[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationThisUnsupportedVersion, "this").WithArguments("11.0").WithLocation(10, 13),
                // (17,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                //     public ref int _element2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "ref int").WithArguments("ref fields", "11.0").WithLocation(17, 12),
                // (17,20): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     public ref int _element2;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element2").WithLocation(17, 20),
                // (19,12): error CS0177: The out parameter 'this' must be assigned to before control leaves the current method
                //     public Buffer2Ref()
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "Buffer2Ref").WithArguments("this").WithLocation(19, 12),
                // (22,13): error CS0188: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version '11.0' to auto-default the unassigned fields.
                //         _ = this;
                Diagnostic(ErrorCode.ERR_UseDefViolationThisUnsupportedVersion, "this").WithArguments("11.0").WithLocation(22, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_48()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    public int _element0;

    public Buffer1()
    {
        _element0 = 1;
        _ = this[0];
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int Buffer1._element0""
  IL_0007:  ldarg.0
  IL_0008:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer1, int>(ref Buffer1)""
  IL_000d:  pop
  IL_000e:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,30): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         System.Console.Write(f[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "f[0]").WithArguments("inline arrays", "12.0").WithLocation(7, 30),
                // (19,13): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         _ = this[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "this[0]").WithArguments("inline arrays", "12.0").WithLocation(19, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_49()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);

        f[0] = 1; 
        System.Console.Write(' ');
        System.Console.Write(f[0]);
        System.Console.Write(' ');

        f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    public int _element0;

    public Buffer1()
    {
        _ = this[0];
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 1 0").VerifyDiagnostics();

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int Buffer1._element0""
  IL_0007:  ldarg.0
  IL_0008:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer1, int>(ref Buffer1)""
  IL_000d:  pop
  IL_000e:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (7,30): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         System.Console.Write(f[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "f[0]").WithArguments("inline arrays", "12.0").WithLocation(7, 30),
                // (9,9): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         f[0] = 1; 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "f[0]").WithArguments("inline arrays", "12.0").WithLocation(9, 9),
                // (11,30): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         System.Console.Write(f[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "f[0]").WithArguments("inline arrays", "12.0").WithLocation(11, 30),
                // (15,30): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         System.Console.Write(f[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "f[0]").WithArguments("inline arrays", "12.0").WithLocation(15, 30),
                // (24,12): error CS0171: Field 'Buffer1._element0' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public Buffer1()
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "Buffer1").WithArguments("Buffer1._element0", "11.0").WithLocation(24, 12),
                // (26,13): error CS8936: Feature 'inline arrays' is not available in C# 10.0. Please use language version 12.0 or greater.
                //         _ = this[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "this[0]").WithArguments("inline arrays", "12.0").WithLocation(26, 13),
                // (26,13): error CS0188: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version '11.0' to auto-default the unassigned fields.
                //         _ = this[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationThisUnsupportedVersion, "this").WithArguments("11.0").WithLocation(26, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_50()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    public int _element0;

    public Buffer1()
    {
        this[0] = 1;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer1, int>(ref Buffer1)""
  IL_0006:  ldc.i4.1
  IL_0007:  stind.i4
  IL_0008:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_51()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer1();
        System.Console.Write(f[0]);
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
public struct Buffer1
{
    public int _element0;

    public Buffer1()
    {
        this[^1] = 1;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();

            verifier.VerifyIL("Buffer1..ctor",
@"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer1, int>(ref Buffer1)""
  IL_0006:  ldc.i4.1
  IL_0007:  stind.i4
  IL_0008:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_52()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;

    public C()
    {
        _ = F[..];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[1]);
        c.F[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
        System.Console.Write(' ');

        c = new C();
        System.Console.Write(c.F[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    private T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 2 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer2<int> C.F""
  IL_0006:  initobj    ""Buffer2<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer2<int> C.F""
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer2<int>, int>(ref Buffer2<int>, int)""
  IL_0018:  pop
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_53()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;

    public C()
    {
        _ = (System.Span<int>)F;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[1]);
        c.F[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
        System.Console.Write(' ');

        c = new C();
        System.Console.Write(c.F[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    private T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 2 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer2<int> C.F""
  IL_0006:  initobj    ""Buffer2<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer2<int> C.F""
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer2<int>, int>(ref Buffer2<int>, int)""
  IL_0018:  pop
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_54()
        {
            var src = @"
struct C
{
    public Buffer2<int> F;

    public C()
    {
        _ = (System.ReadOnlySpan<int>)F;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.F[1]);
        c.F[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(c.F[1]);
        System.Console.Write(' ');

        c = new C();
        System.Console.Write(c.F[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2<T>
{
    private T _element0;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 2 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer2<int> C.F""
  IL_0006:  initobj    ""Buffer2<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer2<int> C.F""
  IL_0012:  ldc.i4.2
  IL_0013:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer2<int>, int>(in Buffer2<int>, int)""
  IL_0018:  pop
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_55()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer2();
        System.Console.Write(f[1]);
        f[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(f[1]);
        System.Console.Write(' ');

        f = new Buffer2();
        System.Console.Write(f[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _ = this[..];
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 2 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.2
  IL_0009:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer2, int>(ref Buffer2, int)""
  IL_000e:  pop
  IL_000f:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_56()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer2();
        System.Console.Write(f[1]);
        f[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(f[1]);
        System.Console.Write(' ');

        f = new Buffer2();
        System.Console.Write(f[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _ = (System.Span<int>)this;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 2 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.2
  IL_0009:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer2, int>(ref Buffer2, int)""
  IL_000e:  pop
  IL_000f:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void DefiniteAssignment_57()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var f = new Buffer2();
        System.Console.Write(f[1]);
        f[1] = 2;

        System.Console.Write(' ');
        System.Console.Write(f[1]);
        System.Console.Write(' ');

        f = new Buffer2();
        System.Console.Write(f[1]);
    }
}

[System.Runtime.CompilerServices.InlineArray(2)]
public struct Buffer2
{
    public int _element0;

    public Buffer2()
    {
        _ = (System.ReadOnlySpan<int>)this;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 2 0", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Buffer2..ctor",
@"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""Buffer2""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.2
  IL_0009:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer2, int>(in Buffer2, int)""
  IL_000e:  pop
  IL_000f:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateReadOnlySpan);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_InteropServices_MemoryMarshal__CreateSpan);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

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

        [Fact]
        public void MissingHelper_15()
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
        foreach (var y in x.F)
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MissingHelper_16()
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
        foreach (var y in x.F)
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__Add_T);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);

            comp.VerifyEmitDiagnostics(
                // (11,27): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.Add'
                //         foreach (var y in x.F)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "Add").WithLocation(11, 27),
                // (11,27): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         foreach (var y in x.F)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 27)
                );
        }

        [Fact]
        public void MissingHelper_17()
        {
            var src = @"#pragma warning disable CS0649 // Field 'C.F' is never assigned to
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(in C x)
    {
        foreach (var y in x.F)
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__Add_T);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__As_T);
            comp.MakeMemberMissing(WellKnownMember.System_Runtime_CompilerServices_Unsafe__AsRef_T);

            comp.VerifyEmitDiagnostics(
                // (11,27): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.AsRef'
                //         foreach (var y in x.F)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "AsRef").WithLocation(11, 27),
                // (11,27): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.Add'
                //         foreach (var y in x.F)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "Add").WithLocation(11, 27),
                // (11,27): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.Unsafe.As'
                //         foreach (var y in x.F)
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F").WithArguments("System.Runtime.CompilerServices.Unsafe", "As").WithLocation(11, 27)
                );
        }

        [Fact]
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
        var i = 0;
        _ = x.F[i];
        _ = x.F[..5];
        _ = (System.Span<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    AssertEx.Equal("System.Span<TElement> <PrivateImplementationDetails>.InlineArrayAsSpan<TBuffer, TElement>(ref TBuffer buffer, System.Int32 length)",
                                   t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName).ToTestDisplayString());
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName));
                }).VerifyDiagnostics();

            verifier.VerifyIL("<PrivateImplementationDetails>." + CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName,
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

        [Fact]
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
        var i = 0;
        _ = x.F[i];
        _ = x.F[..5];
        _ = (System.ReadOnlySpan<int>)x.F;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName));
                    AssertEx.Equal("System.ReadOnlySpan<TElement> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<TBuffer, TElement>(in TBuffer buffer, System.Int32 length)",
                                   t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName).ToTestDisplayString());
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName));
                }).VerifyDiagnostics();

            verifier.VerifyIL("<PrivateImplementationDetails>." + CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName,
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TBuffer System.Runtime.CompilerServices.Unsafe.AsRef<TBuffer>(scoped ref readonly TBuffer)""
  IL_0006:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_000b:  ldarg.1
  IL_000c:  call       ""System.ReadOnlySpan<TElement> System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan<TElement>(scoped ref readonly TElement, int)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void PrivateImplementationDetails_03()
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
        _ = x.F[1];
        foreach (var y in x.F)
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName));
                    Assert.Equal("ref TElement <PrivateImplementationDetails>.InlineArrayElementRef<TBuffer, TElement>(ref TBuffer buffer, System.Int32 index)",
                                 t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName).ToTestDisplayString());
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName));
                }).VerifyDiagnostics();

            verifier.VerifyIL("<PrivateImplementationDetails>." + CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName,
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_0006:  ldarg.1
  IL_0007:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.Add<TElement>(ref TElement, int)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void PrivateImplementationDetails_04()
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
        _ = x.F[1];
        foreach (var y in x.F)
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName));
                    Assert.Equal("ref readonly TElement <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<TBuffer, TElement>(in TBuffer buffer, System.Int32 index)",
                                 t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName).ToTestDisplayString());
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName));
                }).VerifyDiagnostics();

            verifier.VerifyIL("<PrivateImplementationDetails>." + CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName,
@"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TBuffer System.Runtime.CompilerServices.Unsafe.AsRef<TBuffer>(scoped ref readonly TBuffer)""
  IL_0006:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_000b:  ldarg.1
  IL_000c:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.Add<TElement>(ref TElement, int)""
  IL_0011:  ret
}
");
        }

        [Fact]
        public void PrivateImplementationDetails_05()
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName));
                    Assert.Equal("ref TElement <PrivateImplementationDetails>.InlineArrayFirstElementRef<TBuffer, TElement>(ref TBuffer buffer)",
                                 t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName).ToTestDisplayString());
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName));
                }).VerifyDiagnostics();

            verifier.VerifyIL("<PrivateImplementationDetails>." + CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName,
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void PrivateImplementationDetails_06()
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));

            var verifier = CompileAndVerify(comp, verify: VerifyOnMonoOrCoreClr,
                symbolValidator: m =>
                {
                    var t = m.GlobalNamespace.GetTypeMember("<PrivateImplementationDetails>");
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsSpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayAsReadOnlySpanName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayElementRefReadOnlyName));
                    Assert.Empty(t.GetMembers(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefName));
                    Assert.Equal("ref readonly TElement <PrivateImplementationDetails>.InlineArrayFirstElementRefReadOnly<TBuffer, TElement>(in TBuffer buffer)",
                                 t.GetMember(CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName).ToTestDisplayString());
                }).VerifyDiagnostics();

            verifier.VerifyIL("<PrivateImplementationDetails>." + CodeAnalysis.CodeGen.PrivateImplementationDetails.SynthesizedInlineArrayFirstElementRefReadOnlyName,
@"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref TBuffer System.Runtime.CompilerServices.Unsafe.AsRef<TBuffer>(scoped ref readonly TBuffer)""
  IL_0006:  call       ""ref TElement System.Runtime.CompilerServices.Unsafe.As<TBuffer, TElement>(ref TBuffer)""
  IL_000b:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = GetC(s2).F[0].Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "GetC(s2).F[0]").WithLocation(14, 13),
                // (16,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = GetC(s2).F[..5][0].Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "GetC(s2).F[..5][0]").WithLocation(16, 13)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
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

        [Fact]
        public void NullableAnalysis_04()
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
        foreach(var x in GetC(s1).F)
            _ = x.Length;

        foreach(var y in GetC(s2).F)
            _ = y.Length;

        foreach(string a in GetC(s1).F)
        {}

        foreach(string b in GetC(s2).F)
        {}

        foreach(string? c in GetC(s1).F)
        {}

        foreach(string? d in GetC(s2).F)
        {}
    }

    static C<T> GetC<T>(T x)
    {
        var c = new C<T>();
        return c;
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (17,17): warning CS8602: Dereference of a possibly null reference.
                //             _ = y.Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y").WithLocation(17, 17),
                // (22,24): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         foreach(string b in GetC(s2).F)
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "b").WithLocation(22, 24)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 110").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  dup
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.s   111
  IL_000f:  add
  IL_0010:  stind.i4
  IL_0011:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (18,48): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static System.ReadOnlySpan<int> M1(C x) => x.F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F").WithArguments("inline arrays", "12.0").WithLocation(18, 48),
                // (19,40): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     static System.Span<int> M2(C x) => x.F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x.F").WithArguments("inline arrays", "12.0").WithLocation(19, 40)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "F").ToArray();

            Assert.Equal("=> x.F", f[^2].Parent.Parent.ToString());
            var typeInfo = model.GetTypeInfo(f[^2]);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.InlineArray, model.GetConversion(f[^2]).Kind);

            Assert.Equal("=> x.F", f[^1].Parent.Parent.ToString());
            typeInfo = model.GetTypeInfo(f[^1]);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Span<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.InlineArray, model.GetConversion(f[^1]).Kind);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
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

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M1",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
  IL_0008:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
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

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (6,37): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<int> M2() => F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 37),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16),
                // (14,45): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.ReadOnlySpan<int> M4() => F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(14, 45),
                // (19,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(19, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (6,76): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<Buffer10<int>> M2() => ((System.Span<Buffer10<int>>)F)[..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(6, 76),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16),
                // (14,92): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.ReadOnlySpan<Buffer10<int>> M4() => ((System.ReadOnlySpan<Buffer10<int>>)F)[..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F").WithLocation(14, 92),
                // (19,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(19, 16)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         ((System.Span<int>)x.F) = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "(System.Span<int>)x.F").WithLocation(12, 10),
                // (13,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         ((System.ReadOnlySpan<int>)x.F) = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "(System.ReadOnlySpan<int>)x.F").WithLocation(13, 10)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,19): error CS9164: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         return M4(c.F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "c.F").WithArguments("System.Span<int>").WithLocation(24, 19)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (11,19): error CS9164: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         return M4(c.F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "c.F").WithArguments("System.Span<int>").WithLocation(11, 19)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (15,19): error CS9164: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         return M4(F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "F").WithArguments("System.Span<int>").WithLocation(15, 19)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.s   10
  IL_0008:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
  IL_000d:  ret
}
");
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS9165: Cannot convert expression to 'ReadOnlySpan<int>' because it may not be passed or returned by reference
                //     static int M2() => M4(M3(), default);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported, "M3()").WithArguments("System.ReadOnlySpan<int>").WithLocation(9, 27),
                // (23,27): error CS9164: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //     static int M5() => M6(M3(), default);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "M3()").WithArguments("System.Span<int>").WithLocation(23, 27)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m3 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(m => m.Identifier.ValueText == "M3").First().Parent;

            Assert.Equal("M3()", m3.ToString());
            var typeInfo = model.GetTypeInfo(m3);

            Assert.Equal("Buffer10<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.ReadOnlySpan<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.InlineArray, model.GetConversion(m3).Kind);
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>?' to 'System.ReadOnlySpan<int>?'
                //     static int M2() => M4(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>?", "System.ReadOnlySpan<int>?").WithLocation(9, 27),
                // (13,45): error CS9244: The type 'ReadOnlySpan<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     static int M4(System.ReadOnlySpan<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "System.ReadOnlySpan<int>").WithLocation(13, 45),
                // (18,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>?' to 'System.Span<int>?'
                //     static int M5() => M6(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>?", "System.Span<int>?").WithLocation(18, 27),
                // (20,37): error CS9244: The type 'Span<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     static int M6(System.Span<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "System.Span<int>").WithLocation(20, 37)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS0030: Cannot convert type 'Buffer10<int>?' to 'System.ReadOnlySpan<int>'
                //     static int M2() => M4((System.ReadOnlySpan<int>)M3(), default);
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.ReadOnlySpan<int>)M3()").WithArguments("Buffer10<int>?", "System.ReadOnlySpan<int>").WithLocation(9, 27),
                // (18,27): error CS0030: Cannot convert type 'Buffer10<int>?' to 'System.Span<int>'
                //     static int M5() => M6((System.Span<int>)M3(), default);
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Span<int>)M3()").WithArguments("Buffer10<int>?", "System.Span<int>").WithLocation(18, 27)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            // Once we stop reporting error CS0306: The type 'ReadOnlySpan<int>' may not be used as a type argument
            // We might decide to allow a conversion from an inline array expression to nullable span types, but this scenario 
            // should still remain an error in that case because the expression is a value.
            comp.VerifyDiagnostics(
                // (9,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>' to 'System.ReadOnlySpan<int>?'
                //     static int M2() => M4(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>", "System.ReadOnlySpan<int>?").WithLocation(9, 27),
                // (13,45): error CS9244: The type 'ReadOnlySpan<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     static int M4(System.ReadOnlySpan<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "System.ReadOnlySpan<int>").WithLocation(13, 45),
                // (18,27): error CS1503: Argument 1: cannot convert from 'Buffer10<int>' to 'System.Span<int>?'
                //     static int M5() => M6(M3(), default);
                Diagnostic(ErrorCode.ERR_BadArgType, "M3()").WithArguments("1", "Buffer10<int>", "System.Span<int>?").WithLocation(18, 27),
                // (20,37): error CS9244: The type 'Span<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     static int M6(System.Span<int>? x, Buffer10<int> y)
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "x").WithArguments("System.Nullable<T>", "T", "System.Span<int>").WithLocation(20, 37)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
    IL_007c:  call       ""System.ReadOnlySpan<int> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<Buffer10<int>, int>(in Buffer10<int>, int)""
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
    IL_007c:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
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

        [Fact]
        public void Conversion_ExpressionTree_01()
        {
            var src = @"
using System.Linq.Expressions;

class Program
{
    static Expression<System.Func<int>> M1(Buffer10<int> x) =>
        () => ((System.Span<int>)x).Length;

    static Expression<System.Func<int>> M2(Buffer10<int> x) =>
        () => ((System.ReadOnlySpan<int>)x).Length;
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (7,16): error CS9170: An expression tree may not contain an inline array access or conversion
                //         () => ((System.Span<int>)x).Length;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation, "(System.Span<int>)x").WithLocation(7, 16),
                // (10,16): error CS9170: An expression tree may not contain an inline array access or conversion
                //         () => ((System.ReadOnlySpan<int>)x).Length;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation, "(System.ReadOnlySpan<int>)x").WithLocation(10, 16)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80);
            comp.VerifyDiagnostics(
                // (11,13): error CS0030: Cannot convert type 'Program' to 'System.Span<int>'
                //         _ = (System.Span<int>)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Span<int>)x").WithArguments("Program", "System.Span<int>").WithLocation(11, 13),
                // (12,13): error CS0030: Cannot convert type 'Program' to 'System.ReadOnlySpan<int>'
                //         _ = (System.ReadOnlySpan<int>)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.ReadOnlySpan<int>)x").WithArguments("Program", "System.ReadOnlySpan<int>").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics(
                // (17,37): warning CS9183: Inline array conversion operator will not be used for conversion from expression of the declaring type.
                //     public static implicit operator System.ReadOnlySpan<int>(Buffer10 x) => new[] { -111 };
                Diagnostic(ErrorCode.WRN_InlineArrayConversionOperatorNotUsed, "System.ReadOnlySpan<int>").WithLocation(17, 37)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (14,9): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         b[0] = 111;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "b[0]").WithArguments("inline arrays", "12.0").WithLocation(14, 9),
                // (15,34): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "b").WithArguments("inline arrays", "12.0").WithLocation(15, 34)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,31): error CS0457: Ambiguous user defined conversions 'C.implicit operator C(ReadOnlySpan<int>)' and 'C.implicit operator C(Span<int>)' when converting from 'Buffer10<int>' to 'C'
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(C)b").WithArguments("C.implicit operator C(System.ReadOnlySpan<int>)", "C.implicit operator C(System.Span<int>)", "Buffer10<int>", "C").WithLocation(16, 31)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (21,31): error CS0457: Ambiguous user defined conversions 'C.implicit operator C(ReadOnlySpan<int>)' and 'C.implicit operator C(Span<int>)' when converting from 'Buffer10<int>' to 'C'
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "(C)b").WithArguments("C.implicit operator C(System.ReadOnlySpan<int>)", "C.implicit operator C(System.Span<int>)", "Buffer10<int>", "C").WithLocation(21, 31)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (20,34): error CS9164: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         System.Console.Write(((C)b).F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "b").WithArguments("System.Span<int>").WithLocation(20, 34)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
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
        System.Console.Write(((C)new Buffer10<int>()).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (13,34): error CS9165: Cannot convert expression to 'ReadOnlySpan<int>' because it may not be passed or returned by reference
                //         System.Console.Write(((C)new Buffer10<int>()).F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported, "new Buffer10<int>()").WithArguments("System.ReadOnlySpan<int>").WithLocation(13, 34)
                );
        }

        [Fact]
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
        System.Console.Write(((C)new Buffer10<int>()).F);
    }
}
";
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (13,34): error CS9164: Cannot convert expression to 'Span<int>' because it is not an assignable variable
                //         System.Console.Write(((C)new Buffer10<int>()).F);
                Diagnostic(ErrorCode.ERR_InlineArrayConversionToSpanNotSupported, "new Buffer10<int>()").WithArguments("System.Span<int>").WithLocation(13, 34)
                );
        }

        [Fact]
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
            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
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
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80);

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
            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80);

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
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80);
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
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80);
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
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80);
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
";

            var compilation = CreateCompilation(source, targetFramework: TargetFramework.Net80);
            compilation.VerifyDiagnostics(
                // (9,46): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10'
                // [System.Runtime.CompilerServices.InlineArray(C.F[0])]
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "C.F[0]").WithArguments("Buffer10").WithLocation(9, 46)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics(
                // (27,14): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public T this[int i]
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(27, 14)
                );

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldc.i4.s   111
  IL_000d:  stind.i4
  IL_000e:  ret
}
");
        }

        [Fact]
        public void ElementAccess_IndexerIsIgnored_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Buffer10<int> f = default;
        _ = f[0];
        f[0] = 2;
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public ref struct Buffer10<T>
{
    private ref T _element0;

    public T this[int i]
    {
        get => _element0;
        set => _element0 = value;
    }
}
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         _ = f[0];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "f[0]").WithArguments("Buffer10<int>").WithLocation(7, 13),
                // (8,9): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         f[0] = 2;
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "f[0]").WithArguments("Buffer10<int>").WithLocation(8, 9),
                // (15,19): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private ref T _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(15, 19),
                // (17,14): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public T this[int i]
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(17, 14)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics(
                // (27,14): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public T this[int i]
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(27, 14)
                );

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldind.i4
  IL_000c:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer10<int>, int>(ref Buffer10<int>)""
  IL_000b:  ldc.i4.s   111
  IL_000d:  stind.i4
  IL_000e:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
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
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics(
                // (27,14): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public T this[int i]
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(27, 14),
                // (34,29): warning CS9182: Inline array 'Slice' method will not be used for element access expression.
                //     public System.Span<int> Slice(int start, int length) => throw null;
                Diagnostic(ErrorCode.WRN_InlineArraySliceNotUsed, "Slice").WithLocation(34, 29)
                );

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  ldc.i4.5
  IL_0007:  call       ""System.Span<int> <PrivateImplementationDetails>.InlineArrayAsSpan<Buffer10<int>, int>(ref Buffer10<int>, int)""
  IL_000c:  ret
}
");
        }

        [Fact]
        public void ElementAccess_Bounds_01()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[-2];
        _ = f[-1];
        _ = f[0];
        _ = f[9];
        _ = f[10];
        _ = f[11];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[-2];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(6, 15),
                // (7,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[-1];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(7, 15),
                // (10,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[10];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "10").WithLocation(10, 15),
                // (11,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[11];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(11, 15)
                );
        }

        [Fact]
        public void ElementAccess_Bounds_02()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[(System.Index)(-2)];
        _ = f[(System.Index)(-1)];
        _ = f[(System.Index)0];
        _ = f[(System.Index)9];
        _ = f[(System.Index)10];
        _ = f[(System.Index)11];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,30): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[(System.Index)(-2)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(6, 30),
                // (7,30): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[(System.Index)(-1)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(7, 30),
                // (10,29): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[(System.Index)10];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "10").WithLocation(10, 29),
                // (11,29): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[(System.Index)11];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(11, 29)
                );
        }

        [Fact]
        public void ElementAccess_Bounds_03()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[^12];
        _ = f[^11];
        _ = f[^10];
        _ = f[^1];
        _ = f[^0];
        _ = f[^-1];
        _ = f[^int.MinValue];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[^12];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^12").WithLocation(6, 15),
                // (7,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[^11];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^11").WithLocation(7, 15),
                // (10,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[^0];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^0").WithLocation(10, 15),
                // (11,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[^-1];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^-1").WithLocation(11, 15),
                // (12,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[^int.MinValue];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^int.MinValue").WithLocation(12, 15)
                );
        }

        [Fact]
        public void ElementAccess_Bounds_04()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[new System.Index(-2, false)];
        _ = f[new System.Index(-1, false)];
        _ = f[new System.Index(0, false)];
        _ = f[new System.Index(9, false)];
        _ = f[new System.Index(10, false)];
        _ = f[new System.Index(11, false)];

        _ = f[new System.Index(-2)];
        _ = f[new System.Index(-1)];
        _ = f[new System.Index(0)];
        _ = f[new System.Index(9)];
        _ = f[new System.Index(10)];
        _ = f[new System.Index(11)];

        _ = f[new System.Index(12, true)];
        _ = f[new System.Index(11, true)];
        _ = f[new System.Index(10, true)];
        _ = f[new System.Index(1, true)];
        _ = f[new System.Index(0, true)];
        _ = f[new System.Index(-1, true)];
        _ = f[new System.Index(int.MinValue, true)];

        _ = f[new System.Index()];
        _ = f[new System.Index(value: -1)];
        _ = f[new System.Index(value: -1, fromEnd: false)];
        _ = f[new System.Index(fromEnd: false, value: -1)];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-2, false)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(6, 32),
                // (7,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-1, false)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(7, 32),
                // (10,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(10, false)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "10").WithLocation(10, 32),
                // (11,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(11, false)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(11, 32),
                // (13,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-2)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(13, 32),
                // (14,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-1)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(14, 32),
                // (17,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(10)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "10").WithLocation(17, 32),
                // (18,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(11)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(18, 32),
                // (20,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(12, true)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "12").WithLocation(20, 32),
                // (21,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(11, true)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(21, 32),
                // (24,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(0, true)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "0").WithLocation(24, 32),
                // (25,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-1, true)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(25, 32),
                // (26,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(int.MinValue, true)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "int.MinValue").WithLocation(26, 32),
                // (29,39): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(value: -1)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(29, 39),
                // (30,39): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(value: -1, fromEnd: false)];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(30, 39)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementAccess_Bounds_05()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Buffer10<int> f = default;
        f[0] = 111;
        f[9] = 999;
        Test(f, -1);
        Test(f, 0);
        Test(f, 9);
        Test(f, 10);
    }

    static void Test(Buffer10<int> f, int index)
    {
        System.Console.Write(' ');

        try
        {
            System.Console.Write(f[index]);
        }
        catch (System.IndexOutOfRangeException)
        {
            System.Console.Write(""Throw"");
        }
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: " Throw 111 999 Throw", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void Slice_Bounds_01()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[-2..];
        _ = f[-1..];
        _ = f[0..];
        _ = f[9..];
        _ = f[10..];
        _ = f[11..];
        _ = f[12..];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[-2..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(6, 15),
                // (7,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[-1..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(7, 15),
                // (11,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[11..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(11, 15),
                // (12,15): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[12..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "12").WithLocation(12, 15)
                );
        }

        [Fact]
        public void Slice_Bounds_02()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[..-2];
        _ = f[..-1];
        _ = f[..0];
        _ = f[..9];
        _ = f[..10];
        _ = f[..11];
        _ = f[..12];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..-2];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(6, 17),
                // (7,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..-1];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(7, 17),
                // (11,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..11];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(11, 17),
                // (12,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..12];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "12").WithLocation(12, 17)
                );
        }

        [Fact]
        public void Slice_Bounds_03()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[new System.Index(-2)..];
        _ = f[new System.Index(-1)..];
        _ = f[new System.Index(0)..];
        _ = f[new System.Index(9)..];
        _ = f[new System.Index(10)..];
        _ = f[new System.Index(11)..];
        _ = f[new System.Index(12)..];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-2)..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-2").WithLocation(6, 32),
                // (7,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(-1)..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "-1").WithLocation(7, 32),
                // (11,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(11)..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "11").WithLocation(11, 32),
                // (12,32): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[new System.Index(12)..];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "12").WithLocation(12, 32)
                );
        }

        [Fact]
        public void Slice_Bounds_04()
        {
            var src = @"
class Program
{
    static void Test(Buffer10<int> f)
    {
        _ = f[..^12];
        _ = f[..^11];
        _ = f[..^10];
        _ = f[..^1];
        _ = f[..^0];
        _ = f[..^-1];
        _ = f[..^int.MinValue];
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..^12];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^12").WithLocation(6, 17),
                // (7,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..^11];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^11").WithLocation(7, 17),
                // (11,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..^-1];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^-1").WithLocation(11, 17),
                // (12,17): error CS9166: Index is outside the bounds of the inline array
                //         _ = f[..^int.MinValue];
                Diagnostic(ErrorCode.ERR_InlineArrayIndexOutOfRange, "^int.MinValue").WithLocation(12, 17)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Slice_Bounds_05()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Buffer10<int> f = default;
        Test(f, 0..10);
        Test(f, 9..10);
        Test(f, 9..11);
        Test(f, 10..10);
        Test(f, 10..11);
    }

    static void Test(Buffer10<int> f, System.Range range)
    {
        System.Console.Write(' ');

        try
        {
            System.Console.Write(f[range].Length);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            System.Console.Write(""Throw"");
        }
    }
}
";

            var comp = CreateCompilation(src + Buffer10Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: " 10 1 Throw 0 Throw", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void ElementAccess_RuntimeSupport()
        {
            var src = @"
var b = new Buffer();
_ = b[2];


[System.Runtime.CompilerServices.InlineArray(3)]
struct Buffer
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (3,5): error CS9171: Target runtime doesn't support inline array types.
                // _ = b[2];
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, "b[2]").WithLocation(3, 5),
                // (7,8): error CS9171: Target runtime doesn't support inline array types.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, "Buffer").WithLocation(7, 8)
                );

            Assert.False(comp.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes));

            var vbComp = CreateVisualBasicCompilation("", referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Net70, null));
            Assert.False(vbComp.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes));
        }

        [Fact]
        public void Slice_RuntimeSupport()
        {
            var src = @"
var b = new Buffer();
_ = b[2..];


[System.Runtime.CompilerServices.InlineArray(3)]
struct Buffer
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (3,5): error CS9171: Target runtime doesn't support inline array types.
                // _ = b[2..];
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, "b[2..]").WithLocation(3, 5),
                // (7,8): error CS9171: Target runtime doesn't support inline array types.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, "Buffer").WithLocation(7, 8)
                );
        }

        [Fact]
        public void Conversion_RuntimeSupport()
        {
            var src = @"
var b = new Buffer();
_ = (System.Span<int>)b;


[System.Runtime.CompilerServices.InlineArray(3)]
struct Buffer
{
    private int _element0;
}
" + InlineArrayAttributeDefinition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (3,5): error CS9171: Target runtime doesn't support inline array types.
                // _ = (System.Span<int>)b;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, "(System.Span<int>)b").WithLocation(3, 5),
                // (7,8): error CS9171: Target runtime doesn't support inline array types.
                // struct Buffer
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, "Buffer").WithLocation(7, 8)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementPointer_01()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        b[0] = 1;

        int* p1 = &b[0];
        (*p1)++;
        System.Console.Write(b[0]);

        Buffer10<int>* p2 = &b;
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "2", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementPointer_02()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        Test(default);
    }

    static void Test(Buffer10<int> b)
    {
        b[0] = 1;

        int* p1 = &b[0];
        (*p1)++;
        System.Console.Write(b[0]);

        Buffer10<int>* p2 = &b;
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "2", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void ElementPointer_03()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        Test(ref b);
    }

    static void Test(ref Buffer10<int> b)
    {
        b[0] = 1;

        int* p1 = &b[0];
        (*p1)++;
        System.Console.Write(b[0]);

        Buffer10<int>* p2 = &b;
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (14,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* p1 = &b[0];
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&b[0]").WithLocation(14, 19),
                // (18,29): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         Buffer10<int>* p2 = &b;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&b").WithLocation(18, 29)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementPointer_04()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        Test(ref b);
    }

    static void Test(ref Buffer10<int> b)
    {
        b[0] = 1;

        fixed (int* p1 = &b[0])
        {
            (*p1)++;
        }

        System.Console.Write(b[0]);

        fixed (Buffer10<int>* p2 = &b) {}
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "2", verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void ElementPointer_05()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        Buffer10<int> b = default;
        Test(b);
    }

    static void Test(Buffer10<int> b)
    {
        b[0] = 1;

        fixed (int* p1 = &b[0])
        {
            (*p1)++;
        }

        System.Console.Write(b[0]);

        fixed (Buffer10<int>* p2 = &b) {}
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (14,26): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (int* p1 = &b[0])
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&b[0]").WithLocation(14, 26),
                // (21,36): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (Buffer10<int>* p2 = &b) {}
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&b").WithLocation(21, 36)
                );
        }

        [Fact]
        public void ElementPointer_06()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        fixed (int* p1 = &GetBuffer()[0])
        {
            (*p1)++;
        }

        int* p2 = &GetBuffer()[0];
    }

    static Buffer10<int> GetBuffer() => default;
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (6,27): error CS0211: Cannot take the address of the given expression
                //         fixed (int* p1 = &GetBuffer()[0])
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "GetBuffer()[0]").WithLocation(6, 27),
                // (11,20): error CS0211: Cannot take the address of the given expression
                //         int* p2 = &GetBuffer()[0];
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "GetBuffer()[0]").WithLocation(11, 20)
                );
        }

        [Fact]
        public void ElementPointer_07()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        Buffer10<int> b = default;

        fixed (void* p1 = &b[..])
        {
        }

        void* p2 = &b[..];
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (8,28): error CS0211: Cannot take the address of the given expression
                //         fixed (void* p1 = &b[..])
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "b[..]").WithLocation(8, 28),
                // (12,21): error CS0211: Cannot take the address of the given expression
                //         void* p2 = &b[..];
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "b[..]").WithLocation(12, 21)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void ElementPointer_08()
        {
            var src = @"
unsafe class Program
{
    static void Main()
    {
        S s = new S(1);

        int* p1 = &s.b[0];
        (*p1)++;
        System.Console.Write(s.b[0]);

        Buffer10<int>* p2 = &s.b;
    }
}

struct S
{
    public readonly Buffer10<int> b;

    public S(int x)
    {
        b[0] = x;
    }
}
" + Buffer10Definition;

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            CompileAndVerify(comp, expectedOutput: "2", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_Variable_01()
        {
            var src = @"
class C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(x);
        Test(x);
    }

    static void Test(C x)
    {
        foreach (ref int y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
            y *= -1;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 111 112 113 114 -111 -112 -113 -114").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       46 (0x2e)
  .maxstack  3
  .locals init (Buffer4<int>& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer4<int> C.F""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_0029
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
  IL_0012:  ldc.i4.s   32
  IL_0014:  call       ""void System.Console.Write(char)""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  call       ""void System.Console.Write(int)""
  IL_0020:  dup
  IL_0021:  ldind.i4
  IL_0022:  ldc.i4.m1
  IL_0023:  mul
  IL_0024:  stind.i4
  IL_0025:  ldloc.1
  IL_0026:  ldc.i4.1
  IL_0027:  add
  IL_0028:  stloc.1
  IL_0029:  ldloc.1
  IL_002a:  ldc.i4.4
  IL_002b:  blt.s      IL_000b
  IL_002d:  ret
}
");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single().Expression;
            Assert.Equal("x.F", f.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer4<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer4<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(f).IsIdentity);

            var forEachInfo = model.GetForEachStatementInfo((ForEachStatementSyntax)f.Parent);

            Assert.False(forEachInfo.IsAsynchronous);
            Assert.Equal("System.Span<System.Int32>.Enumerator System.Span<System.Int32>.GetEnumerator()", forEachInfo.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Span<System.Int32>.Enumerator.MoveNext()", forEachInfo.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("ref System.Int32 System.Span<System.Int32>.Enumerator.Current { get; }", forEachInfo.CurrentProperty.ToTestDisplayString());
            Assert.Null(forEachInfo.DisposeMethod);
            Assert.Equal("System.Int32", forEachInfo.ElementType.ToTestDisplayString());
            Assert.True(forEachInfo.ElementConversion.IsIdentity);
            Assert.True(forEachInfo.CurrentConversion.IsIdentity);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_Variable_02()
        {
            var src = @"
class C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(x);
        Test(x);
    }

    static void Test(C x)
    {
        foreach (ref readonly int y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
            System.Runtime.CompilerServices.Unsafe.AsRef(in y) *= -1;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 111 112 113 114 -111 -112 -113 -114").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (Buffer4<int>& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer4<int> C.F""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_002e
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
  IL_0012:  ldc.i4.s   32
  IL_0014:  call       ""void System.Console.Write(char)""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  call       ""void System.Console.Write(int)""
  IL_0020:  call       ""ref int System.Runtime.CompilerServices.Unsafe.AsRef<int>(scoped ref readonly int)""
  IL_0025:  dup
  IL_0026:  ldind.i4
  IL_0027:  ldc.i4.m1
  IL_0028:  mul
  IL_0029:  stind.i4
  IL_002a:  ldloc.1
  IL_002b:  ldc.i4.1
  IL_002c:  add
  IL_002d:  stloc.1
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.4
  IL_0030:  blt.s      IL_000b
  IL_0032:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_Variable_03()
        {
            var src = @"
class C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(x);
    }

    static void Test(C x)
    {
        foreach (var y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 111 112 113 114").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (Buffer4<int>& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer4<int> C.F""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_0023
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.s   32
  IL_0015:  call       ""void System.Console.Write(char)""
  IL_001a:  call       ""void System.Console.Write(int)""
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  add
  IL_0022:  stloc.1
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.4
  IL_0025:  blt.s      IL_000b
  IL_0027:  ret
}
");
        }

        [Fact]
        public void Foreach_Variable_04()
        {
            var src = @"
class Program
{
    static ref int Test1(Buffer4<int> x)
    {
        foreach (ref int y in x)
        {
            return ref y;
        }

        throw null;
    }

    static ref int Test2(ref Buffer4<int> x)
    {
        foreach (ref int z in x)
        {
            return ref z;
        }

        throw null;
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (8,24): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(8, 24)
                );
        }

        [Fact]
        public void Foreach_Variable_05_MissingSpan()
        {
            var src = @"
class Program
{
    static void Test(Buffer4<int> x)
    {
        foreach (var y in x)
        {
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.MakeTypeMissing(WellKnownType.System_Span_T);
            comp.VerifyDiagnostics(
                // (6,27): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         foreach (var y in x)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "x").WithArguments("System.Span`1").WithLocation(6, 27)
                );
        }

        [Fact]
        public void Foreach_Variable_06_LanguageVersion()
        {
            var src = @"
class Program
{
    static void Test(Buffer4<int> x)
    {
        foreach (var y in x)
        {
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (6,27): error CS9058: Feature 'inline arrays' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         foreach (var y in x)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "x").WithArguments("inline arrays", "12.0").WithLocation(6, 27)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_Variable_ReadOnly_01()
        {
            var src = @"
struct C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(in x);
        Test(in x);
    }

    static void Test(in C x)
    {
        foreach (ref readonly int y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
            System.Runtime.CompilerServices.Unsafe.AsRef(in y) *= -1;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 111 112 113 114 -111 -112 -113 -114").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (Buffer4<int>& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer4<int> C.F""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_002e
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
  IL_0012:  ldc.i4.s   32
  IL_0014:  call       ""void System.Console.Write(char)""
  IL_0019:  dup
  IL_001a:  ldind.i4
  IL_001b:  call       ""void System.Console.Write(int)""
  IL_0020:  call       ""ref int System.Runtime.CompilerServices.Unsafe.AsRef<int>(scoped ref readonly int)""
  IL_0025:  dup
  IL_0026:  ldind.i4
  IL_0027:  ldc.i4.m1
  IL_0028:  mul
  IL_0029:  stind.i4
  IL_002a:  ldloc.1
  IL_002b:  ldc.i4.1
  IL_002c:  add
  IL_002d:  stloc.1
  IL_002e:  ldloc.1
  IL_002f:  ldc.i4.4
  IL_0030:  blt.s      IL_000b
  IL_0032:  ret
}
");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var f = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single().Expression;
            Assert.Equal("x.F", f.ToString());

            var typeInfo = model.GetTypeInfo(f);

            Assert.Equal("Buffer4<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer4<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(f).IsIdentity);

            var forEachInfo = model.GetForEachStatementInfo((ForEachStatementSyntax)f.Parent);

            Assert.False(forEachInfo.IsAsynchronous);
            Assert.Equal("System.ReadOnlySpan<System.Int32>.Enumerator System.ReadOnlySpan<System.Int32>.GetEnumerator()", forEachInfo.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.ReadOnlySpan<System.Int32>.Enumerator.MoveNext()", forEachInfo.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32 System.ReadOnlySpan<System.Int32>.Enumerator.Current { get; }", forEachInfo.CurrentProperty.ToTestDisplayString());
            Assert.Null(forEachInfo.DisposeMethod);
            Assert.Equal("System.Int32", forEachInfo.ElementType.ToTestDisplayString());
            Assert.True(forEachInfo.ElementConversion.IsIdentity);
            Assert.True(forEachInfo.CurrentConversion.IsIdentity);
        }

        [Fact]
        public void Foreach_Variable_ReadOnly_02()
        {
            var src = @"
public struct C
{
    public Buffer4<int> F;
}

class Program
{
    static void Test(in C x)
    {
        foreach (ref int y in x.F)
        {
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (11,31): error CS8332: Cannot assign to a member of variable 'x' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         foreach (ref int y in x.F)
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "x.F").WithArguments("variable", "x").WithLocation(11, 31)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_Variable_ReadOnly_03()
        {
            var src = @"
struct C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(in x);
    }

    static void Test(in C x)
    {
        foreach (var y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
        }
    }
}

[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer4<T>
{
    private T _element0;

    public System.Collections.Generic.IEnumerator<T> GetEnumerator()
    {
        throw null;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 111 112 113 114").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (Buffer4<int>& V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer4<int> C.F""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_0023
  IL_000b:  ldloc.0
  IL_000c:  ldloc.1
  IL_000d:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.s   32
  IL_0015:  call       ""void System.Console.Write(char)""
  IL_001a:  call       ""void System.Console.Write(int)""
  IL_001f:  ldloc.1
  IL_0020:  ldc.i4.1
  IL_0021:  add
  IL_0022:  stloc.1
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.4
  IL_0025:  blt.s      IL_000b
  IL_0027:  ret
}
");
        }

        [Fact]
        public void Foreach_Variable_ReadOnly_04()
        {
            var src = @"
class Program
{
    static ref readonly int Test1(in Buffer4<int> x)
    {
        foreach (ref readonly int y in x)
        {
            return ref y;
        }

        throw null;
    }

    static ref readonly int Test2()
    {
        Buffer4<int> x = default;
        ref readonly Buffer4<int> xx = ref x;
        foreach (ref readonly int yy in xx)
        {
            return ref yy;
        }

        throw null;
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (20,24): error CS8157: Cannot return 'yy' by reference because it was initialized to a value that cannot be returned by reference
                //             return ref yy;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "yy").WithArguments("yy").WithLocation(20, 24)
                );
        }

        [Fact]
        public void Foreach_Variable_ReadOnly_05_MissingSpan()
        {
            var src = @"
class Program
{
    static void Test(in Buffer4<int> x)
    {
        foreach (var y in x)
        {
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.MakeTypeMissing(WellKnownType.System_ReadOnlySpan_T);
            comp.VerifyDiagnostics(
                // (6,27): error CS0518: Predefined type 'System.ReadOnlySpan`1' is not defined or imported
                //         foreach (var y in x)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "x").WithArguments("System.ReadOnlySpan`1").WithLocation(6, 27)
                );
        }

        [Fact]
        public void Foreach_Value_01()
        {
            var src = @"
class C
{
    public Buffer4<int> F = default;
}

class Program
{
    static void Test(C x)
    {
        foreach (ref readonly int y in GetBuffer(x))
        {
        }
    }

    static Buffer4<int> GetBuffer(C x) => x.F;

    void Test(System.Collections.Generic.IEnumerable<int> x)
    {
        foreach (ref readonly int y in x)
        {}
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            // The error wording is somewhat confusing because 'ref readonly' doesn't actually require assignability.
            // However, it looks like the issue isn't inline array specific (see the second error). In other words,
            // this is a pre-existing condition.
            comp.VerifyDiagnostics(
                // (11,40): error CS1510: A ref or out value must be an assignable variable
                //         foreach (ref readonly int y in GetBuffer(x))
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "GetBuffer(x)").WithLocation(11, 40),
                // (20,40): error CS1510: A ref or out value must be an assignable variable
                //         foreach (ref readonly int y in x)
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(20, 40)
                );
        }

        [Fact]
        public void Foreach_Value_02()
        {
            var src = @"
class C
{
    public Buffer4<int> F = default;
}

class Program
{
    static void Test(C x)
    {
        foreach (ref int y in GetBuffer(x))
        {
        }
    }

    static Buffer4<int> GetBuffer(C x) => x.F;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (11,31): error CS1510: A ref or out value must be an assignable variable
                //         foreach (ref int y in GetBuffer(x))
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "GetBuffer(x)").WithLocation(11, 31)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_Value_03()
        {
            var src = @"
class C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(x);
    }

    static void Test(C x)
    {
        foreach (var y in GetBuffer(x))
        {
            System.Console.Write(' ');
            System.Console.Write(y);
            _ = new Buffer4<int>().ToString();
        }
    }

    static Buffer4<int> GetBuffer(C x) => x.F;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 111 112 113 114").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (Buffer4<int> V_0,
                Buffer4<int>& V_1,
                int V_2,
                Buffer4<int> V_3)
  IL_0000:  ldarg.0
  IL_0001:  call       ""Buffer4<int> Program.GetBuffer(C)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  stloc.1
  IL_000a:  ldc.i4.0
  IL_000b:  stloc.2
  IL_000c:  br.s       IL_003b
  IL_000e:  ldloc.1
  IL_000f:  ldloc.2
  IL_0010:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
  IL_0015:  ldind.i4
  IL_0016:  ldc.i4.s   32
  IL_0018:  call       ""void System.Console.Write(char)""
  IL_001d:  call       ""void System.Console.Write(int)""
  IL_0022:  ldloca.s   V_3
  IL_0024:  dup
  IL_0025:  initobj    ""Buffer4<int>""
  IL_002b:  constrained. ""Buffer4<int>""
  IL_0031:  callvirt   ""string object.ToString()""
  IL_0036:  pop
  IL_0037:  ldloc.2
  IL_0038:  ldc.i4.1
  IL_0039:  add
  IL_003a:  stloc.2
  IL_003b:  ldloc.2
  IL_003c:  ldc.i4.4
  IL_003d:  blt.s      IL_000e
  IL_003f:  ret
}
");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var collection = tree.GetRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single().Expression;
            Assert.Equal("GetBuffer(x)", collection.ToString());

            var typeInfo = model.GetTypeInfo(collection);

            Assert.Equal("Buffer4<System.Int32>", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("Buffer4<System.Int32>", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(collection).IsIdentity);

            var forEachInfo = model.GetForEachStatementInfo((ForEachStatementSyntax)collection.Parent);

            Assert.False(forEachInfo.IsAsynchronous);
            Assert.Equal("System.ReadOnlySpan<System.Int32>.Enumerator System.ReadOnlySpan<System.Int32>.GetEnumerator()", forEachInfo.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.ReadOnlySpan<System.Int32>.Enumerator.MoveNext()", forEachInfo.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32 System.ReadOnlySpan<System.Int32>.Enumerator.Current { get; }", forEachInfo.CurrentProperty.ToTestDisplayString());
            Assert.Null(forEachInfo.DisposeMethod);
            Assert.Equal("System.Int32", forEachInfo.ElementType.ToTestDisplayString());
            Assert.True(forEachInfo.ElementConversion.IsIdentity);
            Assert.True(forEachInfo.CurrentConversion.IsIdentity);
        }

        [Fact]
        public void Foreach_Value_04()
        {
            var src = @"
class Program
{
    static ref int Test()
    {
        foreach (var y in GetBuffer())
        {
            return ref y;
        }

        throw null;
    }

    static Buffer4<int> GetBuffer() => default;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (8,24): error CS1657: Cannot use 'y' as a ref or out value because it is a 'foreach iteration variable'
                //             return ref y;
                Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "y").WithArguments("y", "foreach iteration variable").WithLocation(8, 24)
                );
        }

        [Fact]
        public void Foreach_Value_05()
        {
            var src = @"
class Program
{
    static System.Span<int> Test1()
    {
        System.Span<int> x = stackalloc int[2];
        foreach (var y in GetBuffer(x))
        {
            return y;
        }

        throw null;
    }

    static System.Span<int> Test2(System.Span<int> xx)
    {
        foreach (var yy in GetBuffer(xx))
        {
            return yy;
        }

        throw null;
    }

    static Buffer10 GetBuffer(System.Span<int> x)
    {
        throw null;
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public ref struct Buffer10
{
    private System.Span<int> _element0;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (7,27): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         foreach (var y in GetBuffer(x))
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "GetBuffer(x)").WithArguments("System.Span<int>").WithLocation(7, 27),
                // (9,20): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //             return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(9, 20),
                // (17,28): error CS0306: The type 'Span<int>' may not be used as a type argument
                //         foreach (var yy in GetBuffer(xx))
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "GetBuffer(xx)").WithArguments("System.Span<int>").WithLocation(17, 28),
                // (34,30): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private System.Span<int> _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(34, 30)
                );
        }

        [Fact]
        public void AwaitForeach_01()
        {
            var src = @"
class Program
{
    public static async void M()
    {
        await foreach(var s in GetBuffer())
        {
        }
    }

    static Buffer4<int> GetBuffer() => default;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,32): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'Buffer4<int>' because 'Buffer4<int>' does not contain a public instance or extension definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
                //         await foreach(var s in GetBuffer())
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync, "GetBuffer()").WithArguments("Buffer4<int>", "GetAsyncEnumerator").WithLocation(6, 32)
                );
        }

        [Fact]
        public void AwaitForeach_02()
        {
            var src = @"
class Program
{
    public static async void M(Buffer4<int> x)
    {
        await foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct Span<T>
    {
        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public ref T Current => throw null;

            public bool MoveNext() => false;
        }

        public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
        {
            throw null;
        }

        public sealed class AsyncEnumerator
        {
            public async System.Threading.Tasks.Task<bool> MoveNextAsync(int ok = 1)
            {
                await System.Threading.Tasks.Task.Yield();
                return false;
            }
            public T Current
            {
                get => throw null;
            }
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,32): error CS8415: Asynchronous foreach statement cannot operate on variables of type 'Buffer4<int>' because 'Buffer4<int>' does not contain a public instance or extension definition for 'GetAsyncEnumerator'. Did you mean 'foreach' rather than 'await foreach'?
                //         await foreach(var s in x)
                Diagnostic(ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync, "x").WithArguments("Buffer4<int>", "GetAsyncEnumerator").WithLocation(6, 32)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AwaitForeach_03()
        {
            var src = @"
struct C
{
    public Buffer4<int> F;
}

class Program
{
    static async System.Threading.Tasks.Task Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;

        await foreach (var y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
        }
    }
}

[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer4<T>
{
    private T _element0;

    public AsyncEnumerator GetAsyncEnumerator(System.Threading.CancellationToken token = default)
    {
        return new AsyncEnumerator(new[] { this[0], this[1], this[2], this[3] });
    }

    public sealed class AsyncEnumerator
    {
        private readonly System.Collections.IEnumerator _underlying;

        public AsyncEnumerator(T[] buffer)
        {
            _underlying = buffer.GetEnumerator();
        }
        
        public async System.Threading.Tasks.Task<bool> MoveNextAsync(int ok = 1)
        {
            await System.Threading.Tasks.Task.Yield();
            return _underlying.MoveNext();
        }
        public T Current
        {
            get => (T)_underlying.Current;
        }
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: " 111 112 113 114").VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_Extension_01()
        {
            var src = @"
class Program
{
    public static void M(Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct Span<T>
    {
    }
}

static class Ext 
{
    public static Enumerator<T> GetEnumerator<T>(this System.Span<T> f) => default;

    public ref struct Enumerator<T>
    {
        public ref T Current => throw null;

        public bool MoveNext() => false;
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26),
                // (21,62): warning CS0436: The type 'Span<T>' in '' conflicts with the imported type 'Span<T>' in 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. Using the type defined in ''.
                //     public static Enumerator<T> GetEnumerator<T>(this System.Span<T> f) => default;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Span<T>").WithArguments("", "System.Span<T>", "System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Span<T>").WithLocation(21, 62)
                );
        }

        [Fact]
        public void Foreach_RefMismatch_01()
        {
            var src = @"
class Program
{
    public static void M(Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct Span<T>
    {

        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public ref readonly T Current => throw null;

            public bool MoveNext() => false;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_RefMismatch_02()
        {
            var src = @"
class Program
{
    public static void M(Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct Span<T>
    {

        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public T Current => throw null;

            public bool MoveNext() => false;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_RefMismatch_03()
        {
            var src = @"
class Program
{
    public static void M(in Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {

        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public ref T Current => throw null;

            public bool MoveNext() => false;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_RefMismatch_04()
        {
            var src = @"
class Program
{
    public static void M(in Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {

        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public T Current => throw null;

            public bool MoveNext() => false;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_ElementTypeMismatch_01()
        {
            var src = @"
class Program
{
    public static void M(in Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public ref readonly object Current => throw null;

            public bool MoveNext() => false;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_NotEnumerableSpan_01()
        {
            var src = @"
class Program
{
    public static void M(in Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x").WithArguments("Buffer4<int>").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_NotEnumerableSpan_02()
        {
            var src = @"
class Program
{
    public static void M(in Buffer4<int> x)
    {
        foreach(var s in x)
        {
        }
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        public Enumerator GetEnumerator() => default;

        public ref struct Enumerator
        {
            public bool MoveNext() => false;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,26): error CS0117: 'ReadOnlySpan<int>.Enumerator' does not contain a definition for 'Current'
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_NoSuchMember, "x").WithArguments("System.ReadOnlySpan<int>.Enumerator", "Current").WithLocation(6, 26),
                // (6,26): error CS0202: foreach requires that the return type 'ReadOnlySpan<int>.Enumerator' of 'ReadOnlySpan<int>.GetEnumerator()' must have a suitable public 'MoveNext' method and public 'Current' property
                //         foreach(var s in x)
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "x").WithArguments("System.ReadOnlySpan<int>.Enumerator", "System.ReadOnlySpan<int>.GetEnumerator()").WithLocation(6, 26)
                );
        }

        [Fact]
        public void Foreach_NotEnumerableSpan_03_Fallback()
        {
            var src = @"
struct C
{
    public Buffer4<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = 111;
        x.F[1] = 112;
        x.F[2] = 113;
        x.F[3] = 114;
        Test(in x);
    }

    static void Test(in C x)
    {
        foreach (var y in x.F)
        {
            System.Console.Write(' ');
            System.Console.Write(y);
        }
    }
}

[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer4<T>
{
    private T _element0;

    public System.Collections.Generic.IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < 4; i++)
        {
            yield return this[i];
        }
    }
}

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (21,27): error CS9185: foreach statement on an inline array of type 'Buffer4<int>' is not supported
                //         foreach (var y in x.F)
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "x.F").WithArguments("Buffer4<int>").WithLocation(21, 27)
                );
        }

        [Fact]
        public void Foreach_UnsupportedElementType_01()
        {
            var src = @"
class Program
{
    public void M()
    {
        foreach(var s in GetBuffer())
        {
        }
    }

    static Buffer GetBuffer() => default;
}

[System.Runtime.CompilerServices.InlineArray(10)]
unsafe struct Buffer
{
    private void* _element0;

    public Enumerator GetEnumerator() => throw null;

    public class Enumerator
    {
        public bool MoveNext() => false;
        public int Current => throw null;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
                // (6,26): error CS0306: The type 'void*' may not be used as a type argument
                //         foreach(var s in GetBuffer())
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "GetBuffer()").WithArguments("void*").WithLocation(6, 26),
                // (17,19): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private void* _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(17, 19)
                );
        }

        [Fact]
        public void Foreach_UnsupportedElementType_02()
        {
            var src = @"
class Program
{
    public void M()
    {
        foreach(var s in GetBuffer())
        {
        }
    }

    static Buffer GetBuffer() => default;
}

[System.Runtime.CompilerServices.InlineArray(10)]
ref struct Buffer
{
    private ref int _element0;

    public Enumerator GetEnumerator() => throw null;

    public class Enumerator
    {
        public bool MoveNext() => false;
        public int Current => throw null;
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyEmitDiagnostics(
                // (6,26): error CS9185: foreach statement on an inline array of type 'Buffer' is not supported
                //         foreach(var s in GetBuffer())
                Diagnostic(ErrorCode.ERR_InlineArrayForEachNotSupported, "GetBuffer()").WithArguments("Buffer").WithLocation(6, 26),
                // (17,21): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private ref int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(17, 21)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_01()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static private Buffer4<int> F = default;
    private static int index = 0;

    static void Main()
    {
        Test().Wait();
    }

    static async Task Test()
    {
        await Task.Yield();

        foreach (var y in GetBuffer())
        {
            Increment();
            System.Console.Write(' ');
            System.Console.Write(y);
        }

        await Task.Yield();
    }

    static ref Buffer4<int> GetBuffer()
    {
        System.Console.Write(-1);
        return ref F;
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            F[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      295 (0x127)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Buffer4<int>& V_3,
                int V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d5
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__3)""
    IL_0046:  leave      IL_0126
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  call       ""ref Buffer4<int> Program.GetBuffer()""
    IL_0073:  stloc.3
    IL_0074:  ldc.i4.0
    IL_0075:  stloc.s    V_4
    IL_0077:  br.s       IL_0099
    IL_0079:  ldloc.3
    IL_007a:  ldloc.s    V_4
    IL_007c:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
    IL_0081:  ldind.i4
    IL_0082:  call       ""void Program.Increment()""
    IL_0087:  ldc.i4.s   32
    IL_0089:  call       ""void System.Console.Write(char)""
    IL_008e:  call       ""void System.Console.Write(int)""
    IL_0093:  ldloc.s    V_4
    IL_0095:  ldc.i4.1
    IL_0096:  add
    IL_0097:  stloc.s    V_4
    IL_0099:  ldloc.s    V_4
    IL_009b:  ldc.i4.4
    IL_009c:  blt.s      IL_0079
    IL_009e:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_00a3:  stloc.2
    IL_00a4:  ldloca.s   V_2
    IL_00a6:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_00ab:  stloc.1
    IL_00ac:  ldloca.s   V_1
    IL_00ae:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00b3:  brtrue.s   IL_00f1
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00be:  ldarg.0
    IL_00bf:  ldloc.1
    IL_00c0:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_00cb:  ldloca.s   V_1
    IL_00cd:  ldarg.0
    IL_00ce:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__3)""
    IL_00d3:  leave.s    IL_0126
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00db:  stloc.1
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.m1
    IL_00ea:  dup
    IL_00eb:  stloc.0
    IL_00ec:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00f1:  ldloca.s   V_1
    IL_00f3:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00f8:  leave.s    IL_0113
  }
  catch System.Exception
  {
    IL_00fa:  stloc.s    V_5
    IL_00fc:  ldarg.0
    IL_00fd:  ldc.i4.s   -2
    IL_00ff:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_010a:  ldloc.s    V_5
    IL_010c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0111:  leave.s    IL_0126
  }
  IL_0113:  ldarg.0
  IL_0114:  ldc.i4.s   -2
  IL_0116:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_011b:  ldarg.0
  IL_011c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
  IL_0121:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0126:  ret
}
");

            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_02()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        Test(c).Wait();
    }

    static async Task Test(C x)
    {
        foreach (var y in x.F)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            await Task.Yield();
            await Task.Delay(2);
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            c.F[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      357 (0x165)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_009a
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.1
    IL_000f:  beq        IL_00f2
    IL_0014:  ldarg.0
    IL_0015:  ldarg.0
    IL_0016:  ldfld      ""C Program.<Test>d__3.x""
    IL_001b:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_0020:  ldarg.0
    IL_0021:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_0026:  ldfld      ""Buffer4<int> C.F""
    IL_002b:  pop
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.0
    IL_002e:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0033:  br         IL_0123
    IL_0038:  ldarg.0
    IL_0039:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_003e:  ldflda     ""Buffer4<int> C.F""
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0049:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
    IL_004e:  ldind.i4
    IL_004f:  call       ""void Program.Increment()""
    IL_0054:  ldc.i4.s   32
    IL_0056:  call       ""void System.Console.Write(char)""
    IL_005b:  call       ""void System.Console.Write(int)""
    IL_0060:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0065:  stloc.2
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_006d:  stloc.1
    IL_006e:  ldloca.s   V_1
    IL_0070:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0075:  brtrue.s   IL_00b6
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.0
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0080:  ldarg.0
    IL_0081:  ldloc.1
    IL_0082:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0087:  ldarg.0
    IL_0088:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_008d:  ldloca.s   V_1
    IL_008f:  ldarg.0
    IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__3)""
    IL_0095:  leave      IL_0164
    IL_009a:  ldarg.0
    IL_009b:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00a0:  stloc.1
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00a7:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00ad:  ldarg.0
    IL_00ae:  ldc.i4.m1
    IL_00af:  dup
    IL_00b0:  stloc.0
    IL_00b1:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00b6:  ldloca.s   V_1
    IL_00b8:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00bd:  ldc.i4.2
    IL_00be:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_00c3:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00c8:  stloc.3
    IL_00c9:  ldloca.s   V_3
    IL_00cb:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_00d0:  brtrue.s   IL_010e
    IL_00d2:  ldarg.0
    IL_00d3:  ldc.i4.1
    IL_00d4:  dup
    IL_00d5:  stloc.0
    IL_00d6:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00db:  ldarg.0
    IL_00dc:  ldloc.3
    IL_00dd:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Test>d__3.<>u__2""
    IL_00e2:  ldarg.0
    IL_00e3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_00e8:  ldloca.s   V_3
    IL_00ea:  ldarg.0
    IL_00eb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<Test>d__3)""
    IL_00f0:  leave.s    IL_0164
    IL_00f2:  ldarg.0
    IL_00f3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Test>d__3.<>u__2""
    IL_00f8:  stloc.3
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Program.<Test>d__3.<>u__2""
    IL_00ff:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0105:  ldarg.0
    IL_0106:  ldc.i4.m1
    IL_0107:  dup
    IL_0108:  stloc.0
    IL_0109:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_010e:  ldloca.s   V_3
    IL_0110:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0115:  ldarg.0
    IL_0116:  ldarg.0
    IL_0117:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_011c:  ldc.i4.1
    IL_011d:  add
    IL_011e:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0123:  ldarg.0
    IL_0124:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0129:  ldc.i4.4
    IL_012a:  blt        IL_0038
    IL_012f:  ldarg.0
    IL_0130:  ldnull
    IL_0131:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_0136:  leave.s    IL_0151
  }
  catch System.Exception
  {
    IL_0138:  stloc.s    V_4
    IL_013a:  ldarg.0
    IL_013b:  ldc.i4.s   -2
    IL_013d:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0142:  ldarg.0
    IL_0143:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_0148:  ldloc.s    V_4
    IL_014a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_014f:  leave.s    IL_0164
  }
  IL_0151:  ldarg.0
  IL_0152:  ldc.i4.s   -2
  IL_0154:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0159:  ldarg.0
  IL_015a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
  IL_015f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0164:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_03()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        s_buffer[1] = 3;

        foreach (ref int y in GetBuffer())
        {
            y *= y;
            System.Console.Write(y);
        }

        await System.Threading.Tasks.Task.Yield();

        System.Console.Write(s_buffer[1]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (10,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 26));

            var expectedOutput = "09009";

            CompileAndVerify(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InAsync_04()
        {
            var src = @"
class Program
{
    static async void Test()
    {
        foreach (int y in GetBuffer())
        {
            await System.Threading.Tasks.Task.Yield();
        }
    }

    static ref Buffer4<int> GetBuffer() => throw null;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait,
        @"foreach (int y in GetBuffer())
        {
            await System.Threading.Tasks.Task.Yield();
        }").WithArguments("Program.GetBuffer()").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_05()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static private Buffer4<int> F = default;
    private static int index = 0;

    static void Main()
    {
        Test().Wait();
    }

    static async Task Test()
    {
        await Task.Yield();

        foreach (var y in GetBuffer())
        {
            Increment();
            System.Console.Write(' ');
            System.Console.Write(y);
        }

        await Task.Yield();
    }

    static ref readonly Buffer4<int> GetBuffer()
    {
        System.Console.Write(-1);
        return ref F;
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            F[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      295 (0x127)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                Buffer4<int>& V_3,
                int V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00d5
    IL_0011:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0016:  stloc.2
    IL_0017:  ldloca.s   V_2
    IL_0019:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001e:  stloc.1
    IL_001f:  ldloca.s   V_1
    IL_0021:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0026:  brtrue.s   IL_0067
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.0
    IL_002a:  dup
    IL_002b:  stloc.0
    IL_002c:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0031:  ldarg.0
    IL_0032:  ldloc.1
    IL_0033:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0038:  ldarg.0
    IL_0039:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_003e:  ldloca.s   V_1
    IL_0040:  ldarg.0
    IL_0041:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__3)""
    IL_0046:  leave      IL_0126
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0051:  stloc.1
    IL_0052:  ldarg.0
    IL_0053:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0058:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.m1
    IL_0060:  dup
    IL_0061:  stloc.0
    IL_0062:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_006e:  call       ""ref readonly Buffer4<int> Program.GetBuffer()""
    IL_0073:  stloc.3
    IL_0074:  ldc.i4.0
    IL_0075:  stloc.s    V_4
    IL_0077:  br.s       IL_0099
    IL_0079:  ldloc.3
    IL_007a:  ldloc.s    V_4
    IL_007c:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
    IL_0081:  ldind.i4
    IL_0082:  call       ""void Program.Increment()""
    IL_0087:  ldc.i4.s   32
    IL_0089:  call       ""void System.Console.Write(char)""
    IL_008e:  call       ""void System.Console.Write(int)""
    IL_0093:  ldloc.s    V_4
    IL_0095:  ldc.i4.1
    IL_0096:  add
    IL_0097:  stloc.s    V_4
    IL_0099:  ldloc.s    V_4
    IL_009b:  ldc.i4.4
    IL_009c:  blt.s      IL_0079
    IL_009e:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_00a3:  stloc.2
    IL_00a4:  ldloca.s   V_2
    IL_00a6:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_00ab:  stloc.1
    IL_00ac:  ldloca.s   V_1
    IL_00ae:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_00b3:  brtrue.s   IL_00f1
    IL_00b5:  ldarg.0
    IL_00b6:  ldc.i4.1
    IL_00b7:  dup
    IL_00b8:  stloc.0
    IL_00b9:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00be:  ldarg.0
    IL_00bf:  ldloc.1
    IL_00c0:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00c5:  ldarg.0
    IL_00c6:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_00cb:  ldloca.s   V_1
    IL_00cd:  ldarg.0
    IL_00ce:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__3)""
    IL_00d3:  leave.s    IL_0126
    IL_00d5:  ldarg.0
    IL_00d6:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00db:  stloc.1
    IL_00dc:  ldarg.0
    IL_00dd:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00e2:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00e8:  ldarg.0
    IL_00e9:  ldc.i4.m1
    IL_00ea:  dup
    IL_00eb:  stloc.0
    IL_00ec:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00f1:  ldloca.s   V_1
    IL_00f3:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00f8:  leave.s    IL_0113
  }
  catch System.Exception
  {
    IL_00fa:  stloc.s    V_5
    IL_00fc:  ldarg.0
    IL_00fd:  ldc.i4.s   -2
    IL_00ff:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0104:  ldarg.0
    IL_0105:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_010a:  ldloc.s    V_5
    IL_010c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0111:  leave.s    IL_0126
  }
  IL_0113:  ldarg.0
  IL_0114:  ldc.i4.s   -2
  IL_0116:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_011b:  ldarg.0
  IL_011c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
  IL_0121:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0126:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_06()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        Test(c).Wait();
    }

    static async Task Test(C x)
    {
        foreach (var y in x.F)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            await Task.Yield();
            await Task.Delay(2);
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      357 (0x165)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse    IL_009a
    IL_000d:  ldloc.0
    IL_000e:  ldc.i4.1
    IL_000f:  beq        IL_00f2
    IL_0014:  ldarg.0
    IL_0015:  ldarg.0
    IL_0016:  ldfld      ""C Program.<Test>d__3.x""
    IL_001b:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_0020:  ldarg.0
    IL_0021:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_0026:  ldfld      ""Buffer4<int> C.F""
    IL_002b:  pop
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.0
    IL_002e:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0033:  br         IL_0123
    IL_0038:  ldarg.0
    IL_0039:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_003e:  ldflda     ""Buffer4<int> C.F""
    IL_0043:  ldarg.0
    IL_0044:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0049:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
    IL_004e:  ldind.i4
    IL_004f:  call       ""void Program.Increment()""
    IL_0054:  ldc.i4.s   32
    IL_0056:  call       ""void System.Console.Write(char)""
    IL_005b:  call       ""void System.Console.Write(int)""
    IL_0060:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0065:  stloc.2
    IL_0066:  ldloca.s   V_2
    IL_0068:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_006d:  stloc.1
    IL_006e:  ldloca.s   V_1
    IL_0070:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0075:  brtrue.s   IL_00b6
    IL_0077:  ldarg.0
    IL_0078:  ldc.i4.0
    IL_0079:  dup
    IL_007a:  stloc.0
    IL_007b:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0080:  ldarg.0
    IL_0081:  ldloc.1
    IL_0082:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_0087:  ldarg.0
    IL_0088:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_008d:  ldloca.s   V_1
    IL_008f:  ldarg.0
    IL_0090:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__3)""
    IL_0095:  leave      IL_0164
    IL_009a:  ldarg.0
    IL_009b:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00a0:  stloc.1
    IL_00a1:  ldarg.0
    IL_00a2:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__3.<>u__1""
    IL_00a7:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00ad:  ldarg.0
    IL_00ae:  ldc.i4.m1
    IL_00af:  dup
    IL_00b0:  stloc.0
    IL_00b1:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00b6:  ldloca.s   V_1
    IL_00b8:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00bd:  ldc.i4.2
    IL_00be:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
    IL_00c3:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00c8:  stloc.3
    IL_00c9:  ldloca.s   V_3
    IL_00cb:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_00d0:  brtrue.s   IL_010e
    IL_00d2:  ldarg.0
    IL_00d3:  ldc.i4.1
    IL_00d4:  dup
    IL_00d5:  stloc.0
    IL_00d6:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_00db:  ldarg.0
    IL_00dc:  ldloc.3
    IL_00dd:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Test>d__3.<>u__2""
    IL_00e2:  ldarg.0
    IL_00e3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_00e8:  ldloca.s   V_3
    IL_00ea:  ldarg.0
    IL_00eb:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<Test>d__3>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<Test>d__3)""
    IL_00f0:  leave.s    IL_0164
    IL_00f2:  ldarg.0
    IL_00f3:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Test>d__3.<>u__2""
    IL_00f8:  stloc.3
    IL_00f9:  ldarg.0
    IL_00fa:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Program.<Test>d__3.<>u__2""
    IL_00ff:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0105:  ldarg.0
    IL_0106:  ldc.i4.m1
    IL_0107:  dup
    IL_0108:  stloc.0
    IL_0109:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_010e:  ldloca.s   V_3
    IL_0110:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0115:  ldarg.0
    IL_0116:  ldarg.0
    IL_0117:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_011c:  ldc.i4.1
    IL_011d:  add
    IL_011e:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0123:  ldarg.0
    IL_0124:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
    IL_0129:  ldc.i4.4
    IL_012a:  blt        IL_0038
    IL_012f:  ldarg.0
    IL_0130:  ldnull
    IL_0131:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
    IL_0136:  leave.s    IL_0151
  }
  catch System.Exception
  {
    IL_0138:  stloc.s    V_4
    IL_013a:  ldarg.0
    IL_013b:  ldc.i4.s   -2
    IL_013d:  stfld      ""int Program.<Test>d__3.<>1__state""
    IL_0142:  ldarg.0
    IL_0143:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
    IL_0148:  ldloc.s    V_4
    IL_014a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_014f:  leave.s    IL_0164
  }
  IL_0151:  ldarg.0
  IL_0152:  ldc.i4.s   -2
  IL_0154:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0159:  ldarg.0
  IL_015a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__3.<>t__builder""
  IL_015f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0164:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_07()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        s_buffer[1] = 3;

        int i = 0;
        foreach (ref readonly int y in GetBuffer())
        {
            System.Console.Write(y);
            s_buffer[i++]++;
            System.Console.Write(y);
            System.Console.Write(' ');
        }

        await System.Threading.Tasks.Task.Yield();

        System.Console.Write(s_buffer[1]);
    }

    static ref readonly Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (11,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(11, 35));

            var expectedOutput = "01 34 01 01 4";

            CompileAndVerify(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                verify: Verification.FailsILVerify, expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                verify: Verification.FailsILVerify, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InAsync_08()
        {
            var src = @"
class Program
{
    static async void Test()
    {
        foreach (int y in GetBuffer())
        {
            await System.Threading.Tasks.Task.Yield();
        }
    }

    static ref readonly Buffer4<int> GetBuffer() => throw null;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait,
        @"foreach (int y in GetBuffer())
        {
            await System.Threading.Tasks.Task.Yield();
        }").WithArguments("Program.GetBuffer()").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_09()
        {
            var src = @"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        Test().Wait();
    }

    static async Task Test()
    {
        foreach (var y in GetBuffer())
        {
            System.Console.Write(' ');
            System.Console.Write(y);
            await System.Threading.Tasks.Task.Yield();
        }
    }

    static Buffer4<int> GetBuffer()
    {
        Buffer4<int> x = default;
        x[0] = 111;
        x[1] = 112;
        x[2] = 113;
        x[3] = 114;
 
        System.Console.Write(-1);
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 111 112 113 114").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext",
@"
{
  // Code size      224 (0xe0)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0076
    IL_000a:  ldarg.0
    IL_000b:  call       ""Buffer4<int> Program.GetBuffer()""
    IL_0010:  stfld      ""Buffer4<int> Program.<Test>d__1.<>7__wrap1""
    IL_0015:  ldarg.0
    IL_0016:  ldc.i4.0
    IL_0017:  stfld      ""int Program.<Test>d__1.<>7__wrap2""
    IL_001c:  br         IL_00a7
    IL_0021:  ldarg.0
    IL_0022:  ldflda     ""Buffer4<int> Program.<Test>d__1.<>7__wrap1""
    IL_0027:  ldarg.0
    IL_0028:  ldfld      ""int Program.<Test>d__1.<>7__wrap2""
    IL_002d:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
    IL_0032:  ldind.i4
    IL_0033:  ldc.i4.s   32
    IL_0035:  call       ""void System.Console.Write(char)""
    IL_003a:  call       ""void System.Console.Write(int)""
    IL_003f:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0044:  stloc.2
    IL_0045:  ldloca.s   V_2
    IL_0047:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_004c:  stloc.1
    IL_004d:  ldloca.s   V_1
    IL_004f:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0054:  brtrue.s   IL_0092
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.0
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      ""int Program.<Test>d__1.<>1__state""
    IL_005f:  ldarg.0
    IL_0060:  ldloc.1
    IL_0061:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__1.<>u__1""
    IL_0066:  ldarg.0
    IL_0067:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__1.<>t__builder""
    IL_006c:  ldloca.s   V_1
    IL_006e:  ldarg.0
    IL_006f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Test>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Test>d__1)""
    IL_0074:  leave.s    IL_00df
    IL_0076:  ldarg.0
    IL_0077:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__1.<>u__1""
    IL_007c:  stloc.1
    IL_007d:  ldarg.0
    IL_007e:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Test>d__1.<>u__1""
    IL_0083:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.m1
    IL_008b:  dup
    IL_008c:  stloc.0
    IL_008d:  stfld      ""int Program.<Test>d__1.<>1__state""
    IL_0092:  ldloca.s   V_1
    IL_0094:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0099:  ldarg.0
    IL_009a:  ldarg.0
    IL_009b:  ldfld      ""int Program.<Test>d__1.<>7__wrap2""
    IL_00a0:  ldc.i4.1
    IL_00a1:  add
    IL_00a2:  stfld      ""int Program.<Test>d__1.<>7__wrap2""
    IL_00a7:  ldarg.0
    IL_00a8:  ldfld      ""int Program.<Test>d__1.<>7__wrap2""
    IL_00ad:  ldc.i4.4
    IL_00ae:  blt        IL_0021
    IL_00b3:  leave.s    IL_00cc
  }
  catch System.Exception
  {
    IL_00b5:  stloc.3
    IL_00b6:  ldarg.0
    IL_00b7:  ldc.i4.s   -2
    IL_00b9:  stfld      ""int Program.<Test>d__1.<>1__state""
    IL_00be:  ldarg.0
    IL_00bf:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__1.<>t__builder""
    IL_00c4:  ldloc.3
    IL_00c5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ca:  leave.s    IL_00df
  }
  IL_00cc:  ldarg.0
  IL_00cd:  ldc.i4.s   -2
  IL_00cf:  stfld      ""int Program.<Test>d__1.<>1__state""
  IL_00d4:  ldarg.0
  IL_00d5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Test>d__1.<>t__builder""
  IL_00da:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00df:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "-1 111 112 113 114").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_10()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        s_buffer[1] = 3;

        ref Buffer4<int> buffer = ref GetBuffer();
        foreach (ref int y in buffer)
        {
            y *= y;
            System.Console.Write(y);
        }

        await System.Threading.Tasks.Task.Yield();

        System.Console.Write(s_buffer[1]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (10,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref Buffer4<int> buffer = ref GetBuffer();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "buffer").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 26),
                // (11,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in buffer)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(11, 26));

            var expectedOutput = "09009";

            CompileAndVerify(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InAsync_11()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        s_buffer[1] = 3;

        foreach (ref int y in GetBuffer())
        {
            await System.Threading.Tasks.Task.Yield();
            y *= y;
            System.Console.Write(y);
        }

        await System.Threading.Tasks.Task.Yield();

        System.Console.Write(s_buffer[1]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (10,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 26));

            var expectedDiagnostics = new[]
            {
                // (13,13): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             y *= y;
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(13, 13),
                // (13,18): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             y *= y;
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(13, 18),
                // (14,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(y);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(14, 34)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InAsync_12()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        s_buffer[1] = 3;

        foreach (ref int y in GetBuffer())
        {
            y *= y;
            System.Console.Write(y);
            await System.Threading.Tasks.Task.Yield();
        }

        await System.Threading.Tasks.Task.Yield();

        System.Console.Write(s_buffer[1]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (10,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 26));

            var expectedDiagnostics = new[]
            {
                // (10,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, @"foreach (ref int y in GetBuffer())
        {
            y *= y;
            System.Console.Write(y);
            await System.Threading.Tasks.Task.Yield();
        }").WithArguments("Program.GetBuffer()").WithLocation(10, 9)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InAsync_13()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        s_buffer[1] = 3;

        ref Buffer4<int> buffer = ref GetBuffer();
        foreach (ref int y in buffer)
        {
            y *= y;
            System.Console.Write(y);
            await System.Threading.Tasks.Task.Yield();
        }

        await System.Threading.Tasks.Task.Yield();

        System.Console.Write(s_buffer[1]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (10,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref Buffer4<int> buffer = ref GetBuffer();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "buffer").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 26),
                // (11,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in buffer)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(11, 26));

            var expectedDiagnostics = new[]
            {
                // (11,31): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (ref int y in buffer)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "buffer").WithLocation(11, 31)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InAsync_14()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        Test(c).Wait();
    }

    static async Task Test(C x)
    {
        ref readonly Buffer4<int> f = ref x.F;
        foreach (var y in f)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            await Task.Yield();
            await Task.Delay(2);
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[index] = index;
        }
    }
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (21,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref readonly Buffer4<int> f = ref x.F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "f").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(21, 35));

            var expectedDiagnostics = new[]
            {
                // (22,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var y in f)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(22, 27)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InAsync_15()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        foreach (ref readonly int y in GetBuffer())
        {
            System.Console.Write(y);
            await System.Threading.Tasks.Task.Yield();
        }

        await System.Threading.Tasks.Task.Yield();
    }

    static ref readonly Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (8,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 35));

            var expectedDiagnostics = new[]
            {
                // (8,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, @"foreach (ref readonly int y in GetBuffer())
        {
            System.Console.Write(y);
            await System.Threading.Tasks.Task.Yield();
        }").WithArguments("Program.GetBuffer()").WithLocation(8, 9)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InAsync_16()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static async System.Threading.Tasks.Task Main()
    {
        foreach (ref readonly int y in GetBuffer())
        {
            await System.Threading.Tasks.Task.Yield();
            System.Console.Write(y);
        }

        await System.Threading.Tasks.Task.Yield();
    }

    static ref readonly Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (8,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 35));

            var expectedDiagnostics = new[]
            {
                // (11,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(y);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(11, 34)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InAsync_17()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        Test(c).Wait();
    }

    static async Task Test(C x)
    {
        foreach (ref readonly int y in x.F)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            await Task.Yield();
            await Task.Delay(2);
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[index] = index;
        }
    }
}
" + Buffer4Definition;
            var expectedOutput = " 0 1 2 3";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyDiagnostics();
            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InAsync_18()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    static async Task Test(C x)
    {
        foreach (ref readonly int y in x.F)
        {
            await Task.Yield();
            System.Console.Write(y);
        }
    }
}
" + Buffer4Definition;

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (13,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in x.F)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(13, 35));

            var expectedDiagnostics = new[]
            {
                // (16,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(y);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(16, 34)
            };

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InAsync_19()
        {
            var src = @"
using System.Threading.Tasks;

class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    static async Task Test(C x)
    {
        ref readonly Buffer4<int> f = ref x.F;

        foreach (var i in f) System.Console.Write(i);

        foreach (var y in f)
        {
            System.Console.Write(y);
            await Task.Yield();
        }

        foreach (var j in f) System.Console.Write(j);

        foreach (var z in f)
        {
            System.Console.Write(z);
            await Task.Yield();
        }
    }
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (13,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref readonly Buffer4<int> f = ref x.F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "f").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(13, 35));

            var expectedDiagnostics = new[]
            {
                // (17,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var y in f)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(17, 27),
                // (23,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var j in f) System.Console.Write(j);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(23, 27),
                // (25,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var z in f)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(25, 27)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_01()
        {
            var src = @"
class Program
{
    static private Buffer4<int> F = default;
    private static int index = 0;

    static void Main()
    {
        foreach (var a in Test())
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        yield return -1;

        foreach (var y in GetBuffer())
        {
            Increment();
            System.Console.Write(' ');
            System.Console.Write(y);
        }

        yield return -2;
    }

    static ref Buffer4<int> GetBuffer()
    {
        System.Console.Write(-1);
        return ref F;
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            F[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Collections.IEnumerator.MoveNext",
@"
{
  // Code size      126 (0x7e)
  .maxstack  2
  .locals init (int V_0,
                Buffer4<int>& V_1,
                int V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_001b,
        IL_0032,
        IL_0075)
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.m1
  IL_0024:  stfld      ""int Program.<Test>d__3.<>2__current""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0030:  ldc.i4.1
  IL_0031:  ret
  IL_0032:  ldarg.0
  IL_0033:  ldc.i4.m1
  IL_0034:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0039:  call       ""ref Buffer4<int> Program.GetBuffer()""
  IL_003e:  stloc.1
  IL_003f:  ldc.i4.0
  IL_0040:  stloc.2
  IL_0041:  br.s       IL_0060
  IL_0043:  ldloc.1
  IL_0044:  ldloc.2
  IL_0045:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
  IL_004a:  ldind.i4
  IL_004b:  call       ""void Program.Increment()""
  IL_0050:  ldc.i4.s   32
  IL_0052:  call       ""void System.Console.Write(char)""
  IL_0057:  call       ""void System.Console.Write(int)""
  IL_005c:  ldloc.2
  IL_005d:  ldc.i4.1
  IL_005e:  add
  IL_005f:  stloc.2
  IL_0060:  ldloc.2
  IL_0061:  ldc.i4.4
  IL_0062:  blt.s      IL_0043
  IL_0064:  ldarg.0
  IL_0065:  ldc.i4.s   -2
  IL_0067:  stfld      ""int Program.<Test>d__3.<>2__current""
  IL_006c:  ldarg.0
  IL_006d:  ldc.i4.2
  IL_006e:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0073:  ldc.i4.1
  IL_0074:  ret
  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_007c:  ldc.i4.0
  IL_007d:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_02()
        {
            var src = @"
class C
{
    public Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        foreach (var a in Test(c))
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        foreach (var y in x.F)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            yield return -1;
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            c.F[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Collections.IEnumerator.MoveNext",
@"
{
  // Code size      151 (0x97)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0070
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0017:  ldarg.0
  IL_0018:  ldarg.0
  IL_0019:  ldfld      ""C Program.<Test>d__3.x""
  IL_001e:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_0029:  ldfld      ""Buffer4<int> C.F""
  IL_002e:  pop
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.0
  IL_0031:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_0036:  br.s       IL_0085
  IL_0038:  ldarg.0
  IL_0039:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_003e:  ldflda     ""Buffer4<int> C.F""
  IL_0043:  ldarg.0
  IL_0044:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_0049:  call       ""ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer4<int>, int>(ref Buffer4<int>, int)""
  IL_004e:  ldind.i4
  IL_004f:  call       ""void Program.Increment()""
  IL_0054:  ldc.i4.s   32
  IL_0056:  call       ""void System.Console.Write(char)""
  IL_005b:  call       ""void System.Console.Write(int)""
  IL_0060:  ldarg.0
  IL_0061:  ldc.i4.m1
  IL_0062:  stfld      ""int Program.<Test>d__3.<>2__current""
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.1
  IL_0069:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_006e:  ldc.i4.1
  IL_006f:  ret
  IL_0070:  ldarg.0
  IL_0071:  ldc.i4.m1
  IL_0072:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0077:  ldarg.0
  IL_0078:  ldarg.0
  IL_0079:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_007e:  ldc.i4.1
  IL_007f:  add
  IL_0080:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_0085:  ldarg.0
  IL_0086:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_008b:  ldc.i4.4
  IL_008c:  blt.s      IL_0038
  IL_008e:  ldarg.0
  IL_008f:  ldnull
  IL_0090:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_0095:  ldc.i4.0
  IL_0096:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_03()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static void Main()
    {
        s_buffer[2] = 3;

        foreach (int x in Test())
        {
            System.Console.Write(x);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (ref int y in GetBuffer())
        {
            y *= y;
            System.Console.Write(y);
        }

        yield return -1;

        System.Console.Write(s_buffer[2]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (18,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 26));

            var expectedOutput = "0090-19";

            CompileAndVerify(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InIterator_04()
        {
            var src = @"
class Program
{
    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (int y in GetBuffer())
        {
            yield return -1;
        }
    }

    static ref Buffer4<int> GetBuffer() => throw null;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait,
        @"foreach (int y in GetBuffer())
        {
            yield return -1;
        }").WithArguments("Program.GetBuffer()").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_05()
        {
            var src = @"
class Program
{
    static private Buffer4<int> F = default;
    private static int index = 0;

    static void Main()
    {
        foreach (var a in Test())
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        yield return -1;

        foreach (var y in GetBuffer())
        {
            Increment();
            System.Console.Write(' ');
            System.Console.Write(y);
        }

        yield return -2;
    }

    static ref readonly Buffer4<int> GetBuffer()
    {
        System.Console.Write(-1);
        return ref F;
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            F[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Collections.IEnumerator.MoveNext",
@"
{
  // Code size      126 (0x7e)
  .maxstack  2
  .locals init (int V_0,
                Buffer4<int>& V_1,
                int V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_001b,
        IL_0032,
        IL_0075)
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.m1
  IL_0024:  stfld      ""int Program.<Test>d__3.<>2__current""
  IL_0029:  ldarg.0
  IL_002a:  ldc.i4.1
  IL_002b:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0030:  ldc.i4.1
  IL_0031:  ret
  IL_0032:  ldarg.0
  IL_0033:  ldc.i4.m1
  IL_0034:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0039:  call       ""ref readonly Buffer4<int> Program.GetBuffer()""
  IL_003e:  stloc.1
  IL_003f:  ldc.i4.0
  IL_0040:  stloc.2
  IL_0041:  br.s       IL_0060
  IL_0043:  ldloc.1
  IL_0044:  ldloc.2
  IL_0045:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
  IL_004a:  ldind.i4
  IL_004b:  call       ""void Program.Increment()""
  IL_0050:  ldc.i4.s   32
  IL_0052:  call       ""void System.Console.Write(char)""
  IL_0057:  call       ""void System.Console.Write(int)""
  IL_005c:  ldloc.2
  IL_005d:  ldc.i4.1
  IL_005e:  add
  IL_005f:  stloc.2
  IL_0060:  ldloc.2
  IL_0061:  ldc.i4.4
  IL_0062:  blt.s      IL_0043
  IL_0064:  ldarg.0
  IL_0065:  ldc.i4.s   -2
  IL_0067:  stfld      ""int Program.<Test>d__3.<>2__current""
  IL_006c:  ldarg.0
  IL_006d:  ldc.i4.2
  IL_006e:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0073:  ldc.i4.1
  IL_0074:  ret
  IL_0075:  ldarg.0
  IL_0076:  ldc.i4.m1
  IL_0077:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_007c:  ldc.i4.0
  IL_007d:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "-1 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_06()
        {
            var src = @"
class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        foreach (var a in Test(c))
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        foreach (var y in x.F)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            yield return -1;
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[index] = index;
        }
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__3.System.Collections.IEnumerator.MoveNext",
@"
{
  // Code size      151 (0x97)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__3.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0070
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0017:  ldarg.0
  IL_0018:  ldarg.0
  IL_0019:  ldfld      ""C Program.<Test>d__3.x""
  IL_001e:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_0023:  ldarg.0
  IL_0024:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_0029:  ldfld      ""Buffer4<int> C.F""
  IL_002e:  pop
  IL_002f:  ldarg.0
  IL_0030:  ldc.i4.0
  IL_0031:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_0036:  br.s       IL_0085
  IL_0038:  ldarg.0
  IL_0039:  ldfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_003e:  ldflda     ""Buffer4<int> C.F""
  IL_0043:  ldarg.0
  IL_0044:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_0049:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
  IL_004e:  ldind.i4
  IL_004f:  call       ""void Program.Increment()""
  IL_0054:  ldc.i4.s   32
  IL_0056:  call       ""void System.Console.Write(char)""
  IL_005b:  call       ""void System.Console.Write(int)""
  IL_0060:  ldarg.0
  IL_0061:  ldc.i4.m1
  IL_0062:  stfld      ""int Program.<Test>d__3.<>2__current""
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.1
  IL_0069:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_006e:  ldc.i4.1
  IL_006f:  ret
  IL_0070:  ldarg.0
  IL_0071:  ldc.i4.m1
  IL_0072:  stfld      ""int Program.<Test>d__3.<>1__state""
  IL_0077:  ldarg.0
  IL_0078:  ldarg.0
  IL_0079:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_007e:  ldc.i4.1
  IL_007f:  add
  IL_0080:  stfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_0085:  ldarg.0
  IL_0086:  ldfld      ""int Program.<Test>d__3.<>7__wrap1""
  IL_008b:  ldc.i4.4
  IL_008c:  blt.s      IL_0038
  IL_008e:  ldarg.0
  IL_008f:  ldnull
  IL_0090:  stfld      ""C Program.<Test>d__3.<>7__wrap2""
  IL_0095:  ldc.i4.0
  IL_0096:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: " 0 1 2 3", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_07()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static void Main()
    {
        s_buffer[2] = 3;

        foreach (int x in Test())
        {
            System.Console.Write(x);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        int i = 0;
        foreach (ref readonly int y in GetBuffer())
        {
            System.Console.Write(y);
            s_buffer[i++]++;
            System.Console.Write(y);
            System.Console.Write(' ');
        }

        yield return -1;

        System.Console.Write(s_buffer[2]);
    }

    static ref readonly Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (19,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(19, 35));

            var expectedOutput = "01 01 34 01 -14";

            CompileAndVerify(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                verify: Verification.FailsILVerify, expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                verify: Verification.FailsILVerify, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InIterator_08()
        {
            var src = @"
class Program
{
    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (int y in GetBuffer())
        {
            yield return -1;
        }
    }

    static ref readonly Buffer4<int> GetBuffer() => throw null;
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait,
        @"foreach (int y in GetBuffer())
        {
            yield return -1;
        }").WithArguments("Program.GetBuffer()").WithLocation(6, 9)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_09()
        {
            var src = @"
class Program
{
    static void Main()
    {
        foreach (var a in Test())
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (var y in GetBuffer())
        {
            System.Console.Write(' ');
            System.Console.Write(y);
            yield return -1;
        }
    }

    static Buffer4<int> GetBuffer()
    {
        Buffer4<int> x = default;
        x[0] = 111;
        x[1] = 112;
        x[2] = 113;
        x[3] = 114;
 
        System.Console.Write(-1);
        return x;
    }
}
";
            var comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 111 112 113 114").VerifyDiagnostics();

            verifier.VerifyIL("Program.<Test>d__1.System.Collections.IEnumerator.MoveNext",
@"
{
  // Code size      121 (0x79)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0059
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int Program.<Test>d__1.<>1__state""
  IL_0017:  ldarg.0
  IL_0018:  call       ""Buffer4<int> Program.GetBuffer()""
  IL_001d:  stfld      ""Buffer4<int> Program.<Test>d__1.<>7__wrap1""
  IL_0022:  ldarg.0
  IL_0023:  ldc.i4.0
  IL_0024:  stfld      ""int Program.<Test>d__1.<>7__wrap2""
  IL_0029:  br.s       IL_006e
  IL_002b:  ldarg.0
  IL_002c:  ldflda     ""Buffer4<int> Program.<Test>d__1.<>7__wrap1""
  IL_0031:  ldarg.0
  IL_0032:  ldfld      ""int Program.<Test>d__1.<>7__wrap2""
  IL_0037:  call       ""ref readonly int <PrivateImplementationDetails>.InlineArrayElementRefReadOnly<Buffer4<int>, int>(in Buffer4<int>, int)""
  IL_003c:  ldind.i4
  IL_003d:  ldc.i4.s   32
  IL_003f:  call       ""void System.Console.Write(char)""
  IL_0044:  call       ""void System.Console.Write(int)""
  IL_0049:  ldarg.0
  IL_004a:  ldc.i4.m1
  IL_004b:  stfld      ""int Program.<Test>d__1.<>2__current""
  IL_0050:  ldarg.0
  IL_0051:  ldc.i4.1
  IL_0052:  stfld      ""int Program.<Test>d__1.<>1__state""
  IL_0057:  ldc.i4.1
  IL_0058:  ret
  IL_0059:  ldarg.0
  IL_005a:  ldc.i4.m1
  IL_005b:  stfld      ""int Program.<Test>d__1.<>1__state""
  IL_0060:  ldarg.0
  IL_0061:  ldarg.0
  IL_0062:  ldfld      ""int Program.<Test>d__1.<>7__wrap2""
  IL_0067:  ldc.i4.1
  IL_0068:  add
  IL_0069:  stfld      ""int Program.<Test>d__1.<>7__wrap2""
  IL_006e:  ldarg.0
  IL_006f:  ldfld      ""int Program.<Test>d__1.<>7__wrap2""
  IL_0074:  ldc.i4.4
  IL_0075:  blt.s      IL_002b
  IL_0077:  ldc.i4.0
  IL_0078:  ret
}
");
            comp = CreateCompilation(src + Buffer4Definition, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "-1 111 112 113 114").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_10()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static void Main()
    {
        s_buffer[2] = 3;

        foreach (int x in Test())
        {
            System.Console.Write(x);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        ref Buffer4<int> buffer = ref GetBuffer();
        foreach (ref int y in buffer)
        {
            y *= y;
            System.Console.Write(y);
        }

        yield return -1;

        System.Console.Write(s_buffer[2]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (18,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref Buffer4<int> buffer = ref GetBuffer();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "buffer").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 26),
                // (19,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in buffer)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(19, 26));

            var expectedOutput = "0090-19";

            CompileAndVerify(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();

            CompileAndVerify(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe,
                expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InIterator_11()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static void Main()
    {
        s_buffer[2] = 3;

        foreach (int x in Test())
        {
            System.Console.Write(x);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (ref int y in GetBuffer())
        {
            yield return 1;
            y *= y;
            System.Console.Write(y);
        }

        yield return -1;

        System.Console.Write(s_buffer[2]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (18,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 26));

            var expectedDiagnostics = new[]
            {
                // (21,13): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             y *= y;
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(21, 13),
                // (21,18): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             y *= y;
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(21, 18),
                // (22,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(y);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(22, 34)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InIterator_12()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static void Main()
    {
        s_buffer[2] = 3;

        foreach (int x in Test())
        {
            System.Console.Write(x);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (ref int y in GetBuffer())
        {
            y *= y;
            System.Console.Write(y);
            yield return 1;
        }

        yield return -1;

        System.Console.Write(s_buffer[2]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (18,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 26));

            var expectedDiagnostics = new[]
            {
                // (18,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (ref int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, @"foreach (ref int y in GetBuffer())
        {
            y *= y;
            System.Console.Write(y);
            yield return 1;
        }").WithArguments("Program.GetBuffer()").WithLocation(18, 9)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InIterator_13()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static void Main()
    {
        s_buffer[2] = 3;

        foreach (int x in Test())
        {
            System.Console.Write(x);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        ref Buffer4<int> buffer = ref GetBuffer();
        foreach (ref int y in buffer)
        {
            y *= y;
            System.Console.Write(y);
            yield return 1;
        }

        yield return -1;

        System.Console.Write(s_buffer[2]);
    }

    static ref Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (18,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref Buffer4<int> buffer = ref GetBuffer();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "buffer").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 26),
                // (19,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref int y in buffer)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(19, 26));

            var expectedDiagnostics = new[]
            {
                // (19,31): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (ref int y in buffer)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "buffer").WithLocation(19, 31)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InIterator_14()
        {
            var src = @"
class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        foreach (var a in Test(c))
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        ref readonly Buffer4<int> f = ref x.F;
        foreach (var y in f)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            yield return -1;
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[index] = index;
        }
    }
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (20,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref readonly Buffer4<int> f = ref x.F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "f").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(20, 35));

            var expectedDiagnostics = new[]
            {
                // (21,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var y in f)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(21, 27)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InIterator_15()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (ref readonly int y in GetBuffer())
        {
            System.Console.Write(y);
            yield return 1;
        }

        yield return -1;
    }

    static ref readonly Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (8,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 35));

            var expectedDiagnostics = new[]
            {
                // (8,9): error CS8178: A reference returned by a call to 'Program.GetBuffer()' cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, @"foreach (ref readonly int y in GetBuffer())
        {
            System.Console.Write(y);
            yield return 1;
        }").WithArguments("Program.GetBuffer()").WithLocation(8, 9)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InIterator_16()
        {
            var src = @"
class Program
{
    static Buffer4<int> s_buffer;

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        foreach (ref readonly int y in GetBuffer())
        {
            yield return 1;
            System.Console.Write(y);
        }

        yield return -1;
    }

    static ref readonly Buffer4<int> GetBuffer() => ref s_buffer;
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (8,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in GetBuffer())
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 35));

            var expectedDiagnostics = new[]
            {
                // (11,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(y);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(11, 34)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void Foreach_InIterator_17()
        {
            var src = @"
class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    private static C c = new C();
    private static int index = 0;

    static void Main()
    {
        foreach (var a in Test(c))
        {}
    }

    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        foreach (ref readonly int y in x.F)
        {
            Increment();    
            System.Console.Write(' ');
            System.Console.Write(y);

            yield return -1;
        }
    }

    static void Increment()
    {
        index++;

        if (index < 4)
        {
            System.Runtime.CompilerServices.Unsafe.AsRef(in c.F)[index] = index;
        }
    }
}
" + Buffer4Definition;
            var expectedOutput = " 0 1 2 3";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyDiagnostics();
            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: expectedOutput, verify: Verification.Fails).VerifyDiagnostics();
        }

        [Fact]
        public void Foreach_InIterator_18()
        {
            var src = @"
class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        foreach (ref readonly int y in x.F)
        {
            yield return -1;
            System.Console.Write(y);
        }
    }
}
" + Buffer4Definition;

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
                // (11,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         foreach (ref readonly int y in x.F)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(11, 35));

            var expectedDiagnostics = new[]
            {
                // (14,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //             System.Console.Write(y);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(14, 34)
            };

            CreateCompilation(src, targetFramework: TargetFramework.Net80, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Foreach_InIterator_19()
        {
            var src = @"
class C
{
    public readonly Buffer4<int> F = default;
}

class Program
{
    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        ref readonly Buffer4<int> f = ref x.F;

        foreach (var i in f) System.Console.Write(i);

        foreach (var y in f)
        {
            System.Console.Write(y);
            yield return -1;
        }

        foreach (var j in f) System.Console.Write(j);

        foreach (var z in f)
        {
            System.Console.Write(z);
            yield return -2;
        }
    }
}
" + Buffer4Definition;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (11,35): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                //         ref readonly Buffer4<int> f = ref x.F;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "f").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(11, 35));

            var expectedDiagnostics = new[]
            {
                // (15,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var y in f)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(15, 27),
                // (21,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var j in f) System.Console.Write(j);
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(21, 27),
                // (23,27): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
                //         foreach (var z in f)
                Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "f").WithLocation(23, 27)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);

            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedIndexer_Warning_01()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    string this[int i] => ""int"";

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (7,12): warning CS9181: Inline array indexer will not be used for element access expression.
                //     string this[int i] => "int";
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(7, 12)
                );

            CompileAndVerify(comp, expectedOutput: "0").VerifyDiagnostics(
                // (7,12): warning CS9181: Inline array indexer will not be used for element access expression.
                //     string this[int i] => "int";
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(7, 12)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedIndexer_Warning_02()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    string this[System.Index i] => ""index"";

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[(System.Index)0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "0").VerifyDiagnostics(
                // (7,12): warning CS9181: Inline array indexer will not be used for element access expression.
                //     string this[System.Index i] => "index";
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(7, 12)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedIndexer_Warning_03()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    string this[System.Range i] => ""range"";

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[..][0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Fails).VerifyDiagnostics(
                // (7,12): warning CS9181: Inline array indexer will not be used for element access expression.
                //     string this[System.Range i] => "range";
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(7, 12)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedIndexer_Warning_04()
        {
            var src = @"
Buffer4 b = default;
System.Console.WriteLine(b[(nint)0]);

[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public string this[nint i] => ""nint"";
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            CompileAndVerify(comp, expectedOutput: "nint").VerifyDiagnostics();
        }

        [Fact]
        public void UserDefinedIndexer_Warning_05()
        {
            var src = @"
Buffer4 b = default;
System.Console.WriteLine(b[0]);

[System.Runtime.CompilerServices.InlineArray(4)]
ref struct Buffer4
{
    private ref int _element0;

    public string this[int i] => ""int"";
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (3,26): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer4'
                // System.Console.WriteLine(b[0]);
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "b[0]").WithArguments("Buffer4").WithLocation(3, 26),
                // (8,21): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                //     private ref int _element0;
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "_element0").WithLocation(8, 21),
                // (10,19): warning CS9181: Inline array indexer will not be used for element access expression.
                //     public string this[int i] => "int";
                Diagnostic(ErrorCode.WRN_InlineArrayIndexerNotUsed, "this").WithLocation(10, 19)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedIndexer_Warning_06()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4 : I1
{
    private int _element0;

    int I1.this[int i] => throw null;

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[0]);
    }
}

interface I1
{
    int this[int x] {get;}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "0").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedSlice_Warning_01()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    int Length => 4;
    string Slice(int i, int j) => ""int"";

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[..][0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,12): warning CS9182: Inline array 'Slice' method will not be used for element access expression.
                //     string Slice(int i, int j) => "int";
                Diagnostic(ErrorCode.WRN_InlineArraySliceNotUsed, "Slice").WithLocation(8, 12)
                );

            CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Fails).VerifyDiagnostics(
                // (8,12): warning CS9182: Inline array 'Slice' method will not be used for element access expression.
                //     string Slice(int i, int j) => "int";
                Diagnostic(ErrorCode.WRN_InlineArraySliceNotUsed, "Slice").WithLocation(8, 12)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedSlice_Warning_02()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    int Length => 4;
    string Slice(nint i, int j) => ""int"";
    string Slice(int i, nint j) => ""int"";
    string Slice(int i) => ""int"";

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[..][0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedSlice_Warning_03()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4 : I1
{
    private int _element0;

    int Length => 4;
    string I1.Slice(int i, int j) => ""int"";

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(b[..][0]);
    }
}

interface I1
{
    string Slice(int i, int j);
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_01()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static implicit operator System.Span<int>(Buffer4 b) => throw null; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.Span<int>)b)[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (7,37): warning CS9183: Inline array conversion operator will not be used for conversion from expression of the declaring type.
                //     public static implicit operator System.Span<int>(Buffer4 b) => throw null; 
                Diagnostic(ErrorCode.WRN_InlineArrayConversionOperatorNotUsed, "System.Span<int>").WithLocation(7, 37)
                );

            CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Fails).VerifyDiagnostics(
                // (7,37): warning CS9183: Inline array conversion operator will not be used for conversion from expression of the declaring type.
                //     public static implicit operator System.Span<int>(Buffer4 b) => throw null; 
                Diagnostic(ErrorCode.WRN_InlineArrayConversionOperatorNotUsed, "System.Span<int>").WithLocation(7, 37)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_02()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static explicit operator System.ReadOnlySpan<int>(in Buffer4 b) => throw null; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.ReadOnlySpan<int>)b)[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (7,37): warning CS9183: Inline array conversion operator will not be used for conversion from expression of the declaring type.
                //     public static explicit operator System.ReadOnlySpan<int>(in Buffer4 b) => throw null; 
                Diagnostic(ErrorCode.WRN_InlineArrayConversionOperatorNotUsed, "System.ReadOnlySpan<int>").WithLocation(7, 37)
                );

            CompileAndVerify(comp, expectedOutput: "0", verify: Verification.Fails).VerifyDiagnostics(
                // (7,37): warning CS9183: Inline array conversion operator will not be used for conversion from expression of the declaring type.
                //     public static explicit operator System.ReadOnlySpan<int>(in Buffer4 b) => throw null; 
                Diagnostic(ErrorCode.WRN_InlineArrayConversionOperatorNotUsed, "System.ReadOnlySpan<int>").WithLocation(7, 37)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_03()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static implicit operator System.ReadOnlySpan<char>(Buffer4 b) => ""span""; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.ReadOnlySpan<char>)b)[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "s", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_04()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static implicit operator System.Span<int>(Buffer4? b) => new [] {1, 2, 3, 4}; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.Span<int>)(Buffer4?)b)[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "1", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_05()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static implicit operator System.Span<int>?(Buffer4 b) => new [] {1, 2, 3, 4}; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.Span<int>?)b).Value[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,37): error CS9244: The type 'Span<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //     public static implicit operator System.Span<int>?(Buffer4 b) => new [] {1, 2, 3, 4}; 
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "System.Span<int>?").WithArguments("System.Nullable<T>", "T", "System.Span<int>").WithLocation(7, 37),
                // (12,36): error CS9244: The type 'Span<int>' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         System.Console.WriteLine(((System.Span<int>?)b).Value[0]);
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "System.Span<int>?").WithArguments("System.Nullable<T>", "T", "System.Span<int>").WithLocation(12, 36)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_06()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static implicit operator System.Span<int>(Buffer4 b, int i) => new [] {1, 2, 3, 4}; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.Span<int>)b)[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,53): error CS1019: Overloadable unary operator expected
                //     public static implicit operator System.Span<int>(Buffer4 b, int i) => new [] {1, 2, 3, 4}; 
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "(Buffer4 b, int i)").WithLocation(7, 53)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void UserDefinedConversion_Warning_07()
        {
            var src = @"
[System.Runtime.CompilerServices.InlineArray(4)]
struct Buffer4
{
    private int _element0;

    public static implicit operator System.Span<int>() => new [] {1, 2, 3, 4}; 

    static void Main()
    {
        Buffer4 b = default;
        System.Console.WriteLine(((System.Span<int>)b)[0]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyEmitDiagnostics(
                // (7,53): error CS1019: Overloadable unary operator expected
                //     public static implicit operator System.Span<int>() => new [] {1, 2, 3, 4}; 
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "()").WithLocation(7, 53)
                );
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/70738")]
        public void CoalesceForNullableElement()
        {
            var src = @"
class Program
{
    static void Main()
    {
        MyArray x = default;
        System.Console.Write(Test(x));

        x[0] = 124;
        System.Console.Write(Test(x));
    }

    static int Test(MyArray array)
    {
        return array[0] ?? 123;
    }
}

[System.Runtime.CompilerServices.InlineArray(1)]
struct MyArray
{
    private int? _value;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "123124").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test",
@"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""ref int? <PrivateImplementationDetails>.InlineArrayFirstElementRef<MyArray, int?>(ref MyArray)""
  IL_0007:  ldc.i4.s   123
  IL_0009:  call       ""readonly int int?.GetValueOrDefault(int)""
  IL_000e:  ret
}
");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/70910")]
        public void StringConcatenation()
        {
            var src = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    public static void Main(string[] args)
    {
        Test();
    }

    static void Test()
    {
        var buffer = new ThreeStringBuffer();
        Console.WriteLine(buffer[0] + ""123"" + buffer[1] + ""124 "" + buffer[2]);
    }
}

[InlineArray(3)]
struct ThreeStringBuffer {
    string _;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "123124").VerifyDiagnostics();
        }

        [Fact]
        public void Initialization_Await_RefStruct()
        {
            var src = """
                using System.Threading.Tasks;

                var b = new Buffer();
                b[0] = await GetInt();
                b[1] = await GetInt();

                static Task<int> GetInt() => Task.FromResult(42);
                
                [System.Runtime.CompilerServices.InlineArray(4)]
                ref struct Buffer
                {
                    private int _element0;
                }
                """;

            CreateCompilation(src, parseOptions: TestOptions.Regular12, targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // (3,1): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
                // var b = new Buffer();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "var").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(3, 1),
                // (4,1): error CS0306: The type 'Buffer' may not be used as a type argument
                // b[0] = await GetInt();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "b[0]").WithArguments("Buffer").WithLocation(4, 1),
                // (5,1): error CS0306: The type 'Buffer' may not be used as a type argument
                // b[1] = await GetInt();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "b[1]").WithArguments("Buffer").WithLocation(5, 1),
                // (10,12): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                // ref struct Buffer
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "Buffer").WithLocation(10, 12));

            var expectedDiagnostics = new[]
            {
                // (4,1): error CS0306: The type 'Buffer' may not be used as a type argument
                // b[0] = await GetInt();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "b[0]").WithArguments("Buffer").WithLocation(4, 1),
                // (5,1): error CS0306: The type 'Buffer' may not be used as a type argument
                // b[1] = await GetInt();
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "b[1]").WithArguments("Buffer").WithLocation(5, 1),
                // (10,12): warning CS9184: 'Inline arrays' language feature is not supported for an inline array type that is not valid as a type argument, or has element type that is not valid as a type argument.
                // ref struct Buffer
                Diagnostic(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, "Buffer").WithLocation(10, 12)
            };

            CreateCompilation(src, parseOptions: TestOptions.Regular13, targetFramework: TargetFramework.Net80).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(src, targetFramework: TargetFramework.Net80).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void Initialization_Await()
        {
            var src = """
                using System.Threading.Tasks;

                var b = new Buffer();
                b[0] = await GetInt();
                System.Console.Write(b[1]);
                b[1] = await GetInt();
                System.Console.Write(b[1]);

                static Task<int> GetInt() => Task.FromResult(42);
                
                [System.Runtime.CompilerServices.InlineArray(4)]
                struct Buffer
                {
                    private int _element0;
                }
                """;
            foreach (var parseOptions in new[] { TestOptions.Regular12, TestOptions.Regular13, TestOptions.RegularPreview })
            {
                var verifier = CompileAndVerify(src, expectedOutput: ExecutionConditionUtil.IsDesktop ? null : "042",
                    parseOptions: parseOptions, targetFramework: TargetFramework.Net80, verify: Verification.FailsPEVerify);
                verifier.VerifyDiagnostics();
                verifier.VerifyIL("Program.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", """
                    {
                      // Code size      316 (0x13c)
                      .maxstack  3
                      .locals init (int V_0,
                                    int V_1,
                                    System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                                    System.Exception V_3)
                      IL_0000:  ldarg.0
                      IL_0001:  ldfld      "int Program.<<Main>$>d__0.<>1__state"
                      IL_0006:  stloc.0
                      .try
                      {
                        IL_0007:  ldloc.0
                        IL_0008:  brfalse.s  IL_0054
                        IL_000a:  ldloc.0
                        IL_000b:  ldc.i4.1
                        IL_000c:  beq        IL_00cb
                        IL_0011:  ldarg.0
                        IL_0012:  ldflda     "Buffer Program.<<Main>$>d__0.<b>5__2"
                        IL_0017:  initobj    "Buffer"
                        IL_001d:  call       "System.Threading.Tasks.Task<int> Program.<<Main>$>g__GetInt|0_0()"
                        IL_0022:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                        IL_0027:  stloc.2
                        IL_0028:  ldloca.s   V_2
                        IL_002a:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                        IL_002f:  brtrue.s   IL_0070
                        IL_0031:  ldarg.0
                        IL_0032:  ldc.i4.0
                        IL_0033:  dup
                        IL_0034:  stloc.0
                        IL_0035:  stfld      "int Program.<<Main>$>d__0.<>1__state"
                        IL_003a:  ldarg.0
                        IL_003b:  ldloc.2
                        IL_003c:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
                        IL_0041:  ldarg.0
                        IL_0042:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
                        IL_0047:  ldloca.s   V_2
                        IL_0049:  ldarg.0
                        IL_004a:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<<Main>$>d__0)"
                        IL_004f:  leave      IL_013b
                        IL_0054:  ldarg.0
                        IL_0055:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
                        IL_005a:  stloc.2
                        IL_005b:  ldarg.0
                        IL_005c:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
                        IL_0061:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                        IL_0067:  ldarg.0
                        IL_0068:  ldc.i4.m1
                        IL_0069:  dup
                        IL_006a:  stloc.0
                        IL_006b:  stfld      "int Program.<<Main>$>d__0.<>1__state"
                        IL_0070:  ldloca.s   V_2
                        IL_0072:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                        IL_0077:  stloc.1
                        IL_0078:  ldarg.0
                        IL_0079:  ldflda     "Buffer Program.<<Main>$>d__0.<b>5__2"
                        IL_007e:  call       "ref int <PrivateImplementationDetails>.InlineArrayFirstElementRef<Buffer, int>(ref Buffer)"
                        IL_0083:  ldloc.1
                        IL_0084:  stind.i4
                        IL_0085:  ldarg.0
                        IL_0086:  ldflda     "Buffer Program.<<Main>$>d__0.<b>5__2"
                        IL_008b:  ldc.i4.1
                        IL_008c:  call       "ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer, int>(ref Buffer, int)"
                        IL_0091:  ldind.i4
                        IL_0092:  call       "void System.Console.Write(int)"
                        IL_0097:  call       "System.Threading.Tasks.Task<int> Program.<<Main>$>g__GetInt|0_0()"
                        IL_009c:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                        IL_00a1:  stloc.2
                        IL_00a2:  ldloca.s   V_2
                        IL_00a4:  call       "bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get"
                        IL_00a9:  brtrue.s   IL_00e7
                        IL_00ab:  ldarg.0
                        IL_00ac:  ldc.i4.1
                        IL_00ad:  dup
                        IL_00ae:  stloc.0
                        IL_00af:  stfld      "int Program.<<Main>$>d__0.<>1__state"
                        IL_00b4:  ldarg.0
                        IL_00b5:  ldloc.2
                        IL_00b6:  stfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
                        IL_00bb:  ldarg.0
                        IL_00bc:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
                        IL_00c1:  ldloca.s   V_2
                        IL_00c3:  ldarg.0
                        IL_00c4:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, Program.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref Program.<<Main>$>d__0)"
                        IL_00c9:  leave.s    IL_013b
                        IL_00cb:  ldarg.0
                        IL_00cc:  ldfld      "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
                        IL_00d1:  stloc.2
                        IL_00d2:  ldarg.0
                        IL_00d3:  ldflda     "System.Runtime.CompilerServices.TaskAwaiter<int> Program.<<Main>$>d__0.<>u__1"
                        IL_00d8:  initobj    "System.Runtime.CompilerServices.TaskAwaiter<int>"
                        IL_00de:  ldarg.0
                        IL_00df:  ldc.i4.m1
                        IL_00e0:  dup
                        IL_00e1:  stloc.0
                        IL_00e2:  stfld      "int Program.<<Main>$>d__0.<>1__state"
                        IL_00e7:  ldloca.s   V_2
                        IL_00e9:  call       "int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()"
                        IL_00ee:  stloc.1
                        IL_00ef:  ldarg.0
                        IL_00f0:  ldflda     "Buffer Program.<<Main>$>d__0.<b>5__2"
                        IL_00f5:  ldc.i4.1
                        IL_00f6:  call       "ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer, int>(ref Buffer, int)"
                        IL_00fb:  ldloc.1
                        IL_00fc:  stind.i4
                        IL_00fd:  ldarg.0
                        IL_00fe:  ldflda     "Buffer Program.<<Main>$>d__0.<b>5__2"
                        IL_0103:  ldc.i4.1
                        IL_0104:  call       "ref int <PrivateImplementationDetails>.InlineArrayElementRef<Buffer, int>(ref Buffer, int)"
                        IL_0109:  ldind.i4
                        IL_010a:  call       "void System.Console.Write(int)"
                        IL_010f:  leave.s    IL_0128
                      }
                      catch System.Exception
                      {
                        IL_0111:  stloc.3
                        IL_0112:  ldarg.0
                        IL_0113:  ldc.i4.s   -2
                        IL_0115:  stfld      "int Program.<<Main>$>d__0.<>1__state"
                        IL_011a:  ldarg.0
                        IL_011b:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
                        IL_0120:  ldloc.3
                        IL_0121:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
                        IL_0126:  leave.s    IL_013b
                      }
                      IL_0128:  ldarg.0
                      IL_0129:  ldc.i4.s   -2
                      IL_012b:  stfld      "int Program.<<Main>$>d__0.<>1__state"
                      IL_0130:  ldarg.0
                      IL_0131:  ldflda     "System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<<Main>$>d__0.<>t__builder"
                      IL_0136:  call       "void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
                      IL_013b:  ret
                    }
                    """);
            }
        }
    }
}
