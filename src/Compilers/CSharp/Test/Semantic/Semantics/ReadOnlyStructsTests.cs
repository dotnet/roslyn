// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class ReadOnlyStructsTests : CompilingTestBase
    {
        [Fact()]
        public void WriteableInstanceAutoPropsInRoStructs()
        {
            var text = @"
public readonly struct A
{
    // ok   - no state
    int ro => 5;

    // ok   - ro state
    int ro1 {get;}

    // error
    int rw {get; set;}

    // ok    - static
    static int rws {get; set;}
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,9): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                //     int rw {get; set;}
                Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "rw").WithLocation(11, 9)
    );
        }

        [Fact()]
        public void WriteableInstanceFieldsInRoStructs()
        {
            var text = @"
public readonly struct A
{
    // ok
    public static int s;

    // ok
    public readonly int ro;

    // error
    int x;    

    void AssignField()
    {
        // error
        this.x = 1;

        A a = default;
        // OK
        a.x = 2;
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (11,9): error CS8340: Instance fields of readonly structs must be readonly.
                //     int x;    
                Diagnostic(ErrorCode.ERR_FieldsInRoStruct, "x").WithLocation(11, 9),
                // (16,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this.x = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this.x").WithArguments("this").WithLocation(16, 9)
    );
        }

        [Fact()]
        public void EventsInRoStructs()
        {
            var text = @"
using System;

public readonly struct A : I1
{
    //error
    public event System.Action e;

    //error
    public event Action ei1;

    //ok
    public static event Action es;

    A(int arg)
    {
        // ok
        e = () => { };
        ei1 = () => { };
        es = () => { };
        
        // ok
        M1(ref e);
    }

    //ok
    event Action I1.ei2
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    void AssignEvent()
    {
        // error
        e = () => { };

        // error
        M1(ref e);
    }

    static void M1(ref System.Action arg)
    {
    }
}

interface I1
{
    event System.Action ei1;
    event System.Action ei2;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (7,32): error CS8342: Field-like events are not allowed in readonly structs.
                //     public event System.Action e;
                Diagnostic(ErrorCode.ERR_FieldlikeEventsInRoStruct, "e").WithLocation(7, 32),
                // (10,25): error CS8342: Field-like events are not allowed in readonly structs.
                //     public event Action ei1;
                Diagnostic(ErrorCode.ERR_FieldlikeEventsInRoStruct, "ei1").WithLocation(10, 25),
                // (43,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         e = () => { };
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "e").WithArguments("this").WithLocation(43, 9),
                // (46,16): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         M1(ref e);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "e").WithArguments("this").WithLocation(46, 16)
            );
        }

        [Fact]
        public void WriteableInstanceFields_ReadOnlyMethod()
        {
            var text = @"
public struct A
{
    public static int s;

    public int x;

    readonly void AssignField()
    {
        // error
        this.x = 1;

        A a = default;
        // OK
        a.x = 3;
        // OK
        s = 5;
    }
}
";
            // PROTOTYPE: should give ERR_AssgReadonlyLocal
            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyStruct_PassThisByRef()
        {
            var csharp = @"
public readonly struct S
{
    public static void M1(ref S s) {}
    public static void M2(in S s) {}

    public void M3()
    {
        M1(ref this); // error
        M2(in this); // ok
    }

    public readonly void M4()
    {
        M1(ref this); // error
        M2(in this); // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (9,16): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         M1(ref this); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(9, 16),
                // (15,16): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         M1(ref this); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(15, 16));
        }

        [Fact]
        public void ReadOnlyMethod_PassThisByRef()
        {
            var csharp = @"
public struct S
{
    public static void M1(ref S s) {}
    public static void M2(in S s) {}

    public void M3()
    {
        M1(ref this); // ok
        M2(in this); // ok
    }

    public readonly void M4()
    {
        M1(ref this); // error
        M2(in this); // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            // PROTOTYPE: should give ERR_RefReadonlyLocal
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_PassFieldByRef()
        {
            var csharp = @"
public struct S
{
    public static int f1;
    public int f2;

    public static void M1(ref int s) {}
    public static void M2(in int s) {}

    public void M3()
    {
        M1(ref f1); // ok
        M1(ref f2); // ok
        M2(in f1); // ok
        M2(in f2); // ok
    }

    public readonly void M4()
    {
        M1(ref f1); // ok
        M1(ref f2); // error
        M2(in f1); // ok
        M2(in f2); // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            // PROTOTYPE: should give ERR_RefReadonlyLocal
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_CallStaticMethod()
        {
            var csharp = @"
public struct S
{
    public static int i;
    public readonly int M1() => M2() + 1;
    public static int M2() => i;
}
";
            var comp = CompileAndVerify(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyAccessor_CallNormalMethod()
        {
            var csharp = @"
public struct S
{
    public int i;

    public int P
    {
        readonly get
        {
            // should create local copy
            M();
            System.Console.Write(i);

            // explicit local copy, no warning
            var copy = this;
            copy.M();
            System.Console.Write(copy.i);

            return i;
        }
    }

    void M()
    {
        i = 23;
    }

    static void Main()
    {
        var s = new S { i = 1 };
        _ = s.P;
    }
}
";

            var verifier = CompileAndVerify(csharp, expectedOutput: "123");
            // PROTOTYPE: should warn about copying 'this' when calling M
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyStruct_CallNormalMethodOnField()
        {
            var csharp = @"
public readonly struct S1
{
    public readonly S2 s2;
    public void M1()
    {
        s2.M2();
    }
}

public struct S2
{
    public int i;
    public void M2()
    {
        i = 42;
    }

    static void Main()
    {
        var s1 = new S1();
        s1.M1();
        System.Console.Write(s1.s2.i);
    }
}
";
            // should warn about calling s2.M2 in warning wave (see https://github.com/dotnet/roslyn/issues/33968)
            CompileAndVerify(csharp, expectedOutput: "0");
        }

        [Fact]
        public void ReadOnlyMethod_CallNormalMethodOnField()
        {
            var csharp = @"
public struct S1
{
    public S2 s2;
    public readonly void M1()
    {
        s2.M2();
        System.Console.Write(s2.i);

        var copy = s2;
        copy.M2();
        System.Console.Write(copy.i);
    }
}

public struct S2
{
    public int i;
    public void M2()
    {
        i = 23;
    }

    static void Main()
    {
        var s1 = new S1() { s2 = new S2 { i = 1 } };
        s1.M1();
    }
}
";
            var verifier = CompileAndVerify(csharp, expectedOutput: "123");
            // should warn about calling s2.M2 in warning wave (see https://github.com/dotnet/roslyn/issues/33968)
            verifier.VerifyDiagnostics();
        }

        private static string ilreadonlyStructWithWriteableFieldIL = @"
.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method EmbeddedAttribute::.ctor

} // end of class Microsoft.CodeAnalysis.EmbeddedAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = ( 01 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  ret
  } // end of method IsReadOnlyAttribute::.ctor

} // end of class System.Runtime.CompilerServices.IsReadOnlyAttribute



.class public sequential ansi sealed beforefieldinit S1
       extends [mscorlib]System.ValueType
{
  .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 ) 

   // WRITEABLE FIELD!!!
  .field public int32 'field'

} // end of class S1

";

        [Fact()]
        public void UseWriteableInstanceFieldsInRoStructs()
        {
            var csharp = @"
public class Program 
{ 
    public static void Main() 
    { 
        S1 s = new S1();
        s.field = 123;
        System.Console.WriteLine(s.field);
    }
}
";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, ilreadonlyStructWithWriteableFieldIL, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "123");
        }

        [Fact()]
        public void UseWriteableInstanceFieldsInRoStructsErr()
        {
            var csharp = @"
public class Program 
{ 
    static readonly S1 s = new S1();

    public static void Main() 
    { 
        s.field = 123;
        System.Console.WriteLine(s.field);
    }
}
";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, ilreadonlyStructWithWriteableFieldIL, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): error CS1650: Fields of static readonly field 'Program.s' cannot be assigned to (except in a static constructor or a variable initializer)
                //         s.field = 123;
                Diagnostic(ErrorCode.ERR_AssgReadonlyStatic2, "s.field").WithArguments("Program.s").WithLocation(8, 9)
                );
        }

        // PROTOTYPE: readonly members features should require C# 8.0 or greater

        [Fact]
        public void ReadOnlyStructMethod()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly int M()
    {
        return i;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();

            var method = comp.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("M");
            Assert.True(method.IsDeclaredReadOnly);
            Assert.True(method.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyClass()
        {
            var csharp = @"
using System;

public readonly class C
{
    public readonly int M() => 42;
    public readonly int P { get; set; }
    public readonly int this[int i] => i;
    public readonly event Action<EventArgs> E { add {} remove {} }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,23): error CS0106: The modifier 'readonly' is not valid for this item
                // public readonly class C
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("readonly").WithLocation(4, 23),
                // (6,25): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly int M() => 42;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("readonly").WithLocation(6, 25),
                // (7,25): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly int P { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P").WithArguments("readonly").WithLocation(7, 25),
                // (8,25): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly int this[int i] => i;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("readonly").WithLocation(8, 25),
                // (9,45): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly event Action<EventArgs> E { add {} remove {} }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("readonly").WithLocation(9, 45));
        }

        [Fact]
        public void ReadOnlyInterface()
        {
            var csharp = @"
using System;

public readonly interface I
{
    readonly int M();
    readonly int P { get; set; }
    readonly int this[int i] { get; }
    readonly event Action<EventArgs> E;
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,27): error CS0106: The modifier 'readonly' is not valid for this item
                // public readonly interface I
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I").WithArguments("readonly").WithLocation(4, 27),
                // (6,18): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly int M();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("readonly").WithLocation(6, 18),
                // (7,18): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly int P { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P").WithArguments("readonly").WithLocation(7, 18),
                // (8,18): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly int this[int i] { get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("readonly").WithLocation(8, 18),
                // (9,38): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly event Action<EventArgs> E;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("readonly").WithLocation(9, 38));
        }

        [Fact]
        public void ReadOnlyEnum()
        {
            var csharp = @"
public readonly enum E
{
    readonly A, readonly B
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (2,22): error CS0106: The modifier 'readonly' is not valid for this item
                // public readonly enum E
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("readonly").WithLocation(2, 22),
                // (3,2): error CS1041: Identifier expected; 'readonly' is a keyword
                // {
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "readonly").WithLocation(3, 2),
                // (4,17): error CS1041: Identifier expected; 'readonly' is a keyword
                //     readonly A, readonly B;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "readonly").WithArguments("", "readonly").WithLocation(4, 17));
        }

        [Fact]
        public void ReadOnlyStructStaticMethod()
        {
            var csharp = @"
public struct S
{
    public static int i;
    public static readonly int M()
    {
        return i;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (5,32): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly int M()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("readonly").WithLocation(5, 32));
        }

        [Fact]
        public void ReadOnlyStructProperty()
        {
            var csharp = @"
public struct S
{
    public int i;
    public int P
    {
        readonly get
        {
            return i;
        }
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyStructStaticProperty()
        {
            var csharp = @"
public struct S
{
    public static int i;
    public static int P
    {
        readonly get
        {
            return i;
        }
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (7,18): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly get
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("readonly").WithLocation(7, 18));
        }

        [Fact]
        public void ReadOnlyStructStaticExpressionProperty()
        {
            var csharp = @"
public struct S
{
    public static int i;
    public static readonly int P => i;
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (5,32): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly int P => i;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P").WithArguments("readonly").WithLocation(5, 32));
        }

        [Fact]
        public void ReadOnlyStructExpressionProperty()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly int P => i;
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyStructBlockProperty()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly int P { get { return i; } }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyAutoProperty()
        {
            var csharp = @"
public struct S
{
    public int P1 { readonly get; }
    public readonly int P2 { get; }
    public int P3 { readonly get; set; }
    public int P4 { readonly get; readonly set; } // PROTOTYPE: readonly set on an auto-property should give an error
    public readonly int P5 { get; set; } // PROTOTYPE: readonly set on an auto-property should give an error
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyProperty_RedundantReadOnlyAccessor()
        {
            var csharp = @"
public struct S
{
    public readonly int P { readonly get; }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,38): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly int P { readonly get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("readonly").WithLocation(4, 38));
        }

        [Fact]
        public void ReadOnlyStaticAutoProperty()
        {
            var csharp = @"
public struct S
{
    public static readonly int P1 { get; set; }
    public static int P2 { readonly get; }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,32): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly int P1 { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "P1").WithArguments("readonly").WithLocation(4, 32),
                // (5,37): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static int P2 { readonly get; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "get").WithArguments("readonly").WithLocation(5, 37));
        }

        [Fact]
        public void RefReturningReadOnlyMethod()
        {
            // PROTOTYPE: would be good to add some more mutation here
            // as well as expected diagnostics once that part of the feature is ready.
            var csharp = @"
public struct S
{
    private static int f1;
    public readonly ref int M1() => ref f1;

    private static readonly int f2;
    public readonly ref readonly int M2() => ref f2;

    private static readonly int f3;
    public ref readonly int M3()
    {
        f1++;
        return ref f3;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyConstructor()
        {
            var csharp = @"
public struct S
{
    static readonly S() { }
    public readonly S(int i) { }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,21): error CS0106: The modifier 'readonly' is not valid for this item
                //     static readonly S() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("readonly").WithLocation(4, 21),
                // (5,21): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly S(int i) { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("readonly").WithLocation(5, 21));
        }

        [Fact]
        public void ReadOnlyDestructor()
        {
            var csharp = @"
public struct S
{
    readonly ~S() { }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,15): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly ~S() { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("readonly").WithLocation(4, 15),
                // (4,15): error CS0575: Only class types can contain destructors
                //     readonly ~S() { }
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "S").WithArguments("S.~S()").WithLocation(4, 15));
        }

        [Fact]
        public void ReadOnlyOperator()
        {
            var csharp = @"
public struct S
{
    public static readonly S operator +(S lhs, S rhs) => lhs;
    public static readonly explicit operator int(S s) => 42;
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,39): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly S operator +(S lhs, S rhs) => lhs;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("readonly").WithLocation(4, 39),
                // (5,46): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly explicit operator int(S s) => 42;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "int").WithArguments("readonly").WithLocation(5, 46));
        }

        [Fact]
        public void ReadOnlyDelegate()
        {
            var csharp = @"
public readonly delegate int Del();
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (2,30): error CS0106: The modifier 'readonly' is not valid for this item
                // public readonly delegate int Del();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "Del").WithArguments("readonly").WithLocation(2, 30));
        }

        [Fact]
        public void ReadOnlyIndexer()
        {
            var csharp = @"
public struct S1
{
    public readonly int this[int i]
    {
        get => 42;
        set {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyExpressionIndexer()
        {
            var csharp = @"
public struct S1
{
    public readonly int this[int i] => 42;
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyGetExpressionIndexer()
        {
            var csharp = @"
public struct S1
{
    public int this[int i]
    {
        readonly get => 42;
        readonly set {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyFieldLikeEvent()
        {
            var csharp = @"
using System;

public struct S1
{
    public readonly event Action<EventArgs> E;
    public void M() { E?.Invoke(new EventArgs()); }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (6,45): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly event Action<EventArgs> E;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("readonly").WithLocation(6, 45));
        }

        [Fact]
        public void ReadOnlyEventExplicitAddRemove()
        {
            var csharp = @"
using System;

public struct S1
{
    public readonly event Action<EventArgs> E
    {
        add {}
        remove {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyStaticEvent()
        {
            var csharp = @"
using System;

public struct S1
{
    public static readonly event Action<EventArgs> E
    {
        add {}
        remove {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (6,52): error CS0106: The modifier 'readonly' is not valid for this item
                //     public static readonly event Action<EventArgs> E
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("readonly").WithLocation(6, 52));
        }

        [Fact]
        public void ReadOnlyEventReadOnlyAccessors()
        {
            var csharp = @"
using System;

public struct S1
{
    public event Action<EventArgs> E
    {
        readonly add {}
        readonly remove {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (8,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         readonly add {}
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "readonly").WithLocation(8, 9),
                // (9,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         readonly remove {}
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "readonly").WithLocation(9, 9));
        }
    }
}
