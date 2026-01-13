// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class UnionsTests : CSharpTestBase
    {
        public static string IUnionSource => @"
namespace System.Runtime.CompilerServices
{
    public interface IUnion
    {
#nullable enable
        object? Value { get; }
#nullable disable
    }
}
";

        [Fact]
        public void UnionType_01()
        {
            var src = @"
public interface IUnion
{
#nullable enable
    object? Value { get; }
#nullable disable
}

interface I1 : System.Runtime.CompilerServices.IUnion;

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}

struct S2 : IUnion
{
    public object Value => null;
}

class C1 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}

sealed class C2 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}

sealed class C3 : IUnion
{
    public object Value => null;
}

sealed class C4 : C1
{
}

";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyEmitDiagnostics();

            Assert.True(comp.GetTypeByMetadataName("S1").IsUnionTypeNoUseSiteDiagnostics);
            Assert.True(comp.GetTypeByMetadataName("C1").IsUnionTypeNoUseSiteDiagnostics);
            Assert.True(comp.GetTypeByMetadataName("C2").IsUnionTypeNoUseSiteDiagnostics);
            Assert.True(comp.GetTypeByMetadataName("C4").IsUnionTypeNoUseSiteDiagnostics);

            Assert.False(comp.GetTypeByMetadataName("I1").IsUnionTypeNoUseSiteDiagnostics);
            Assert.False(comp.GetTypeByMetadataName("S2").IsUnionTypeNoUseSiteDiagnostics);
            Assert.False(comp.GetTypeByMetadataName("C3").IsUnionTypeNoUseSiteDiagnostics);
        }

        [Fact]
        public void UnionType_02_IUnionNotPublic()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}

namespace System.Runtime.CompilerServices
{
    internal interface IUnion
    {
#nullable enable
        object? Value { get; }
#nullable disable
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();

            Assert.False(comp.GetTypeByMetadataName("S1").IsUnionTypeNoUseSiteDiagnostics);
        }

        [Fact]
        public void UnionType_03_ManyIUnionTypes()
        {
            var src1 = @"
public struct S1 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}
";
            var comp1 = CreateCompilation([src1, IUnionSource]);
            comp1.VerifyEmitDiagnostics();
            Assert.True(comp1.GetTypeByMetadataName("S1").IsUnionTypeNoUseSiteDiagnostics);

            var src2 = @"
struct S2 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}
";
            var comp2 = CreateCompilation([src2, IUnionSource], references: [comp1.EmitToImageReference()]);
            comp1.VerifyEmitDiagnostics();

            Assert.True(comp2.GetTypeByMetadataName("S1").IsUnionTypeNoUseSiteDiagnostics);
            Assert.True(comp2.GetTypeByMetadataName("S2").IsUnionTypeNoUseSiteDiagnostics);
        }

        [Fact]
        public void CaseTypes_01()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public object Value => null;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x){}
    public S2(string x){}
    public object Value => null;
}

struct S3 : System.Runtime.CompilerServices.IUnion
{
    private S3(int x){}
    internal S3(string x){}
    public object Value => null;
}

struct S4 : System.Runtime.CompilerServices.IUnion
{
    public S4(int x, string y){}
    public object Value => null;
}

class C5 : System.Runtime.CompilerServices.IUnion
{
    protected C5(int x){}
    protected internal C5(string x){}
    private protected C5(decimal x){}
    public object Value => null;
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyEmitDiagnostics();

            VerifyCaseTypes(comp, "S1", []);
            VerifyCaseTypes(comp, "S2", ["System.Int32", "System.String"]);
            VerifyCaseTypes(comp, "S3", []);
            VerifyCaseTypes(comp, "S4", []);
            VerifyCaseTypes(comp, "C5", []);
        }

        private static void VerifyCaseTypes(CSharpCompilation comp, string typeName, string[] caseTypes)
        {
            var type = comp.GetTypeByMetadataName(typeName);
            Assert.True(type.IsUnionTypeNoUseSiteDiagnostics);
            AssertEx.SequenceEqual(caseTypes, type.UnionCaseTypes.ToTestDisplayStrings());
        }

        [Fact]
        public void CaseTypes_02()
        {
            var src = @"
struct S2 : System.Runtime.CompilerServices.IUnion
{
    public static S2(int x){}
    public object Value => null;
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (4,19): error CS0515: 'S2.S2(int)': access modifiers are not allowed on static constructors
                //     public static S2(int x){}
                Diagnostic(ErrorCode.ERR_StaticConstructorWithAccessModifiers, "S2").WithArguments("S2.S2(int)").WithLocation(4, 19),
                // (4,19): error CS0132: 'S2.S2(int)': a static constructor must be parameterless
                //     public static S2(int x){}
                Diagnostic(ErrorCode.ERR_StaticConstParam, "S2").WithArguments("S2.S2(int)").WithLocation(4, 19)
                );

            VerifyCaseTypes(comp, "S2", []);
        }

        [Fact]
        public void CaseTypes_03()
        {
            var src = @"
struct S2
{
    public S2(int x){}
    public S2(string x){}
    public object Value => null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();

            var type = comp.GetTypeByMetadataName("S2");
            Assert.False(type.IsUnionTypeNoUseSiteDiagnostics);
            AssertEx.SequenceEqual([], type.UnionCaseTypes.ToTestDisplayStrings());
        }

        [Fact]
        public void CaseTypes_04()
        {
            var src = @"
class C1 : System.Runtime.CompilerServices.IUnion
{
    public C1(int x){}
    public object Value => null;
}

sealed class C2 : C1, System.Runtime.CompilerServices.IUnion
{
    public C2(string x) : base(0) {}
    public new object Value => null;
}

class C3
{
    public C3(int x){}
}

sealed class C4 : C3, System.Runtime.CompilerServices.IUnion
{
    public C4(string x) : base(0) {}
    public object Value => null;
}

sealed class C5 : C1
{
    public C5(string x) : base(0) {}
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyEmitDiagnostics();

            VerifyCaseTypes(comp, "C1", ["System.Int32"]);
            VerifyCaseTypes(comp, "C2", ["System.String"]);
            VerifyCaseTypes(comp, "C4", ["System.String"]);

            // PROTOTYPE: This looks strange. C5 simply inherits from C1 and because of that it is treated as a union type.
            //            It doesn't change what IUnion.Value returns, but its constructors are treated as though they
            //            define possible types returned by IUnion.Value.
            VerifyCaseTypes(comp, "C5", ["System.String"]);
        }

        [Fact]
        public void CaseTypes_05()
        {
            var src = @"
#nullable enable
struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(string?[] x){}
    public S2(string[] x){}
    public S2((int a, int b) x){}
    public S2((int, int) x){}

    public object Value => null!;
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (6,12): error CS0111: Type 'S2' already defines a member called 'S2' with the same parameter types
                //     public S2(string? x){}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "S2").WithArguments("S2", "S2").WithLocation(6, 12),
                // (8,12): error CS0111: Type 'S2' already defines a member called 'S2' with the same parameter types
                //     public S2((int, int) x){}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "S2").WithArguments("S2", "S2").WithLocation(8, 12)
                );

            VerifyCaseTypes(comp, "S2", ["System.String?[]", "(System.Int32 a, System.Int32 b)"]);
        }

        [Fact]
        public void UnionMatching_01_Discard()
        {
            var src = @"
class S1 : System.Runtime.CompilerServices.IUnion
{
    public S1() { Value = null; }
    public S1(int x) { Value = x; }
    public S1(string x) { Value = x; }
    public object Value { get; }
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test(new S1(10)));
        System.Console.Write(Test(null));
        System.Console.Write(Test(new S1()));
    }

    static bool Test(S1 u)
    {
        if (u switch {_ => true })
        {
            return true;
        }

        return false;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueTrueTrue").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brfalse.s  IL_0005
  IL_0003:  ldc.i4.1
  IL_0004:  ret
  IL_0005:  ldc.i4.0
  IL_0006:  ret
}
");
        }

        [Fact]
        public void UnionMatching_02_Var()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) { Value = x; }
    public S1(string x) { Value = x; }
    public object Value { get; }

    public int Int => 123;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test(new S1(10)));
        System.Console.Write(Test(default));
    }

    static int Test(S1 u)
    {
        return (u switch {var v => v }).Int;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "123123").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_03_Var_Deconstruct()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) { Value = x; }
    public S1(string x) { Value = x; }
    public object Value { get; }

    public void Deconstruct(out int x, out int y) => throw null;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test(new S1(10)));
        System.Console.Write(Test(default));
    }

    static int Test(S1 u)
    {
        return (u switch {var (a, b) => a * 1000 + b * 10, _ => -1 } );
    }   
}

static class Extensions
{
    public static void Deconstruct(this object o, out int x, out int y)
    {
        x = 1;
        y = 2;
    }
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "1020-1").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_04_Var_ITuple()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(C x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(new C())));

        System.Console.Write(' ');
        System.Console.Write(Test2((new S1(10), -1)));
        System.Console.Write(Test2((default, -1)));
        System.Console.Write(Test2((new S1(new C()), -1)));

        System.Console.Write(' ');
        System.Console.Write(Test3(new C2(new S1(10))));
        System.Console.Write(Test3(new C2(default)));
        System.Console.Write(Test3(new C2(new S1(new C()))));
    }

    static bool Test1(S1 u)
    {
        return u is var (_, i) && (int)i == 10;
    }   

    static bool Test2((S1, int) u)
    {
        return u is var ((_, i), _) && (int)i == 10;
    }   

    static bool Test3(C2 u)
    {
        return u is var (_, ((_, i), _, _)) && (int)i == 10;
    }   
}

public class C : System.Runtime.CompilerServices.ITuple
{
    int System.Runtime.CompilerServices.ITuple.Length => 2;
    object System.Runtime.CompilerServices.ITuple.this[int i] => i * 10;
}

class C2 : System.Runtime.CompilerServices.ITuple
{
    private readonly S1 _value;
    public C2(S1 x) { _value = x; }
    int System.Runtime.CompilerServices.ITuple.Length => 2;
    object System.Runtime.CompilerServices.ITuple.this[int i] => _value;
}

static class Extensions
{
    public static void Deconstruct(this object o, out S1 x, out int y, out int z)
    {
        x = (S1)o;
        y = 2;
        z = 3;
    }
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "FalseFalseTrue FalseFalseTrue FalseFalseTrue" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_05_Constant()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(0)));
        System.Console.Write(Test2(new S1(11)));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(11)));
        System.Console.Write(Test3(default));
        System.Console.Write(Test3(new S1(""11"")));

        System.Console.Write(' ');
        System.Console.Write(Test4(new S1(11)));
        System.Console.Write(Test4(default));
        System.Console.Write(Test4(new S1(""11"")));
    }

    static bool Test1(S1 u)
    {
        return u is 10;
    }   

    static bool Test2(S1 u)
    {
        return u is 10 or 11;
    }   

    static bool Test3(S1 u)
    {
        return u is ""11"" and ['1', '1'];
    }   

    static bool Test4(S1 u)
    {
        return u is null;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse TrueFalseFalseFalseTrue FalseFalseTrue FalseTrueFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  constrained. ""S1""
  IL_0008:  callvirt   ""object System.Runtime.CompilerServices.IUnion.Value.get""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  isinst     ""int""
  IL_0014:  brfalse.s  IL_0021
  IL_0016:  ldloc.0
  IL_0017:  unbox.any  ""int""
  IL_001c:  ldc.i4.s   10
  IL_001e:  ceq
  IL_0020:  ret
  IL_0021:  ldc.i4.0
  IL_0022:  ret
}
");
        }

        [Fact]
        public void UnionMatching_06_Constant()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public C1() {}
    public C1(int x) { _value = x; }
    public C1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(11)));
        System.Console.Write(Test1(new S1()));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(null));

        System.Console.Write(' ');
        System.Console.Write(Test2(new C1(11)));
        System.Console.Write(Test2(new C1()));
        System.Console.Write(Test2(new C1(""11"")));
        System.Console.Write(Test2(null));
    }

    static bool Test1(S1? u)
    {
        return u is null;
    }   

    static bool Test2(C1 u)
    {
        return u is null;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "FalseFalseFalseTrue FalseTrueFalseFalse").VerifyDiagnostics();

            // PROTOTYPE: Note the difference in behavior between S1? and C1.
            // For S1?, 'is null' is true only when S1? itself is null value. 
            // For C1, 'is null' is true when the C1?.Value is null, it is false even for the case when C1 itself is a null reference.
            // This behavior could be very confusing.

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000d
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""object System.Runtime.CompilerServices.IUnion.Value.get""
  IL_0009:  ldnull
  IL_000a:  ceq
  IL_000c:  ret
  IL_000d:  ldc.i4.0
  IL_000e:  ret
}
");
        }

        [Fact]
        public void UnionMatching_07_Constant()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test4(new S1(11)));
        System.Console.Write(Test4(default));
        System.Console.Write(Test4(new S1(""11"")));
    }

    const int _int_10 = 10;
    const object _object_null = null;

    static bool Test1(S1 u)
    {
        return u is _int_10;
    }   

    static bool Test4(S1 u)
    {
        return u is _object_null;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseFalse FalseTrueFalse").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_08_Recursive_Property()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(S2<int> x) { _value = x; }
    public S1(S2<string> x) { _value = x; }
    public S1(S2<object> x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2<T>
{
    public T Value;
}

class A;
class B;

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(new S2<int>() { Value = 10 })));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(new S2<string>() { Value = ""11"" })));
        System.Console.Write(Test1(new S1(new S2<int>() { Value = 0 })));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(new S2<int>() { Value = 10 })));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(new S2<string>() { Value = ""11"" })));
        System.Console.Write(Test2(new S1(new S2<int>() { Value = 0 })));
        System.Console.Write(Test2(new S1(new S2<int>() { Value = 11 })));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(new S2<int>() { Value = 11 })));
        System.Console.Write(Test3(default));
        System.Console.Write(Test3(new S1(new S2<string>() { Value = ""11"" })));
    }

    static bool Test1(S1 u)
    {
        return u is S2<int> { Value: 10 };
    }   

    static bool Test2(S1 u)
    {
        return u is S2<int> { Value: 10 or 11 };
    }   

    static bool Test3(S1 u)
    {
        return u is S2<string> { Value: ""11"" } and { Value: ['1', '1'] };
    }   

    static bool Test4(S1 u)
    {
        return u is S2<object> { Value: not A or B };
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse TrueFalseFalseFalseTrue FalseFalseTrue" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics(
                // (58,50): warning CS9336: The pattern is redundant.
                //         return u is S2<object> { Value: not A or B };
                Diagnostic(ErrorCode.WRN_RedundantPattern, "B").WithLocation(58, 50)
                );
        }

        [Fact]
        public void UnionMatching_09_Recursive_ITuple()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(C x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(new C())));
    }

    static bool Test1(S1 u)
    {
        return u is (_, 10);
    }   
}

public class C : System.Runtime.CompilerServices.ITuple
{
    int System.Runtime.CompilerServices.ITuple.Length => 2;
    object System.Runtime.CompilerServices.ITuple.this[int i] => i * 10;
}

