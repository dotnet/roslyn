// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StructsTests : CompilingTestBase
    {
        [WorkItem(540982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540982")]
        [Fact()]
        public void TestInitFieldStruct()
        {
            var text = @"
public struct A
{
    A a = new A();   // CS8036
    public static int Main() { return 1; }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (2,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // public struct A
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "A").WithLocation(2, 15),
                // (4,7): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     A a = new A();   // CS8036
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "a").WithArguments("struct field initializers", "10.0").WithLocation(4, 7),
                // (4,7): error CS0523: Struct member 'A.a' of type 'A' causes a cycle in the struct layout
                //     A a = new A();   // CS8036
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "a").WithArguments("A.a", "A").WithLocation(4, 7),
                // (4,7): warning CS0169: The field 'A.a' is never used
                //     A a = new A();   // CS8036
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("A.a").WithLocation(4, 7));
        }

        [WorkItem(1075325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075325"), WorkItem(343, "CodePlex")]
        [Fact()]
        public void TestInitEventStruct_01()
        {
            var text = @"
struct S {
    event System.Action E = null;

    void M()
    {
        E();
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S {
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(2, 8),
                // (3,25): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     event System.Action E = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "E").WithArguments("struct field initializers", "10.0").WithLocation(3, 25));

            CreateCompilation(text).VerifyDiagnostics(
                // (2,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S {
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(2, 8));
        }

        [Fact()]
        public void TestInitEventStruct_02()
        {
            var text = @"
struct S {
    event System.Action E = null;
    public S() { }
    void M()
    {
        E();
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (3,25): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     event System.Action E = null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "E").WithArguments("struct field initializers", "10.0").WithLocation(3, 25),
                // (4,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S").WithArguments("parameterless struct constructors", "10.0").WithLocation(4, 12));

            CreateCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(1075325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075325"), WorkItem(343, "CodePlex")]
        [Fact()]
        public void TestStaticInitInStruct()
        {
            var text = @"
struct S {
    static event System.Action E = M;
    static int F = 10;
    static int P {get; set;} = 20;

    static void M()
    {
    }

    static void Main()
    {
        System.Console.WriteLine(""{0} {1} {2}"", S.F, S.P, S.E == null);
    }
}
";
            var comp = CreateCompilation(text, options: TestOptions.DebugExe);

            CompileAndVerify(comp, expectedOutput: "10 20 False").VerifyDiagnostics();
        }

        // Test constructor forwarding works for structs
        [WorkItem(540896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540896")]
        [Fact]
        public void TestConstructorStruct()
        {
            var text = @"
struct  Goo
{
    public Goo(int x) : this(5, 6)
    {
    }
    public Goo(int x, int y) 
    {
        m_x = x;
        m_y = y;
    }
    public int m_x;
    public int m_y;
    public static void Main()
    { }
}
";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        // Calling struct default constructor in another constructor
        [WorkItem(540896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540896")]
        [Fact]
        public void TestConstructorStruct02()
        {
            var text = @"
public struct Struct
{
    public int x;
    public Struct(int x) : this()
    {
        this.x = x;
    }
    public static void Main()
    {
    }
}
";
            CompileAndVerify(text).VerifyDiagnostics();
        }

        // Test constructor forwarding works for structs
        [WorkItem(540896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540896")]
        [Fact]
        public void TestConstructorStruct03()
        {
            var text = @"
struct S
{
    public int i;
    public int j;
    public S(int x)
    {
        j = i = x;
        Init(x);
    }
    void Init(int x)
    {
    }
    public void Set(S s)
    {
        s.Copy(out this);
    }
    public void CopySelf()
    {
        this.Copy(out this);
    }
    public void Copy(out S s)
    {
        s = this;
    }
}

class Program
{ 
    static void Main(string[] args)
    {
        S s;
        s.i = 0;
        s.j = 1;
        S s2 = s;
        s2.i = 2;
        s.Set(s2);
        System.Console.Write(s.i);
        s.CopySelf();
        System.Console.Write(s.i);
    }
}
";
            CompileAndVerify(text, expectedOutput: "22").VerifyDiagnostics();
        }

        // Overriding base System.Object methods on struct
        [WorkItem(20496, "https://github.com/dotnet/roslyn/issues/20496")]
        [WorkItem(540990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540990")]
        [ClrOnlyFact(ClrOnlyReason.MemberOrder)]
        public void TestOverridingBaseConstructorStruct()
        {
            var text = @"
using System;
public struct Gen<T>
{
    public override bool Equals(object obj)
    {
        Console.WriteLine(""Gen{0}::Equals"", typeof(T));
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        Console.WriteLine(""Gen{0}::GetHashCode"", typeof(T));
        return base.GetHashCode();
    }
    public override string ToString()
    {
        Console.WriteLine(""Gen{0}::ToString"", typeof(T));
        return base.ToString();
    }
}
public struct S
{
    public override bool Equals(object obj)
    {
        Console.WriteLine(""S::Equals"");
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        Console.WriteLine(""S::GetHashCode"");
        return base.GetHashCode();
    }
    public override string ToString()
    {
        Console.WriteLine(""S::ToString"");
        return base.ToString();
    }
}
public class Test
{
    public static int counter = 0;
    public static bool result = true;
    public static void Eval(bool exp)
    {
        counter++;
        if (!exp)
        {
            result = exp;
            Console.WriteLine(""Test Failed at location: "" + counter);
        }
    }
    public static void Main()
    {
        Gen<int> gInt = new Gen<int>();
        Test.Eval(gInt.Equals(null) == false);
        Test.Eval(gInt.GetHashCode() == gInt.GetHashCode());
        Test.Eval(gInt.ToString() == ""Gen`1[System.Int32]"");
        Gen<object> gObject = new Gen<object>();
        Test.Eval(gObject.Equals(null) == false);
        Test.Eval(gObject.GetHashCode() == gObject.GetHashCode());
        Test.Eval(gObject.ToString() == ""Gen`1[System.Object]"");
        S s = new S();
        Test.Eval(s.Equals(null) == false);
        Test.Eval(s.GetHashCode() == s.GetHashCode());
        Test.Eval(s.ToString() == ""S"");
    }
}
";
            var expectedOutput = @"GenSystem.Int32::Equals
GenSystem.Int32::GetHashCode
GenSystem.Int32::GetHashCode
GenSystem.Int32::ToString
GenSystem.Object::Equals
GenSystem.Object::GetHashCode
GenSystem.Object::GetHashCode
GenSystem.Object::ToString
S::Equals
S::GetHashCode
S::GetHashCode
S::ToString";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Test constructor for generic struct
        [WorkItem(540993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540993")]
        [Fact]
        public void TestConstructorForGenericStruct()
        {
            var text = @"
using System;
struct C<T>
{
    public int num;
    public int Goo1()
    {
        return this.num;
    }
}
class Test
{
    static void Main(string[] args)
    {
        C<object> c;
        c.num = 1;
        bool verify = c.Goo1() == 1;
        Console.WriteLine(verify);
    }
}
";
            var expectedOutput = @"True";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Assign to decimal in struct constructor
        [WorkItem(540994, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540994")]
        [Fact]
        public void TestAssigntoDecimalInStructConstructor()
        {
            var text = @"
using System;
public struct Struct
{
    public decimal Price;
    public Struct(decimal price)
    {
        Price = price;
    }
}
class Test
{
    public static void Main()
    {
    }
}
";
            var expectedIL = @"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""decimal Struct.Price""
  IL_0007:  ret
}";
            CompileAndVerify(text).VerifyIL("Struct..ctor(decimal)", expectedIL);
        }

        [Fact]
        public void RetargetedSynthesizedStructConstructor()
        {
            var oldMsCorLib = Net40.References.mscorlib;

            var c1 = CSharpCompilation.Create("C1",
                new[] { Parse(@"public struct S { }") },
                new[] { oldMsCorLib },
                TestOptions.ReleaseDll);

            var c2 = CSharpCompilation.Create("C2",
                new[] { Parse(@"public class C { void M() { S s = new S(); System.Console.WriteLine(s);} }") },
                new[] { MscorlibRef, new CSharpCompilationReference(c1) },
                TestOptions.ReleaseDll);

            var c1AsmRef = c2.GetReferencedAssemblySymbol(new CSharpCompilationReference(c1));

            Assert.NotSame(c1.Assembly, c1AsmRef);

            var mscorlibAssembly = c2.GetReferencedAssemblySymbol(MscorlibRef);

            Assert.NotSame(mscorlibAssembly, c1.GetReferencedAssemblySymbol(oldMsCorLib));

            var @struct = c2.GlobalNamespace.GetMember<RetargetingNamedTypeSymbol>("S");
            var method = (RetargetingMethodSymbol)@struct.GetMembers().Single();

            Assert.True(method.IsDefaultValueTypeConstructor());

            //TODO (tomat)
            CompileAndVerify(c2).VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  box        ""S""
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void SubstitutedSynthesizedStructConstructor()
        {
            string text = @"
public struct S<T>
{
}

public class C 
{ 
    void M() 
    { 
        S<int> s = new S<int>(); 
        System.Console.WriteLine(s);
    }
}
";

            CompileAndVerify(text).VerifyIL("C.M", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (S<int> V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S<int>""
  IL_0008:  ldloc.0
  IL_0009:  box        ""S<int>""
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ret
}");
        }

        [Fact]
        public void PublicParameterlessConstructorInMetadata()
        {
            string ilSource = @"
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method public hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
  {
    ret
  }
}
";

            string csharpSource = @"
public class C 
{ 
    void M() 
    { 
        S s = new S();
        System.Console.WriteLine(s);
        s = default(S);
        System.Console.WriteLine(s);
    }
}
";

            // Calls constructor (vs initobj), then initobj
            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            // TODO (tomat)
            CompileAndVerify(compilation).VerifyIL("C.M", @"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  newobj     ""S..ctor()""
  IL_0005:  box        ""S""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""S""
  IL_0017:  ldloc.0
  IL_0018:  box        ""S""
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  ret
}");
        }

        [WorkItem(541309, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541309")]
        [Fact]
        public void PrivateParameterlessConstructorInMetadata()
        {
            string ilSource = @"
.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1

  .method private hidebysig specialname rtspecialname 
        instance void  .ctor() cil managed
  {
    ret
  }
}
";

            string csharpSource = @"
public class C 
{ 
    void M() 
    { 
        S s = new S();
        System.Console.WriteLine(s);
        s = default(S);
        System.Console.WriteLine(s);
    }
}
";

            // Uses initobj for both
            // CONSIDER: This is the dev10 behavior, but it seems like a bug.
            // Shouldn't there be an error for trying to call an inaccessible ctor?
            var comp = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);
            CompileAndVerify(comp).VerifyIL("C.M", @"
{
  // Code size       39 (0x27)
  .maxstack  1
  .locals init (S V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  ldloc.0
  IL_0009:  box        ""S""
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ldloca.s   V_0
  IL_0015:  initobj    ""S""
  IL_001b:  ldloc.0
  IL_001c:  box        ""S""
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  ret
}");
        }

        [WorkItem(543934, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543934")]
        [Fact]
        public void ObjectCreationExprStructTypeInstanceFieldAssign()
        {
            var csSource = @"
public struct TestStruct
{
    public int IntI;
}

public class TestClass
{
    public static void Main()
    {
        new TestStruct().IntI = 3;
    }
}
";
            CreateCompilation(csSource).VerifyDiagnostics(
                // (13,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new TestStruct().IntI")
                );
        }

        [WorkItem(543896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543896")]
        [Fact]
        public void ObjectCreationExprStructTypePropertyAssign()
        {
            var csSource = @"
public struct S
{
    int n;
    public int P 
    {
        set { n = value; System.Console.WriteLine(n); } 
    }
}
public class mem033
{
    public static void Main()
    {
        new S().P = 1; // CS0131 
    }
}";
            CreateCompilation(csSource).VerifyDiagnostics(
                // (14,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         new S().P = 1; // CS0131 
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "new S().P")
                );
        }

        [WorkItem(545498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545498")]
        [Fact]
        public void StructMemberNullableTypeCausesCycle()
        {
            string source = @"
public struct X
{
    public X? recursiveFld;
}
";
            CreateCompilation(source, targetFramework: TargetFramework.Mscorlib461).VerifyDiagnostics(
                // (4,15): error CS0523: Struct member 'X.recursiveFld' of type 'X?' causes a cycle in the struct layout
                //     public X? recursiveFld;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "recursiveFld").WithArguments("X.recursiveFld", "X?")
                );
        }

        [Fact]
        public void StructParameterlessCtorNotPublic()
        {
            string source = @"
public struct X
{
    private X()
    {
    }
}

public struct X1
{
    X1()
    {
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (4,13): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     private X()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X").WithArguments("parameterless struct constructors", "10.0").WithLocation(4, 13),
                // (4,13): error CS8938: The parameterless struct constructor must be 'public'.
                //     private X()
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "X").WithLocation(4, 13),
                // (11,5): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     X1()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "X1").WithArguments("parameterless struct constructors", "10.0").WithLocation(11, 5),
                // (11,5): error CS8938: The parameterless struct constructor must be 'public'.
                //     X1()
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "X1").WithLocation(11, 5));

            CreateCompilation(source).VerifyDiagnostics(
                // (4,13): error CS8918: The parameterless struct constructor must be 'public'.
                //     private X()
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "X").WithLocation(4, 13),
                // (11,5): error CS8918: The parameterless struct constructor must be 'public'.
                //     X1()
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "X1").WithLocation(11, 5));
        }

        [Fact]
        public void StructNonAutoPropertyInitializer()
        {
            var text = @"struct S
{
    public int I { get { throw null; } set {} } = 9;
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(1, 8),
                // (3,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int I { get { throw null; } set {} } = 9;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I").WithArguments("struct field initializers", "10.0").WithLocation(3, 16),
                // (3,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int I { get { throw null; } set {} } = 9;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "I").WithLocation(3, 16));

            comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (1,8): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // struct S
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(1, 8),
                // (3,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int I { get { throw null; } set {} } = 9;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "I").WithLocation(3, 16));
        }
    }
}
