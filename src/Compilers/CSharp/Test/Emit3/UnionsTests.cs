// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
";
            var comp = CreateCompilation([src, IUnionSource]);
            comp.VerifyEmitDiagnostics();

            VerifyCaseTypes(comp, "C1", ["System.Int32"]);
            VerifyCaseTypes(comp, "C2", ["System.String"]);
            VerifyCaseTypes(comp, "C4", ["System.String"]);
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
            CompileAndVerify(comp, expectedOutput: "FalseFalseFalseTrue FalseTrueFalseFalse").VerifyDiagnostics();

            // PROTOTYPE: Note the difference in behavior between S1? and C1.
            // For S1?, 'is null' is true only when S1? itself is null value. 
            // For C1, 'is null' is true when the C1?.Value is null, it is false even for the case when C1 itself is a null reference.
            // This behavior could be very confusing.
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
    static bool Test1(S1 u)
    {
        return u is int;
    }   

    static bool Test2(S1 u)
    {
        return u is string and ['1', .., '2'];
    }   
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // PROTOTYPE: This isn't actually a type pattern, but it looks like one, and the behavior is very confusing.

                // (14,16): warning CS0184: The given expression is never of the provided ('int') type
                //         return u is int;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "u is int").WithArguments("int").WithLocation(14, 16)
                );

            var tree = comp.SyntaxTrees[0];

            Assert.Equal("u is int", tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single().ToString());
            Assert.Equal("string and ['1', .., '2']", tree.GetRoot().DescendantNodes().OfType<TypePatternSyntax>().Single().Parent.ToString());
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
}
";
            var comp = CreateCompilation([src, IUnionSource], targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? "FalseTrueTrueTrue FalseTrueTrueTrueFalse TrueTrueFalse TrueFalseTrue" : null, verify: Verification.FailsPEVerify).VerifyDiagnostics();
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
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("S1?", "int").WithLocation(34, 22)
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
            // For C1, 'is null' is false when the C1?.Value is null (i.e. either the instance or it's Value in null).
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
    }
}