";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "FalseFalseTrue" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_10_Recursive_Deconstruct()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(S2<int> x) { _value = x; }
    public S1(S2<string> x) { _value = x; }
    public S1(S2<object> x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2<T>
{
    public T Value;

    public void Deconstruct(out T value, out int x)
    {
        value = Value;
        x = 0;
    }
}

class A;
class B;

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(new S2<int>() { Value = 10 })));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(new S2<string>() { Value = ""11"" })));
        System.Console.Write(Test1(new S1(new S2<int>() { Value = 0 })));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(new S2<int>() { Value = 10 })));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(new S2<string>() { Value = ""11"" })));
        System.Console.Write(Test2(new S1(new S2<int>() { Value = 0 })));
        System.Console.Write(Test2(new S1(new S2<int>() { Value = 11 })));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(new S2<int>() { Value = 11 })));
        System.Console.Write(Test3(default));
        System.Console.Write(Test3(new S1(new S2<string>() { Value = ""11"" })));
    }

    static bool Test1(S1 u)
    {
        return u is S2<int> (10, _);
    }   

    static bool Test2(S1 u)
    {
        return u is S2<int> (10 or 11, _);
    }   

    static bool Test3(S1 u)
    {
        return u is S2<string> (""11"", _) and (['1', '1'], _);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse TrueFalseFalseFalseTrue FalseFalseTrue" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_11_Type()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));
    }

    static bool Test1(S1 u)
    {
        return u is int;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueFalseFalseTrue").VerifyDiagnostics(
                );

            var tree = comp.SyntaxTrees[0];

            Assert.Equal("u is int", tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single().ToString());

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  constrained. ""S1""
  IL_0008:  callvirt   ""object System.Runtime.CompilerServices.IUnion.Value.get""
  IL_000d:  isinst     ""int""
  IL_0012:  ldnull
  IL_0013:  cgt.un
  IL_0015:  ret
}
");
        }

        [Fact]
        public void UnionMatching_12_Type()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(11)));
        System.Console.Write(Test3(default));
        System.Console.Write(Test3(new S1(""11"")));
    }

    static bool Test1(S1 u)
    {
        return u switch { int => true, _ => false };
    }   

    static bool Test3(S1 u)
    {
        return u is string and ['1', '1'];
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseTrue FalseFalseTrue" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_13_Declaration()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(0)));
        System.Console.Write(Test2(new S1(11)));
    }

    static bool Test1(S1 u)
    {
        return u is int x;
    }   

    static bool Test2(S1 u)
    {
        return u is int x ? (x == 10 || x == 11) : false;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseTrue TrueFalseFalseFalseTrue").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_14_Negated()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(0)));
        System.Console.Write(Test2(new S1(11)));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(11)));
        System.Console.Write(Test3(default));
        System.Console.Write(Test3(new S1(""11"")));

        System.Console.Write(' ');
        System.Console.Write(Test4(new S1(11)));
        System.Console.Write(Test4(default));
        System.Console.Write(Test4(new S1(""11"")));

        System.Console.Write(' ');
        System.Console.Write(Test5(new S1(10)));
        System.Console.Write(Test5(default));
        System.Console.Write(Test5(new S1(""11"")));
        System.Console.Write(Test5(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test6(new S1(10)));
        System.Console.Write(Test6(new S1()));
        System.Console.Write(Test6(null));
        System.Console.Write(Test6(new S1(""11"")));
        System.Console.Write(Test6(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test7(new S1(10)));
        System.Console.Write(Test7(new S1()));
        System.Console.Write(Test7(null));
        System.Console.Write(Test7(new S1(""11"")));
        System.Console.Write(Test7(new S1(0)));
    }
 
    static bool Test1(S1 u)
    {
        return u is not 10;
    }   

    static bool Test2(S1 u)
    {
        return u is not (10 or 11);
    }   

    static bool Test3(S1 u)
    {
        return u is not (""11"" and ['1', '1']);
    }   

    static bool Test4(S1 u)
    {
        return u is not null;
    }   
 
    static bool Test5(S1 u)
    {
        return u is not ({ } and int);
    }   
 
    static bool Test6(S1? u)
    {
        return u is not ({ } and int);
    }   
 
    static bool Test7(S1? u)
    {
        return u is not (S1 and int);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "FalseTrueTrueTrue FalseTrueTrueTrueFalse TrueTrueFalse TrueFalseTrue FalseTrueTrueFalse FalseTrueTrueTrueFalse FalseTrueTrueTrueFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_15_Negated()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test5(new S1(11)));
        System.Console.Write(Test5(default));
        System.Console.Write(Test5(new S1(""11"")));

        System.Console.Write(' ');
        System.Console.Write(Test6(new S1(11)));
        System.Console.Write(Test6(default));
        System.Console.Write(Test6(new S1(""11"")));
    }
 
    static int Test5(S1 u)
    {
        if (u is not int x)
        {
            return -1;
        }

        return x;
    }   
 
    static int Test6(S1 u)
    {
        if (u is not not not int x)
        {
            return -1;
        }

        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "11-1-1 11-1-1").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_16_Negated()
        {
            var src = @"
sealed class C1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public C1(int x) { _value = x; }
    public C1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test5(C1 u)
    {
        if (u is not int x)
        {
            return -1;
        }

        return x;
    }   

    static int Test6(S1 u)
    {
        if (u is not not int y)
        {
            return y - 1;
        }

        return y;
    }   

    static int Test7(S1? u)
    {
        if (u is not int z)
        {
            return -1;
        }

        return z;
    }   
 
    static bool Test8(S1 u)
    {
        return u is not (S1 and int);
    }   
}

struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            // There is an implicit null check for class union types.  
            comp.VerifyDiagnostics(
                // (14,26): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (u is not int x)
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "x").WithLocation(14, 26),
                // (19,16): error CS0165: Use of unassigned local variable 'x'
                //         return x;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(19, 16),
                // (29,16): error CS0165: Use of unassigned local variable 'y'
                //         return y;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(29, 16),
                // (34,22): error CS8121: An expression of type 'S1?' cannot be handled by a pattern of type 'int'.
                //         if (u is not int z)
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1?", "int").WithLocation(34, 22),

                // PROTOTYPE: The diagnostics is somewhat confusing in this case.
                //            A type cannot be handled by the pattern of the same type.
                //            Syntactially it is not obvious that we are doing union matching.

                // (44,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'S1'.
                //         return u is not (S1 and int);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "S1").WithArguments("S1", "S1").WithLocation(44, 26)
                );
        }

        [Fact]
        public void UnionMatching_17_Negated()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public C1() {}
    public C1(int x) { _value = x; }
    public C1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(11)));
        System.Console.Write(Test1(new S1()));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(null));

        System.Console.Write(' ');
        System.Console.Write(Test2(new C1(11)));
        System.Console.Write(Test2(new C1()));
        System.Console.Write(Test2(new C1(""11"")));
        System.Console.Write(Test2(null));
    }

    static bool Test1(S1? u)
    {
        return u is not null;
    }   

    static bool Test2(C1 u)
    {
        return u is not null;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueTrueTrueFalse TrueFalseTrueFalse").VerifyDiagnostics();

            // PROTOTYPE: Note the difference in behavior between S1? and C1.
            // For S1?, 'is not null' is false only when S1? itself is null value. 
            // For C1, 'is not null' is false when the C1?.Value is null (i.e. either the instance or it's Value in null).
            // This behavior could be very confusing.
        }

        [Fact]
        public void UnionMatching_17_BinaryOr()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(0)));
        System.Console.Write(Test2(new S1(11)));
        System.Console.Write(Test2(new S1(""111"")));
    }

    static bool Test2(S1 u)
    {
        return u is 10 or ""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseTrueFalseFalseFalse").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_18_BinaryAnd()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(null));
        System.Console.Write(Test2(new S1()));
        System.Console.Write(Test2(new S1(""11"")));
    }

    static string Test2(object u)
    {
        if (u is S1 and int x)
        {
            return x.ToString();
        }

        return ""_"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "10___").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_19_BinaryAnd()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
    }

    static string Test2(S1 u)
    {
        if (u is 10 and var x)
        {
            return x.GetType().ToString();
        }

        return ""_"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "System.Int32__").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_20_BinaryAnd()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(S2 x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S2(int x) { _value = x; }
    public S2(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1(new S2(10))));
        System.Console.Write(Test2(new S1(new S2(11))));
        System.Console.Write(Test2(null));
        System.Console.Write(Test2(new S1()));
        System.Console.Write(Test2(new S1(new S2())));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(new S2(""11""))));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(new S2(10))));
        System.Console.Write(Test3(new S1(new S2(11))));
        System.Console.Write(Test3(null));
        System.Console.Write(Test3(new S1()));
        System.Console.Write(Test3(new S1(new S2())));
        System.Console.Write(Test3(new S1(""11"")));
        System.Console.Write(Test3(new S1(new S2(""11""))));
    }

    static bool Test2(object u)
    {
        return u is S1 and S2 and 10;
    }   

    static bool Test3(object u)
    {
        return u is S1 and (S2 and 10);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseFalseFalseFalseFalse TrueFalseFalseFalseFalseFalseFalse").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_21_BinaryAnd()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(S2 x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S2(S3 x) { _value = x; }
    public S2(int x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S3 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S3(int x) { _value = x; }
    public S3(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1(new S2(new S3(10)))));
        System.Console.Write(Test2(new S1(new S2(new S3(11)))));
        System.Console.Write(Test2(null));
        System.Console.Write(Test2(new S1()));
        System.Console.Write(Test2(new S1(new S2())));
        System.Console.Write(Test2(new S1(new S2(new S3()))));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(new S2(10))));
        System.Console.Write(Test2(new S1(new S2(11))));
        System.Console.Write(Test2(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test3(new S1(new S2(new S3(10)))));
        System.Console.Write(Test3(new S1(new S2(new S3(11)))));
        System.Console.Write(Test3(null));
        System.Console.Write(Test3(new S1()));
        System.Console.Write(Test3(new S1(new S2())));
        System.Console.Write(Test3(new S1(new S2(new S3()))));
        System.Console.Write(Test3(new S1(""11"")));
        System.Console.Write(Test3(new S1(new S2(10))));
        System.Console.Write(Test3(new S1(new S2(11))));
        System.Console.Write(Test3(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test4(new S1(new S2(new S3(10)))));
        System.Console.Write(Test4(new S1(new S2(new S3(11)))));
        System.Console.Write(Test4(null));
        System.Console.Write(Test4(new S1()));
        System.Console.Write(Test4(new S1(new S2())));
        System.Console.Write(Test4(new S1(new S2(new S3()))));
        System.Console.Write(Test4(new S1(""11"")));
        System.Console.Write(Test4(new S1(new S2(10))));
        System.Console.Write(Test4(new S1(new S2(11))));
        System.Console.Write(Test4(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test5(new S1(new S2(new S3(10)))));
        System.Console.Write(Test5(new S1(new S2(new S3(11)))));
        System.Console.Write(Test5(null));
        System.Console.Write(Test5(new S1()));
        System.Console.Write(Test5(new S1(new S2())));
        System.Console.Write(Test5(new S1(new S2(new S3()))));
        System.Console.Write(Test5(new S1(""11"")));
        System.Console.Write(Test5(new S1(new S2(10))));
        System.Console.Write(Test5(new S1(new S2(11))));
        System.Console.Write(Test5(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test6(new S1(new S2(new S3(10)))));
        System.Console.Write(Test6(new S1(new S2(new S3(11)))));
        System.Console.Write(Test6(null));
        System.Console.Write(Test6(new S1()));
        System.Console.Write(Test6(new S1(new S2())));
        System.Console.Write(Test6(new S1(new S2(new S3()))));
        System.Console.Write(Test6(new S1(""11"")));
        System.Console.Write(Test6(new S1(new S2(10))));
        System.Console.Write(Test6(new S1(new S2(11))));
        System.Console.Write(Test6(new S1(new S2(new S3(""11"")))));
    }

    static bool Test2(object u)
    {
        return u is ((S1 and S2) and S3) and 10;
    }   

    static bool Test3(object u)
    {
        return u is (S1 and S2) and (S3 and 10);
    }   

    static bool Test4(object u)
    {
        return u is (S1 and (S2 and S3)) and 10;
    }   

    static bool Test5(object u)
    {
        return u is S1 and (S2 and S3 and 10);
    }   

    static bool Test6(object u)
    {
        return u is S1 and (S2 and (S3 and 10));
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_22_BinaryAnd()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(S2 x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S2(S3 x) { _value = x; }
    public S2(int x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S3 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S3(int x) { _value = x; }
    public S3(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1(new S2(new S3(10)))));
        System.Console.Write(Test2(new S1(new S2(new S3(11)))));
        System.Console.Write(Test2(null));
        System.Console.Write(Test2(new S1()));
        System.Console.Write(Test2(new S1(new S2())));
        System.Console.Write(Test2(new S1(new S2(new S3()))));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(new S2(10))));
        System.Console.Write(Test2(new S1(new S2(11))));
        System.Console.Write(Test2(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test3(new S1(new S2(new S3(10)))));
        System.Console.Write(Test3(new S1(new S2(new S3(11)))));
        System.Console.Write(Test3(null));
        System.Console.Write(Test3(new S1()));
        System.Console.Write(Test3(new S1(new S2())));
        System.Console.Write(Test3(new S1(new S2(new S3()))));
        System.Console.Write(Test3(new S1(""11"")));
        System.Console.Write(Test3(new S1(new S2(10))));
        System.Console.Write(Test3(new S1(new S2(11))));
        System.Console.Write(Test3(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test4(new S1(new S2(new S3(10)))));
        System.Console.Write(Test4(new S1(new S2(new S3(11)))));
        System.Console.Write(Test4(null));
        System.Console.Write(Test4(new S1()));
        System.Console.Write(Test4(new S1(new S2())));
        System.Console.Write(Test4(new S1(new S2(new S3()))));
        System.Console.Write(Test4(new S1(""11"")));
        System.Console.Write(Test4(new S1(new S2(10))));
        System.Console.Write(Test4(new S1(new S2(11))));
        System.Console.Write(Test4(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test5(new S1(new S2(new S3(10)))));
        System.Console.Write(Test5(new S1(new S2(new S3(11)))));
        System.Console.Write(Test5(null));
        System.Console.Write(Test5(new S1()));
        System.Console.Write(Test5(new S1(new S2())));
        System.Console.Write(Test5(new S1(new S2(new S3()))));
        System.Console.Write(Test5(new S1(""11"")));
        System.Console.Write(Test5(new S1(new S2(10))));
        System.Console.Write(Test5(new S1(new S2(11))));
        System.Console.Write(Test5(new S1(new S2(new S3(""11"")))));

        System.Console.WriteLine();
        System.Console.Write(Test6(new S1(new S2(new S3(10)))));
        System.Console.Write(Test6(new S1(new S2(new S3(11)))));
        System.Console.Write(Test6(null));
        System.Console.Write(Test6(new S1()));
        System.Console.Write(Test6(new S1(new S2())));
        System.Console.Write(Test6(new S1(new S2(new S3()))));
        System.Console.Write(Test6(new S1(""11"")));
        System.Console.Write(Test6(new S1(new S2(10))));
        System.Console.Write(Test6(new S1(new S2(11))));
        System.Console.Write(Test6(new S1(new S2(new S3(""11"")))));
    }

    static bool Test2(object u)
    {
        return u is ((S1 and S2) and S3) and var x and 10;
    }   

    static bool Test3(object u)
    {
        return u is (S1 and S2) and (var x and S3 and 10);
    }   

    static bool Test4(object u)
    {
        return u is (S1 and (var x and S2 and S3)) and 10;
    }   

    static bool Test5(object u)
    {
        return u is S1 and (S2 and var x and S3 and 10);
    }   

    static bool Test6(object u)
    {
        return u is S1 and (S2 and (var x and S3 and 10));
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
TrueFalseFalseFalseFalseFalseFalseFalseFalseFalse
").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_23_Parenthesized()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(0)));
        System.Console.Write(Test2(new S1(11)));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S1(11)));
        System.Console.Write(Test3(default));
        System.Console.Write(Test3(new S1(""11"")));

        System.Console.Write(' ');
        System.Console.Write(Test4(new S1(11)));
        System.Console.Write(Test4(default));
        System.Console.Write(Test4(new S1(""11"")));
    }

    static bool Test1(S1 u)
    {
        return u is (10);
    }   

    static bool Test2(S1 u)
    {
        return u is (10 or 11);
    }   

    static bool Test3(S1 u)
    {
        return u is (""11"" and ['1', '1']);
    }   

    static bool Test4(S1 u)
    {
        return u is (null);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse TrueFalseFalseFalseTrue FalseFalseTrue FalseTrueFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_24_Relational()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""11"")));
        System.Console.Write(Test2(new S1(0)));
        System.Console.Write(Test2(new S1(11)));
    }

    static bool Test1(S1 u)
    {
        return u is >=10;
    }   

    static bool Test2(S1 u)
    {
        return u is <10 or 11;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseFalse FalseFalseFalseTrueTrue").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_25_List()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int[] x) { _value = x; }
    public S1(string[] x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static bool Test1(S1 u)
    {
        return u is [10];
    }   
}

static class Extensions
{
    extension(object o)
    {
        public int Length => 0;
    }
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            // PROTOTYPE: It looks like list pattern cannot work with union types.
            comp.VerifyDiagnostics(
                // (14,21): error CS8985: List patterns may not be used for a value of type 'object'. No suitable 'Length' or 'Count' property was found.
                //         return u is [10];
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[10]").WithArguments("object").WithLocation(14, 21),
                // (14,21): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
                //         return u is [10];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[10]").WithArguments("object").WithLocation(14, 21)
                );
        }

        [Fact]
        public void UnionMatching_26_List_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2
{
    private S1 _value;
    public S2(S1 x) {_value = x;}
    public int Length => 2;
    public S1 this[int i] => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S2(new S1(10))));
        System.Console.Write(Test1(new S2(default)));
        System.Console.Write(Test1(new S2(new S1(""11""))));
        System.Console.Write(Test1(new S2(new S1(0))));
    }

    static bool Test1(S2 u)
    {
        return u is [10, _];
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_27_Slice_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2
{
    private S1 _value;
    public S2(S1 x) {_value = x;}
    public int Length => 2;
    public int this[int i] => 0;
    public S1 this[System.Range r] => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S2(new S1(10))));
        System.Console.Write(Test1(new S2(default)));
        System.Console.Write(Test1(new S2(new S1(""11""))));
        System.Console.Write(Test1(new S2(new S1(0))));
    }

    static bool Test1(S2 u)
    {
        return u is [0, ..10];
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_28_Tuple_Deconstruction_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1((new S1(10), -1)));
        System.Console.Write(Test1((default, -1)));
        System.Console.Write(Test1((new S1(""11""), -1)));
        System.Console.Write(Test1((new S1(0), -1)));
    }

    static bool Test1((S1, int) u)
    {
        return u is (10, _);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_29_ITuple_Deconstruction_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new C(new S1(10))));
        System.Console.Write(Test1(new C(default)));
        System.Console.Write(Test1(new C(new S1(11))));
        System.Console.Write(Test1(new C(new S1(""10""))));
    }

    static bool Test1(C u)
    {
        return u is (S1 and 10, _);
    }   
}

class C : System.Runtime.CompilerServices.ITuple
{
    private readonly S1 _value;
    public C(S1 x) { _value = x; }
    int System.Runtime.CompilerServices.ITuple.Length => 2;
    object System.Runtime.CompilerServices.ITuple.this[int i] => _value;
}

";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "TrueFalseFalseFalse" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_30_Deconstruction_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new C(new S1(10))));
        System.Console.Write(Test1(new C(default)));
        System.Console.Write(Test1(new C(new S1(11))));
        System.Console.Write(Test1(new C(new S1(""10""))));
    }

    static bool Test1(C u)
    {
        return u is (10, _);
    }   
}

class C
{
    private readonly S1 _value;
    public C(S1 x) { _value = x; }
    public void Deconstruct(out S1 a, out int b) { a = _value; b = -1; }
}

";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseFalse").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_31_Property_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new C(new S1(10))));
        System.Console.Write(Test1(new C(default)));
        System.Console.Write(Test1(new C(new S1(11))));
        System.Console.Write(Test1(new C(new S1(""10""))));
    }

    static bool Test1(C u)
    {
        return u is { P: 10 };
    }   
}

class C
{
    private readonly S1 _value;
    public C(S1 x) { _value = x; }
    public S1 P => _value;
}

