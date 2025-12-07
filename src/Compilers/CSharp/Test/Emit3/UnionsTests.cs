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
    }
}
