// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RecordTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source)
            => CSharpTestBase.CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);

        private CompilationVerifier CompileAndVerify(CSharpTestSource src, string? expectedOutput = null)
            => base.CompileAndVerify(new[] { src, IsExternalInitTypeDefinition },
                expectedOutput: expectedOutput,
                parseOptions: TestOptions.Regular9,
                // init-only fails verification
                verify: Verification.FailsPEVerify);

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
                // (4,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C(int a, string b)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 12),
                // (4,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public C(int a, string b)
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(4, 12)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctor = (MethodSymbol)c.GetMembers(".ctor")[2];
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
                // (4,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
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
                    if (ctor is SynthesizedPrimaryConstructor)
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
            Assert.Equal(MethodKind.PropertyGet, x.GetMethod!.MethodKind);
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
            Assert.Equal(x, setAccessor!.AssociatedSymbol);
            Assert.True(setAccessor.IsImplicitlyDeclared);
            Assert.Equal(c, setAccessor.ContainingSymbol);
            Assert.Equal(c, setAccessor.ContainingType);
            Assert.Equal(Accessibility.Public, setAccessor.DeclaredAccessibility);
            Assert.True(setAccessor.IsInitOnly);

            var y = (SourcePropertySymbolBase)c.GetProperty("y");
            Assert.NotNull(y.GetMethod);
            Assert.Equal(MethodKind.PropertyGet, y.GetMethod!.MethodKind);
            Assert.Equal(SpecialType.System_Int32, y.Type.SpecialType);
            Assert.False(y.IsReadOnly);
            Assert.False(y.IsWriteOnly);
            Assert.False(y.IsImplicitlyDeclared);
            Assert.Equal(Accessibility.Public, y.DeclaredAccessibility);
            Assert.False(x.IsVirtual);
            Assert.False(x.IsStatic);
            Assert.Equal(c, y.ContainingType);
            Assert.Equal(c, y.ContainingSymbol);

            backing = y.BackingField;
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
            Assert.Equal(y, setAccessor!.AssociatedSymbol);
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
                // (4,17): error CS8872: 'C.Equals(C)' must allow overriding because the containing record is not sealed.
                //     public bool Equals(C c) => throw null;
                Diagnostic(ErrorCode.ERR_NotOverridableAPIInRecord, "Equals").WithArguments("C.Equals(C)").WithLocation(4, 17),
                // (4,17): warning CS8851: 'C' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(C c) => throw null;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("C").WithLocation(4, 17),
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
    public virtual bool Equals(C c) => false;
}", expectedOutput: "False").VerifyDiagnostics(
    // (10,25): warning CS8851: 'C' defines 'Equals' but not 'GetHashCode'
    //     public virtual bool Equals(C c) => false;
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("C").WithLocation(10, 25)
);
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
True").VerifyDiagnostics();
        }

        [Fact]
        public void RecordEquals_03()
        {
            var verifier = CompileAndVerify(@"
using System;
sealed record C(int X, int Y)
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
False").VerifyDiagnostics(
    // (13,17): warning CS8851: 'C' defines 'Equals' but not 'GetHashCode'
    //     public bool Equals(C c) => X == c.X && Y == c.Y;
    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("C").WithLocation(13, 17)
);

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
False").VerifyDiagnostics();

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
  // Code size       59 (0x3b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0039
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_0037
  IL_0007:  ldarg.0
  IL_0008:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0013:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0018:  brfalse.s  IL_0037
  IL_001a:  ldarg.0
  IL_001b:  ldfld      ""int C.<X>k__BackingField""
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.<X>k__BackingField""
  IL_0026:  bne.un.s   IL_0037
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<Y>k__BackingField""
  IL_002e:  ldarg.1
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ceq
  IL_0036:  ret
  IL_0037:  ldc.i4.0
  IL_0038:  ret
  IL_0039:  ldc.i4.1
  IL_003a:  ret
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
False").VerifyDiagnostics();
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
True").VerifyDiagnostics();
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
True").VerifyDiagnostics();

            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       73 (0x49)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0047
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_0045
  IL_0007:  ldarg.0
  IL_0008:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0013:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0018:  brfalse.s  IL_0045
  IL_001a:  ldarg.0
  IL_001b:  ldfld      ""int C.<X>k__BackingField""
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.<X>k__BackingField""
  IL_0026:  bne.un.s   IL_0045
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<Y>k__BackingField""
  IL_002e:  ldarg.1
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  bne.un.s   IL_0045
  IL_0036:  ldarg.0
  IL_0037:  ldfld      ""int C.Z""
  IL_003c:  ldarg.1
  IL_003d:  ldfld      ""int C.Z""
  IL_0042:  ceq
  IL_0044:  ret
  IL_0045:  ldc.i4.0
  IL_0046:  ret
  IL_0047:  ldc.i4.1
  IL_0048:  ret
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
True").VerifyDiagnostics();
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
True").VerifyDiagnostics();

            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0039
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_0037
  IL_0007:  ldarg.0
  IL_0008:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0013:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0018:  brfalse.s  IL_0037
  IL_001a:  ldarg.0
  IL_001b:  ldfld      ""int C.<X>k__BackingField""
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.<X>k__BackingField""
  IL_0026:  bne.un.s   IL_0037
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<Y>k__BackingField""
  IL_002e:  ldarg.1
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ceq
  IL_0036:  ret
  IL_0037:  ldc.i4.0
  IL_0038:  ret
  IL_0039:  ldc.i4.1
  IL_003a:  ret
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
  // Code size       59 (0x3b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_0039
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_0037
  IL_0007:  ldarg.0
  IL_0008:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0013:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0018:  brfalse.s  IL_0037
  IL_001a:  ldarg.0
  IL_001b:  ldfld      ""int C.<X>k__BackingField""
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.<X>k__BackingField""
  IL_0026:  bne.un.s   IL_0037
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<Y>k__BackingField""
  IL_002e:  ldarg.1
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  ceq
  IL_0036:  ret
  IL_0037:  ldc.i4.0
  IL_0038:  ret
  IL_0039:  ldc.i4.1
  IL_003a:  ret
}");
        }

        [Fact]
        public void RecordEquals_12()
        {
            var verifier = CompileAndVerify(@"
using System;
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
True").VerifyDiagnostics();

            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       81 (0x51)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_004f
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_004d
  IL_0007:  ldarg.0
  IL_0008:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0013:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0018:  brfalse.s  IL_004d
  IL_001a:  ldarg.0
  IL_001b:  ldfld      ""int C.<X>k__BackingField""
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.<X>k__BackingField""
  IL_0026:  bne.un.s   IL_004d
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<Y>k__BackingField""
  IL_002e:  ldarg.1
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  bne.un.s   IL_004d
  IL_0036:  call       ""System.Collections.Generic.EqualityComparer<System.Action> System.Collections.Generic.EqualityComparer<System.Action>.Default.get""
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""System.Action C.E""
  IL_0041:  ldarg.1
  IL_0042:  ldfld      ""System.Action C.E""
  IL_0047:  callvirt   ""bool System.Collections.Generic.EqualityComparer<System.Action>.Equals(System.Action, System.Action)""
  IL_004c:  ret
  IL_004d:  ldc.i4.0
  IL_004e:  ret
  IL_004f:  ldc.i4.1
  IL_0050:  ret
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

            // PEVerify: Cannot change initonly field outside its .ctor.
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics();
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

            // PEVerify: Cannot change initonly field outside its .ctor.
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics();
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
            comp.VerifyDiagnostics(
                // (5,25): warning CS0067: The event 'C.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(5, 25)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var clone = c.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(c, clone.ReturnType);

            var ctor = (MethodSymbol)c.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(c, TypeCompareKind.ConsiderEverything));

            // PEVerify: Cannot change initonly field outside its .ctor.
            var verifier = CompileAndVerify(comp, verify: Verification.FailsPEVerify).VerifyDiagnostics(
                // (5,25): warning CS0067: The event 'C.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(5, 25)
                );

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
}", verify: Verification.Passes, expectedOutput: @"False
False
True
True").VerifyDiagnostics(
                // (5,17): warning CS0414: The field 'C.X' is assigned but its value is never used
                //     private int X;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("C.X").WithLocation(5, 17)
                );

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
  // Code size       81 (0x51)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_004f
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_004d
  IL_0007:  ldarg.0
  IL_0008:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000d:  ldarg.1
  IL_000e:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0013:  call       ""bool System.Type.op_Equality(System.Type, System.Type)""
  IL_0018:  brfalse.s  IL_004d
  IL_001a:  ldarg.0
  IL_001b:  ldfld      ""int C.X""
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.X""
  IL_0026:  bne.un.s   IL_004d
  IL_0028:  ldarg.0
  IL_0029:  ldfld      ""int C.<Y>k__BackingField""
  IL_002e:  ldarg.1
  IL_002f:  ldfld      ""int C.<Y>k__BackingField""
  IL_0034:  bne.un.s   IL_004d
  IL_0036:  call       ""System.Collections.Generic.EqualityComparer<System.Action> System.Collections.Generic.EqualityComparer<System.Action>.Default.get""
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""System.Action C.E""
  IL_0041:  ldarg.1
  IL_0042:  ldfld      ""System.Action C.E""
  IL_0047:  callvirt   ""bool System.Collections.Generic.EqualityComparer<System.Action>.Equals(System.Action, System.Action)""
  IL_004c:  ret
  IL_004d:  ldc.i4.0
  IL_004e:  ret
  IL_004f:  ldc.i4.1
  IL_0050:  ret
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
").VerifyDiagnostics(
                // (5,25): warning CS0067: The event 'C.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(5, 25)
                );

            var v2 = CompileAndVerify(@"
using System;
record C
{
    public int X { get; }
    public string Y { get; }
    public event Action E;
}", verify: Verification.Passes).VerifyDiagnostics(
                // (7,25): warning CS0067: The event 'C.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(7, 25)
                );

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
                "System.Type! C.EqualityContract.get",
                "System.Type! C.EqualityContract { get; }",
                "System.Int32 C.X.field",
                "System.Int32 C.X { get; init; }",
                "System.Int32 C.X.get",
                "void C.X.init",
                "System.String! C.Y.field",
                "System.String! C.Y { get; init; }",
                "System.String! C.Y.get",
                "void C.Y.init",
                "System.String! C.ToString()",
                "System.Boolean C." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder! builder)",
                "System.Boolean C.operator !=(C? left, C? right)",
                "System.Boolean C.operator ==(C? left, C? right)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? obj)",
                "System.Boolean C.Equals(C? other)",
                "C! C." + WellKnownMemberNames.CloneMethodName + "()",
                "C.C(C! original)",
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
                // (13,17): error CS8863: Only a single partial type declaration may have a parameter list
                // partial record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int X, int Y)").WithLocation(13, 17)
                );

            Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor(C original)" }, comp.GetTypeByMetadataName("C")!.Constructors.Select(m => m.ToTestDisplayString()));
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
                // (13,17): error CS8863: Only a single partial type declaration may have a parameter list
                // partial record C(int X)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int X)").WithLocation(13, 17)
                );

            Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor(C original)" }, comp.GetTypeByMetadataName("C")!.Constructors.Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void PartialTypes_03()
        {
            var src = @"
partial record C
{
    public int X = 1;
}
partial record C(int Y);
partial record C
{
    public int Z { get; } = 2;
}";
            var verifier = CompileAndVerify(src).VerifyDiagnostics();
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
        public void PartialTypes_04_PartialBeforeModifiers()
        {
            var src = @"
partial public record C
{
}
";
            CreateCompilation(src).VerifyDiagnostics(
                // (2,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public record C
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(2, 1)
                );
        }

        [Fact]
        public void DataClassAndStruct()
        {
            var src = @"
data class C1 { }
data class C2(int X, int Y);
data struct S1 { }
data struct S2(int X, int Y);";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,6): error CS1001: Identifier expected
                // data class C1 { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "class").WithLocation(2, 6),
                // (2,6): error CS1002: ; expected
                // data class C1 { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(2, 6),
                // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "data ").WithLocation(3, 1),
                // (3,6): error CS1001: Identifier expected
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "class").WithLocation(3, 6),
                // (3,6): error CS1002: ; expected
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(3, 6),
                // (3,19): warning CS9113: Parameter 'X' is unread.
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "X").WithArguments("X").WithLocation(3, 19),
                // (3,26): warning CS9113: Parameter 'Y' is unread.
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Y").WithArguments("Y").WithLocation(3, 26),
                // (5,20): warning CS9113: Parameter 'X' is unread.
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "X").WithArguments("X").WithLocation(5, 20),
                // (5,27): warning CS9113: Parameter 'Y' is unread.
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Y").WithArguments("Y").WithLocation(5, 27),
                // (4,6): error CS1001: Identifier expected
                // data struct S1 { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(4, 6),
                // (4,6): error CS1002: ; expected
                // data struct S1 { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(4, 6),
                // (5,6): error CS1001: Identifier expected
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(5, 6),
                // (5,6): error CS1002: ; expected
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(5, 6),
                // (2,1): error CS8805: Program using top-level statements must be an executable.
                // data class C1 { }
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "data ").WithLocation(2, 1),
                // (2,1): error CS0246: The type or namespace name 'data' could not be found (are you missing a using directive or an assembly reference?)
                // data class C1 { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "data").WithArguments("data").WithLocation(2, 1),
                // (3,1): error CS0246: The type or namespace name 'data' could not be found (are you missing a using directive or an assembly reference?)
                // data class C2(int X, int Y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "data").WithArguments("data").WithLocation(3, 1),
                // (4,1): error CS0246: The type or namespace name 'data' could not be found (are you missing a using directive or an assembly reference?)
                // data struct S1 { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "data").WithArguments("data").WithLocation(4, 1),
                // (5,1): error CS0246: The type or namespace name 'data' could not be found (are you missing a using directive or an assembly reference?)
                // data struct S2(int X, int Y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "data").WithArguments("data").WithLocation(5, 1)
            );
        }

        [WorkItem(44781, "https://github.com/dotnet/roslyn/issues/44781")]
        [Fact]
        public void ClassInheritingFromRecord()
        {
            var src = @"
abstract record AbstractRecord {}
class SomeClass : AbstractRecord {}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,19): error CS8865: Only records may inherit from records.
                // class SomeClass : AbstractRecord {}
                Diagnostic(ErrorCode.ERR_BadInheritanceFromRecord, "AbstractRecord").WithLocation(3, 19)
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
                parseOptions: TestOptions.Regular9,
                references: new[] {
                emitReference ? comp.EmitToImageReference() : comp.ToMetadataReference()
            });

            comp2.VerifyDiagnostics(
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
8 7").VerifyDiagnostics();
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
                parseOptions: TestOptions.Regular9,
                options: TestOptions.ReleaseExe);

            var r = comp.GlobalNamespace.GetTypeMember("R");
            var clone = (MethodSymbol)r.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.False(clone.IsOverride);
            Assert.False(clone.IsVirtual);
            Assert.True(clone.IsAbstract);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(0, clone.Arity);
            Assert.Equal("R R." + WellKnownMemberNames.CloneMethodName + "()", clone.ToTestDisplayString());
            Assert.True(clone.IsImplicitlyDeclared);

            var r2 = comp.GlobalNamespace.GetTypeMember("R2");
            var clone2 = (MethodSymbol)r2.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone2.IsOverride);
            Assert.False(clone2.IsVirtual);
            Assert.True(clone2.IsAbstract);
            Assert.Equal(0, clone2.ParameterCount);
            Assert.Equal(0, clone2.Arity);
            Assert.True(clone2.OverriddenMethod.Equals(clone, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R2." + WellKnownMemberNames.CloneMethodName + "()", clone2.ToTestDisplayString());
            Assert.True(clone2.IsImplicitlyDeclared);

            var r3 = comp.GlobalNamespace.GetTypeMember("R3");
            var clone3 = (MethodSymbol)r3.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone3.IsOverride);
            Assert.False(clone3.IsVirtual);
            Assert.False(clone3.IsAbstract);
            Assert.Equal(0, clone3.ParameterCount);
            Assert.Equal(0, clone3.Arity);
            Assert.True(clone3.OverriddenMethod.Equals(clone2, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R3." + WellKnownMemberNames.CloneMethodName + "()", clone3.ToTestDisplayString());
            Assert.True(clone3.IsImplicitlyDeclared);

            var r4 = comp.GlobalNamespace.GetTypeMember("R4");
            var clone4 = (MethodSymbol)r4.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone4.IsOverride);
            Assert.False(clone4.IsVirtual);
            Assert.True(clone4.IsAbstract);
            Assert.Equal(0, clone4.ParameterCount);
            Assert.Equal(0, clone4.Arity);
            Assert.True(clone4.OverriddenMethod.Equals(clone3, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R4." + WellKnownMemberNames.CloneMethodName + "()", clone4.ToTestDisplayString());
            Assert.True(clone4.IsImplicitlyDeclared);

            var r5 = comp.GlobalNamespace.GetTypeMember("R5");
            var clone5 = (MethodSymbol)r5.GetMembers(WellKnownMemberNames.CloneMethodName).Single();
            Assert.True(clone5.IsOverride);
            Assert.False(clone5.IsVirtual);
            Assert.False(clone5.IsAbstract);
            Assert.Equal(0, clone5.ParameterCount);
            Assert.Equal(0, clone5.Arity);
            Assert.True(clone5.OverriddenMethod.Equals(clone4, TypeCompareKind.ConsiderEverything));
            Assert.Equal("R R5." + WellKnownMemberNames.CloneMethodName + "()", clone5.ToTestDisplayString());
            Assert.True(clone5.IsImplicitlyDeclared);

            var verifier = CompileAndVerify(comp, expectedOutput: "", verify: Verification.Passes).VerifyDiagnostics();
            verifier.VerifyIL("C.Main", @"
{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  newobj     ""R3..ctor()""
  IL_0005:  callvirt   ""R R." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_000a:  pop
  IL_000b:  newobj     ""R5..ctor()""
  IL_0010:  callvirt   ""R R." + WellKnownMemberNames.CloneMethodName + @"()""
  IL_0015:  castclass  ""R4""
  IL_001a:  pop
  IL_001b:  ret
}");
        }

        [Fact]
        [WorkItem(49286, "https://github.com/dotnet/roslyn/issues/49286")]
        public void RecordWithEventImplicitlyImplementingAnInterface()
        {
            var src = @"
using System;

public interface I1
{
    event Action E1;
}

public record R1 : I1
{
    public event Action E1 {  add { } remove { } }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(49286, "https://github.com/dotnet/roslyn/issues/49286")]
        public void RecordWithPropertyImplicitlyImplementingAnInterface()
        {
            var src = @"
using System;

public interface I1
{
    Action P1 { get; set; }
}

public record R1 : I1
{
    public Action P1 {  get; set; }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(49286, "https://github.com/dotnet/roslyn/issues/49286")]
        public void RecordWithMethodImplicitlyImplementingAnInterface()
        {
            var src = @"
using System;

public interface I1
{
    Action M1();
}

public record R1 : I1
{
    public Action M1() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void MergeInitializers_01()
        {
            var src = @"
record C(int X)
{
    public int Y = 22;
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src, expectedOutput: "C { X = 11, Y = 22 }").VerifyDiagnostics();
        }

        [Fact]
        public void MergeInitializers_02()
        {
            var src1 = @"
partial record C(int X)
{
}
";
            var src2 = @"
partial record C
{
    public int Y = 22;
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src1 + src2, expectedOutput: "C { X = 11, Y = 22 }").VerifyDiagnostics();
            CompileAndVerify(new[] { src1, src2 }, expectedOutput: "C { X = 11, Y = 22 }").VerifyDiagnostics();
        }

        [Fact]
        public void MergeInitializers_03()
        {
            var src1 = @"
partial record C
{
    public int Y = 22;
}
";
            var src2 = @"
partial record C(int X)
{
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src1 + src2, expectedOutput: "C { Y = 22, X = 11 }").VerifyDiagnostics();
            CompileAndVerify(new[] { src1, src2 }, expectedOutput: "C { Y = 22, X = 11 }").VerifyDiagnostics();
        }

        [Fact]
        public void MergeInitializers_04()
        {
            var src1 = @"
partial record C
{
    public int Y = 22;
}
";
            var src2 = @"
partial record C(int X)
{
}
";
            var src3 = @"
partial record C
{
    public int Z = 33;
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src1 + src2 + src3, expectedOutput: "C { Y = 22, X = 11, Z = 33 }").VerifyDiagnostics();
            CompileAndVerify(new[] { src1, src2, src3 }, expectedOutput: "C { Y = 22, X = 11, Z = 33 }").VerifyDiagnostics();
        }

        [Fact]
        public void MergeInitializers_05()
        {
            var src1 = @"
partial record C(int X)
{
    public int U = 44;
}
";
            var src2 = @"
partial record C
{
    public int Y = 22;
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src1 + src2, expectedOutput: "C { X = 11, U = 44, Y = 22 }").VerifyDiagnostics();
            CompileAndVerify(new[] { src1, src2 }, expectedOutput: "C { X = 11, U = 44, Y = 22 }").VerifyDiagnostics();
        }

        [Fact]
        public void MergeInitializers_06()
        {
            var src1 = @"
partial record C
{
    public int Y = 22;
}
";
            var src2 = @"
partial record C(int X)
{
    public int U = 44;
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src1 + src2, expectedOutput: "C { Y = 22, X = 11, U = 44 }").VerifyDiagnostics();
            CompileAndVerify(new[] { src1, src2 }, expectedOutput: "C { Y = 22, X = 11, U = 44 }").VerifyDiagnostics();
        }

        [Fact]
        public void MergeInitializers_07()
        {
            var src1 = @"
partial record C
{
    public int Y = 22;
}
";
            var src2 = @"
partial record C(int X)
{
    public int U = 44;
}
";
            var src3 = @"
partial record C
{
    public int Z = 33;
}

class Test
{
    static void Main()
    {
        System.Console.Write((new C(11)).ToString());
    }
}
";
            CompileAndVerify(src1 + src2 + src3, expectedOutput: "C { Y = 22, X = 11, U = 44, Z = 33 }").VerifyDiagnostics();
            CompileAndVerify(new[] { src1, src2, src3 }, expectedOutput: "C { Y = 22, X = 11, U = 44, Z = 33 }").VerifyDiagnostics();
        }
    }
}