";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseFalse").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_32_Negated_Subpattern()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(new S1()));
        System.Console.Write(Test1(new S1(11)));
        System.Console.Write(Test1(new S1(""10"")));
        System.Console.Write(Test1(null));
    }

    static bool Test1(object u)
    {
        return u is not (S1 and 10);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "FalseTrueTrueTrueTrue").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_33_SwitchLabel()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S1(10)));
        System.Console.Write(Test1(default));
        System.Console.Write(Test1(new S1(""11"")));
        System.Console.Write(Test1(new S1(0)));

        System.Console.Write(' ');
        System.Console.Write(Test2(new S1(10)));
        System.Console.Write(Test2(default));
        System.Console.Write(Test2(new S1(""10"")));
        System.Console.Write(Test2(new S1(0)));
    }

    static bool Test1(S1 u)
    {
        switch (u)
        {
            case int: return true;
            default: return false;
        }
    }   

    static bool Test2(S1 u)
    {
        switch (u)
        {
            case 10: return true;
            default: return false;
        }
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseTrue TrueFalseFalseFalse").VerifyDiagnostics();
        }

        [Fact]
        public void UnionMatching_34_BinaryAnd()
        {
            var src = @"
struct S0 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S0(S1 x) { _value = x; }
    public S0(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(S2 x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S2(int x) { _value = x; }
    public S2(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S0(new S1(new S2(10)))));
        System.Console.Write(Test2(new S0(new S1(new S2(11)))));
        System.Console.Write(Test2(new S0(null)));
        System.Console.Write(Test2(new S0(new S1())));
        System.Console.Write(Test2(new S0(new S1(new S2()))));
        System.Console.Write(Test2(new S0(new S1(""11""))));
        System.Console.Write(Test2(new S0(new S1(new S2(""11"")))));

        System.Console.Write(' ');
        System.Console.Write(Test3(new S0(new S1(new S2(10)))));
        System.Console.Write(Test3(new S0(new S1(new S2(11)))));
        System.Console.Write(Test3(new S0(null)));
        System.Console.Write(Test3(new S0(new S1())));
        System.Console.Write(Test3(new S0(new S1(new S2()))));
        System.Console.Write(Test3(new S0(new S1(""11""))));
        System.Console.Write(Test3(new S0(new S1(new S2(""11"")))));
    }

    static bool Test2(S0 u)
    {
        return u is S1 and S2 and 10;
    }   

    static bool Test3(S0 u)
    {
        return u is S1 and (S2 and 10);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetLatest, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueFalseFalseFalseFalseFalseFalse TrueFalseFalseFalseFalseFalseFalse").VerifyDiagnostics();
        }

        [Fact]
        public void PatternWrongType_TypePattern_01_BindConstantPatternWithFallbackToTypePattern_UnionType_Out_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is C1 and C2;
        _ = u is C1 and C3;
        _ = u is C1 and C4;
        _ = u switch { C4 => 1, _ => 0 };
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,25): hidden CS9335: The pattern is redundant.
                //         _ = u is C1 and C2;
                Diagnostic(ErrorCode.HDN_RedundantPattern, "C2").WithLocation(100, 25),
                // (101,25): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(101, 25),
                // (102,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(102, 25),
                // (103,24): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u switch { C4 => 1, _ => 0 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(103, 24)
                );
        }

        [Fact]
        public void PatternWrongType_TypePattern_02_BindTypePattern_UnionType_Out_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test4(S1 u)
    {
#line 400
        _ = u is System.IComparable and string;
        _ = u is string and int;
        _ = u is object and byte;
        _ = u switch { byte => 1, _ => 0 };
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (401,29): error CS8121: An expression of type 'string' cannot be handled by a pattern of type 'int'.
                //         _ = u is string and int;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("string", "int").WithLocation(401, 29),
                // (402,29): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'byte'.
                //         _ = u is object and byte;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "byte").WithArguments("S1", "byte").WithLocation(402, 29),
                // (403,24): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'byte'.
                //         _ = u switch { byte => 1, _ => 0 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "byte").WithArguments("S1", "byte").WithLocation(403, 24)
                );
        }

        [Fact]
        public void PatternWrongType_TypePattern_03()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test4(S1 u)
    {
#line 400
        switch (u)
        {
            case string:
                break;  
            case byte:
                break;  
        }
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (404,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'byte'.
                //             case byte:
                Diagnostic(ErrorCode.ERR_PatternWrongType, "byte").WithArguments("S1", "byte").WithLocation(404, 18)
                );
        }

        [Fact]
        public void PatternWrongType_TypePattern_04_BindIsOperator()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}
";
            var src2 = @"
class Program
{
    static void Test4(S1 u)
    {
        _ = u is System.IComparable;
        _ = u is int;
        _ = u is string;
        _ = u is object;
        _ = u is long;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (10,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'long'.
                //         _ = u is long;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "long").WithArguments("S1", "long").WithLocation(10, 18)
                );
        }

        [Fact]
        public void PatternWrongType_RecursivePattern_01_BindRecursivePattern_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test2(S1 u)
    {
#line 200
        _ = u is C1 and C2 {};
        _ = u is C1 and C3 {};
        _ = u is C1 and C4 {};
        _ = u is C4 {};
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (200,25): hidden CS9335: The pattern is redundant.
                //         _ = u is C1 and C2 {};
                Diagnostic(ErrorCode.HDN_RedundantPattern, "C2 {}").WithLocation(200, 25),
                // (201,25): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 and C3 {};
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(201, 25),
                // (202,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 and C4 {};
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(202, 25),
                // (203,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C4 {};
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(203, 18)
                );
        }

        [Fact]
        public void PatternWrongType_RecursivePattern_02_BindRecursivePattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test10(S1 u)
    {
#line 1000
        _ = u is C1 {} and C2;
        _ = u is C1 {} and C3;
        _ = u is C1 {} and C4;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (1000,28): hidden CS9335: The pattern is redundant.
                //         _ = u is C1 {} and C2;
                Diagnostic(ErrorCode.HDN_RedundantPattern, "C2").WithLocation(1000, 28),
                // (1001,28): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 {} and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(1001, 28),
                // (1002,28): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 {} and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(1002, 28)
                );
        }

        [Fact]
        public void PatternWrongType_DeclarationPattern_01_BindDeclarationPattern_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test3(S1 u)
    {
#line 300
        _ = u is C1 and C2 a;
        _ = u is C1 and C3 b;
        _ = u is C1 and C4 c;
        _ = u is C4 d;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (301,25): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 and C3 b;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(301, 25),
                // (302,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 and C4 c;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(302, 25),
                // (303,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C4 d;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(303, 18)
                );
        }

        [Fact]
        public void PatternWrongType_DeclarationPattern_02_BindDeclarationPattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test9(S1 u)
    {
#line 900
        _ = u is C1 a and C2;
        _ = u is C1 b and C3;
        _ = u is C1 c and C4;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (900,27): hidden CS9335: The pattern is redundant.
                //         _ = u is C1 a and C2;
                Diagnostic(ErrorCode.HDN_RedundantPattern, "C2").WithLocation(900, 27),
                // (901,27): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 b and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(901, 27),
                // (902,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 c and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(902, 27)
                );
        }

        [Fact]
        public void PatternWrongType_NegatedPattern_01_BindUnaryPattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test5(S1 u)
    {
#line 500
        _ = u is not C5 and C2;
        _ = u is not C5 and C4;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (501,29): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is not C5 and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(501, 29)
                );
        }

        [Fact]
        public void PatternWrongType_NegatedPattern_02_BindUnaryPattern_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test7(S1 u)
    {
#line 700
        _ = u is C1 and not C5;
        _ = u is C1 and not C3;
        _ = u is C1 and not C4;
        _ = u is not C4;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (701,29): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 and not C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(701, 29),
                // (702,29): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 and not C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(702, 29),
                // (703,22): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is not C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(703, 22)
                );
        }

        [Fact]
        public void PatternWrongType_ParenthesizedPattern_01_BindParenthesizedPattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test6(S1 u)
    {
#line 600
        _ = u is (not C5) and C2;
        _ = u is (not C5) and C4;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (601,31): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is (not C5) and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(601, 31)
                );
        }

        [Fact]
        public void PatternWrongType_ParenthesizedPattern_01_BindParenthesizedPattern_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test8(S1 u)
    {
#line 800
        _ = u is C1 and (not C2);
        _ = u is C1 and (not C3);
        _ = u is C1 and (not C4);
        _ = u is (not C4);
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (801,30): error CS8121: An expression of type 'C1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 and (not C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("C1", "C3").WithLocation(801, 30),
                // (802,30): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is C1 and (not C4);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(802, 30),
                // (803,23): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is (not C4);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(803, 23)
                );
        }

        [Fact]
        public void PatternWrongType_ListPattern_01_BindListPattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test11(S1 u)
    {
#line 1100
        _ = u is [] and C2;
        _ = u is [] and C4;
        _ = u is string and ['a'];
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (1100,18): error CS8985: List patterns may not be used for a value of type 'object'. No suitable 'Length' or 'Count' property was found.
                //         _ = u is [] and C2;
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("object").WithLocation(1100, 18),
                // (1100,18): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
                //         _ = u is [] and C2;
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("object").WithLocation(1100, 18),
                // (1101,18): error CS8985: List patterns may not be used for a value of type 'object'. No suitable 'Length' or 'Count' property was found.
                //         _ = u is [] and C4;
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[]").WithArguments("object").WithLocation(1101, 18),
                // (1101,18): error CS0021: Cannot apply indexing with [] to an expression of type 'object'
                //         _ = u is [] and C4;
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[]").WithArguments("object").WithLocation(1101, 18),
                // (1101,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is [] and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(1101, 25)
                );
        }

        [Fact]
        public void PatternWrongType_VarDeconstructionPattern_01_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(string x) { _value = x; }
    public S1(C2 x) { _value = x; }
    public S1(C3 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;
class C3;
class C4 : C1;
class C5 : C2;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is var (a, b) and C2;
        _ = u is var (c, d) and C4;
    } 
}

