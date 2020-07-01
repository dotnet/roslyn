// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RecordTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source, CSharpCompilationOptions? options = null)
            => CSharpTestBase.CreateCompilation(
                new[] { source, IsExternalInitTypeDefinition },
                options: options,
                parseOptions: TestOptions.RegularPreview);

        private CompilationVerifier CompileAndVerify(CSharpTestSource src, string? expectedOutput = null)
            => base.CompileAndVerify(new[] { src, IsExternalInitTypeDefinition },
                expectedOutput: expectedOutput,
                parseOptions: TestOptions.RegularPreview,
                // init-only fails verification
                verify: Verification.Skipped);

        [Fact]
        public void GeneratedConstructor()
        {
            var comp = CreateCompilation(@"record C(int x, string y);");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctor = (MethodSymbol)c.GetMembers(".ctor")[0];
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var y = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, y.Type.SpecialType);
            Assert.Equal("y", y.Name);
        }

        [Fact]
        public void GeneratedConstructorDefaultValues()
        {
            var comp = CreateCompilation(@"record C<T>(int x, T t = default);");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");
            Assert.Equal(1, c.Arity);
            var ctor = (MethodSymbol)c.GetMembers(".ctor")[0];
            Assert.Equal(0, ctor.Arity);
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var t = ctor.Parameters[1];
            Assert.Equal(c.TypeParameters[0], t.Type);
            Assert.Equal("t", t.Name);
        }

        [Fact]
        public void RecordExistingConstructor1()
        {
            var comp = CreateCompilation(@"
record C(int x, string y)
{
    public C(int a, string b)
    {
    }
}");
            comp.VerifyDiagnostics(
                // (2,9): error CS8851: There cannot be a primary constructor and a member constructor with the same parameter types.
                // record C(int x, string y)
                Diagnostic(ErrorCode.ERR_DuplicateRecordConstructor, "(int x, string y)").WithLocation(2, 9)
            );
            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctor = (MethodSymbol)c.GetMembers(".ctor")[1];
            Assert.Equal(2, ctor.ParameterCount);

            var a = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, a.Type.SpecialType);
            Assert.Equal("a", a.Name);

            var b = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, b.Type.SpecialType);
            Assert.Equal("b", b.Name);
        }

        [Fact]
        public void RecordExistingConstructor01()
        {
            var comp = CreateCompilation(@"
record C(int x, string y)
{
    public C(int a, int b) // overload
    {
    }
}");
            comp.VerifyDiagnostics(
                // (4,12): error CS8862: A constructor declared in a record with parameters must have 'this' constructor initializer.
                //     public C(int a, int b) // overload
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(4, 12)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctors = c.GetMembers(".ctor");
            Assert.Equal(3, ctors.Length);

            foreach (MethodSymbol ctor in ctors)
            {
                if (ctor.ParameterCount == 2)
                {
                    var p1 = ctor.Parameters[0];
                    Assert.Equal(SpecialType.System_Int32, p1.Type.SpecialType);
                    var p2 = ctor.Parameters[1];
                    if (ctor is SynthesizedRecordConstructor)
                    {
                        Assert.Equal("x", p1.Name);
                        Assert.Equal("y", p2.Name);
                        Assert.Equal(SpecialType.System_String, p2.Type.SpecialType);
                    }
                    else
                    {
                        Assert.Equal("a", p1.Name);
                        Assert.Equal("b", p2.Name);
                        Assert.Equal(SpecialType.System_Int32, p2.Type.SpecialType);
                    }
                }
                else
                {
                    Assert.Equal(1, ctor.ParameterCount);
                    Assert.True(c.Equals(ctor.Parameters[0].Type, TypeCompareKind.ConsiderEverything));
                }
            }
        }

        [Fact]
        public void GeneratedProperties()
        {
            var comp = CreateCompilation("record C(int x, int y);");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");

            var x = (SourcePropertySymbolBase)c.GetProperty("x");
            Assert.NotNull(x.GetMethod);
            Assert.Equal(MethodKind.PropertyGet, x.GetMethod.MethodKind);
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.False(x.IsReadOnly);
            Assert.False(x.IsWriteOnly);
            Assert.False(x.IsImplicitlyDeclared);
            Assert.Equal(Accessibility.Public, x.DeclaredAccessibility);
            Assert.False(x.IsVirtual);
            Assert.False(x.IsStatic);
            Assert.Equal(c, x.ContainingType);
            Assert.Equal(c, x.ContainingSymbol);

            var backing = x.BackingField;
            Debug.Assert(backing != null);
            Assert.Equal(x, backing.AssociatedSymbol);
            Assert.Equal(c, backing.ContainingSymbol);
            Assert.Equal(c, backing.ContainingType);
            Assert.True(backing.IsImplicitlyDeclared);

            var getAccessor = x.GetMethod;
            Assert.Equal(x, getAccessor.AssociatedSymbol);
            Assert.True(getAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, getAccessor.ContainingSymbol);
            Assert.Equal(c, getAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, getAccessor.DeclaredAccessibility);

            var setAccessor = x.SetMethod;
            Assert.Equal(x, setAccessor.AssociatedSymbol);
            Assert.True(setAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, setAccessor.ContainingSymbol);
            Assert.Equal(c, setAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, setAccessor.DeclaredAccessibility);
            Assert.True(setAccessor.IsInitOnly);

            var y = (SourcePropertySymbolBase)c.GetProperty("y");
            Assert.NotNull(y.GetMethod);
            Assert.Equal(MethodKind.PropertyGet, y.GetMethod.MethodKind);
            Assert.Equal(SpecialType.System_Int32, y.Type.SpecialType);
            Assert.False(y.IsReadOnly);
            Assert.False(y.IsWriteOnly);
            Assert.False(y.IsImplicitlyDeclared);
            Assert.Equal(Accessibility.Public, y.DeclaredAccessibility);
            Assert.False(y.IsVirtual);
            Assert.False(y.IsStatic);
            Assert.Equal(c, y.ContainingType);
            Assert.Equal(c, y.ContainingSymbol);

            backing = y.BackingField;
            Debug.Assert(backing != null);
            Assert.Equal(y, backing.AssociatedSymbol);
            Assert.Equal(c, backing.ContainingSymbol);
            Assert.Equal(c, backing.ContainingType);
            Assert.True(backing.IsImplicitlyDeclared);

            getAccessor = y.GetMethod;
            Assert.Equal(y, getAccessor.AssociatedSymbol);
            Assert.True(getAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, getAccessor.ContainingSymbol);
            Assert.Equal(c, getAccessor.ContainingType);

            setAccessor = y.SetMethod;
            Assert.Equal(y, setAccessor.AssociatedSymbol);
            Assert.True(setAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, setAccessor.ContainingSymbol);
            Assert.Equal(c, setAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, setAccessor.DeclaredAccessibility);
            Assert.True(setAccessor.IsInitOnly);
        }

        [Fact]
        public void RecordEquals_01()
        {
            var comp = CreateCompilation(@"
record C(int X, int Y)
{
    public bool Equals(C c) => throw null;
    public override bool Equals(object o) => false;
}
");
            comp.VerifyDiagnostics(
                // (5,26): error CS0111: Type 'C' already defines a member called 'Equals' with the same parameter types
                //     public override bool Equals(object o) => false;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "C").WithLocation(5, 26)
                );

            comp = CreateCompilation(@"
record C
{
    public int Equals(object o) => throw null;
}

record D : C
{
}
");
            comp.VerifyDiagnostics(
                // (4,16): warning CS0114: 'C.Equals(object)' hides inherited member 'object.Equals(object)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     public int Equals(object o) => throw null;
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "Equals").WithArguments("C.Equals(object)", "object.Equals(object)").WithLocation(4, 16),
                // (4,16): error CS0111: Type 'C' already defines a member called 'Equals' with the same parameter types
                //     public int Equals(object o) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "C").WithLocation(4, 16)
                );

            CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(0, 0);
        Console.WriteLine(c.Equals(c));
    }
    public bool Equals(C c) => false;
}", expectedOutput: "False");
        }

        [Fact]
        public void RecordEquals_02()
        {
            CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(1, 1);
        var c2 = new C(1, 1);
        Console.WriteLine(c.Equals(c));
        Console.WriteLine(c.Equals(c2));
    }
}", expectedOutput: @"True
True");
        }

        [Fact]
        public void RecordEquals_03()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(0, 0);
        var c2 = new C(0, 0);
        var c3 = new C(1, 1);
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
    public bool Equals(C c) => X == c.X && Y == c.Y;
}", expectedOutput: @"True
False");
            verifier.VerifyIL("C.Equals(object)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  call       ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int C.X.get""
  IL_0006:  ldarg.1
  IL_0007:  callvirt   ""int C.X.get""
  IL_000c:  bne.un.s   IL_001d
  IL_000e:  ldarg.0
  IL_000f:  call       ""int C.Y.get""
  IL_0014:  ldarg.1
  IL_0015:  callvirt   ""int C.Y.get""
  IL_001a:  ceq
  IL_001c:  ret
  IL_001d:  ldc.i4.0
  IL_001e:  ret
}");
        }

        [Fact]
        public void RecordEquals_04()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(0, 0);
        var c2 = new C(0, 0);
        var c3 = new C(1, 1);
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
}", expectedOutput: @"True
False");
            verifier.VerifyIL("C.Equals(object)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0040
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0040
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  brfalse.s  IL_0040
  IL_0029:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ldarg.1
  IL_0035:  ldfld      ""int C.<Y>k__BackingField""
  IL_003a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_003f:  ret
  IL_0040:  ldc.i4.0
  IL_0041:  ret
}");
        }

        [Fact]
        public void RecordEquals_06()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(0, 0);
        object c2 = null;
        C c3 = null;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
}", expectedOutput: @"False
False");
        }

        [Fact]
        public void RecordEquals_07()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int[] X, string Y)
{
    public static void Main()
    {
        var arr = new[] {1, 2};
        var c = new C(arr, ""abc"");
        var c2 = new C(new[] {1, 2}, ""abc"");
        var c3 = new C(arr, ""abc"");
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
}", expectedOutput: @"False
True");
        }

        [Fact]
        public void RecordEquals_08()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public int Z;
    public static void Main()
    {
        var c = new C(1, 2);
        c.Z = 3;
        var c2 = new C(1, 2);
        c2.Z = 4;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
        c2.Z = 3;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
    }
}", expectedOutput: @"False
False
True
True");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0058
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0058
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  brfalse.s  IL_0058
  IL_0029:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ldarg.1
  IL_0035:  ldfld      ""int C.<Y>k__BackingField""
  IL_003a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_003f:  brfalse.s  IL_0058
  IL_0041:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0046:  ldarg.0
  IL_0047:  ldfld      ""int C.Z""
  IL_004c:  ldarg.1
  IL_004d:  ldfld      ""int C.Z""
  IL_0052:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0057:  ret
  IL_0058:  ldc.i4.0
  IL_0059:  ret
}");
        }

        [Fact]
        public void RecordEquals_09()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public int Z { get; set; }
    public static void Main()
    {
        var c = new C(1, 2);
        c.Z = 3;
        var c2 = new C(1, 2);
        c2.Z = 4;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
        c2.Z = 3;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
    }
}", expectedOutput: @"False
False
True
True");
        }

        [Fact]
        public void RecordEquals_10()
        {
            var verifier = CompileAndVerify(@"
using System;
record C(int X, int Y)
{
    public static int Z;
    public static void Main()
    {
        var c = new C(1, 2);
        C.Z = 3;
        var c2 = new C(1, 2);
        C.Z = 4;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
        C.Z = 3;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
    }
}", expectedOutput: @"True
True
True
True");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0040
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0040
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  brfalse.s  IL_0040
  IL_0029:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ldarg.1
  IL_0035:  ldfld      ""int C.<Y>k__BackingField""
  IL_003a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_003f:  ret
  IL_0040:  ldc.i4.0
  IL_0041:  ret
}");
        }

        [Fact]
        public void RecordEquals_11()
        {
            var verifier = CompileAndVerify(@"
using System;
using System.Collections.Generic;
record C(int X, int Y)
{
    static Dictionary<C, int> s_dict = new Dictionary<C, int>();
    public int Z { get => s_dict[this]; set => s_dict[this] = value; }
    public static void Main()
    {
        var c = new C(1, 2);
        c.Z = 3;
        var c2 = new C(1, 2);
        c2.Z = 4;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
        c2.Z = 3;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
    }
}", expectedOutput: @"True
True
True
True");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0040
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0040
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  brfalse.s  IL_0040
  IL_0029:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ldarg.1
  IL_0035:  ldfld      ""int C.<Y>k__BackingField""
  IL_003a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_003f:  ret
  IL_0040:  ldc.i4.0
  IL_0041:  ret
}");
        }

        [Fact]
        public void RecordEquals_12()
        {
            var verifier = CompileAndVerify(@"
using System;
using System.Collections.Generic;
record C(int X, int Y)
{
    private event Action E;
    public static void Main()
    {
        var c = new C(1, 2);
        c.E = () => { };
        var c2 = new C(1, 2);
        c2.E = () => { };
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
        c2.E = c.E;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
    }
}", expectedOutput: @"False
False
True
True");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0058
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0058
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  brfalse.s  IL_0058
  IL_0029:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ldarg.1
  IL_0035:  ldfld      ""int C.<Y>k__BackingField""
  IL_003a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_003f:  brfalse.s  IL_0058
  IL_0041:  call       ""System.Collections.Generic.EqualityComparer<System.Action> System.Collections.Generic.EqualityComparer<System.Action>.Default.get""
  IL_0046:  ldarg.0
  IL_0047:  ldfld      ""System.Action C.E""
  IL_004c:  ldarg.1
  IL_004d:  ldfld      ""System.Action C.E""
  IL_0052:  callvirt   ""bool System.Collections.Generic.EqualityComparer<System.Action>.Equals(System.Action, System.Action)""
  IL_0057:  ret
  IL_0058:  ldc.i4.0
  IL_0059:  ret
}");
        }

        [Fact]
        public void RecordClone1()
        {
            var comp = CreateCompilation("record C(int x, int y);");
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var clone = c.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(c, clone.ReturnType);

            var ctor = (MethodSymbol)c.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(c, TypeCompareKind.ConsiderEverything));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails);
            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""int C.<x>k__BackingField""
  IL_000d:  stfld      ""int C.<x>k__BackingField""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      ""int C.<y>k__BackingField""
  IL_0019:  stfld      ""int C.<y>k__BackingField""
  IL_001e:  ret
}");
        }

        [Fact]
        public void RecordClone2_0()
        {
            var comp = CreateCompilation(@"
record C(int x, int y)
{
    public C(C other)
    {
        x = other.x;
        y = other.y;
    }
}");
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var clone = c.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(c, clone.ReturnType);

            var ctor = (MethodSymbol)c.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(c, TypeCompareKind.ConsiderEverything));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails);
            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}