static class Extensions
{
    public static void Deconstruct(this object o, out int x, out int y)
    {
        x = 1;
        y = 2;
    }
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (101,33): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C4'.
                //         _ = u is var (c, d) and C4;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C4").WithArguments("S1", "C4").WithLocation(101, 33)
                );
        }

        [Fact]
        public void PatternWrongType_ConstantPattern_01_BindConstantPatternWithFallbackToTypePattern_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(C1 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1
{
    public static implicit operator C1(string c) => null;
}

class C2;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is {} and ""1"";
        _ = u is C1 and (C2)null;
        _ = u is C1 and ""1"";
        _ = u is System.IComparable and ""1"";
        _ = u is ""1"";
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and "1";
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""1""").WithArguments("S1", "string").WithLocation(100, 25),
                // (101,25): error CS0029: Cannot implicitly convert type 'C2' to 'C1'
                //         _ = u is C1 and (C2)null;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(C2)null").WithArguments("C2", "C1").WithLocation(101, 25),
                // (102,25): error CS9135: A constant value of type 'C1' is expected
                //         _ = u is C1 and "1";
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, @"""1""").WithArguments("C1").WithLocation(102, 25),
                // (103,41): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is System.IComparable and "1";
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""1""").WithArguments("S1", "string").WithLocation(103, 41),
                // (104,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is "1";
                Diagnostic(ErrorCode.ERR_PatternWrongType, @"""1""").WithArguments("S1", "string").WithLocation(104, 18)
                );
        }

        [Fact]
        public void PatternWrongType_ConstantPattern_02_BindConstantPatternWithFallbackToTypePattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(byte x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C2;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is null and C2;
        _ = u is 1 and byte;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C2'.
                //         _ = u is null and C2;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C2").WithArguments("S1", "C2").WithLocation(100, 27),
                // (101,24): error CS8121: An expression of type 'int' cannot be handled by a pattern of type 'byte'.
                //         _ = u is 1 and byte;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "byte").WithArguments("int", "byte").WithLocation(101, 24)
                );
        }

        [Fact]
        public void PatternWrongType_ConstantPattern_03_BindIsOperator_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(byte x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
        const string empty ="""";
#line 100
        _ = u is empty;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is empty;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "empty").WithArguments("S1", "string").WithLocation(100, 18)
                );
        }

        [Fact]
        public void PatternWrongType_ConstantPattern_04_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(byte x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
        const string empty ="""";
#line 100
        switch (u)
        {
            case 1:
                goto case empty;
        }   
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (102,13): error CS8070: Control cannot fall out of switch from final case label ('case 1:')
                //             case 1:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 1:").WithArguments("case 1:").WithLocation(102, 13),

                // PROTOTYPE: This doesn't look like a union matching error. Something is likely missing in implementation.

                // (103,17): error CS0029: Cannot implicitly convert type 'string' to 'S1'
                //                 goto case empty;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "goto case empty;").WithArguments("string", "S1").WithLocation(103, 17)
                );
        }

        [Fact]
        public void PatternWrongType_RelationalPattern_01_BindRelationalPattern_UnionType_In()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(string x) { _value = x; }
    public S1(C1 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1
{
    public static implicit operator C1(int c) => null;
}

class C2;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is {} and > 1;
        _ = u is C1 and > 1;
        _ = u is System.IComparable and > 1;
        _ = u is > 1;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and > 1;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "1").WithArguments("S1", "int").WithLocation(100, 27),
                // (101,27): error CS9135: A constant value of type 'C1' is expected
                //         _ = u is C1 and > 1;
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "1").WithArguments("C1").WithLocation(101, 27),
                // (102,43): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is System.IComparable and > 1;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "1").WithArguments("S1", "int").WithLocation(102, 43),
                // (103,20): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is > 1;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "1").WithArguments("S1", "int").WithLocation(103, 20)
                );
        }

        [Fact]
        public void PatternWrongType_RelationalPattern_02_BindRelationalPattern_UnionType_Out()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(byte x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C2;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is > 1 and byte;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,26): error CS8121: An expression of type 'int' cannot be handled by a pattern of type 'byte'.
                //         _ = u is > 1 and byte;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "byte").WithArguments("int", "byte").WithLocation(100, 26)
                );
        }

        [Fact]
        public void PatternWrongType_BinaryPattern_01_Disjunction_Snap_To_Previous_UnionType()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(C1 x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2;
class C3;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is int or string or C3;
        _ = u is int or (string or C3);
        _ = u is C1 or string or C3;
        _ = u is int or C2 or C3;
        _ = u is int or string or C1;
        _ = u is int or (C2 or C3);
        _ = u is int or (string or C1);
        _ = u is (int or string) or C3;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is int or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(100, 18),
                // (100,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is int or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(100, 25),
                // (100,35): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is int or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(100, 35),
                // (101,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is int or (string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(101, 18),
                // (101,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is int or (string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(101, 26),
                // (101,36): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is int or (string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(101, 36),
                // (102,24): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is C1 or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(102, 24),
                // (102,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is C1 or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(102, 34),
                // (103,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is int or C2 or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(103, 18),
                // (103,31): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is int or C2 or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(103, 31),
                // (104,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is int or string or C1;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(104, 18),
                // (104,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is int or string or C1;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(104, 25),
                // (105,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is int or (C2 or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(105, 18),
                // (105,32): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is int or (C2 or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(105, 32),
                // (106,18): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is int or (string or C1);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(106, 18),
                // (106,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is int or (string or C1);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(106, 26),
                // (107,19): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is (int or string) or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(107, 19),
                // (107,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is (int or string) or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(107, 26),
                // (107,37): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is (int or string) or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(107, 37)
                );
        }

        [Fact]
        public void PatternWrongType_BinaryPattern_02_Disjunction_Snap_To_Previous_UnionType()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(C1 x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2;
class C3;
";
            var src2 = @"
class Program
{
    static void Test1(S1 u)
    {
#line 100
        _ = u is {} and (int or string or C3);
        _ = u is {} and (int or (string or C3));
        _ = u is {} and (C1 or string or C3);
        _ = u is {} and (int or C2 or C3);
        _ = u is {} and (int or string or C1);
        _ = u is {} and (int or (C2 or C3));
        _ = u is {} and (int or (string or C1));
        _ = u is {} and ((int or string) or C3);
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(100, 26),
                // (100,33): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (int or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(100, 33),
                // (100,43): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (int or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(100, 43),
                // (101,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or (string or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(101, 26),
                // (101,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (int or (string or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(101, 34),
                // (101,44): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (int or (string or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(101, 44),
                // (102,32): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (C1 or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(102, 32),
                // (102,42): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (C1 or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(102, 42),
                // (103,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or C2 or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(103, 26),
                // (103,39): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (int or C2 or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(103, 39),
                // (104,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or string or C1);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(104, 26),
                // (104,33): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (int or string or C1);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(104, 33),
                // (105,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or (C2 or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(105, 26),
                // (105,40): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (int or (C2 or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(105, 40),
                // (106,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or (string or C1));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(106, 26),
                // (106,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (int or (string or C1));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(106, 34),
                // (107,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and ((int or string) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(107, 27),
                // (107,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and ((int or string) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(107, 34),
                // (107,45): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and ((int or string) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(107, 45)
                );
        }

        [Fact]
        public void PatternWrongType_BinaryPattern_03_Disjunction_Snap_To_Previous_UnionType()
        {
            var src1 = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(C1 x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2;
class C3;
";
            var src2 = @"
class Program
{
    static void Test1(object u)
    {
#line 100
        _ = u is (S1 and int) or string or C3;
        _ = u is (S1 and int) or (string or C3);
        _ = u is int or (S1 and C2) or C3;
        _ = u is int or ((S1 and C2) or C3);
        _ = u is ((S1 and int) or string) or C3;
        _ = u is S1 and int or string or C3;
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is (S1 and int) or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(100, 26),
                // (101,26): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is (S1 and int) or (string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(101, 26),
                // (104,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is ((S1 and int) or string) or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(104, 27),
                // (105,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is S1 and int or string or C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(105, 25)
                );
        }

        [Fact]
        public void PatternWrongType_BinaryPattern_04_Disjunction_Snap_To_Previous_UnionType()
        {
            var src1 = @"
struct S0 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S0(byte x) { _value = x; }
    public S0(S1 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(C1 x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2;
class C3;
";
            var src2 = @"
class Program
{
    static void Test1(S0 u)
    {
#line 100
        _ = u is {} and ((S1 and int) or string or C3);
        _ = u is {} and ((S1 and int) or (string or C3));
        _ = u is {} and (int or (S1 and C2) or C3);
        _ = u is {} and (int or ((S1 and C2) or C3));
        _ = u is {} and (((S1 and int) or string) or C3);
        _ = u is {} and (S1 and int or string or C3);
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and ((S1 and int) or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(100, 34),
                // (100,42): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and ((S1 and int) or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S0", "string").WithLocation(100, 42),
                // (100,52): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and ((S1 and int) or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S0", "C3").WithLocation(100, 52),
                // (101,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and ((S1 and int) or (string or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(101, 34),
                // (101,43): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and ((S1 and int) or (string or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S0", "string").WithLocation(101, 43),
                // (101,53): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and ((S1 and int) or (string or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S0", "C3").WithLocation(101, 53),
                // (102,26): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or (S1 and C2) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S0", "int").WithLocation(102, 26),
                // (102,48): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (int or (S1 and C2) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S0", "C3").WithLocation(102, 48),
                // (103,26): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (int or ((S1 and C2) or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S0", "int").WithLocation(103, 26),
                // (103,49): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (int or ((S1 and C2) or C3));
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S0", "C3").WithLocation(103, 49),
                // (104,35): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (((S1 and int) or string) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(104, 35),
                // (104,43): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (((S1 and int) or string) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S0", "string").WithLocation(104, 43),
                // (104,54): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (((S1 and int) or string) or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S0", "C3").WithLocation(104, 54),
                // (105,33): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         _ = u is {} and (S1 and int or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(105, 33),
                // (105,40): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'string'.
                //         _ = u is {} and (S1 and int or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S0", "string").WithLocation(105, 40),
                // (105,50): error CS8121: An expression of type 'S0' cannot be handled by a pattern of type 'C3'.
                //         _ = u is {} and (S1 and int or string or C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S0", "C3").WithLocation(105, 50)
                );
        }

        [Fact]
        public void PatternWrongType_BinaryPattern_05_Conjunction_Pass_UnionType_Through()
        {
            var src1 = @"
struct S0 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S0(byte x) { _value = x; }
    public S0(S1 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(C1 x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2;
class C3;
";
            var src2 = @"
class Program
{
    static void Test1(object u)
    {
#line 100
        _ = u is S1 and string;
        _ = u is (S1 and object) and C3;
        _ = u is S1 and object and C3;
    } 

    static void Test2(object u)
    {
#line 200
        _ = u is S0 and S1 and C3;
        _ = u is (S0 and S1) and C3;
        _ = u is S0 and (S1 and C3);
    } 
}
";
            var comp = CreateCompilation([src2, src1, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (100,25): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'string'.
                //         _ = u is S1 and string;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "string").WithArguments("S1", "string").WithLocation(100, 25),
                // (101,38): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is (S1 and object) and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(101, 38),
                // (102,36): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is S1 and object and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(102, 36),
                // (200,32): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is S0 and S1 and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(200, 32),
                // (201,34): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is (S0 and S1) and C3;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(201, 34),
                // (202,33): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         _ = u is S0 and (S1 and C3);
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(202, 33)
                );
        }

        [Fact]
        public void Exhaustiveness_01()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
#nullable enable
    public S1(string? x) { _value = x; }
#nullable disable
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test1(S1 u)
    {
#line 100
        return u switch { int => 1, string => 2, null => 3 };
    } 

    static int Test2(S1 u)
    {
#line 200
        return u switch { int => 1, null => 3, string => 2 };
    } 

    static int Test3(S1 u)
    {
#line 300
        return u switch { null => 3, int => 1, string => 2 };
    } 

    static int Test4(S1 u)
    {
#line 400
        return u switch { int => 1, string => 2 };
    }   

    static int Test5(S1 u)
    {
#nullable enable
#line 500
        return u switch { int => 1, string => 2 };
#nullable disable
    }   

    static int Test6(S1 u)
    {
#line 600
        return u switch { int => 1, null => 3 };
    }   

    static int Test7(S1 u)
    {
#line 700
        return u switch { null => 3, int => 1 };
    }   

    static int Test8(S1 u)
    {
#line 800
        return u switch { int => 1 };
    }   

    static int Test9(S1 u)
    {
#line 900
        return u switch { not int => 1 };
    }   

    static int Test10(S1 u)
    {
#line 1000
        return u switch {  null => 3, not int => 1 };
    }   

    static int Test11(S1 u)
    {
#line 1100
        return u switch { not null => 1 };
    } 

    static int Test11_5(S1 u)
    {
#nullable enable
#line 1150
        return u switch { not null => 1 };
#nullable disable
    } 

    static int Test12(S1 u)
    {
#line 1200
        return u switch { null => 3, not null => 1 };
    } 

    static int Test13(S1 u)
    {
#line 1300
        return u switch { not null => 3, null => 1 };
    } 

    static int Test14(S1 u)
    {
#line 1400
        return u switch { { } => 1, null => 3 };
    } 

    static int Test15(S1 u)
    {
#line 1500
        return u switch { null => 3, var x => 1 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            var verifier = CompileAndVerify(comp).VerifyDiagnostics(
                // (100,50): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, string => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(100, 50),
                // (200,48): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, null => 3, string => 2 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "string").WithLocation(200, 48),
                // (300,48): hidden CS9335: The pattern is redundant.
                //         return u switch { null => 3, int => 1, string => 2 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "string").WithLocation(300, 48),
                // (500,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { int => 1, string => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(500, 18),
                // (600,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
                //         return u switch { int => 1, null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(600, 18),
                // (700,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
                //         return u switch { null => 3, int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(700, 18),
                // (800,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
                //         return u switch { int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(800, 18),
                // (900,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'int' is not covered.
                //         return u switch { not int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("int").WithLocation(900, 18),
                // (1000,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'int' is not covered.
                //         return u switch {  null => 3, not int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("int").WithLocation(1000, 18),
                // (1150,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { not null => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(1150, 18),
                // (1200,42): hidden CS9335: The pattern is redundant.
                //         return u switch { null => 3, not null => 1 };
                // Note the location, the diagnostic is for 'null' in 'not null' of the second case rather than for 'null' in the first case.  
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(1200, 42),
                // (1300,42): hidden CS9335: The pattern is redundant.
                //         return u switch { not null => 3, null => 1 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(1300, 42),
                // (1400,37): hidden CS9335: The pattern is redundant.
                //         return u switch { { } => 1, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(1400, 37)
                );

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (int V_0,
                object V_1)
  IL_0000:  ldarga.s   V_0
  IL_0002:  constrained. ""S1""
  IL_0008:  callvirt   ""object System.Runtime.CompilerServices.IUnion.Value.get""
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  isinst     ""int""
  IL_0014:  brtrue.s   IL_0023
  IL_0016:  ldloc.1
  IL_0017:  isinst     ""string""
  IL_001c:  brtrue.s   IL_0027
  IL_001e:  ldloc.1
  IL_001f:  brfalse.s  IL_002b
  IL_0021:  br.s       IL_002f
  IL_0023:  ldc.i4.1
  IL_0024:  stloc.0
  IL_0025:  br.s       IL_0034
  IL_0027:  ldc.i4.2
  IL_0028:  stloc.0
  IL_0029:  br.s       IL_0034
  IL_002b:  ldc.i4.3
  IL_002c:  stloc.0
  IL_002d:  br.s       IL_0034
  IL_002f:  call       ""void <PrivateImplementationDetails>.ThrowInvalidOperationException()""
  IL_0034:  ldloc.0
  IL_0035:  ret
}
");
        }

        [Fact]
        public void Exhaustiveness_02()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int? x) { _value = x; }
    public S1(string x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test1(S1 u)
    {
#line 100
        return u switch { int => 1, string => 2, null => 3 };
    } 

    static int Test2(S1 u)
    {
#line 200
        return u switch { int => 1, null => 3, string => 2 };
    } 

    static int Test3(S1 u)
    {
#line 300
        return u switch { null => 3, int => 1, string => 2 };
    } 

    static int Test4(S1 u)
    {
#line 400
        return u switch { int => 1, string => 2 };
    }   

    static int Test5(S1 u)
    {
#nullable enable
#line 500
        return u switch { int => 1, string => 2 };
#nullable disable
    }   

    static int Test6(S1 u)
    {
#line 600
        return u switch { int => 1, null => 3 };
    }   

    static int Test7(S1 u)
    {
#line 700
        return u switch { null => 3, int => 1 };
    }   

    static int Test8(S1 u)
    {
#line 800
        return u switch { int => 1 };
    }   

    static int Test9(S1 u)
    {
#line 900
        return u switch { not int => 1 };
    }   

    static int Test10(S1 u)
    {
#line 1000
        return u switch {  null => 3, not int => 1 };
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,50): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, string => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(100, 50),
                // (200,48): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, null => 3, string => 2 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "string").WithLocation(200, 48),
                // (300,48): hidden CS9335: The pattern is redundant.
                //         return u switch { null => 3, int => 1, string => 2 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "string").WithLocation(300, 48),
                // (500,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { int => 1, string => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(500, 18),
                // (600,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
                //         return u switch { int => 1, null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(600, 18),
                // (700,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
                //         return u switch { null => 3, int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(700, 18),
                // (800,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'string' is not covered.
                //         return u switch { int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("string").WithLocation(800, 18),
                // (900,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'int' is not covered.
                //         return u switch { not int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("int").WithLocation(900, 18),
                // (1000,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'int' is not covered.
                //         return u switch {  null => 3, not int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("int").WithLocation(1000, 18)
                );
        }

        [Fact]
        public void Exhaustiveness_03()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
#nullable enable
    public S1(string? x) { _value = x; }
#nullable disable
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test1(S1 u)
    {
#line 100
        return u switch { not null => 2, null => 3 };
    }   

    static int Test2(S1 u)
    {
#nullable enable
#line 200
        return u switch { not null => 2, null => 3 };
#nullable disable
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,42): hidden CS9335: The pattern is redundant.
                //         return u switch { not null => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(100, 42),
                // (200,42): hidden CS9335: The pattern is redundant.
                //         return u switch { not null => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(200, 42)
                );
        }

        [Fact]
        public void Exhaustiveness_04()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(bool x) { _value = x; }
#nullable enable
    public S1(string? x) { _value = x; }
#nullable disable
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test1(S1 u)
    {
#line 100
        return u switch { true => 1, false => 4, string => 2, null => 3 };
    } 

    static int Test2(S1 u)
    {
#line 200
        return u switch { true => 1, false => 4, string => 2 };
    } 

    static int Test3(S1 u)
    {
#nullable enable
#line 300
        return u switch { true => 1, false => 4, string => 2 };
#nullable disable
    } 

    static int Test4(S1 u)
    {
#line 400
        return u switch { true => 1, string => 2, null => 3 };
    } 

    static int Test5(S1 u)
    {
#line 500
        return u switch { null => 3 , true => 1, string => 2 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,63): hidden CS9335: The pattern is redundant.
                //         return u switch { true => 1, false => 4, string => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(100, 63),
                // (300,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { true => 1, false => 4, string => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(300, 18),
                // (400,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'false' is not covered.
                //         return u switch { true => 1, string => 2, null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("false").WithLocation(400, 18),
                // (500,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'false' is not covered.
                //         return u switch { null => 3 , true => 1, string => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("false").WithLocation(500, 18)
                );
        }

        [Fact]
        public void Exhaustiveness_05()
        {
            var src = @"
#nullable enable

class C1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object? _value;

    public C1(){}
    public C1(int x) { _value = x; }
    public C1(string? x) { _value = x; }
    object? System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new C1(10)));
        System.Console.Write(Test1(new C1()));
        System.Console.Write(Test1(new C1(""10"")));

        System.Console.Write(' ');
        System.Console.Write(Test2(new C1(10)));
        System.Console.Write(Test2(new C1()));
        System.Console.Write(Test2(new C1(""10"")));
        System.Console.Write(Test2(null));
    }

    static int Test1(C1? u)
    {
#line 26
        return u switch { int => 1, string => 2, null => 3 };
    }

    static int Test2(C1? u)
    {
        return u switch { int => -1, string => -2, null => -3, _ => -4 };
    }

    static int Test3(C2? u)
    {
        return u switch { { Value: int } => -1, { Value: string } => -2, { Value: null } => -3, _ => -4 };
    }

    static int Test4(C1? u)
    {
        return u switch { _ => 3 };
    }

    static int Test5(C2 u)
    {
        return u switch { null => -4, { Value: int } => -1, { Value: string } => -2, { Value: object } => -3 };
    }
}

class C2
{
    public object? Value => null;
}

";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "132 -1-3-2-4").VerifyDiagnostics(

                // PROTOTYPE: The WRN_SwitchExpressionNotExhaustiveForNull below is very confusing, especially that there is 
                //            a case 'null => 3' in the switch expression. It looks like the only way to shut off the warning
                //            is to use case '_'

                // (26,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { int => 1, string => 2, null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(26, 18),

                // PROTOTYPE: The following two warnings are misleading. The case covers a distinct value. 

                // (26,50): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, string => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(26, 50),
                // (31,52): hidden CS9335: The pattern is redundant.
                //         return u switch { int => -1, string => -2, null => -3, _ => -4 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(31, 52),

                // (46,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '{ Value: null }' is not covered.
                //         return u switch { null => -4, { Value: int } => -1, { Value: string } => -2, { Value: object } => -3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("{ Value: null }").WithLocation(46, 18)
                );
        }

        [Fact]
        public void Exhaustiveness_06()
        {
            var src = @"
#nullable enable

class C1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object? _value;

    public C1(){}
    public C1(int x) { _value = x; }
    public C1(string? x) { _value = x; }
    object? System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test4(C1? u)
    {
#line 41
        return u switch { int => 1, string => 2, not null => 3 };
    }
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (41,50): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //         return u switch { int => 1, string => 2, not null => 3 };
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "not null").WithLocation(41, 50),

                // The following warning is for 'u.Value' missing null check. 

                // (41,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { int => 1, string => 2, not null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(41, 18)
                );
        }

        [Fact]
        public void Exhaustiveness_07()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(C1 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;

class Program
{
    static int Test1(S1 u)
    {
        return u switch { int => 1, C2 => 2, null => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (17,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C1' is not covered.
                //         return u switch { int => 1, C2 => 2, null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C1").WithLocation(17, 18)
                );
        }

        [Fact]
        public void Exhaustiveness_08()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C1;
class C2 : C1;

class Program
{
    static int Test1(S1 u)
    {
        return u switch { int => 1, C1 => 2, null => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (17,46): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, C1 => 2, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(17, 46)
                );
        }

        [Fact]
        public void Exhaustiveness_09()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
    public S1(C2 x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

interface I1;
class C2;
class C3;

class Program
{
    static int Test1(S1 u)
    {
        return u switch { int => 1, I1 => 2, null => 3 };
    } 

    static int Test2(S1 u)
    {
        return u switch { int => 1, I1 => 2, C2 => 4, null => 3 };
    } 

    static int Test3(S1 u)
    {
        return u switch { int => 1, I1 => 2, C3 => 5, C2 => 4, null => 3 };
    } 

    static int Test4(S1 u)
    {
        return u switch { int => 1, I1 => 2, null => 3, C2 => 4 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (18,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'C2' is not covered.
                //         return u switch { int => 1, I1 => 2, null => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("C2").WithLocation(18, 18),
                // (23,55): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, I1 => 2, C2 => 4, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(23, 55),
                // (28,46): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'C3'.
                //         return u switch { int => 1, I1 => 2, C3 => 5, C2 => 4, null => 3 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "C3").WithArguments("S1", "C3").WithLocation(28, 46),
                // (33,57): hidden CS9335: The pattern is redundant.
                //         return u switch { int => 1, I1 => 2, null => 3, C2 => 4 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "C2").WithLocation(33, 57)
                );
        }

        [Fact]
        public void Exhaustiveness_10()
        {
            var src =
@"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(Q x) { _value = x; }
    public S1(int x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C
{
    int M2(S1 o) => o switch { not (Q(1, 2.5) { P1: 1 } and Q(3, 4, 5) { P2: 2 }) => 1 };
    int M3(S1 o) => o switch { null => 0, not (Q(1, 2.5) { P1: 1 } and Q(3, 4, 5) { P2: 2 }) => 1 };
}
class Q
{
    public void Deconstruct(out object o1, out object o2) => throw null!;
    public void Deconstruct(out object o1, out object o2, out object o3) => throw null!;
    public int P1 = 5;
    public int P2 = 6;
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (12,23): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1, 2.5D) and (3, 4, 5) { P1: 1,  P2: 2 }' is not covered.
                //     int M2(S1 o) => o switch { not (Q(1, 2.5) { P1: 1 } and Q(3, 4, 5) { P2: 2 }) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1, 2.5D) and (3, 4, 5) { P1: 1,  P2: 2 }").WithLocation(12, 23),
                // (13,23): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1, 2.5D) and (3, 4, 5) { P1: 1,  P2: 2 }' is not covered.
                //     int M3(S1 o) => o switch { null => 0, not (Q(1, 2.5) { P1: 1 } and Q(3, 4, 5) { P2: 2 }) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1, 2.5D) and (3, 4, 5) { P1: 1,  P2: 2 }").WithLocation(13, 23)
                );
        }

        [Fact]
        public void Exhaustiveness_11()
        {
            var src =
@"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(Q x) { _value = x; }
    public S1(int x) { _value = x; }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class C
{
    int M2(S1 o)
#line 100
        => o switch { int => 1, Q { P1: true } => 2 };

    int M3(S1 o)
#line 200
        => o switch { Q { P1: true } => 2, int => 1 };

    int M4(S1 o)
#line 300
        => o switch { null => 0, int => 1, Q { P1: true } => 2 };

    int M5(S1 o)
#line 400
        => o switch { null => 0, Q { P1: true } => 2, int => 1 };
}
class Q
{
    public bool P1 = false;
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q{ P1: false }' is not covered.
                //         => o switch { int => 1, Q { P1: true } => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q{ P1: false }").WithLocation(100, 14),
                // (200,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q{ P1: false }' is not covered.
                //         => o switch { Q { P1: true } => 2, int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q{ P1: false }").WithLocation(200, 14),
                // (300,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q{ P1: false }' is not covered.
                //         => o switch { null => 0, int => 1, Q { P1: true } => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q{ P1: false }").WithLocation(300, 14),
                // (400,14): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q{ P1: false }' is not covered.
                //         => o switch { null => 0, Q { P1: true } => 2, int => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q{ P1: false }").WithLocation(400, 14)
                );
        }

        [Fact]
        public void Exhaustiveness_12()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x) { _value = x; }
#nullable enable
    public S1(string? x) { _value = x; }
#nullable disable
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static int Test1(S1 u)
    {
#line 100
        return u switch { object => 1, null => 3 };
    } 

    static int Test3(S1 u)
    {
#line 300
        return u switch { null => 3, object => 2 };
    } 

    static int Test4(S1 u)
    {
#line 400
        return u switch { object => 2 };
    }   

    static int Test5(S1 u)
    {
#nullable enable
#line 500
        return u switch { object => 2 };
#nullable disable
    }   

    static int Test9(S1 u)
    {
#line 900
        return u switch { not object => 1 };
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,40): hidden CS9335: The pattern is redundant.
                //         return u switch { object => 1, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(100, 40),
                // (300,38): hidden CS9335: The pattern is redundant.
                //         return u switch { null => 3, object => 2 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "object").WithLocation(300, 38),
                // (500,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { object => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(500, 18),
                // (900,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
                //         return u switch { not object => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(900, 18)
                );
        }

        [Fact]
        public void EmptyUnion_01()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    object System.Runtime.CompilerServices.IUnion.Value => null!;
}

class Program
{
    static int Test1(S1 u)
    {
#line 100
        return u switch { int => 1, null => 3 };
    } 

    static int Test2(S1 u)
    {
#line 200
        return u switch { null => 1 };
    } 

    static int Test3(S1 u)
    {
#line 300
        return u switch { null => 3, object => 1 };
    } 

    static int Test4(S1 u)
    {
#line 400
        return u switch { int => 1 };
    }   

    static int Test5(S1 u)
    {
#line 500
        return u switch { object => 1 };
    }   

    static int Test6(S1 u)
    {
#line 600
        return u switch { not object => 1 };
    }   

    static int Test7(S1 u)
    {
#line 700
        return u switch {  null => 3, not object => 1 };
    }   

    static int Test8(S1 u)
    {
#line 800
        return u switch { object => 1, null => 3 };
    } 

    static int Test9(S1 u)
    {
#line 900
        return u switch { not null => 1 };
    } 

    static int Test10(S1 u)
    {
#line 1000
        return u switch { null => 3, not null => 1 };
    } 

    static int Test11(S1 u)
    {
#line 1100
        return u switch { not null => 3, null => 1 };
    } 

    static int Test12(S1 u)
    {
#line 1200
        return u switch { { } => 1, null => 3 };
    } 

    static int Test13(S1 u)
    {
#line 1300
        return u switch { null => 3, var x => 1 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         return u switch { int => 1, null => 3 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(100, 27),
                // (200,18): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
                //         return u switch { null => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(200, 18),
                // (300,38): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'object'.
                //         return u switch { null => 3, object => 1 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("S1", "object").WithLocation(300, 38),
                // (400,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'int'.
                //         return u switch { int => 1 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1", "int").WithLocation(400, 27),
                // (500,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'object'.
                //         return u switch { object => 1 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("S1", "object").WithLocation(500, 27),
                // (600,31): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'object'.
                //         return u switch { not object => 1 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("S1", "object").WithLocation(600, 31),
                // (700,43): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'object'.
                //         return u switch {  null => 3, not object => 1 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("S1", "object").WithLocation(700, 43),
                // (800,27): error CS8121: An expression of type 'S1' cannot be handled by a pattern of type 'object'.
                //         return u switch { object => 1, null => 3 };
                Diagnostic(ErrorCode.ERR_PatternWrongType, "object").WithArguments("S1", "object").WithLocation(800, 27),
                // (900,18): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         return u switch { not null => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(900, 18),
                // (1000,42): hidden CS9335: The pattern is redundant.
                //         return u switch { null => 3, not null => 1 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(1000, 42),
                // (1100,42): hidden CS9335: The pattern is redundant.
                //         return u switch { not null => 3, null => 1 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(1100, 42),
                // (1200,37): hidden CS9335: The pattern is redundant.
                //         return u switch { { } => 1, null => 3 };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(1200, 37)
                );
        }

        [Fact]
        public void UnionConversion_01_Implicit()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
        Test2();
        Test3();
        Test4();
        Test5();
    }

    static S1 Test1()
    {
        System.Console.Write(""1-"");
        /*<bind>*/ return 10; /*</bind>*/
    }   

    static S1 Test2()
    {
        System.Console.Write(""2-"");
        return default;
    }   

    static S1 Test3()
    {
        System.Console.Write(""3-"");
        return default(S1);
    }   

    static S1 Test4()
    {
        System.Console.Write(""4-"");
        return null;
    }   

    static S1 Test5()
    {
        System.Console.Write(""5-"");
        return ""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var ten = GetSyntax<LiteralExpressionSyntax>(tree, "10");

            var symbolInfo = model.GetSymbolInfo(ten);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(ten);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(ten);
            Assert.True(conversion.Exists);
            Assert.True(conversion.IsValid);
            Assert.True(conversion.IsImplicit);
            Assert.False(conversion.IsExplicit);
            Assert.Equal(ConversionKind.Union, conversion.Kind);
            Assert.Equal(LookupResultKind.Viable, conversion.ResultKind);
            Assert.True(conversion.IsUnion);
            Assert.False(conversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", conversion.Method.ToTestDisplayString());
            AssertEx.Equal("S1..ctor(System.Int32 x)", conversion.MethodSymbol.ToTestDisplayString());
            Assert.Null(conversion.BestUserDefinedConversionAnalysis);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedFromConversion);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedToConversion);
            Assert.NotNull(conversion.BestUnionConversionAnalysis);
            Assert.Empty(conversion.OriginalUserDefinedConversions);
            Assert.True(conversion.UnderlyingConversions.IsDefault);
            Assert.False(conversion.IsArrayIndex);
            Assert.False(conversion.IsExtensionMethod);

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", commonConversion.MethodSymbol.ToTestDisplayString());

            VerifyOperationTreeForTest<ReturnStatementSyntax>(comp, """
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return 10;')
  ReturnedValue:
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1, IsImplicit) (Syntax: '10')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
      Operand:
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
""");

            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} 2-3-4-string {} 5-string {11}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  newobj     ""S1..ctor(int)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test4", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldstr      ""4-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  newobj     ""S1..ctor(string)""
  IL_0010:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(string)""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void UnionConversion_02_Implicit_Class()
        {
            var src = @"
class S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
        Test2();
        Test3();
        Test4();
        Test5();
    }

    static S1 Test1()
    {
        System.Console.Write(""1-"");
        return 10;
    }   

    static S1 Test2()
    {
        System.Console.Write(""2-"");
        return default;
    }   

    static S1 Test3()
    {
        System.Console.Write(""3-"");
        return default(S1);
    }   

    static S1 Test4()
    {
        System.Console.Write(""4-"");
        return null;
    }   

    static S1 Test5()
    {
        System.Console.Write(""5-"");
        return ""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} 2-3-4-5-string {11}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  newobj     ""S1..ctor(int)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  ret
}
");

            verifier.VerifyIL("Program.Test4", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""4-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(string)""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void UnionConversion_03_Cast()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
        Test2();
        Test3();
        Test4();
        Test5();
    }

    static S1 Test1()
    {
        System.Console.Write(""1-"");
        return /*<bind>*/ (S1)10 /*</bind>*/;
    }   

    static S1 Test2()
    {
        System.Console.Write(""2-"");
        return (S1)default;
    }   

    static S1 Test3()
    {
        System.Console.Write(""3-"");
        return (S1)default(S1);
    }   

    static S1 Test4()
    {
        System.Console.Write(""4-"");
        return (S1)null;
    }   

    static S1 Test5()
    {
        System.Console.Write(""5-"");
        return (S1)""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var cast = GetSyntax<CastExpressionSyntax>(tree, "(S1)10");

            var typeInfo = model.GetTypeInfo(cast);
            Assert.Equal("S1", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(cast);
            Assert.True(conversion.IsIdentity);

            var symbolInfo = model.GetSymbolInfo(cast);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("S1..ctor(System.Int32 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            VerifyOperationTreeForTest<CastExpressionSyntax>(comp, """
IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1) (Syntax: '(S1)10')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
  Operand:
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
""");

            var ten = GetSyntax<LiteralExpressionSyntax>(tree, "10");

            typeInfo = model.GetTypeInfo(ten);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            conversion = model.GetConversion(ten);
            Assert.True(conversion.IsIdentity);

            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} 2-3-4-string {} 5-string {11}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  newobj     ""S1..ctor(int)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test4", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldstr      ""4-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  newobj     ""S1..ctor(string)""
  IL_0010:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(string)""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void UnionConversion_04_Cast_Class()
        {
            var src = @"
class S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
        Test2();
        Test3();
        Test4();
        Test5();
    }

    static S1 Test1()
    {
        System.Console.Write(""1-"");
        return (S1)10;
    }   

    static S1 Test2()
    {
        System.Console.Write(""2-"");
        return (S1)default;
    }   

    static S1 Test3()
    {
        System.Console.Write(""3-"");
        return (S1)default(S1);
    }   

    static S1 Test4()
    {
        System.Console.Write(""4-"");
        return (S1)null;
    }   

    static S1 Test5()
    {
        System.Console.Write(""5-"");
        return (S1)""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} 2-3-4-5-string {11}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  newobj     ""S1..ctor(int)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  ret
}
");

            verifier.VerifyIL("Program.Test4", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      ""4-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldnull
  IL_000b:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(string)""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void UnionConversion_05_No_Lifted_Form()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        _value = x;
    }
    public S1(string x)
    {
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static S1 Test1(int? x)
    {
        return x;
    }   

    static S1? Test2(int? y)
    {
        return y;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);

            // PROTOTYPE: Confirm that there are no lifted forms.

            comp.VerifyDiagnostics(
                // (20,16): error CS0029: Cannot implicitly convert type 'int?' to 'S1'
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int?", "S1").WithLocation(20, 16),
                // (25,16): error CS0029: Cannot implicitly convert type 'int?' to 'S1?'
                //         return y;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "y").WithArguments("int?", "S1?").WithLocation(25, 16)
                );
        }

        [Fact]
        public void UnionConversion_06_No_Lifted_Form_Class()
        {
            var src = @"
class S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        _value = x;
    }
    public S1(string x)
    {
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static S1 Test1(int? x)
    {
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (20,16): error CS0029: Cannot implicitly convert type 'int?' to 'S1'
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int?", "S1").WithLocation(20, 16)
                );
        }

        [Fact]
        public void UnionConversion_07_Ambiguity_First_Declared_Wins()
        {
            var src1 = @"
public struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(C1 x)
    {
        System.Console.Write(""C1"");
        _value = x;
    }
    public S1(C2 x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

public struct S2 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S2(C2 x)
    {
        System.Console.Write(""C2"");
        _value = x;
    }
    public S2(C1 x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

public class C1 { }
public class C2 { }
";
            var src2 = @"
class Program
{
    static void Main()
    {
        Test1();
        Test2();
    }

    static S1 Test1()
    {
        return null;
    }   

    static S2 Test2()
    {
        return null;
    }   
}
";
            var comp = CreateCompilation([src1, src2, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "C1C2").VerifyDiagnostics();

            comp = CreateCompilation(src2, references: [comp.EmitToImageReference()], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "C1C2").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_08_Standard_Conversion_For_Source_Allowed()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1(15);
        Test2(16);
    }

    static S1 Test1(byte x1)
    {
        return x1;
    }   

    static S1 Test2(byte x2)
    {
        return (S1)x2;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {15} int {16}").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var x1 = GetSyntax<IdentifierNameSyntax>(tree, "x1");

            var symbolInfo = model.GetSymbolInfo(x1);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("System.Byte x1", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(x1);
            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(x1);
            Assert.True(conversion.IsUnion);
            Assert.False(conversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", conversion.Method.ToTestDisplayString());
            Assert.Null(conversion.BestUserDefinedConversionAnalysis);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedFromConversion);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedToConversion);
            Assert.NotNull(conversion.BestUnionConversionAnalysis);
            Assert.Empty(conversion.OriginalUserDefinedConversions);
            Assert.True(conversion.UnderlyingConversions.IsDefault);

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", commonConversion.MethodSymbol.ToTestDisplayString());

            VerifyOperationTreeForNode(comp, model, GetSyntax<ReturnStatementSyntax>(tree, "return x1;"), """
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return x1;')
  ReturnedValue:
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1, IsImplicit) (Syntax: 'x1')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand:
            IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'x1')
""");

            var x2 = GetSyntax<IdentifierNameSyntax>(tree, "x2");

            symbolInfo = model.GetSymbolInfo(x2);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("System.Byte x2", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            typeInfo = model.GetTypeInfo(x2);
            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Byte", typeInfo.ConvertedType.ToTestDisplayString());

            conversion = model.GetConversion(x2);
            Assert.True(conversion.IsIdentity);
            Assert.False(conversion.IsUnion);
            Assert.False(conversion.IsUserDefined);

            var cast = GetSyntax<CastExpressionSyntax>(tree, "(S1)x2");

            symbolInfo = model.GetSymbolInfo(cast);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("S1..ctor(System.Int32 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            typeInfo = model.GetTypeInfo(cast);
            Assert.Equal("S1", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1", typeInfo.ConvertedType.ToTestDisplayString());

            VerifyOperationTreeForNode(comp, model, GetSyntax<ReturnStatementSyntax>(tree, "return (S1)x2;"), """
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (S1)x2;')
  ReturnedValue:
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1) (Syntax: '(S1)x2')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: '(S1)x2')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand:
            IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'x2')
""");
        }

        [Fact]
        public void UnionConversion_09_NonStandard_Conversion_For_Source_Not_Allowed()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(C1 x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class C1
{
    public static implicit operator C1(byte x) => new C1();
}

class Program
{
    static S1 Test1(byte x)
    {
#line 100
        return x;
    }   

    static S1 Test2(byte x)
    {
#line 200
        return (S1)x;
    }   

    static C1 Test3(byte x)
    {
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,16): error CS0029: Cannot implicitly convert type 'byte' to 'S1'
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("byte", "S1").WithLocation(100, 16),
                // (200,16): error CS0030: Cannot convert type 'byte' to 'S1'
                //         return (S1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)x").WithArguments("byte", "S1").WithLocation(200, 16)
                );
        }

        [Fact]
        public void UnionConversion_10_Explicit_Conversion_For_Source_Not_Allowed()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static S1 Test1(long x)
    {
#line 100
        return x;
    }   
    static S1 Test2(long x)
    {
#line 200
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,16): error CS0029: Cannot implicitly convert type 'long' to 'S1'
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("long", "S1").WithLocation(100, 16),
                // (200,16): error CS0030: Cannot convert type 'long' to 'S1'
                //         return (S1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)x").WithArguments("long", "S1").WithLocation(200, 16)
                );
        }

        [Fact]
        public void UnionConversion_11_Not_Standard_Conversion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(S2 x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null;
    public S2(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class C1
{
    public static implicit operator C1(S2 x) => new C1();
}

class Program
{
    static S1 Test1(int x)
    {
#line 100
        return x;
    }   

    static C1 Test2(int x)
    {
#line 200
        return x;
    }   

    static S1 Test3(int x)
    {
#line 300
        return (S2)x;
    }   

    static C1 Test4(int x)
    {
#line 400
        return (S2)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,16): error CS0029: Cannot implicitly convert type 'int' to 'S1'
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int", "S1").WithLocation(100, 16),
                // (200,16): error CS0029: Cannot implicitly convert type 'int' to 'C1'
                //         return x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("int", "C1").WithLocation(200, 16)
                );
        }

        [Fact]
        public void UnionConversion_12_Implicit_UserDefined_Conversion_Wins()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static implicit operator S1(int x)
    {
        System.Console.Write(""implicit operator "");
        return new S1(x.ToString());
    }
}

class Program
{
    static void Main()
    {
        Test1();
        Test2();
    }

    static S1 Test1()
    {
        return 10;
    }   

    static S1 Test2()
    {
        return (S1)20;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "implicit operator string {10} implicit operator string {20}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_13_Cast_Explicit_UserDefined_Conversion_Wins()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static explicit operator S1(int x)
    {
        System.Console.Write(""explicit operator "");
        return new S1(x.ToString());
    }
}

class Program
{
    static void Main()
    {
        Test2();
    }

    static S1 Test2()
    {
        return (S1)20;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "explicit operator string {20}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_14_Explicit_UserDefined_Conversion_Loses()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static explicit operator S1(int x) => throw null;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1 Test1()
    {
        return 10;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {10}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_15_Cast_From_Base_Class_Not_Union_Conversion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(System.ValueType x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1()));
    }

    static S1 Test2(System.ValueType x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "S1").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""S1""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void UnionConversion_16_Implicit_From_Base_Class_Union_Conversion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(System.ValueType x)
    {
        System.Console.Write(""System.ValueType "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1()));
    }

    static S1 Test2(System.ValueType x)
    {
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "System.ValueType S1").VerifyDiagnostics();

            // PROTOTYPE: Confirm that we are fine with this conversion behavior. See previous test as well.
            //            Cast performs unboxing conversion, but implicit conversion performs union conversion.
            //            Might be too confusing.
            //            Note, language disallows user-defined conversions like that, see errors below.

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""S1..ctor(System.ValueType)""
  IL_0006:  ret
}
");
            var src2 = @"
struct S1
{
    public static implicit operator S1(System.ValueType x)
         => throw null;
}
struct S2
{
    public static explicit operator S2(System.ValueType x)
         => throw null;
}
";
            CreateCompilation(src2).VerifyDiagnostics(
                // (4,37): error CS0553: 'S1.implicit operator S1(ValueType)': user-defined conversions to or from a base type are not allowed
                //     public static implicit operator S1(System.ValueType x)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "S1").WithArguments("S1.implicit operator S1(System.ValueType)").WithLocation(4, 37),
                // (9,37): error CS0553: 'S2.explicit operator S2(ValueType)': user-defined conversions to or from a base type are not allowed
                //     public static explicit operator S2(System.ValueType x)
                Diagnostic(ErrorCode.ERR_ConversionWithBase, "S2").WithArguments("S2.explicit operator S2(System.ValueType)").WithLocation(9, 37)
                );
        }

        [Fact]
        public void UnionConversion_17_Cast_From_Implemented_Interface_Not_Union_Conversion()
        {
            var src = @"
struct S1 : I1, System.Runtime.CompilerServices.IUnion
{
    public S1(I1 x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

interface I1 { }

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1()));
    }

    static S1 Test2(I1 x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "S1").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  unbox.any  ""S1""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void UnionConversion_18_Implicit_From_Implemented_Interface_Union_Conversion()
        {
            var src = @"
struct S1 : I1, System.Runtime.CompilerServices.IUnion
{
    public S1(I1 x)
    {
        System.Console.Write(""I1 "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

interface I1 { }

class Program
{
    static void Main()
    {
        System.Console.Write(Test2(new S1()));
    }

    static S1 Test2(I1 x)
    {
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "I1 S1").VerifyDiagnostics();

            // PROTOTYPE: Confirm that we are fine with this conversion behavior. See previous test as well.
            //            Cast performs unboxing conversion, but implicit conversion performs union conversion.
            //            Might be too confusing.
            //            Note, language disallows user-defined conversions like that, see errors below.

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""S1..ctor(I1)""
  IL_0006:  ret
}
");
            var src2 = @"
interface I1 { }

struct S1 : I1
{
    public static implicit operator S1(I1 x)
         => throw null;
}
struct S2 : I1
{
    public static explicit operator S2(I1 x)
         => throw null;
}
";
            CreateCompilation(src2).VerifyDiagnostics(
                // (6,37): error CS0552: 'S1.implicit operator S1(I1)': user-defined conversions to or from an interface are not allowed
                //     public static implicit operator S1(I1 x)
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "S1").WithArguments("S1.implicit operator S1(I1)").WithLocation(6, 37),
                // (11,37): error CS0552: 'S2.explicit operator S2(I1)': user-defined conversions to or from an interface are not allowed
                //     public static explicit operator S2(I1 x)
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "S2").WithArguments("S2.explicit operator S2(I1)").WithLocation(11, 37)
                );
        }

        [Fact]
        public void UnionConversion_19_From_Not_Implemented_Interface_Union_Conversion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(I1 x)
    {
        System.Console.Write(""I1 "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

interface I1 { }

struct S2 : I1;

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S2()));
        System.Console.Write(' ');
        System.Console.Write(Test2(new S2()));
    }

    static S1 Test1(I1 x)
    {
        return x;
    }   

    static S1 Test2(I1 x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "I1 S1 I1 S1").VerifyDiagnostics();

            // PROTOTYPE: Confirm that we are fine with this conversion behavior.
            //            Might be too confusing.
            //            Note, language disallows user-defined conversions like that, see errors below.

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""S1..ctor(I1)""
  IL_0006:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""S1..ctor(I1)""
  IL_0006:  ret
}
");
            var src2 = @"
interface I1 { }

struct S1
{
    public static implicit operator S1(I1 x)
         => throw null;
}
struct S2
{
    public static explicit operator S2(I1 x)
         => throw null;
}
";
            CreateCompilation(src2).VerifyDiagnostics(
                // (6,37): error CS0552: 'S1.implicit operator S1(I1)': user-defined conversions to or from an interface are not allowed
                //     public static implicit operator S1(I1 x)
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "S1").WithArguments("S1.implicit operator S1(I1)").WithLocation(6, 37),
                // (11,37): error CS0552: 'S2.explicit operator S2(I1)': user-defined conversions to or from an interface are not allowed
                //     public static explicit operator S2(I1 x)
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "S2").WithArguments("S2.explicit operator S2(I1)").WithLocation(11, 37)
                );
        }

        [Fact]
        public void UnionConversion_20_From_Not_Implemented_Interface_Union_Conversion()
        {
            var src = @"
class S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(I1 x)
    {
        System.Console.Write(""I1 "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

interface I1 { }

struct S2 : I1;

class Program
{
    static void Main()
    {
        System.Console.Write(Test1(new S2()));
    }

    static S1 Test1(I1 x)
    {
        return x;
    }   

    static S1 Test2(I1 x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "I1 S1").VerifyDiagnostics();

            // PROTOTYPE: Confirm that we are fine with this conversion behavior.
            //            Cast performs castclass conversion, but implicit conversion performs union conversion.
            //            Might be too confusing.
            //            Note, language disallows user-defined conversions like that, see errors below.

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""S1..ctor(I1)""
  IL_0006:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  castclass  ""S1""
  IL_0006:  ret
}
");
            var src2 = @"
interface I1 { }

class S1
{
    public static implicit operator S1(I1 x)
         => throw null;
}
class S2
{
    public static explicit operator S2(I1 x)
         => throw null;
}
";
            CreateCompilation(src2).VerifyDiagnostics(
                // (6,37): error CS0552: 'S1.implicit operator S1(I1)': user-defined conversions to or from an interface are not allowed
                //     public static implicit operator S1(I1 x)
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "S1").WithArguments("S1.implicit operator S1(I1)").WithLocation(6, 37),
                // (11,37): error CS0552: 'S2.explicit operator S2(I1)': user-defined conversions to or from an interface are not allowed
                //     public static explicit operator S2(I1 x)
                Diagnostic(ErrorCode.ERR_ConversionWithInterface, "S2").WithArguments("S2.explicit operator S2(I1)").WithLocation(11, 37)
                );
        }

        [Fact]
        public void UnionConversion_21_Through_Base_Class_Or_Interface()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(System.ValueType x)
    {
        System.Console.Write(""System.ValueType {"");
        System.Console.Write(x.GetType());
        System.Console.Write(' ');
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(System.IComparable x)
    {
        System.Console.Write(""System.IComparable {"");
        System.Console.Write(x.GetType());
        System.Console.Write(' ');
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
        Test5();
    }

    static S1 Test1()
    {
        System.Console.Write(""1-"");
        return 10;
    }   

    static S1 Test5()
    {
        System.Console.Write(""5-"");
        return ""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1-System.ValueType {System.Int32 10} 5-System.IComparable {System.String 11}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  box        ""int""
  IL_0011:  newobj     ""S1..ctor(System.ValueType)""
  IL_0016:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(System.IComparable)""
  IL_0014:  ret
}
");
        }

        [Fact]
        public void UnionConversion_22_ExpressionTree()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static System.Linq.Expressions.Expression<System.Func<S1>> Test1(int x)
    {
        return () => x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (13,22): error CS9400: An expression tree may not contain a union conversion.
                //         return () => x;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsUnionConversion, "x").WithLocation(13, 22)
                );
        }

        [Fact]
        public void UnionConversion_23_ClassifyImplicitConversionFromType()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        Test1(10);
    }

    static S1 Test1(int? x)
    {
        return x ?? new S1("""");
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {10}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_24_ClassifyConversionFromType()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        var x = new S1();
        var y = (0, 123);
        (var z, x) = y; 
    }
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {123}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_25_ClassifyConversionFromTypeForCast_Implicit_UserDefined_Conversion_Wins()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static implicit operator S1(int x)
    {
        System.Console.Write(""implicit operator "");
        return new S1(x.ToString());
    }
}