");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  callvirt   ""int C.x.get""
  IL_000d:  call       ""void C.x.init""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  callvirt   ""int C.y.get""
  IL_0019:  call       ""void C.y.init""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void RecordClone2_0_WithThisInitializer()
        {
            var comp = CreateCompilation(@"
record C(int x, int y)
{
    public C(C other) : this(other.x, other.y) { }
}");
            comp.VerifyDiagnostics(
                // (4,25): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C other) : this(other.x, other.y) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "this").WithLocation(4, 25)
                );
        }

        [Fact]
        [WorkItem(44781, "https://github.com/dotnet/roslyn/issues/44781")]
        public void RecordClone2_1()
        {
            var comp = CreateCompilation(@"
record C(int x, int y)
{
    public C(C other) { }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44781, "https://github.com/dotnet/roslyn/issues/44781")]
        public void RecordClone2_2()
        {
            var comp = CreateCompilation(@"
record C(int x, int y)
{
    public C(C other) : base() { }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44782, "https://github.com/dotnet/roslyn/issues/44782")]
        public void RecordClone3()
        {
            var comp = CreateCompilation(@"
using System;
public record C(int x, int y)
{
    public event Action E;
    public int Z;
    public int W = 123;
}");
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var clone = c.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(c, clone.ReturnType);

            var ctor = (MethodSymbol)c.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(c, TypeCompareKind.ConsiderEverything));

            var verifier = CompileAndVerify(comp, verify: Verification.Fails);
            verifier.VerifyIL("C." + WellKnownMemberNames.CloneMethodName, @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""C..ctor(C)""
  IL_0006:  ret
}");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       67 (0x43)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""int C.<x>k__BackingField""
  IL_000d:  stfld      ""int C.<x>k__BackingField""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      ""int C.<y>k__BackingField""
  IL_0019:  stfld      ""int C.<y>k__BackingField""
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  ldfld      ""System.Action C.E""
  IL_0025:  stfld      ""System.Action C.E""
  IL_002a:  ldarg.0
  IL_002b:  ldarg.1
  IL_002c:  ldfld      ""int C.Z""
  IL_0031:  stfld      ""int C.Z""
  IL_0036:  ldarg.0
  IL_0037:  ldarg.1
  IL_0038:  ldfld      ""int C.W""
  IL_003d:  stfld      ""int C.W""
  IL_0042:  ret
}
");
        }

        [Fact(Skip = "record struct")]
        public void RecordClone4_0()
        {
            var comp = CreateCompilation(@"
using System;
public data struct S(int x, int y)
{
    public event Action E;
    public int Z;
}");
            comp.VerifyDiagnostics(
                // (3,21): error CS0171: Field 'S.E' must be fully assigned before control is returned to the caller
                // public data struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x, int y)").WithArguments("S.E").WithLocation(3, 21),
                // (3,21): error CS0171: Field 'S.Z' must be fully assigned before control is returned to the caller
                // public data struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x, int y)").WithArguments("S.Z").WithLocation(3, 21),
                // (5,25): warning CS0067: The event 'S.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("S.E").WithLocation(5, 25)
            );

            var s = comp.GlobalNamespace.GetTypeMember("S");
            var clone = s.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(s, clone.ReturnType);

            var ctor = (MethodSymbol)s.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(s, TypeCompareKind.ConsiderEverything));
        }

        [Fact(Skip = "record struct")]
        public void RecordClone4_1()
        {
            var comp = CreateCompilation(@"
using System;
public data struct S(int x, int y)
{
    public event Action E = null;
    public int Z = 0;
}");
            comp.VerifyDiagnostics(
                // (5,25): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public event Action E = null;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "E").WithArguments("S").WithLocation(5, 25),
                // (5,25): warning CS0414: The field 'S.E' is assigned but its value is never used
                //     public event Action E = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("S.E").WithLocation(5, 25),
                // (6,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int Z = 0;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "Z").WithArguments("S").WithLocation(6, 16)
                );
        }

        [Fact]
        public void NominalRecordEquals()
        {
            var verifier = CompileAndVerify(@"
using System;
record C
{
    private int X;
    private int Y { get; set; }
    private event Action E;

    public static void Main()
    {
        var c = new C { X = 1, Y = 2 };
        c.E = () => { };
        var c2 = new C { X = 1, Y = 2 };
        c2.E = () => { };
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
        c2.E = c.E;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals((object)c2));
    }
}", expectedOutput: @"False
False
True
True");
            verifier.VerifyIL("C.Equals(object)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       90 (0x5a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0058
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0058
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C.X""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C.X""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  brfalse.s  IL_0058
  IL_0029:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_002e:  ldarg.0
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ldarg.1
  IL_0035:  ldfld      ""int C.<Y>k__BackingField""
  IL_003a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_003f:  brfalse.s  IL_0058
  IL_0041:  call       ""System.Collections.Generic.EqualityComparer<System.Action> System.Collections.Generic.EqualityComparer<System.Action>.Default.get""
  IL_0046:  ldarg.0
  IL_0047:  ldfld      ""System.Action C.E""
  IL_004c:  ldarg.1
  IL_004d:  ldfld      ""System.Action C.E""
  IL_0052:  callvirt   ""bool System.Collections.Generic.EqualityComparer<System.Action>.Equals(System.Action, System.Action)""
  IL_0057:  ret
  IL_0058:  ldc.i4.0
  IL_0059:  ret
}");
        }

        [Fact]
        public void PositionalAndNominalSameEquals()
        {
            var v1 = CompileAndVerify(@"
using System;
record C(int X, string Y)
{
    public event Action E;
}
");
            var v2 = CompileAndVerify(@"
using System;
record C
{
    public int X { get; }
    public string Y { get; }
    public event Action E;
}");
            Assert.Equal(v1.VisualizeIL("C.Equals(C)"), v2.VisualizeIL("C.Equals(C)"));
            Assert.Equal(v1.VisualizeIL("C.Equals(object)"), v2.VisualizeIL("C.Equals(object)"));
        }

        [Fact]
        public void NominalRecordMembers()
        {
            var comp = CreateCompilation(@"
#nullable enable
record C
{
    public int X { get; init; }
    public string Y { get; init; }
}");
            var members = comp.GlobalNamespace.GetTypeMember("C").GetMembers();
            AssertEx.Equal(new[] {
                "C! C.<>Clone()",
                "System.Type! C.EqualityContract.get",
                "System.Type! C.EqualityContract { get; }",
                "System.Int32 C.<X>k__BackingField",
                "System.Int32 C.X { get; init; }",
                "System.Int32 C.X.get",
                "void C.X.init",
                "System.String! C.<Y>k__BackingField",
                "System.String! C.Y { get; init; }",
                "System.String! C.Y.get",
                "void C.Y.init",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? )",
                "C.C(C! )",
                "C.C()",
            }, members.Select(m => m.ToTestDisplayString(includeNonNullable: true)));
        }

        [Fact]
        public void PartialTypes_01()
        {
            var src = @"
using System;
partial record C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}

partial record C(int X, int Y)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,17): error CS8863: Only a single record partial declaration may have a parameter list
                // partial record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int X, int Y)").WithLocation(13, 17)
                );

            Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor(C )" }, comp.GetTypeByMetadataName("C")!.Constructors.Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void PartialTypes_02()
        {
            var src = @"
using System;
partial record C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}

partial record C(int X)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,17): error CS8863: Only a single record partial declaration may have a parameter list
                // partial record C(int X)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int X)").WithLocation(13, 17)
                );

            Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor(C )" }, comp.GetTypeByMetadataName("C")!.Constructors.Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void PartialTypes_03()
        {
            var src = @"
using System;
partial record C
{
    public int X = 1;
}
partial record C(int Y);
partial record C
{
    public int Z { get; } = 2;
}";
            var verifier = CompileAndVerify(src);
            verifier.VerifyIL("C..ctor(int)", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int C.X""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.2
  IL_0010:  stfld      ""int C.<Z>k__BackingField""
  IL_0015:  ldarg.0
  IL_0016:  call       ""object..ctor()""
  IL_001b:  ret
}");
        }

        [Fact]
        public void DataClassAndStruct()
        {
            var src = @"
data class C1 { }
data class C2(int X, int Y);
data struct S1 { }
data struct S2(int X, int Y);";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // data class C1 { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "data").WithLocation(2, 1),
                // (3,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "data").WithLocation(3, 1),
                // (3,14): error CS1514: { expected
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(3, 14),
                // (3,14): error CS1513: } expected
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(3, 14),
                // (3,14): error CS8803: Top-level statements must precede namespace and type declarations.
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int X, int Y);").WithLocation(3, 14),
                // (3,14): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int X, int Y)").WithLocation(3, 14),
                // (3,15): error CS8185: A declaration is not allowed in this context.
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int X").WithLocation(3, 15),
                // (3,15): error CS0165: Use of unassigned local variable 'X'
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int X").WithArguments("X").WithLocation(3, 15),
                // (3,22): error CS8185: A declaration is not allowed in this context.
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int Y").WithLocation(3, 22),
                // (3,22): error CS0165: Use of unassigned local variable 'Y'
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int Y").WithArguments("Y").WithLocation(3, 22),
                // (4,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // data struct S1 { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "data").WithLocation(4, 1),
                // (5,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "data").WithLocation(5, 1),
                // (5,15): error CS1514: { expected
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(5, 15),
                // (5,15): error CS1513: } expected
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(5, 15),
                // (5,15): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int X, int Y)").WithLocation(5, 15),
                // (5,16): error CS8185: A declaration is not allowed in this context.
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int X").WithLocation(5, 16),
                // (5,16): error CS0165: Use of unassigned local variable 'X'
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int X").WithArguments("X").WithLocation(5, 16),
                // (5,20): error CS0128: A local variable or function named 'X' is already defined in this scope
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "X").WithArguments("X").WithLocation(5, 20),
                // (5,23): error CS8185: A declaration is not allowed in this context.
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int Y").WithLocation(5, 23),
                // (5,23): error CS0165: Use of unassigned local variable 'Y'
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int Y").WithArguments("Y").WithLocation(5, 23),
                // (5,27): error CS0128: A local variable or function named 'Y' is already defined in this scope
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "Y").WithArguments("Y").WithLocation(5, 27)
            );
        }

        [Fact]
        public void RecordInheritance()
        {
            var src = @"
class A { }
record B : A { }
record C : B { }
class D : C { }
interface E : C { }
struct F : C { }
enum G : C { }";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,8): error CS8867: No accessible copy constructor found in base type 'A'.
                // record B : A { }
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "B").WithArguments("A").WithLocation(3, 8),
                // (3,12): error CS8864: Records may only inherit from object or another record
                // record B : A { }
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(3, 12),
                // (5,11): error CS8865: Only records may inherit from records.
                // class D : C { }
                Diagnostic(ErrorCode.ERR_BadInheritanceFromRecord, "C").WithLocation(5, 11),
                // (6,15): error CS0527: Type 'C' in interface list is not an interface
                // interface E : C { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "C").WithArguments("C").WithLocation(6, 15),
                // (7,12): error CS0527: Type 'C' in interface list is not an interface
                // struct F : C { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "C").WithArguments("C").WithLocation(7, 12),
                // (8,10): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum G : C { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "C").WithLocation(8, 10)
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RecordInheritance2(bool emitReference)
        {
            var src = @"
public class A { }
public record B { }
public record C : B { }";
            var comp = CreateCompilation(src);

            var src2 = @"
record D : C { }
record E : A { }
interface F : C { }
struct G : C { }
enum H : C { }
";

            var comp2 = CreateCompilation(src2,
                parseOptions: TestOptions.RegularPreview,
                references: new[] {
                emitReference ? comp.EmitToImageReference() : comp.ToMetadataReference()
            });

            comp2.VerifyDiagnostics(
                // (3,8): error CS8867: No accessible copy constructor found in base type 'A'.
                // record E : A { }
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "E").WithArguments("A").WithLocation(3, 8),
                // (3,12): error CS8864: Records may only inherit from object or another record
                // record E : A { }
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(3, 12),
                // (4,15): error CS0527: Type 'C' in interface list is not an interface
                // interface F : C { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "C").WithArguments("C").WithLocation(4, 15),
                // (5,12): error CS0527: Type 'C' in interface list is not an interface
                // struct G : C { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "C").WithArguments("C").WithLocation(5, 12),
                // (6,10): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                // enum H : C { }
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "C").WithLocation(6, 10)
            );
        }

        [Fact]
        public void DataPropertiesLangVersion()
        {
            var src = @"
class X
{
    data int A;
    public data int B = 0;
    data C;
}
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (4,5): error CS8652: The feature 'data properties' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     data int A;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("data properties").WithLocation(4, 5),
                // (4,5): error CS8652: The feature 'init-only setters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     data int A;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("init-only setters").WithLocation(4, 5),
                // (4,5): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                //     data int A;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "data").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(4, 5),
                // (5,12): error CS8652: The feature 'data properties' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public data int B = 0;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("data properties").WithLocation(5, 12),
                // (5,12): error CS8652: The feature 'init-only setters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public data int B = 0;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("init-only setters").WithLocation(5, 12),
                // (5,12): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                //     public data int B = 0;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "data").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(5, 12),
                // (6,5): error CS0246: The type or namespace name 'data' could not be found (are you missing a using directive or an assembly reference?)
                //     data C;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "data").WithArguments("data").WithLocation(6, 5),
                // (6,10): warning CS0169: The field 'X.C' is never used
                //     data C;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "C").WithArguments("X.C").WithLocation(6, 10)
            );
        }

        [Theory]
        [CombinatorialData]
        public void DataProperties1([CombinatorialValues("class", "struct", "record")] string typeKind)
        {
            var src = @$"
{typeKind} X
{{
    data int A;
    public data int B;
    readonly data int C;
    static data int D;
    private data int E;
    new data int F;
}}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,23): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly data int C;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("readonly").WithLocation(6, 23),
                // (7,21): error CS0106: The modifier 'static' is not valid for this item
                //     static data int D;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("static").WithLocation(7, 21),
                // (8,22): error CS0106: The modifier 'private' is not valid for this item
                //     private data int E;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("private").WithLocation(8, 22),
                // (9,18): warning CS0109: The member 'X.F' does not hide an accessible member. The new keyword is not required.
                //     new data int F;
                Diagnostic(ErrorCode.WRN_NewNotRequired, "F").WithArguments("X.F").WithLocation(9, 18)
            );
        }

        [Fact]
        public void DataProperties2()
        {
            var src = @"
class C
{
    data int X = 0;
    data int Y = """";
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,18): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //     data int Y = "";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(5, 18)
            );
        }

        [Fact]
        public void DataProperties3()
        {
            var src = @"
using System;
unsafe class C
{
    data void P1;
    data C2 P2;
    data int* P3;
    data delegate*<int> P4;
    [Obsolete(""Obsolete"", false)]
    data int P5;

    data bool P6 = M2(out var X);
    data int P7 = X;
    data bool P8 = ("""".Length == 0 || M2(out var y)) && y == 0;

    void M()
    {
        int x = P5;
    }

    static bool M2(out int x)
    {
        x = 0;
        return true;
    }

    private class C2 {}
}";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition },
                options: TestOptions.UnsafeDebugDll,
                parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,10): error CS1547: Keyword 'void' cannot be used in this context
                //     data void P1;
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(5, 10),
                // (5,15): error CS0547: 'C.P1': property or indexer cannot have void type
                //     data void P1;
                Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "P1").WithArguments("C.P1").WithLocation(5, 15),
                // (6,13): error CS0053: Inconsistent accessibility: property type 'C.C2' is less accessible than property 'C.P2'
                //     data C2 P2;
                Diagnostic(ErrorCode.ERR_BadVisPropertyType, "P2").WithArguments("C.P2", "C.C2").WithLocation(6, 13),
                // (13,19): error CS0103: The name 'X' does not exist in the current context
                //     data int P7 = X;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "X").WithArguments("X").WithLocation(13, 19),
                // (14,57): error CS0165: Use of unassigned local variable 'y'
                //     data bool P8 = ("".Length == 0 || M2(out var y)) && y == 0;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(14, 57),
                // (18,17): warning CS0618: 'C.P5' is obsolete: 'Obsolete'
                //         int x = P5;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "P5").WithArguments("C.P5", "Obsolete").WithLocation(18, 17)
            );

            var c = comp.GlobalNamespace.GetTypeMember("C");

            var p1 = (DataPropertySymbol)c.GetMember("P1");
            Assert.False(p1.HasPointerType);

            var p2 = (DataPropertySymbol)c.GetMember("P2");
            Assert.False(p2.HasPointerType);

            var p3 = (DataPropertySymbol)c.GetMember("P3");
            Assert.True(p3.HasPointerType);

            var p4 = (DataPropertySymbol)c.GetMember("P4");
            Assert.True(p4.HasPointerType);

            var p5 = (DataPropertySymbol)c.GetMember("P5");
            Assert.Equal(ObsoleteAttributeKind.Obsolete, p5.ObsoleteKind);
        }

        [Fact]
        public void DataProperties4()
        {
            var src = @"
// Data properties are not legal on the top-level
data int X;
data int Y = 0;";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics(
                // (3,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // data int X;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "data").WithLocation(3, 1),
                // (3,10): warning CS0168: The variable 'X' is declared but never used
                // data int X;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "X").WithArguments("X").WithLocation(3, 10),
                // (4,1): error CS0116: A namespace cannot directly contain members such as fields or methods
                // data int Y = 0;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "data").WithLocation(4, 1),
                // (4,10): warning CS0219: The variable 'Y' is assigned but its value is never used
                // data int Y = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "Y").WithArguments("Y").WithLocation(4, 10)
            );

            comp = CreateCompilation(
                src,
                parseOptions: TestOptions.Script.WithLanguageVersion(LanguageVersion.Preview));
            comp.VerifyDiagnostics(
                // (3,1): error CS0103: The name 'data' does not exist in the current context
                // data int X;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "data").WithArguments("data").WithLocation(3, 1),
                // (3,6): error CS1002: ; expected
                // data int X;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(3, 6),
                // (4,1): error CS0103: The name 'data' does not exist in the current context
                // data int Y = 0;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "data").WithArguments("data").WithLocation(4, 1),
                // (4,6): error CS1002: ; expected
                // data int Y = 0;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(4, 6)
            );
        }

        [Fact]
        public void DataProperties5()
        {
            var src = @"
#nullable enable

using System;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
sealed class A1 : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
sealed class A2 : Attribute { }

class C
{
    [A1]
    data int P1;

    [A2]
    [field: A1]
    data string? P2;
}";
            Action<ModuleSymbol> symbolValidator = module =>
            {
                var c = module.GlobalNamespace.GetTypeMember("C");
                var p1 = c.GetMember<PropertySymbol>("P1");
                Assert.Equal("P1", p1.Name);
                Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
                Assert.Equal(SpecialType.System_Int32, p1.Type.SpecialType);
                var a1 = module.GlobalNamespace.GetTypeMember("A1");
                Assert.True(a1.Equals(p1.GetAttributes().Single().AttributeClass, TypeCompareKind.ConsiderEverything));

                var p2 = c.GetMember<PropertySymbol>("P2");
                Assert.Equal("P2", p2.Name);
                Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
                Assert.Equal(SpecialType.System_String, p2.Type.SpecialType);
                Assert.Equal(NullableAnnotation.Annotated, p2.TypeWithAnnotations.NullableAnnotation);
                var a2 = module.GlobalNamespace.GetTypeMember("A2");
                Assert.True(a2.Equals(p2.GetAttributes().Single().AttributeClass, TypeCompareKind.ConsiderEverything));

                var backing = p2 is SourcePropertySymbolBase s
                    ? s.BackingField!
                    : c.GetMember<FieldSymbol>(GeneratedNames.MakeBackingFieldName("P2"));
                Assert.True(backing.GetAttributes()
                    .Select(attrData => attrData.AttributeClass)
                    .Contains(a1));
            };
            _ = CompileAndVerify(
                new[] { src, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                sourceSymbolValidator: symbolValidator,
                symbolValidator: symbolValidator,
                verify: Verification.Fails /* init-only */);
        }

        [Fact]
        public void DataProperties6()
        {
            var src = @"
#nullable enable
class C
{
    data nint P1;
    data string? P2;
}";
            var comp = CompileAndVerify(
                new[] { src, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview,
                symbolValidator: m =>
                {
                    var c = m.GlobalNamespace.GetTypeMember("C");

                    var p1 = c.GetMember<PropertySymbol>("P1");
                    var nint = p1.TypeWithAnnotations;
                    Assert.Equal(SpecialType.System_IntPtr, nint.Type.SpecialType);
                    Assert.True(nint.Type.IsNativeIntegerType);

                    // Assert that we synthesized the attribute in this assembly
                    var nativeIntegerAttribute = m.ContainingAssembly.GetTypeByMetadataName(
                        WellKnownType.System_Runtime_CompilerServices_NativeIntegerAttribute.GetMetadataName());
                    Assert.Equal(nativeIntegerAttribute, p1.GetAttributes().Single().AttributeClass);

                    var p2 = c.GetMember<PropertySymbol>("P2");
                    var nullableString = p2.TypeWithAnnotations;
                    Assert.Equal(SpecialType.System_String, nullableString.SpecialType);
                    Assert.True(NullableAnnotation.Annotated.IsAnnotated());
                    var nullableAttribute = m.ContainingAssembly.GetTypeByMetadataName(
                        WellKnownType.System_Runtime_CompilerServices_NativeIntegerAttribute.GetMetadataName());
                    Assert.NotNull(nullableAttribute);
                },
                verify: Verification.Fails /* init-only */);
        }

        [Fact]
        public void DataProperties7()
        {
            var src = @"
class C
{
    /// <summary>Property P1</summary>
    data int P1;
}";
            var comp = CreateCompilation(src);
            var p1 = comp.GlobalNamespace.GetTypeMember("C").GetMember("P1");
            Assert.Equal(
@"<member name=""P:C.P1"">
    <summary>Property P1</summary>
</member>
", p1.GetDocumentationCommentXml(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void DataProperties8()
        {
            var src = @"
using System;
class C
{
    data IntPtr P = (IntPtr)M();

    static unsafe void* M() => null;
}";
            var comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (5,29): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     data IntPtr P = (IntPtr)M();
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "M()").WithLocation(5, 29)
            );

            src = @"
using System;
unsafe class C
{
    data IntPtr P = (IntPtr)M();

    static unsafe void* M() => null;
}";
            comp = CreateCompilation(src, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
            );
        }

        [Fact]
        public void DataProperties9()
        {
            var src = @"
static class C
{
    data int P1;
    data int P2 = 0;
    static data int P3;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,14): error CS0708: 'C.P1': cannot declare instance members in a static class
                //     data int P1;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "P1").WithArguments("C.P1").WithLocation(4, 14),
                // (5,14): error CS0708: 'C.P2': cannot declare instance members in a static class
                //     data int P2 = 0;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "P2").WithArguments("C.P2").WithLocation(5, 14),
                // (6,21): error CS0106: The modifier 'static' is not valid for this item
                //     static data int P3;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P3").WithArguments("static").WithLocation(6, 21),
                // (6,21): error CS0708: 'C.P3': cannot declare instance members in a static class
                //     static data int P3;
                Diagnostic(ErrorCode.ERR_InstanceMemberInStaticClass, "P3").WithArguments("C.P3").WithLocation(6, 21)
            );
        }

        [Fact]
        public void DataProperties10()
        {
            var src = @"
class C
{
    data ref int P1;
    data ref readonly int P2;
    data out int P3;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,5): error CS8147: Properties which return by reference cannot have set accessors
                //     data ref int P1;
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "data").WithArguments("C.P1.init").WithLocation(4, 5),
                // (4,18): error CS8145: Auto-implemented properties cannot return by reference
                //     data ref int P1;
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P1").WithArguments("C.P1").WithLocation(4, 18),
                // (5,5): error CS8147: Properties which return by reference cannot have set accessors
                //     data ref readonly int P2;
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "data").WithArguments("C.P2.init").WithLocation(5, 5),
                // (5,27): error CS8145: Auto-implemented properties cannot return by reference
                //     data ref readonly int P2;
                Diagnostic(ErrorCode.ERR_AutoPropertyCannotBeRefReturning, "P2").WithArguments("C.P2").WithLocation(5, 27),
                // (6,10): error CS1519: Invalid token 'out' in class, struct, or interface member declaration
                //     data out int P3;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "out").WithArguments("out").WithLocation(6, 10),
                // (6,10): error CS1519: Invalid token 'out' in class, struct, or interface member declaration
                //     data out int P3;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "out").WithArguments("out").WithLocation(6, 10),
                // (6,18): warning CS0169: The field 'C.P3' is never used
                //     data out int P3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "P3").WithArguments("C.P3").WithLocation(6, 18)
            );
        }

        [Fact]
        public void DataProperties11()
        {
            var comp = CreateCompilation(@"
abstract class C
{
    data int P1;
    virtual data int P2;
    abstract data int P3;
}");
            comp.VerifyDiagnostics();
            var c = comp.GlobalNamespace.GetTypeMember("C");

            var p1 = (SourcePropertySymbolBase)c.GetProperty("P1");
            Assert.NotNull(p1.GetMethod);
            Assert.Equal(MethodKind.PropertyGet, p1.GetMethod.MethodKind);
            Assert.Equal(SpecialType.System_Int32, p1.Type.SpecialType);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
            Assert.False(p1.IsImplicitlyDeclared);
            Assert.Equal(Accessibility.Public, p1.DeclaredAccessibility);
            Assert.False(p1.IsVirtual);
            Assert.False(p1.IsStatic);
            Assert.Equal(c, p1.ContainingType);
            Assert.Equal(c, p1.ContainingSymbol);

            var backing = p1.BackingField;
            Debug.Assert(backing != null);
            Assert.Equal(p1, backing.AssociatedSymbol);
            Assert.Equal(c, backing.ContainingSymbol);
            Assert.Equal(c, backing.ContainingType);
            Assert.True(backing.IsImplicitlyDeclared);

            var getAccessor = p1.GetMethod;
            Assert.Equal(p1, getAccessor.AssociatedSymbol);
            Assert.True(getAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, getAccessor.ContainingSymbol);
            Assert.Equal(c, getAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, getAccessor.DeclaredAccessibility);

            var setAccessor = p1.SetMethod;
            Assert.Equal(p1, setAccessor.AssociatedSymbol);
            Assert.True(setAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, setAccessor.ContainingSymbol);
            Assert.Equal(c, setAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, setAccessor.DeclaredAccessibility);
            Assert.True(setAccessor.IsInitOnly);

            var p2 = (SourcePropertySymbolBase)c.GetProperty("P2");
            Assert.NotNull(p2.GetMethod);
            Assert.Equal(MethodKind.PropertyGet, p2.GetMethod.MethodKind);
            Assert.Equal(SpecialType.System_Int32, p2.Type.SpecialType);
            Assert.False(p2.IsReadOnly);
            Assert.False(p2.IsWriteOnly);
            Assert.False(p2.IsImplicitlyDeclared);
            Assert.Equal(Accessibility.Public, p2.DeclaredAccessibility);
            Assert.True(p2.IsVirtual);
            Assert.False(p2.IsStatic);
            Assert.Equal(c, p2.ContainingType);
            Assert.Equal(c, p2.ContainingSymbol);

            backing = p2.BackingField;
            Debug.Assert(backing != null);
            Assert.Equal(p2, backing.AssociatedSymbol);
            Assert.Equal(c, backing.ContainingSymbol);
            Assert.Equal(c, backing.ContainingType);
            Assert.True(backing.IsImplicitlyDeclared);

            getAccessor = p2.GetMethod;
            Assert.Equal(p2, getAccessor.AssociatedSymbol);
            Assert.True(getAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, getAccessor.ContainingSymbol);
            Assert.Equal(c, getAccessor.ContainingType);

            setAccessor = p2.SetMethod;
            Assert.Equal(p2, setAccessor.AssociatedSymbol);
            Assert.True(setAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, setAccessor.ContainingSymbol);
            Assert.Equal(c, setAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, setAccessor.DeclaredAccessibility);
            Assert.True(setAccessor.IsInitOnly);

            var p3 = (SourcePropertySymbolBase)c.GetProperty("P3");
            Assert.NotNull(p3.GetMethod);
            Assert.Equal(MethodKind.PropertyGet, p3.GetMethod.MethodKind);
            Assert.Equal(SpecialType.System_Int32, p3.Type.SpecialType);
            Assert.False(p3.IsReadOnly);
            Assert.False(p3.IsWriteOnly);
            Assert.False(p3.IsImplicitlyDeclared);
            Assert.Equal(Accessibility.Public, p3.DeclaredAccessibility);
            Assert.False(p3.IsVirtual);
            Assert.True(p3.IsAbstract);
            Assert.False(p3.IsStatic);
            Assert.Equal(c, p3.ContainingType);
            Assert.Equal(c, p3.ContainingSymbol);

            Assert.Null(p3.BackingField);

            getAccessor = p3.GetMethod;
            Assert.True(getAccessor.IsAbstract);
            Assert.Equal(p3, getAccessor.AssociatedSymbol);
            Assert.True(getAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, getAccessor.ContainingSymbol);
            Assert.Equal(c, getAccessor.ContainingType);

            setAccessor = p3.SetMethod;
            Assert.True(setAccessor.IsAbstract);
            Assert.Equal(p3, setAccessor.AssociatedSymbol);
            Assert.True(setAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, setAccessor.ContainingSymbol);
            Assert.Equal(c, setAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, setAccessor.DeclaredAccessibility);
            Assert.True(setAccessor.IsInitOnly);
        }

        [Fact]
        public void DataProperties12()
        {
            var src = @"
class C
{
    data dynamic P;
}";
            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Runtime_CompilerServices_DynamicAttribute);
            comp.VerifyDiagnostics(
                // (4,10): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     data dynamic P;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(4, 10)
            );

            CompileAndVerify(new[] { src, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview,
                symbolValidator: m =>
            {
                var p = m.GlobalNamespace.GetTypeMember("C").GetMember("P");
                var attr = Assert.Single(p.GetAttributes()).AttributeClass!;
                Assert.Equal(
                    "System.Runtime.CompilerServices.DynamicAttribute",
                    attr.ToTestDisplayString());
                Assert.NotEqual(m.ContainingAssembly, attr.ContainingAssembly);
            });
        }

        [Fact]
        public void DataPropertiesInterface()
        {
            var src = @"
interface I
{
    data int P1;
    data int P2 = 0;
    static data int P3;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,14): error CS8053: Instance properties in interfaces cannot have initializers.
                //     data int P2 = 0;
                Diagnostic(ErrorCode.ERR_InstancePropertyInitializerInInterface, "P2").WithArguments("I.P2").WithLocation(5, 14),
                // (6,21): error CS0106: The modifier 'static' is not valid for this item
                //     static data int P3;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P3").WithArguments("static").WithLocation(6, 21)
            );

            var p1 = comp.GlobalNamespace.GetTypeMember("I").GetMember<PropertySymbol>("P1");
            Assert.True(p1.IsAbstract);
            Assert.True(p1.GetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsAbstract);
            Assert.True(p1.SetMethod.IsInitOnly);
        }

        [Fact]
        public void DataPropertiesOverride()
        {
            var src = @"
abstract class C1
{
    data int P1;
    virtual data int P2;
    abstract data int P3;
}
abstract class C2 : C1
{
    data int P1; // warn 1
    data int P2; // warn 2
    data int P3; // error
}
class C3 : C1
{
    new data int P1;
    new data int P2;
    override data int P3;
}
class C4 : C1
{
    public new int P1 { get; init; }
    public new virtual int P2 { get; }
    public override int P3 { get; init; }
}
class C5 : C4
{
    new data int P1;
    override data int P2; // error
    override data int P3;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (10,14): warning CS0108: 'C2.P1' hides inherited member 'C1.P1'. Use the new keyword if hiding was intended.
                //     data int P1; // warn 1
                Diagnostic(ErrorCode.WRN_NewRequired, "P1").WithArguments("C2.P1", "C1.P1").WithLocation(10, 14),
                // (11,14): warning CS0114: 'C2.P2' hides inherited member 'C1.P2'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     data int P2; // warn 2
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P2").WithArguments("C2.P2", "C1.P2").WithLocation(11, 14),
                // (12,14): error CS0533: 'C2.P3' hides inherited abstract member 'C1.P3'
                //     data int P3; // error
                Diagnostic(ErrorCode.ERR_HidingAbstractMethod, "P3").WithArguments("C2.P3", "C1.P3").WithLocation(12, 14),
                // (12,14): warning CS0114: 'C2.P3' hides inherited member 'C1.P3'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
                //     data int P3; // error
                Diagnostic(ErrorCode.WRN_NewOrOverrideExpected, "P3").WithArguments("C2.P3", "C1.P3").WithLocation(12, 14),
                // (29,14): error CS0546: 'C5.P2.init': cannot override because 'C4.P2' does not have an overridable set accessor
                //     override data int P2; // error
                Diagnostic(ErrorCode.ERR_NoSetToOverride, "data").WithArguments("C5.P2.init", "C4.P2").WithLocation(29, 14)
            );
        }

        [Fact]
        public void DataPropertiesSealed()
        {
            var src = @"
abstract class C1
{
    abstract data int P1;
}
class C2 : C1
{
    sealed override data int P1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,30): error CS0106: The modifier 'sealed' is not valid for this item
                //     sealed override data int P1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("sealed").WithLocation(8, 30)
            );
        }

        [Fact]
        public void GenericRecord()
        {
            var src = @"
using System;
record A<T>
{
    public T Prop { get; init; }
}
record B : A<int>;
record C<T>(T Prop2) : A<T>;
class P
{
    public static void Main()
    {
        var a = new A<int>() { Prop = 1 };
        var a2 = a with { Prop = 2 };
        Console.WriteLine(a.Prop + "" "" + a2.Prop);

        var b = new B() { Prop = 3 };
        var b2 = b with { Prop = 4 };
        Console.WriteLine(b.Prop + "" "" + b2.Prop);

        var c = new C<int>(5) { Prop = 6 };
        var c2 = c with { Prop = 7, Prop2 = 8 };
        Console.WriteLine(c.Prop + "" "" + c.Prop2);
        Console.WriteLine(c2.Prop2 + "" "" + c2.Prop);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
1 2
3 4
6 5
8 7");
        }

        [Fact]
        public void RecordCloneSymbol()
        {
            var src = @"
record R;
record R2 : R";
            var comp = CreateCompilation(src);
            var r = comp.GlobalNamespace.GetTypeMember("R");
            var clone = (MethodSymbol)r.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.False(clone.IsOverride);
            Assert.True(clone.IsVirtual);
            Assert.False(clone.IsAbstract);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(0, clone.Arity);

            var r2 = comp.GlobalNamespace.GetTypeMember("R2");
            var clone2 = (MethodSymbol)r2.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone2.IsOverride);
            Assert.False(clone2.IsVirtual);
            Assert.False(clone2.IsAbstract);
            Assert.Equal(0, clone2.ParameterCount);
            Assert.Equal(0, clone2.Arity);
            Assert.True(clone2.OverriddenMethod.Equals(clone, TypeCompareKind.ConsiderEverything));
        }

        [Fact]
        public void AbstractRecordClone()
        {
            var src = @"
abstract record R;
abstract record R2 : R;
record R3 : R2;
abstract record R4 : R3;
record R5 : R4;

class C
{
    public static void Main()
    {
        R r = new R3();
        r = r with { };
        R4 r4 = new R5();
        r4 = r4 with { };
    }
}";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.ReleaseExe);

            var r = comp.GlobalNamespace.GetTypeMember("R");
            var clone = (MethodSymbol)r.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(0, clone.Arity);
            Assert.Equal("R R.<>Clone()", clone.ToTestDisplayString());

            var r2 = comp.GlobalNamespace.GetTypeMember("R2");
            var clone2 = (MethodSymbol)r2.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone2.IsOverride);
            Assert.False(clone2.IsVirtual);
            Assert.True(clone2.IsAbstract);
            Assert.Equal(0, clone2.ParameterCount);
            Assert.Equal(0, clone2.Arity);
            Assert.True(clone2.OverriddenMethod.Equals(clone, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R2.<>Clone()", clone2.ToTestDisplayString());

            var r3 = comp.GlobalNamespace.GetTypeMember("R3");
            var clone3 = (MethodSymbol)r3.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone3.IsOverride);
            Assert.False(clone3.IsVirtual);
            Assert.False(clone3.IsAbstract);
            Assert.Equal(0, clone3.ParameterCount);
            Assert.Equal(0, clone3.Arity);
            Assert.True(clone3.OverriddenMethod.Equals(clone2, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R3.<>Clone()", clone3.ToTestDisplayString());

            var r4 = comp.GlobalNamespace.GetTypeMember("R4");
            var clone4 = (MethodSymbol)r4.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone4.IsOverride);
            Assert.False(clone4.IsVirtual);
            Assert.True(clone4.IsAbstract);
            Assert.Equal(0, clone4.ParameterCount);
            Assert.Equal(0, clone4.Arity);
            Assert.True(clone4.OverriddenMethod.Equals(clone3, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R4.<>Clone()", clone4.ToTestDisplayString());

            var r5 = comp.GlobalNamespace.GetTypeMember("R5");
            var clone5 = (MethodSymbol)r5.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone5.IsOverride);
            Assert.False(clone5.IsVirtual);
            Assert.False(clone5.IsAbstract);
            Assert.Equal(0, clone5.ParameterCount);
            Assert.Equal(0, clone5.Arity);
            Assert.True(clone5.OverriddenMethod.Equals(clone4, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R5.<>Clone()", clone5.ToTestDisplayString());

            var verifier = CompileAndVerify(comp, expectedOutput: "", verify: Verification.Passes);
            verifier.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  newobj     ""R3..ctor()""
  IL_0005:  callvirt   ""R R.<>Clone()""
  IL_000a:  pop
  IL_000b:  newobj     ""R5..ctor()""
  IL_0010:  callvirt   ""R R.<>Clone()""
  IL_0015:  castclass  ""R4""
  IL_001a:  pop
  IL_001b:  ret
}");
        }

        [Fact]
        public void DataPropertiesInitOnly()
        {
            var src = @"
class C
{
    data int X;

    public static void Main()
    {
        var c = new C() { X = 0 };
        c.X = 2;
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,9): error CS8852: Init-only property or indexer 'C.X' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                //         c.X = 2;
                Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "c.X").WithArguments("C.X").WithLocation(9, 9));
        }

        [Fact]
        public void DataPropertiesEmit()
        {
            var src = @"
using System;
class C
{
    data int X;

    public static void Main()
    {
        var c = new C() { X = 5 };
        Console.WriteLine(c.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: "5");
            verifier.VerifyIL("C.X.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<X>k__BackingField""
  IL_0006:  ret
}");
            verifier.VerifyIL("C.X.init", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ret
}");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  3
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.5
  IL_0007:  callvirt   ""void C.X.init""
  IL_000c:  callvirt   ""int C.X.get""
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  ret
}");
        }

        [Fact]
        public void DataPropertiesSemanticModel()
        {
            var src = @"
#nullable enable
abstract class B
{
    public abstract int P2 { get; init; }
}
class C : B
{
    data int P1 = 2;
    override data int P2 = 2;
    data int P3 = P1;
    data string P4 = null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,19): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.P1'
                //     data int P3 = P1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "P1").WithArguments("C.P1").WithLocation(11, 19),
                // (12,22): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     data string P4 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 22)
            );
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var props = tree.GetRoot().DescendantNodes().OfType<DataPropertyDeclarationSyntax>().ToList();
            var p1 = (IPropertySymbol)model.GetDeclaredSymbol(props[0])!;
            Assert.NotNull(p1);
            Assert.Equal("P1", p1.Name);
            Assert.False(p1.IsReadOnly);
            Assert.False(p1.IsWriteOnly);
            Assert.True(p1.SetMethod!.IsInitOnly);
            Assert.False(p1.IsAbstract);
            Assert.False(p1.IsOverride);
            Assert.False(p1.IsVirtual);

            var typeInfo = model.GetTypeInfo(props[0].Initializer!.Value);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type!.SpecialType);
            Assert.Equal(CodeAnalysis.NullableFlowState.NotNull, typeInfo.Nullability.FlowState);

            var initializer = props[1].Initializer;
            Assert.NotNull(initializer);
            typeInfo = model.GetTypeInfo(initializer!.Value);
            Assert.Equal(SpecialType.System_Int32, typeInfo.Type!.SpecialType);

            var p2 = (IPropertySymbol)model.GetDeclaredSymbol(props[1])!;
            Assert.NotNull(p2);
            Assert.Equal("P2", p2.Name);
            Assert.False(p2.IsReadOnly);
            Assert.False(p2.IsWriteOnly);
            Assert.True(p2.SetMethod!.IsInitOnly);
            Assert.False(p2.IsAbstract);
            Assert.True(p2.IsOverride);
            Assert.False(p2.IsVirtual);

            var symbolInfo = model.GetSymbolInfo(props[2].Initializer!.Value);
            Assert.True(symbolInfo.Symbol!.Equals(p1, SymbolEqualityComparer.Default));

            var p4 = (IPropertySymbol)model.GetDeclaredSymbol(props[3])!;
            Assert.Equal(CodeAnalysis.NullableAnnotation.NotAnnotated, p4.Type.NullableAnnotation);
            typeInfo = model.GetTypeInfo(props[3].Initializer!.Value);
            Assert.Equal(CodeAnalysis.NullableFlowState.MaybeNull, typeInfo.Nullability.FlowState);
        }
    }
}