class Program
{
    static void Main()
    {
        Test1(); 
    }

    static void Test1()
    {
        foreach (S1 y in new int[] { 10 })
        {
        }
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "implicit operator string {10}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_26_ClassifyConversionFromTypeForCast_Explicit_UserDefined_Conversion_Wins()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static explicit operator S1(int x)
    {
        System.Console.Write(""explicit operator "");
        return new S1(x.ToString());
    }
}

class Program
{
    static void Main()
    {
        Test2();
    }

    static void Test2()
    {
        foreach (S1 y in new int[] { 20 })
        {
        }
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "explicit operator string {20}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_27_ClassifyConversionFromTypeForCast()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static void Test1()
    {
        foreach (S1 y in new int[] { 10 })
        {
        }
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {10}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_28_Under_Tuple_Conversion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(double x)
    {
        System.Console.Write(""double {"");
        System.Console.Write((int)x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        Test1((0, 10));
    }

    static (int, S1) Test1((int, byte) x)
    {
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "double {10}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.ValueTuple<int, byte> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldfld      ""int System.ValueTuple<int, byte>.Item1""
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""byte System.ValueTuple<int, byte>.Item2""
  IL_000e:  conv.r8
  IL_000f:  newobj     ""S1..ctor(double)""
  IL_0014:  newobj     ""System.ValueTuple<int, S1>..ctor(int, S1)""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void UnionConversion_30_In_Parameter()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(in int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        Test1();
        Test2(11);
        Test3(12);
    }

    static S1 Test1()
    {
        System.Console.Write(""1-"");
        return 10;
    }   

    static S1 Test2(int x)
    {
        System.Console.Write(""2-"");
        return x;
    }   

    static S1 Test3(byte x)
    {
        System.Console.Write(""3-"");
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} 2-int {11} 3-int {12}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  newobj     ""S1..ctor(in int)""
  IL_0014:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldarga.s   V_0
  IL_000c:  newobj     ""S1..ctor(in int)""
  IL_0011:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int V_0)
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  newobj     ""S1..ctor(in int)""
  IL_0013:  ret
}
");
        }

        [Fact]
        public void UnionConversion_31_Ambiguity_In_Vs_Val_First_Declared_Wins()
        {
            var src1 = @"
public struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(in int x)
    {
        System.Console.Write(""In"");
    }
    public S1(int x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

public struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x)
    {
        System.Console.Write(""Val"");
    }
    public S2(in int x) => throw null;
    public S2(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}
";
            var src2 = @"
class Program
{
    static void Main()
    {
        Test1();
        Test2();
    }

    static S1 Test1()
    {
        return 10;
    }   

    static S2 Test2()
    {
        return 10;
    }   
}
";
            var comp = CreateCompilation([src1, src2, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "InVal").VerifyDiagnostics();

            comp = CreateCompilation(src2, references: [comp.EmitToImageReference()], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "InVal").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_32_No_Params_Expansion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(params int[] x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static S1 Test1(int x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (13,16): error CS0030: Cannot convert type 'int' to 'S1'
                //         return (S1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)x").WithArguments("int", "S1").WithLocation(13, 16)
                );
        }

        [Fact]
        public void UnionConversion_33_No_Optional()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(byte  x) => throw null;
    public S1(string x) => throw null;
    public S1(int x, object o = null) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static S1 Test1(int x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (14,16): error CS0030: Cannot convert type 'int' to 'S1'
                //         return (S1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)x").WithArguments("int", "S1").WithLocation(14, 16)
                );
        }

        [Fact]
        public void UnionConversion_34_No_Non_Public()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(byte  x) => throw null;
    public S1(string x) => throw null;
    internal S1(int x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static S1 Test1(int x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (14,16): error CS0030: Cannot convert type 'int' to 'S1'
                //         return (S1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)x").WithArguments("int", "S1").WithLocation(14, 16)
                );
        }

        [Theory]
        [CombinatorialData]
        public void UnionConversion_35_No_Ref_Out([CombinatorialValues("ref", "out", "ref readonly")] string refModifier)
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(" + refModifier + @" int x) => throw null;
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static S1 Test1(int x)
    {
        return (S1)x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (13,16): error CS0030: Cannot convert type 'int' to 'S1'
                //         return (S1)x;
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(S1)x").WithArguments("int", "S1").WithLocation(13, 16)
                );
        }

        [Fact]
        public void UnionConversion_36_Implicit_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test2().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test3().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test4().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test5().HasValue ? ""[not null] "" : ""null "");
    }

    static S1? Test1()
    {
        System.Console.Write(""1-"");
        /*<bind>*/ return 10; /*</bind>*/
    }   

    static S1? Test2()
    {
        System.Console.Write(""2-"");
        return default;
    }   

    static S1? Test3()
    {
        System.Console.Write(""3-"");
        return default(S1);
    }   

    static S1? Test4()
    {
        System.Console.Write(""4-"");
        return null;
    }   

    static S1? Test5()
    {
        System.Console.Write(""5-"");
        return ""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var ten = GetSyntax<LiteralExpressionSyntax>(tree, "10");

            var symbolInfo = model.GetSymbolInfo(ten);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(ten);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1?", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(ten);
            Assert.True(conversion.Exists);
            Assert.True(conversion.IsValid);
            Assert.True(conversion.IsImplicit);
            Assert.False(conversion.IsExplicit);
            Assert.Equal(ConversionKind.Union, conversion.Kind);
            Assert.Equal(LookupResultKind.Viable, conversion.ResultKind);
            Assert.True(conversion.IsUnion);
            Assert.False(conversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", conversion.Method.ToTestDisplayString());
            AssertEx.Equal("S1..ctor(System.Int32 x)", conversion.MethodSymbol.ToTestDisplayString());
            Assert.Null(conversion.BestUserDefinedConversionAnalysis);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedFromConversion);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedToConversion);
            Assert.NotNull(conversion.BestUnionConversionAnalysis);
            Assert.Empty(conversion.OriginalUserDefinedConversions);
            Assert.True(conversion.UnderlyingConversions.IsDefault);
            Assert.False(conversion.IsArrayIndex);
            Assert.False(conversion.IsExtensionMethod);

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", commonConversion.MethodSymbol.ToTestDisplayString());

            VerifyOperationTreeForTest<ReturnStatementSyntax>(comp, """
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return 10;')
  ReturnedValue:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1?, IsImplicit) (Syntax: '10')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1, IsImplicit) (Syntax: '10')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
          Operand:
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
""");

            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} [not null] 2-null 3-[not null] 4-null 5-string {11} [not null]").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  newobj     ""S1..ctor(int)""
  IL_0011:  newobj     ""S1?..ctor(S1)""
  IL_0016:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1? V_0)
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1?""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloc.0
  IL_0013:  newobj     ""S1?..ctor(S1)""
  IL_0018:  ret
}
");

            verifier.VerifyIL("Program.Test4", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1? V_0)
  IL_0000:  ldstr      ""4-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1?""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(string)""
  IL_0014:  newobj     ""S1?..ctor(S1)""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void UnionConversion_37_Cast_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        System.Console.Write(Test1().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test2().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test3().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test4().HasValue ? ""[not null] "" : ""null "");
        System.Console.Write(Test5().HasValue ? ""[not null] "" : ""null "");
    }

    static S1? Test1()
    {
        System.Console.Write(""1-"");
        return /*<bind>*/ (S1?)10 /*</bind>*/;
    }   

    static S1? Test2()
    {
        System.Console.Write(""2-"");
        return (S1?)default;
    }   

    static S1? Test3()
    {
        System.Console.Write(""3-"");
        return (S1?)default(S1);
    }   

    static S1? Test4()
    {
        System.Console.Write(""4-"");
        return (S1?)null;
    }   

    static S1? Test5()
    {
        System.Console.Write(""5-"");
        return (S1?)""11"";
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var cast = GetSyntax<CastExpressionSyntax>(tree, "(S1?)10");

            var typeInfo = model.GetTypeInfo(cast);
            Assert.Equal("S1?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1?", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(cast);
            Assert.True(conversion.IsIdentity);

            var symbolInfo = model.GetSymbolInfo(cast);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("S1..ctor(System.Int32 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            VerifyOperationTreeForTest<CastExpressionSyntax>(comp, """
IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1?) (Syntax: '(S1?)10')
  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Operand:
    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1, IsImplicit) (Syntax: '(S1?)10')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
      Operand:
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
""");

            var ten = GetSyntax<LiteralExpressionSyntax>(tree, "10");

            typeInfo = model.GetTypeInfo(ten);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());

            conversion = model.GetConversion(ten);
            Assert.True(conversion.IsIdentity);

            var verifier = CompileAndVerify(comp, expectedOutput: "1-int {10} [not null] 2-null 3-[not null] 4-null 5-string {11} [not null]").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldstr      ""1-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldc.i4.s   10
  IL_000c:  newobj     ""S1..ctor(int)""
  IL_0011:  newobj     ""S1?..ctor(S1)""
  IL_0016:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1? V_0)
  IL_0000:  ldstr      ""2-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1?""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldstr      ""3-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1""
  IL_0012:  ldloc.0
  IL_0013:  newobj     ""S1?..ctor(S1)""
  IL_0018:  ret
}
");

            verifier.VerifyIL("Program.Test4", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S1? V_0)
  IL_0000:  ldstr      ""4-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldloca.s   V_0
  IL_000c:  initobj    ""S1?""
  IL_0012:  ldloc.0
  IL_0013:  ret
}
");

            verifier.VerifyIL("Program.Test5", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldstr      ""5-""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ldstr      ""11""
  IL_000f:  newobj     ""S1..ctor(string)""
  IL_0014:  newobj     ""S1?..ctor(S1)""
  IL_0019:  ret
}
");
        }

        [Fact]
        public void UnionConversion_38_Standard_Conversion_For_Source_Allowed_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1(15);
        Test2(16);
    }

    static S1? Test1(byte x1)
    {
        return x1;
    }   

    static S1? Test2(byte x2)
    {
        return (S1?)x2;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {15} int {16}").VerifyDiagnostics();

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var x1 = GetSyntax<IdentifierNameSyntax>(tree, "x1");

            var symbolInfo = model.GetSymbolInfo(x1);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("System.Byte x1", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(x1);
            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1?", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(x1);
            Assert.True(conversion.IsUnion);
            Assert.False(conversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", conversion.Method.ToTestDisplayString());
            Assert.Null(conversion.BestUserDefinedConversionAnalysis);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedFromConversion);
            Assert.Equal(Conversion.NoConversion, conversion.UserDefinedToConversion);
            Assert.NotNull(conversion.BestUnionConversionAnalysis);
            Assert.Empty(conversion.OriginalUserDefinedConversions);
            Assert.True(conversion.UnderlyingConversions.IsDefault);

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor(System.Int32 x)", commonConversion.MethodSymbol.ToTestDisplayString());

            VerifyOperationTreeForNode(comp, model, GetSyntax<ReturnStatementSyntax>(tree, "return x1;"), """
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return x1;')
  ReturnedValue:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1?, IsImplicit) (Syntax: 'x1')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1, IsImplicit) (Syntax: 'x1')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
          Operand:
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand:
                IParameterReferenceOperation: x1 (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'x1')
""");

            var x2 = GetSyntax<IdentifierNameSyntax>(tree, "x2");

            symbolInfo = model.GetSymbolInfo(x2);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("System.Byte x2", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            typeInfo = model.GetTypeInfo(x2);
            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Byte", typeInfo.ConvertedType.ToTestDisplayString());

            conversion = model.GetConversion(x2);
            Assert.True(conversion.IsIdentity);
            Assert.False(conversion.IsUnion);
            Assert.False(conversion.IsUserDefined);

            var cast = GetSyntax<CastExpressionSyntax>(tree, "(S1?)x2");

            symbolInfo = model.GetSymbolInfo(cast);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            AssertEx.Equal("S1..ctor(System.Int32 x)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Empty(symbolInfo.CandidateSymbols);

            typeInfo = model.GetTypeInfo(cast);
            Assert.Equal("S1?", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("S1?", typeInfo.ConvertedType.ToTestDisplayString());

            VerifyOperationTreeForNode(comp, model, GetSyntax<ReturnStatementSyntax>(tree, "return (S1?)x2;"), """
IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return (S1?)x2;')
  ReturnedValue:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: S1?) (Syntax: '(S1?)x2')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: S1..ctor(System.Int32 x)) (OperationKind.Conversion, Type: S1, IsImplicit) (Syntax: '(S1?)x2')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False, IsUnion: True) (MethodSymbol: S1..ctor(System.Int32 x))
          Operand:
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: '(S1?)x2')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand:
                IParameterReferenceOperation: x2 (OperationKind.ParameterReference, Type: System.Byte) (Syntax: 'x2')
""");
        }

        [Fact]
        public void UnionConversion_39_Implicit_UserDefined_Conversion_Wins_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static implicit operator S1(int x)
    {
        System.Console.Write(""implicit operator "");
        return new S1(x.ToString());
    }
}

class Program
{
    static void Main()
    {
        Test1();
        Test2();
    }

    static S1? Test1()
    {
        return 10;
    }   

    static S1? Test2()
    {
        return (S1?)20;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "implicit operator string {10} implicit operator string {20}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_40_Cast_Explicit_UserDefined_Conversion_Wins_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null;
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static explicit operator S1(int x)
    {
        System.Console.Write(""explicit operator "");
        return new S1(x.ToString());
    }
}

class Program
{
    static void Main()
    {
        Test2();
    }

    static S1? Test2()
    {
        return (S1?)20;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "explicit operator string {20}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_41_Explicit_UserDefined_Conversion_Loses_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x)
    {
        System.Console.Write(""int {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;

    public static explicit operator S1(int x) => throw null;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1? Test1()
    {
        return 10;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "int {10}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_42_Under_Tuple_Conversion_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(double x)
    {
        System.Console.Write(""double {"");
        System.Console.Write((int)x);
        System.Console.Write(""} "");
    }
    public S1(string x) => throw null;
    object System.Runtime.CompilerServices.IUnion.Value => throw null;
}

class Program
{
    static void Main()
    {
        Test1((0, 10));
    }

    static (int, S1?) Test1((int, byte) x)
    {
        return x;
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "double {10}").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (System.ValueTuple<int, byte> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldfld      ""int System.ValueTuple<int, byte>.Item1""
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""byte System.ValueTuple<int, byte>.Item2""
  IL_000e:  conv.r8
  IL_000f:  newobj     ""S1..ctor(double)""
  IL_0014:  newobj     ""S1?..ctor(S1)""
  IL_0019:  newobj     ""System.ValueTuple<int, S1?>..ctor(int, S1?)""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void UnionConversion_43_From_TupleLiteral()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1((int, object) x)
    {
        System.Console.Write(""(int, object) {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1 Test1()
    {
        return (10, null);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var tuple = GetSyntax<TupleExpressionSyntax>(tree, "(10, null)");

            var symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(tuple);
            Assert.Null(typeInfo.Type);
            Assert.Equal("S1", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(tuple);
            Assert.Equal(ConversionKind.Union, conversion.Kind);
            Assert.Equal(LookupResultKind.Viable, conversion.ResultKind);
            Assert.True(conversion.IsUnion);
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", conversion.Method.ToTestDisplayString());
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", conversion.MethodSymbol.ToTestDisplayString());

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", commonConversion.MethodSymbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(int, object) {(10, )}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_44_From_TupleLiteral()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1((int, object) x)
    {
        System.Console.Write(""(int, object) {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1 Test1()
    {
        return ((byte)10, null);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var tuple = GetSyntax<TupleExpressionSyntax>(tree, "((byte)10, null)");

            var symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(tuple);
            Assert.Null(typeInfo.Type);
            Assert.Equal("S1", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(tuple);
            Assert.Equal(ConversionKind.Union, conversion.Kind);
            Assert.Equal(LookupResultKind.Viable, conversion.ResultKind);
            Assert.True(conversion.IsUnion);
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", conversion.Method.ToTestDisplayString());
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", conversion.MethodSymbol.ToTestDisplayString());

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", commonConversion.MethodSymbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(int, object) {(10, )}").VerifyDiagnostics();
        }

        [Fact]
        public void UnionConversion_45_From_TupleLiteral_ToNullableOfUnion()
        {
            var src = @"
struct S1 : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;
    public S1((int, object) x)
    {
        System.Console.Write(""(int, object) {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    public S1(string x)
    {
        System.Console.Write(""string {"");
        System.Console.Write(x);
        System.Console.Write(""} "");
        _value = x;
    }
    object System.Runtime.CompilerServices.IUnion.Value => _value;
}

class Program
{
    static void Main()
    {
        Test1();
    }

    static S1? Test1()
    {
        return ((byte)10, null);
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var tuple = GetSyntax<TupleExpressionSyntax>(tree, "((byte)10, null)");

            var symbolInfo = model.GetSymbolInfo(tuple);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);

            var typeInfo = model.GetTypeInfo(tuple);
            Assert.Null(typeInfo.Type);
            Assert.Equal("S1?", typeInfo.ConvertedType.ToTestDisplayString());

            Conversion conversion = model.GetConversion(tuple);
            Assert.Equal(ConversionKind.Union, conversion.Kind);
            Assert.Equal(LookupResultKind.Viable, conversion.ResultKind);
            Assert.True(conversion.IsUnion);
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", conversion.Method.ToTestDisplayString());
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", conversion.MethodSymbol.ToTestDisplayString());

            CommonConversion commonConversion = conversion.ToCommonConversion();

            Assert.True(commonConversion.Exists);
            Assert.True(commonConversion.IsImplicit);
            Assert.True(commonConversion.IsUnion);
            Assert.False(commonConversion.IsUserDefined);
            AssertEx.Equal("S1..ctor((System.Int32, System.Object) x)", commonConversion.MethodSymbol.ToTestDisplayString());

            CompileAndVerify(comp, expectedOutput: "(int, object) {(10, )}").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71773")]
        public void UserDefinedCast_RefStruct_Explicit()
        {
            var source = """
                class C
                {
                    S M1()
                    {
                        S s;
                        s = (S)100; // 1
                        return s;
                    }

                    S M2()
                    {
                        return (S)200; // 2
                    }

                    S M3(in int x)
                    {
                        S s;
                        s = (S)x; // 3
                        return s;
                    }

                    S M4(in int x)
                    {
                        return (S)x;
                    }

                    S M4s(scoped in int x)
                    {
                        return (S)x; // 4
                    }

                    S M5(in int x)
                    {
                        S s = (S)x;
                        return s;
                    }

                    S M5s(scoped in int x)
                    {
                        S s = (S)x;
                        return s; // 5
                    }

                    S M6()
                    {
                        S s = (S)300;
                        return s; // 6
                    }

                    void M7(in int x)
                    {
                        scoped S s;
                        s = (S)x;
                        s = (S)100;
                    }
                }

                ref struct S : System.Runtime.CompilerServices.IUnion
                {
                    public S(in int x) => throw null;
                    object System.Runtime.CompilerServices.IUnion.Value => throw null;
                }
                """;
            CreateCompilation([source, IUnionSource]).VerifyDiagnostics(
                // (6,13): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = (S)100; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "(S)100").WithArguments("S.S(in int)", "x").WithLocation(6, 13),
                // (6,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         s = (S)100; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "100").WithLocation(6, 16),
                // (12,16): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return (S)200; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "(S)200").WithArguments("S.S(in int)", "x").WithLocation(12, 16),
                // (12,19): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return (S)200; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "200").WithLocation(12, 19),
                // (18,13): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = (S)x; // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "(S)x").WithArguments("S.S(in int)", "x").WithLocation(18, 13),
                // (18,16): error CS9077: Cannot return a parameter by reference 'x' through a ref parameter; it can only be returned in a return statement
                //         s = (S)x; // 3
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "x").WithArguments("x").WithLocation(18, 16),
                // (29,16): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return (S)x; // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "(S)x").WithArguments("S.S(in int)", "x").WithLocation(29, 16),
                // (29,19): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return (S)x; // 4
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(29, 19),
                // (41,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(41, 16),
                // (47,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(47, 16));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71773")]
        public void UserDefinedCast_RefStruct_Implicit()
        {
            var source = """
                class C
                {
                    S M1()
                    {
                        S s;
                        s = 100; // 1
                        return s;
                    }

                    S M2()
                    {
                        return 200; // 2
                    }

                    S M3(in int x)
                    {
                        S s;
                        s = x; // 3
                        return s;
                    }

                    S M4(in int x)
                    {
                        return x;
                    }

                    S M4s(scoped in int x)
                    {
                        return x; // 4
                    }

                    S M5(in int x)
                    {
                        S s = x;
                        return s;
                    }

                    S M5s(scoped in int x)
                    {
                        S s = x;
                        return s; // 5
                    }

                    S M6()
                    {
                        S s = 300;
                        return s; // 6
                    }

                    void M7(in int x)
                    {
                        scoped S s;
                        s = x;
                        s = 100;
                    }
                }

                ref struct S : System.Runtime.CompilerServices.IUnion
                {
                    public S(in int x) => throw null;
                    object System.Runtime.CompilerServices.IUnion.Value => throw null;
                }
                """;
            CreateCompilation([source, IUnionSource]).VerifyDiagnostics(
                // (6,13): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         s = 100; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "100").WithLocation(6, 13),
                // (6,13): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = 100; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "100").WithArguments("S.S(in int)", "x").WithLocation(6, 13),
                // (12,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return 200; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "200").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return 200; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "200").WithArguments("S.S(in int)", "x").WithLocation(12, 16),
                // (18,13): error CS9077: Cannot return a parameter by reference 'x' through a ref parameter; it can only be returned in a return statement
                //         s = x; // 3
                Diagnostic(ErrorCode.ERR_RefReturnOnlyParameter, "x").WithArguments("x").WithLocation(18, 13),
                // (18,13): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = x; // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "x").WithArguments("S.S(in int)", "x").WithLocation(18, 13),
                // (29,16): error CS9075: Cannot return a parameter by reference 'x' because it is scoped to the current method
                //         return x; // 4
                Diagnostic(ErrorCode.ERR_RefReturnScopedParameter, "x").WithArguments("x").WithLocation(29, 16),
                // (29,16): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return x; // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "x").WithArguments("S.S(in int)", "x").WithLocation(29, 16),
                // (41,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 5
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(41, 16),
                // (47,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(47, 16));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71773")]
        public void UserDefinedCast_RefStructArgument()
        {
            var source = """
                class C
                {
                    S2 M1()
                    {
                        int x = 1;
                        S1 s1 = (S1)x;
                        return (S2)s1; // 1
                    }
                }

                ref struct S1
                {
                    public static implicit operator S1(in int x) => throw null;
                }

                ref struct S2 : System.Runtime.CompilerServices.IUnion
                {
                    public S2(S1 s1) => throw null;
                    object System.Runtime.CompilerServices.IUnion.Value => throw null;
                }
                """;
            CreateCompilation([source, IUnionSource]).VerifyDiagnostics(
                // (7,16): error CS8347: Cannot use a result of 'S2.S2(S1)' in this context because it may expose variables referenced by parameter 's1' outside of their declaration scope
                //         return (S2)s1; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "(S2)s1").WithArguments("S2.S2(S1)", "s1").WithLocation(7, 16),
                // (7,20): error CS8352: Cannot use variable 's1' in this context because it may expose referenced variables outside of their declaration scope
                //         return (S2)s1; // 1
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s1").WithArguments("s1").WithLocation(7, 20));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71773")]
        public void UserDefinedCast_StandardImplicitConversion_Input()
        {
            var source = """
                class C
                {
                    S M1()
                    {
                        S s;
                        s = 100; // 1
                        return s;
                    }

                    S M2()
                    {
                        return 200; // 2
                    }

                    S M3(in int x)
                    {
                        S s;
                        s = x; // 3
                        return s;
                    }

                    S M4(in int x)
                    {
                        return x; // 4
                    }

                    S M4s(scoped in int x)
                    {
                        return x; // 5
                    }

                    S M5(in int x)
                    {
                        S s = x;
                        return s; // 6
                    }

                    S M5s(scoped in int x)
                    {
                        S s = x;
                        return s; // 7
                    }

                    S M6()
                    {
                        S s = 300;
                        return s; // 8
                    }
                }

                ref struct S : System.Runtime.CompilerServices.IUnion
                {
                    public S(in int? x) => throw null;
                    object System.Runtime.CompilerServices.IUnion.Value => throw null;
                }
                """;
            CreateCompilation([source, IUnionSource]).VerifyDiagnostics(
                // (6,13): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         s = 100; // 1
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "100").WithLocation(6, 13),
                // (6,13): error CS8347: Cannot use a result of 'S.S(in int?)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = 100; // 1
                Diagnostic(ErrorCode.ERR_EscapeCall, "100").WithArguments("S.S(in int?)", "x").WithLocation(6, 13),
                // (12,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return 200; // 2
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "200").WithLocation(12, 16),
                // (12,16): error CS8347: Cannot use a result of 'S.S(in int?)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return 200; // 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "200").WithArguments("S.S(in int?)", "x").WithLocation(12, 16),
                // (18,13): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         s = x; // 3
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "x").WithLocation(18, 13),
                // (18,13): error CS8347: Cannot use a result of 'S.S(in int?)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         s = x; // 3
                Diagnostic(ErrorCode.ERR_EscapeCall, "x").WithArguments("S.S(in int?)", "x").WithLocation(18, 13),
                // (24,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return x; // 4
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "x").WithLocation(24, 16),
                // (24,16): error CS8347: Cannot use a result of 'S.S(in int?)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return x; // 4
                Diagnostic(ErrorCode.ERR_EscapeCall, "x").WithArguments("S.S(in int?)", "x").WithLocation(24, 16),
                // (29,16): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return x; // 5
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "x").WithLocation(29, 16),
                // (29,16): error CS8347: Cannot use a result of 'S.S(in int?)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return x; // 5
                Diagnostic(ErrorCode.ERR_EscapeCall, "x").WithArguments("S.S(in int?)", "x").WithLocation(29, 16),
                // (35,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 6
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(35, 16),
                // (41,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 7
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(41, 16),
                // (47,16): error CS8352: Cannot use variable 's' in this context because it may expose referenced variables outside of their declaration scope
                //         return s; // 8
                Diagnostic(ErrorCode.ERR_EscapeVariable, "s").WithArguments("s").WithLocation(47, 16));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71773")]
        public void UserDefinedCast_Call()
        {
            var source = """
                class C
                {
                    S M1(int x)
                    {
                        return M2(x);
                    }

                    S M2(S s) => s;
                }

                ref struct S : System.Runtime.CompilerServices.IUnion
                {
                    public S(in int x) => throw null;
                    object System.Runtime.CompilerServices.IUnion.Value => throw null;
                }
                """;
            CreateCompilation([source, IUnionSource]).VerifyDiagnostics(
                // (5,16): error CS8347: Cannot use a result of 'C.M2(S)' in this context because it may expose variables referenced by parameter 's' outside of their declaration scope
                //         return M2(x);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M2(x)").WithArguments("C.M2(S)", "s").WithLocation(5, 16),
                // (5,19): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //         return M2(x);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(5, 19),
                // (5,19): error CS8347: Cannot use a result of 'S.S(in int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return M2(x);
                Diagnostic(ErrorCode.ERR_EscapeCall, "x").WithArguments("S.S(in int)", "x").WithLocation(5, 19));
        }

        [Fact]
        public void NullableAnalysis_01_State_From_Default()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1 s = default;
        _ = s switch { int => 1, bool => 3 };
    } 

    static void Test3()
    {
#line 300
        S2 s = default;
        _ = s switch { int => 1, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (101,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(101, 15)
                );
        }

        [Fact]
        public void NullableAnalysis_02_State_From_Default()
        {
            var src = @"
#nullable enable

class S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1? s = null;
        _ = s switch { int => 1, bool => 3 };
    } 

    static void Test3()
    {
#line 300
        S2? s = null;
        _ = s switch { int => 1, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (101,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(101, 15),
                // (301,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_03_State_From_Default([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

" + typeKind + @" S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
#line 200
        _ = s switch { int => 1, bool => 3 };
    } 

    static void Test4(S2 s)
    {
#line 400
        _ = s switch { int => 1, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (200,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_04_State_From_Default_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}
class Program
{
    static void Test2(S1 s)
    {
#line 200
        _ = s switch { int => 1, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);

            // PROTOTYPE: Confirm that post-condition attributes aren't respected for the purpose of default nullability of a Union instance.
            //            We respect them for constructor invocations and conversions  
            comp.VerifyDiagnostics(
                // (200,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_05_State_From_Constructor([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        var s = new S1(1);
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        var s = new S1("""");
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        var s = new S1(x);
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        var s = new S1(x);
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        var s = new S1(x);
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 15),
                // (501,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_06_State_From_Constructor_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        var s = new S1(1);
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        var s = new S1("""");
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        var s = new S1(x);
        _ = s switch { int => 1, string => 2, bool => 3 };
        x.ToString();
    } 

    static void Test4(bool x)
    {
#line 400
        var s = new S1(x);
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        var s = new S1(x);
        _ = s switch { int => 1, string => 2, bool => 3 };
        x.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_07_State_From_Conversion([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1 s = 1;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1 s = """";
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1 s = x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        S1 s = x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1 s = x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 15),
                // (501,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_08_State_From_Conversion_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1 s = 1;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1 s = """";
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1 s = x;
        _ = s switch { int => 1, string => 2, bool => 3 };
        x.ToString();
    } 

    static void Test4(bool x)
    {
#line 400
        S1 s = x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1 s = x;
        _ = s switch { int => 1, string => 2, bool => 3 };
        x.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_09_State_From_Conversion_TupleLiteral([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        (S1, int) s = (1, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        (S1, int) s = ("""", 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        (S1, int) s = (x, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        (S1, int) s = (x, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        (S1, int) s = (x, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_10_State_From_Conversion_TupleLiteral_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        (S1, int) s = (1, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        (S1, int) s = ("""", 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        (S1, int) s = (x, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.ToString();
    } 

    static void Test4(bool x)
    {
#line 400
        (S1, int) s = (x, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        (S1, int) s = (x, 1);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_11_State_From_Conversion_TupleValue([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1((int, int) x)
    {
#line 100
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2((string, int) x)
    {
#line 200
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3((string?, int) x)
    {
#line 300
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4((bool, int) x)
    {
#line 400
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5((bool?, int) x)
    {
#line 500
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_12_State_From_Conversion_TupleValue_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1((int, int) x)
    {
#line 100
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2((string, int) x)
    {
#line 200
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3((string?, int) x)
    {
#line 300
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.Item1.ToString();
    } 

    static void Test4((bool, int) x)
    {
#line 400
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5((bool?, int) x)
    {
#line 500
        (S1, int) s = x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.Item1.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_13_State_From_Conversion_Cast([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1 s = (S1)1;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1 s = (S1)"""";
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1 s = (S1)x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        S1 s = (S1)x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1 s = (S1)x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 15),
                // (501,15): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 15)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_14_State_From_Conversion_Cast_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1 s = (S1)1;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1 s = (S1)"""";
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1 s = (S1)x;
        _ = s switch { int => 1, string => 2, bool => 3 };
        x.ToString();
    } 

    static void Test4(bool x)
    {
#line 400
        S1 s = (S1)x;
        _ = s switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1 s = (S1)x;
        _ = s switch { int => 1, string => 2, bool => 3 };
        x.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_15_State_From_Conversion_Cast_TupleLiteral([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        (S1, int) s = ((S1, int))(1, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        (S1, int) s = ((S1, int))("""", 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        (S1, int) s = ((S1, int))(x, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        (S1, int) s = ((S1, int))(x, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        (S1, int) s = ((S1, int))(x, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_16_State_From_Conversion_Cast_TupleLiteral_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        (S1, int) s = ((S1, int))(1, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        (S1, int) s = ((S1, int))("""", 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        (S1, int) s = ((S1, int))(x, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.ToString();
    } 

    static void Test4(bool x)
    {
#line 400
        (S1, int) s = ((S1, int))(x, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        (S1, int) s = ((S1, int))(x, 1.0);
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_17_State_From_Conversion_Cast_TupleValue([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1((int, long) x)
    {
#line 100
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2((string, long) x)
    {
#line 200
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3((string?, long) x)
    {
#line 300
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4((bool, long) x)
    {
#line 400
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5((bool?, long) x)
    {
#line 500
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_18_State_From_Conversion_Cast_TupleValue_PostCondition([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.NotNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1((int, long) x)
    {
#line 100
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2((string, long) x)
    {
#line 200
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3((string?, long) x)
    {
#line 300
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.Item1.ToString();
    } 

    static void Test4((bool, long) x)
    {
#line 400
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5((bool?, long) x)
    {
#line 500
        (S1, int) s = ((S1, int))x;
        _ = s.Item1 switch { int => 1, string => 2, bool => 3 };
        x.Item1.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, NotNullAttributeDefinition]);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullableAnalysis_19_State_From_Null_Test()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is null)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is null)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 19),
                // (300,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(300, 19)
                );
        }

        [Fact]
        public void NullableAnalysis_20_State_From_Null_Test_Class()
        {
            var src1 = @"
#nullable enable

class S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is null)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is null)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp1 = CreateCompilation([src1, IUnionSource]);

            // PROTOTYPE: The fact that exhausiveness warning are reported for all branches might look surprising,
            //            but it matches the behavior for scenario when property pattern is used explicitly. See below.
            //            Is this expected, a bug or do we need special rules for Unions here?
            comp1.VerifyDiagnostics(
                // (100,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 19),
                // (200,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 19),
                // (300,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(300, 19),
                // (400,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(400, 19)
                );

            var src2 = @"
#nullable enable

class S1
{
    public object? Value => throw null!;
}

class S2
{
    public object Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        _ = s.Value;
        if (s is  { Value: null })
        {
#line 1000
            _ = s switch { { Value: {} } => 1 };
        }
        else
        {
#line 2000
            _ = s switch { { Value: {} } => 1 };
        }
    } 

    static void Test4(S2 s)
    {
        _ = s.Value;
        if (s is { Value: null })
        {
#line 3000
            _ = s switch { { Value: {} } => 1 };
        }
        else
        {
#line 4000
            _ = s switch { { Value: {} } => 1 };
        }
    } 
}
";
            var comp2 = CreateCompilation(src2);

            comp2.VerifyDiagnostics(
                // (1000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(1000, 19),
                // (2000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(2000, 19),
                // (3000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(3000, 19),
                // (4000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(4000, 19)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_21_State_From_NotNull_Test([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

" + typeKind + @" S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is not null)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is not null)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (200,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 19),
                // (400,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(400, 19)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_22_State_From_Type_Test([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

" + typeKind + @" S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is int)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is int)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (200,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 19)
                );
        }

        [Fact]
        public void NullableAnalysis_23_State_From_NotType_Test()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is not int)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is not int)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 19)
                );
        }

        [Fact]
        public void NullableAnalysis_24_State_From_NotType_Test_Class()
        {
            var src = @"
#nullable enable

class S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is not int)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is not int)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);

            // PROTOTYPE: This is another case of behavior consistent with explicit property patterns. See below
            comp.VerifyDiagnostics(
                // (100,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 19),
                // (200,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 19)
                );

            var src2 = @"
#nullable enable

class S1
{
    public object? Value => throw null!;
}
class Program
{
    static void Test2(S1 s)
    {
        _ = s.Value;
        if (s is  { Value: not int })
        {
#line 1000
            _ = s switch { { Value: {} } => 1 };
        }
        else
        {
#line 2000
            _ = s switch { { Value: {} } => 1 };
        }
    } 
}
";
            var comp2 = CreateCompilation(src2);

            comp2.VerifyDiagnostics(
                // (1000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(1000, 19),
                // (2000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(2000, 19)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_25_State_From_Value_Test([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

" + typeKind + @" S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is 1)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is 1)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (200,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 19)
                );
        }

        [Fact]
        public void NullableAnalysis_26_State_From_NotValue_Test()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is not 1)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is not 1)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 19)
                );
        }

        [Fact]
        public void NullableAnalysis_27_State_From_NotValue_Test_Class()
        {
            var src = @"
#nullable enable

class S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
        if (s is not 1)
        {
#line 100
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 200
            _ = s switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2 s)
    {
        if (s is not 1)
        {
#line 300
            _ = s switch { int => 1, bool => 3 };
        }
        else
        {
#line 400
            _ = s switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);

            // PROTOTYPE: This is another case of behavior consistent with explicit property patterns. See below
            comp.VerifyDiagnostics(
                // (100,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 19),
                // (200,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 19)
                );

            var src2 = @"
#nullable enable

class S1
{
    public object? Value => throw null!;
}
class Program
{
    static void Test2(S1 s)
    {
        _ = s.Value;
        if (s is  { Value: not 1 })
        {
#line 1000
            _ = s switch { { Value: {} } => 1 };
        }
        else
        {
#line 2000
            _ = s switch { { Value: {} } => 1 };
        }
    } 
}
";
            var comp2 = CreateCompilation(src2);

            comp2.VerifyDiagnostics(
                // (1000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(1000, 19),
                // (2000,19): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s switch { { Value: {} } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(2000, 19)
                );
        }

        [Fact]
        public void NullableAnalysis_28_ValuePropertyOfTheInterfaceIsTargetedToImplementPatternMatching()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(OtherProp))]
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
    public string? OtherProp => throw null!;
}

public interface I1
{
    object? Value { get; }
}

struct S2 : I1
{
    public S2(int x) => throw null!;
    public S2(bool? x) => throw null!;
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(OtherProp))]
    object? I1.Value => throw null!;
    public string? OtherProp => throw null!;
}

class Program
{
    static void Test2(S1 s)
    {
#line 200
         _ = s switch { bool => s.OtherProp.ToString(), _ => """" };
    } 

    static void Test3(S2 s)
    {
#line 300
         _ = s switch { I1 and { Value: bool } => s.OtherProp.ToString(), _ => """" };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, MemberNotNullAttributeDefinition]);

            // From spec: The Value property of the interface is targeted by the compiler to implement pattern matching
            comp.VerifyDiagnostics(
                // (200,33): warning CS8602: Dereference of a possibly null reference.
                //          _ = s switch { bool => s.OtherProp.ToString(), _ => "" };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.OtherProp").WithLocation(200, 33),
                // (300,25): hidden CS9335: The pattern is redundant.
                //          _ = s switch { I1 and { Value: bool } => s.OtherProp.ToString(), _ => "" };
                Diagnostic(ErrorCode.HDN_RedundantPattern, "I1").WithLocation(300, 25),
                // (300,51): warning CS8602: Dereference of a possibly null reference.
                //          _ = s switch { I1 and { Value: bool } => s.OtherProp.ToString(), _ => "" };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.OtherProp").WithLocation(300, 51)
                );
        }

        [Fact]
        public void NullableAnalysis_29_ValuePropertyOfTheInterfaceIsTargetedToImplementPatternMatching()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(OtherProp))]
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
    string? System.Runtime.CompilerServices.IUnion.OtherProp => throw null!;
    public string? OtherProp => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool? x) => throw null!;
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(OtherProp))]
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
    public string? OtherProp => throw null!;
}

namespace System.Runtime.CompilerServices
{
    public interface IUnion
    {
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(OtherProp))]
        object? Value { get; }
        string? OtherProp { get; }
    }
}

class Program
{
    static void Test2(S1 s)
    {
#line 200
         _ = s switch { bool => s.OtherProp.ToString(), _ => """" };
    } 

    static void Test3(S2 s)
    {
#line 300
         _ = s switch { bool => s.OtherProp.ToString(), _ => """" };
    } 
}
";
            var comp = CreateCompilation([src, MemberNotNullAttributeDefinition]);

            // From spec: The Value property of the interface is targeted by the compiler to implement pattern matching
            comp.VerifyDiagnostics(
                // (200,33): warning CS8602: Dereference of a possibly null reference.
                //          _ = s switch { bool => s.OtherProp.ToString(), _ => "" };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.OtherProp").WithLocation(200, 33),
                // (300,33): warning CS8602: Dereference of a possibly null reference.
                //          _ = s switch { bool => s.OtherProp.ToString(), _ => "" };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.OtherProp").WithLocation(300, 33)
                );
        }

        [Fact]
        public void NullableAnalysis_30_ValuePropertyOfTheInterfaceIsTargetedToImplementPatternMatching()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(OtherProp))]
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
    public string? OtherProp => throw null!;
}

namespace System.Runtime.CompilerServices
{
    public interface IUnion
    {
#line 100
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(S1.OtherProp))]
        object? Value { get; }
    }
}

class Program
{
    static void Test2(S1 s)
    {
#line 200
         _ = s switch { bool => s.OtherProp.ToString(), _ => """" };
    } 
}
";
            var comp = CreateCompilation([src, MemberNotNullAttributeDefinition]);

            // From spec: The Value property of the interface is targeted by the compiler to implement pattern matching
            comp.VerifyDiagnostics(
                // (100,10): warning CS8776: Member 'OtherProp' cannot be used in this attribute.
                //         [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(S1.OtherProp))]
                Diagnostic(ErrorCode.WRN_MemberNotNullBadMember, "System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(S1.OtherProp))").WithArguments("OtherProp").WithLocation(100, 10),
                // (200,33): warning CS8602: Dereference of a possibly null reference.
                //          _ = s switch { bool => s.OtherProp.ToString(), _ => "" };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s.OtherProp").WithLocation(200, 33)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_31_Conversion_Value_Check([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(string x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1(string? x)
    {
#line 100
        S1 s = x;
        x.ToString();
    } 

    static void Test2(string? x)
    {
#line 200
        var s = new S1(x);
        x.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,16): warning CS8604: Possible null reference argument for parameter 'x' in 'S1.S1(string x)'.
                //         S1 s = x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "S1.S1(string x)").WithLocation(100, 16),
                // (200,24): warning CS8604: Possible null reference argument for parameter 'x' in 'S1.S1(string x)'.
                //         var s = new S1(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "S1.S1(string x)").WithLocation(200, 24)
                );
        }

        [Theory]
        [CombinatorialData]
        public void NullableAnalysis_32_Conversion_Value_Check([CombinatorialValues("class", "struct")] string typeKind)
        {
            var src = @"
#nullable enable

" + typeKind + @" S1 : System.Runtime.CompilerServices.IUnion
{
    public S1([System.Diagnostics.CodeAnalysis.DisallowNull] string? x) => throw null!;
    public S1([System.Diagnostics.CodeAnalysis.DisallowNull] bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1(string? x)
    {
#line 100
        S1 s = x;
        x.ToString();
    } 

    static void Test2(string? x)
    {
#line 200
        var s = new S1(x);
        x.ToString();
    } 

    static void Test3(bool? x)
    {
#line 300
        S1 s = x;
        x.Value.ToString();
    } 

    static void Test4(bool? x)
    {
#line 400
        var s = new S1(x);
        x.Value.ToString();
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource, DisallowNullAttributeDefinition]);
            comp.VerifyDiagnostics(
                // (100,16): warning CS8604: Possible null reference argument for parameter 'x' in 'S1.S1(string? x)'.
                //         S1 s = x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "S1.S1(string? x)").WithLocation(100, 16),
                // (200,24): warning CS8604: Possible null reference argument for parameter 'x' in 'S1.S1(string? x)'.
                //         var s = new S1(x);
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "S1.S1(string? x)").WithLocation(200, 24),
                // (300,16): warning CS8607: A possible null value may not be used for a type marked with [NotNull] or [DisallowNull]
                //         S1 s = x;
                Diagnostic(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment, "x").WithLocation(300, 16),
                // (400,24): warning CS8607: A possible null value may not be used for a type marked with [NotNull] or [DisallowNull]
                //         var s = new S1(x);
                Diagnostic(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment, "x").WithLocation(400, 24)
                );
        }

        [Fact]
        public void NullableAnalysis_33_State_From_Default_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1? s = default(S1);
        _ = s.Value switch { int => 1, bool => 3 };
    } 

    static void Test3()
    {
#line 300
        S2? s = default(S2);
        _ = s.Value switch { int => 1, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (101,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(101, 21)
                );
        }

        [Fact]
        public void NullableAnalysis_34_State_From_Default_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1? s)
    {
        if (s is null) return;
#line 200
        _ = s.Value switch { int => 1, bool => 3 };
    } 

    static void Test4(S2? s)
    {
        if (s is null) return;
#line 400
        _ = s.Value switch { int => 1, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (200,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 21)
                );
        }

        [Fact]
        public void NullableAnalysis_35_State_From_Constructor_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1? s = new S1(1);
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1? s = new S1("""");
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1? s = new S1(x);
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        S1? s = new S1(x);
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1? s = new S1(x);
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Fact]
        public void NullableAnalysis_36_State_From_Conversion_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1? s = 1;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1? s = """";
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1? s = x;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        S1? s = x;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1? s = x;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Fact]
        public void NullableAnalysis_37_State_From_Conversion_TupleLiteral_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        (S1?, int) s = (1, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        (S1?, int) s = ("""", 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        (S1?, int) s = (x, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        (S1?, int) s = (x, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        (S1?, int) s = (x, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 27),
                // (501,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 27)
                );
        }

        [Fact]
        public void NullableAnalysis_38_State_From_Conversion_TupleValue_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1((int, int) x)
    {
#line 100
        (S1?, int) s = x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2((string, int) x)
    {
#line 200
        (S1?, int) s = x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3((string?, int) x)
    {
#line 300
        (S1?, int) s = x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4((bool, int) x)
    {
#line 400
        (S1?, int) s = x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5((bool?, int) x)
    {
#line 500
        (S1?, int) s = x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 27),
                // (501,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 27)
                );
        }

        [Fact]
        public void NullableAnalysis_39_State_From_Conversion_Cast_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        S1? s = (S1?)1;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        S1? s = (S1?)"""";
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        S1? s = (S1?)x;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        S1? s = (S1?)x;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        S1? s = (S1?)x;
        _ = s.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 21),
                // (501,21): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 21)
                );
        }

        [Fact]
        public void NullableAnalysis_40_State_From_Conversion_Cast_TupleLiteral_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1()
    {
#line 100
        (S1?, int) s = ((S1?, int))(1, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2()
    {
#line 200
        (S1?, int) s = ((S1?, int))("""", 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3(string? x)
    {
#line 300
        (S1?, int) s = ((S1?, int))(x, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4(bool x)
    {
#line 400
        (S1?, int) s = ((S1?, int))(x, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5(bool? x)
    {
#line 500
        (S1?, int) s = ((S1?, int))(x, 1);
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 27),
                // (501,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 27)
                );
        }

        [Fact]
        public void NullableAnalysis_41_State_From_Conversion_Cast_TupleValue_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(string? x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1((int, int) x)
    {
#line 100
        (S1?, int) s = ((S1?, int))x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test2((string, int) x)
    {
#line 200
        (S1?, int) s = ((S1?, int))x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test3((string?, int) x)
    {
#line 300
        (S1?, int) s = ((S1?, int))x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test4((bool, int) x)
    {
#line 400
        (S1?, int) s = ((S1?, int))x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 

    static void Test5((bool?, int) x)
    {
#line 500
        (S1?, int) s = ((S1?, int))x;
        _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (301,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(301, 27),
                // (501,27): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //         _ = s.Item1.Value switch { int => 1, string => 2, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(501, 27)
                );
        }

        [Fact]
        public void NullableAnalysis_42_State_From_Null_Test_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1? s0)
    {
        if (s0 is null) return;

        if (s0.Value is null)
        {
            var s = s0;
#line 100
            _ = s.Value switch { int => 1, bool => 3 };
        }
        else
        {
            var s = s0;
#line 200
            _ = s.Value switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2? s0)
    {
        if (s0 is null) return;

        if (s0.Value is null)
        {
            var s = s0;
#line 300
            _ = s.Value switch { int => 1, bool => 3 };
        }
        else
        {
            var s = s0;
#line 400
            _ = s.Value switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (100,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s.Value switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(100, 25),
                // (300,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s.Value switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(300, 25)
                );
        }

        [Fact]
        public void NullableAnalysis_43_State_From_NotNull_Test_NullableOfUnion()
        {
            var src = @"
#nullable enable

struct S1 : System.Runtime.CompilerServices.IUnion
{
    public S1(int x) => throw null!;
    public S1(bool? x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

struct S2 : System.Runtime.CompilerServices.IUnion
{
    public S2(int x) => throw null!;
    public S2(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test2(S1? s0)
    {
        if (s0 is null) return;

        if (s0.Value is not null)
        {
            var s = s0;
#line 100
            _ = s.Value switch { int => 1, bool => 3 };
        }
        else
        {
            var s = s0;
#line 200
            _ = s.Value switch { int => 1, bool => 3 };
        }
    } 

    static void Test4(S2? s0)
    {
        if (s0 is null) return;

        if (s0.Value is not null)
        {
            var s = s0;
#line 300
            _ = s.Value switch { int => 1, bool => 3 };
        }
        else
        {
            var s = s0;
#line 400
            _ = s.Value switch { int => 1, bool => 3 };
        }
    } 
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (200,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s.Value switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(200, 25),
                // (400,25): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //             _ = s.Value switch { int => 1, bool => 3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(400, 25)
                );
        }

        [Fact]
        public void NullableAnalysis_44_Conversion_Value_Check_ReinferConstructor()
        {
            var src = @"
#nullable enable

struct S1<T> : System.Runtime.CompilerServices.IUnion
{
    public S1(T x) => throw null!;
    public S1(bool x) => throw null!;
    object? System.Runtime.CompilerServices.IUnion.Value => throw null!;
}

class Program
{
    static void Test1(string? x, string? y)
    {
        var s = GetS1(y);
#line 100
        s = x;
        x.ToString();
    } 

    static void Test2(string? x, string y)
    {
        var s = GetS1(y);
#line 200
        s = x;
        x.ToString();
    } 

    static S1<T> GetS1<T>(T x)
    {
        return default;
    }
}
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyDiagnostics(
                // (101,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(101, 9),
                // (200,13): warning CS8604: Possible null reference argument for parameter 'x' in 'S1<string>.S1(string x)'.
                //         s = x;
                Diagnostic(ErrorCode.WRN_NullReferenceArgument, "x").WithArguments("x", "S1<string>.S1(string x)").WithLocation(200, 13)
                );
        }
    }
}
