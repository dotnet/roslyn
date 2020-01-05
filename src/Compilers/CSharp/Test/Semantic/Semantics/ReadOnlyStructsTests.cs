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
        this = default;
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
                //         this = default;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(16, 9),
                // (18,9): error CS1604: Cannot assign to 'this.x' because it is read-only
                //         this.x = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this.x").WithArguments("this.x").WithLocation(18, 9));
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
                // (43,9): error CS1604: Cannot assign to 'e' because it is read-only
                //         e = () => { };
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "e").WithArguments("e").WithLocation(43, 9),
                // (46,16): error CS1605: Cannot use 'e' as a ref or out value because it is read-only
                //         M1(ref e);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "e").WithArguments("e").WithLocation(46, 16)
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
        this = default;
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
            CreateCompilation(text).VerifyDiagnostics(
                // (11,9): error CS1604: Cannot assign to 'this' because it is read-only
                //         this = default;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this").WithArguments("this").WithLocation(11, 9),
                // (13,9): error CS1604: Cannot assign to 'this.x' because it is read-only
                //         this.x = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "this.x").WithArguments("this.x").WithLocation(13, 9));
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
            comp.VerifyDiagnostics(
                // (15,16): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         M1(ref this); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(15, 16));
        }

        [Fact]
        public void ReadOnlyMethod_RefLocal()
        {
            var csharp = @"
public struct S
{
    public void M1()
    {
        ref S s1 = ref this; // ok
        ref readonly S s2 = ref this; // ok
    }

    public readonly void M2()
    {
        ref S s1 = ref this; // error
        ref readonly S s2 = ref this; // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (12,24): error CS1605: Cannot use 'this' as a ref or out value because it is read-only
                //         ref S s1 = ref this; // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "this").WithArguments("this").WithLocation(12, 24));
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
            comp.VerifyDiagnostics(
                // (21,16): error CS1605: Cannot use 'f2' as a ref or out value because it is read-only
                //         M1(ref f2); // error
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "f2").WithArguments("f2").WithLocation(21, 16));
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
        public void ReadOnlyMethod_Override_AssignThis()
        {
            var csharp = @"
public struct S
{
    public int i;

    // error
    public readonly override string ToString() => (i++).ToString();
    public readonly override int GetHashCode() => (i++).GetHashCode();
    public readonly override bool Equals(object o) => (i++).Equals(o);
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (7,52): error CS1604: Cannot assign to 'i' because it is read-only
                //     public readonly override string ToString() => (i++).ToString();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(7, 52),
                // (8,52): error CS1604: Cannot assign to 'i' because it is read-only
                //     public readonly override int GetHashCode() => (i++).GetHashCode();
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(8, 52),
                // (9,56): error CS1604: Cannot assign to 'i' because it is read-only
                //     public readonly override bool Equals(object o) => (i++).Equals(o);
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(9, 56));
        }

        [Fact]
        public void ReadOnlyMethod_Partial_01()
        {
            var csharp = @"
public partial struct S
{
    public int i;
    readonly partial void M();
}

public partial struct S
{
    readonly partial void M()
    {
        i++;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (12,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(12, 9));

            var method = comp.GetMember<NamedTypeSymbol>("S").GetMethod("M");
            Assert.True(method.IsDeclaredReadOnly);
            Assert.True(method.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyMethod_Partial_02()
        {
            var csharp = @"
public partial struct S
{
    public int i;
    partial void M();
}

public partial struct S
{
    readonly partial void M()
    {
        i++;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (10,27): error CS8662: Both partial method declarations must be readonly or neither may be readonly
                //     readonly partial void M()
                Diagnostic(ErrorCode.ERR_PartialMethodReadOnlyDifference, "M").WithLocation(10, 27),
                // (12,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(12, 9));

            var method = comp.GetMember<NamedTypeSymbol>("S").GetMethod("M");
            // Symbol APIs always return the declaration part of the partial method.
            Assert.False(method.IsDeclaredReadOnly);
            Assert.False(method.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyMethod_Partial_03()
        {
            var csharp = @"
public partial struct S
{
    public int i;
    readonly partial void M();
}

public partial struct S
{
    partial void M()
    {
        i++;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (10,18): error CS8662: Both partial method declarations must be readonly or neither may be readonly
                //     partial void M()
                Diagnostic(ErrorCode.ERR_PartialMethodReadOnlyDifference, "M").WithLocation(10, 18));

            var method = comp.GetMember<NamedTypeSymbol>("S").GetMethod("M");
            // Symbol APIs always return the declaration part of the partial method.
            Assert.True(method.IsDeclaredReadOnly);
            Assert.True(method.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyMethod_ExplicitInterfaceImplementation()
        {
            var csharp = @"
public interface I
{
    void M();
}

public struct S : I
{
    int i;
    readonly void I.M()
    {
        i = 0;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (12,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(12, 9));
        }

        [Fact]
        public void ReadOnlyMethod_GetPinnableReference()
        {
            var csharp = @"
public unsafe struct S1
{
    static int i = 0;
    ref int GetPinnableReference() => ref i;
    void M1()
    {
        fixed (int *i = this) {} // ok
    }
    readonly void M2()
    {
        fixed (int *i = this) {} // warn
    }
}

public unsafe struct S2
{
    static int i = 0;
    readonly ref int GetPinnableReference() => ref i;
    void M1()
    {
        fixed (int *i = this) {} // ok
    }
    readonly void M2()
    {
        fixed (int *i = this) {} // ok
    }
}
";
            var comp = CreateCompilation(csharp, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (12,25): warning CS8655: Call to non-readonly member 'S1.GetPinnableReference()' from a 'readonly' member results in an implicit copy of 'this'.
                //         fixed (int *i = this) {} // warn
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S1.GetPinnableReference()", "this").WithLocation(12, 25));
        }

        [Fact]
        public void ReadOnlyMethod_Iterator()
        {
            var csharp = @"
using System.Collections.Generic;

public struct S
{
    public int i;
    public readonly IEnumerable<int> M1()
    {
        yield return i;
        yield return i+1;
    }

    public readonly IEnumerable<int> M2()
    {
        yield return i;
        i++;
        yield return i;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (16,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(16, 9));
        }

        [Fact]
        public void ReadOnlyMethod_Async()
        {
            var csharp = @"
using System.Threading.Tasks;

public struct S
{
    public int i;
    public readonly async Task<int> M1()
    {
        await Task.Delay(1);
        return i;
    }

    public readonly async Task<int> M2()
    {
        await Task.Delay(1);
        i++;
        await Task.Delay(1);
        return i;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (16,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(16, 9));
        }

        [Fact]
        public void ReadOnlyAccessor_CallNormalMethod()
        {
            var csharp = @"
public struct S
{
    public int i;

    public readonly int P
    {
        get
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
            verifier.VerifyDiagnostics(
                // (11,13): warning CS8655: Call to non-readonly member 'S.M()' from a 'readonly' member results in an implicit copy of 'this'.
                //             M();
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "M").WithArguments("S.M()", "this").WithLocation(11, 13));
        }

        [Fact]
        public void ReadOnlyAccessor_CallNormalGetAccessor()
        {
            var csharp = @"
public struct S
{
    public int i;

    public readonly int P1
    {
        get
        {
            // should create local copy
            _ = P2; // warning
            System.Console.Write(i);

            // explicit local copy, no warning
            var copy = this;
            _ = copy.P2; // ok
            System.Console.Write(copy.i);

            return i;
        }
    }

    int P2 => i = 23;

    static void Main()
    {
        var s = new S { i = 1 };
        _ = s.P1;
    }
}
";

            var verifier = CompileAndVerify(csharp, expectedOutput: "123");
            verifier.VerifyDiagnostics(
                // (11,17): warning CS8655: Call to non-readonly member 'S.P2.get' from a 'readonly' member results in an implicit copy of 'this'.
                //             _ = P2; // warning
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "P2").WithArguments("S.P2.get", "this").WithLocation(11, 17));
        }

        [Fact]
        public void ReadOnlyMethod_CallSetAccessor()
        {
            var csharp = @"
public class C
{
    public int P { get; set; }
}

public struct S
{
    C c;
    public S(C c)
    {
        this.c = c;
        P2 = 0;
    }

    static int P1 { get; set; }
    int P2 { get; set; }
    readonly int P3
    {
        get => 42;
        set {}
    }

    public readonly void M()
    {
        P1 = 1; // ok
        P2 = 2; // error
        P3 = 2; // ok
        c.P = 42; // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (27,9): error CS1604: Cannot assign to 'P2' because it is read-only
                //         P2 = 2; // error
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "P2").WithArguments("P2").WithLocation(27, 9));
        }

        [Fact]
        public void ReadOnlyMethod_IncrementOperator()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly void M()
    {
        i++;
        i--;
        ++i;
        --i;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (7,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(7, 9),
                // (8,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i--;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(8, 9),
                // (9,11): error CS1604: Cannot assign to 'i' because it is read-only
                //         ++i;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(9, 11),
                // (10,11): error CS1604: Cannot assign to 'i' because it is read-only
                //         --i;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(10, 11));
        }

        [Fact]
        public void ReadOnlyMethod_CompoundAssignmentOperator()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly void M()
    {
        i += 1;
        i -= 1;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (7,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i += 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(7, 9),
                // (8,9): error CS1604: Cannot assign to 'i' because it is read-only
                //         i -= 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(8, 9));
        }

        [Fact]
        public void ReadOnlyMethod_EventAssignment()
        {
            var csharp = @"
using System;

public struct S
{
    public readonly void M()
    {
        Console.WriteLine(E is null);
        // should create local copy
        E += () => {}; // warning
        Console.WriteLine(E is null);
        E -= () => {}; // warning

        // explicit local copy, no warning
        var copy = this;
        copy.E += () => {};
        Console.WriteLine(copy.E is null);
    }

    public event Action E;

    static void Main()
    {
        var s = new S();
        s.M();
    }
}
";

            var verifier = CompileAndVerify(csharp, expectedOutput:
@"True
True
False");
            verifier.VerifyDiagnostics(
                // (10,9): warning CS8656: Call to non-readonly member 'S.E.add' from a 'readonly' member results in an implicit copy of 'this'.
                //         E += () => {}; // warning
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "E").WithArguments("S.E.add", "this").WithLocation(10, 9),
                // (12,9): warning CS8656: Call to non-readonly member 'S.E.remove' from a 'readonly' member results in an implicit copy of 'this'.
                //         E -= () => {}; // warning
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "E").WithArguments("S.E.remove", "this").WithLocation(12, 9));
        }

        [Fact]
        public void ReadOnlyStruct_EventAssignment()
        {
            var csharp = @"
using System;

public struct S
{
    public void M()
    {
        E += () => {};
        E -= () => {};
    }

    public event Action E { add {} remove {} }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_ReadOnlyEventAssignment()
        {
            var csharp = @"
using System;

public struct S
{
    public readonly void M()
    {
        E += () => {};
        E -= () => {};
    }

    public readonly event Action E { add {} remove {} }
}
";

            var verifier = CreateCompilation(csharp);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_Field_EventAssignment()
        {
            var csharp = @"
#pragma warning disable 0067
using System;

public struct S2
{
    public event Action E;
}

public struct S1
{
    public S2 s2;

    public readonly void M()
    {
        s2.E += () => {};
        s2.E -= () => {};
    }
}
";

            // TODO: should warn in warning wave https://github.com/dotnet/roslyn/issues/33968
            var verifier = CreateCompilation(csharp);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyStruct_Field_EventAssignment()
        {
            var csharp = @"
#pragma warning disable 0067
using System;

public struct S2
{
    public event Action E;
}

public readonly struct S1
{
    public readonly S2 s2;

    public void M()
    {
        s2.E += () => {};
        s2.E -= () => {};
    }
}
";

            // TODO: should warn in warning wave https://github.com/dotnet/roslyn/issues/33968
            var verifier = CreateCompilation(csharp);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_EventAssignment_Error()
        {
            var csharp = @"
using System;
public struct S
{
    public event Action<EventArgs> E;

    public readonly void M()
    {
        E += handler;
        E -= handler;
        E = handler;
        E(new EventArgs());

        void handler(EventArgs args) { }
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (9,9): warning CS8656: Call to non-readonly member 'S.E.add' from a 'readonly' member results in an implicit copy of 'this'.
                //         E += handler;
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "E").WithArguments("S.E.add", "this").WithLocation(9, 9),
                // (10,9): warning CS8656: Call to non-readonly member 'S.E.remove' from a 'readonly' member results in an implicit copy of 'this'.
                //         E -= handler;
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "E").WithArguments("S.E.remove", "this").WithLocation(10, 9),
                // (11,9): error CS1604: Cannot assign to 'E' because it is read-only
                //         E = handler;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "E").WithArguments("E").WithLocation(11, 9));
        }

        [Fact]
        public void ReadOnlyEventAccessors()
        {
            var csharp = @"
using System;
public struct S
{
    public int i;
    public readonly event Action<EventArgs> E
    {
        add { i++; }
        remove { i--; }
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (8,15): error CS1604: Cannot assign to 'i' because it is read-only
                //         add { i++; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(8, 15),
                // (9,18): error CS1604: Cannot assign to 'i' because it is read-only
                //         remove { i--; }
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "i").WithArguments("i").WithLocation(9, 18));
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
        public void ReadOnlyMembers_SemanticModel()
        {
            var csharp = @"
using System;

public struct S1
{
    public void M1() {}
    public readonly void M2() {}

    public int P1 { get; set; }
    public readonly int P2 => 42;
    public int P3 { readonly get => 123; }
    public int P4 { readonly set {} }
    public static int P5 { get; set; }
    public readonly event Action<EventArgs> E { add {} remove {} }
}

public readonly struct S2
{
    public void M1() {}
    public static void M2() {}

    public int P1 { get; }
    public int P2 => 42;
    public int P3 { set {} }
    public static int P4 { get; set; }
    public event Action<EventArgs> E { add {} remove {} }
}
";
            Compilation comp = CreateCompilation(csharp);
            var s1 = (INamedTypeSymbol)comp.GetSymbolsWithName("S1").Single();

            Assert.False(getMethod(s1, "M1").IsReadOnly);

            Assert.True(getMethod(s1, "M2").IsReadOnly);

            Assert.True(getProperty(s1, "P1").GetMethod.IsReadOnly);
            Assert.False(getProperty(s1, "P1").SetMethod.IsReadOnly);

            Assert.True(getProperty(s1, "P2").GetMethod.IsReadOnly);

            Assert.True(getProperty(s1, "P3").GetMethod.IsReadOnly);

            Assert.True(getProperty(s1, "P4").SetMethod.IsReadOnly);

            Assert.False(getProperty(s1, "P5").GetMethod.IsReadOnly);
            Assert.False(getProperty(s1, "P5").SetMethod.IsReadOnly);

            Assert.True(getEvent(s1, "E").AddMethod.IsReadOnly);
            Assert.True(getEvent(s1, "E").RemoveMethod.IsReadOnly);

            var s2 = comp.GetMember<INamedTypeSymbol>("S2");
            Assert.True(getMethod(s2, "M1").IsReadOnly);
            Assert.False(getMethod(s2, "M2").IsReadOnly);

            Assert.True(getProperty(s2, "P1").GetMethod.IsReadOnly);

            Assert.True(getProperty(s2, "P2").GetMethod.IsReadOnly);

            Assert.True(getProperty(s2, "P3").SetMethod.IsReadOnly);

            Assert.False(getProperty(s2, "P4").GetMethod.IsReadOnly);
            Assert.False(getProperty(s2, "P4").SetMethod.IsReadOnly);

            Assert.True(getEvent(s2, "E").AddMethod.IsReadOnly);
            Assert.True(getEvent(s2, "E").RemoveMethod.IsReadOnly);

            static IMethodSymbol getMethod(INamedTypeSymbol symbol, string name) => (IMethodSymbol)symbol.GetMembers(name).Single();
            static IPropertySymbol getProperty(INamedTypeSymbol symbol, string name) => (IPropertySymbol)symbol.GetMembers(name).Single();
            static IEventSymbol getEvent(INamedTypeSymbol symbol, string name) => (IEventSymbol)symbol.GetMembers(name).Single();
        }

        [Fact]
        public void ReadOnlyMembers_ExtensionMethods_SemanticModel()
        {
            var csharp = @"
public struct S1 {}
public readonly struct S2 {}

public static class C
{
    static void M1(this S1 s1) {}
    static void M2(this ref S1 s1) {}
    static void M3(this in S1 s1) {}
    static void M4(this S2 s2) {}
    static void M5(this ref S2 s2) {}
    static void M6(this in S2 s2) {}

    static void Test()
    {
        var s1 = new S1();
        s1.M1();
        s1.M2();
        s1.M3();

        var s2 = new S2();
        s2.M4();
        s2.M5();
        s2.M6();
    }
}
";
            Compilation comp = CreateCompilation(csharp);

            var c = comp.GetMember<IMethodSymbol>("C.Test");
            var testMethodSyntax = (MethodDeclarationSyntax)c.DeclaringSyntaxReferences.Single().GetSyntax();

            var semanticModel = comp.GetSemanticModel(testMethodSyntax.SyntaxTree);
            var statements = testMethodSyntax.Body.Statements;

            testStatement(statements[1], false);
            testStatement(statements[2], false);
            testStatement(statements[3], true);

            testStatement(statements[5], false);
            testStatement(statements[6], false);
            testStatement(statements[7], true);

            void testStatement(StatementSyntax statementSyntax, bool isEffectivelyReadOnly)
            {
                var expressionStatement = (ExpressionStatementSyntax)statementSyntax;
                var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;

                var symbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol;
                var reducedFrom = symbol.ReducedFrom;

                Assert.Equal(isEffectivelyReadOnly, symbol.GetSymbol().IsEffectivelyReadOnly);
                Assert.Equal(isEffectivelyReadOnly, symbol.IsReadOnly);

                Assert.False(symbol.GetSymbol().IsDeclaredReadOnly);
                Assert.False(reducedFrom.GetSymbol().IsDeclaredReadOnly);
                Assert.False(reducedFrom.GetSymbol().IsEffectivelyReadOnly);
                Assert.False(((IMethodSymbol)reducedFrom).IsReadOnly);
            }
        }

        [Fact]
        public void ReadOnlyMembers_RefReturningProperty()
        {
            var csharp = @"
public struct S1
{
    private static int i = 0;

    public ref int P1 => ref i;
    public readonly ref int P2 => ref i;
    public ref readonly int P3 => ref i;
    public readonly ref readonly int P4 => ref i;
}
";
            var comp = CreateCompilation(csharp);

            var s1 = comp.GetMember<NamedTypeSymbol>("S1");

            check(s1.GetProperty("P1"), true, false, false);
            check(s1.GetProperty("P2"), true, false, true);
            check(s1.GetProperty("P3"), false, true, false);
            check(s1.GetProperty("P4"), false, true, true);

            static void check(PropertySymbol property, bool returnsByRef, bool returnsByRefReadonly, bool isReadOnly)
            {
                Assert.Equal(returnsByRef, property.ReturnsByRef);
                Assert.Equal(returnsByRefReadonly, property.ReturnsByRefReadonly);

                Assert.True(property.IsReadOnly);
                Assert.Equal(isReadOnly, property.GetMethod.IsDeclaredReadOnly);
                Assert.Equal(isReadOnly, property.GetMethod.IsEffectivelyReadOnly);
                Assert.Equal(isReadOnly, property.GetMethod.GetPublicSymbol().IsReadOnly);
            }
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
        public void ReadOnlyClass_NormalMethod()
        {
            var csharp = @"
public readonly class C
{
    int i;
    void M()
    {
        i++;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (2,23): error CS0106: The modifier 'readonly' is not valid for this item
                // public readonly class C
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("readonly").WithLocation(2, 23));
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
                // (5,32): error CS8656: Static member 'S.M()' cannot be marked 'readonly'.
                //     public static readonly int M()
                Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "M").WithArguments("S.M()").WithLocation(5, 32));

            var method = comp.GetMember<NamedTypeSymbol>("S").GetMethod("M");
            Assert.True(method.IsDeclaredReadOnly);
            Assert.False(method.IsEffectivelyReadOnly);
        }

        [Fact]
        public void ReadOnlyStructProperty()
        {
            var csharp = @"
public struct S
{
    public int i;
    public readonly int P
    {
        get
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
        set
        {
            i = value;
        }
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (7,18): error CS8656: Static member 'S.P.get' cannot be marked 'readonly'.
                //         readonly get
                Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "get").WithArguments("S.P.get").WithLocation(7, 18));
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
                // (5,32): error CS8656: Static member 'S.P' cannot be marked 'readonly'.
                //     public static readonly int P => i;
                Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "P").WithArguments("S.P").WithLocation(5, 32));
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
    public int P4 { readonly get; readonly set; }
    public readonly int P5 { get; set; }
    public readonly int P6 { readonly get; }
    public int P7 { get; readonly set; }
    public readonly int P8 { get; readonly set; }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,16): error CS8663: 'S.P1': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                //     public int P1 { readonly get; }
                Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P1").WithArguments("S.P1").WithLocation(4, 16),
                // (7,16): error CS8660: Cannot specify 'readonly' modifiers on both accessors of property or indexer 'S.P4'. Instead, put a 'readonly' modifier on the property itself.
                //     public int P4 { readonly get; readonly set; }
                Diagnostic(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, "P4").WithArguments("S.P4").WithLocation(7, 16),
                // (7,44): error CS8657: Auto-implemented 'set' accessor 'S.P4.set' cannot be marked 'readonly'.
                //     public int P4 { readonly get; readonly set; }
                Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P4.set").WithLocation(7, 44),
                // (8,25): error CS8658: Auto-implemented property 'S.P5' cannot be marked 'readonly' because it has a 'set' accessor.
                //     public readonly int P5 { get; set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P5").WithArguments("S.P5").WithLocation(8, 25),
                // (9,25): error CS8663: 'S.P6': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                //     public readonly int P6 { readonly get; }
                Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "P6").WithArguments("S.P6").WithLocation(9, 25),
                // (9,39): error CS8659: Cannot specify 'readonly' modifiers on both property or indexer 'S.P6' and its accessor. Remove one of them.
                //     public readonly int P6 { readonly get; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyReadOnlyMods, "get").WithArguments("S.P6").WithLocation(9, 39),
                // (10,35): error CS8657: Auto-implemented 'set' accessor 'S.P7.set' cannot be marked 'readonly'.
                //     public int P7 { get; readonly set; }
                Diagnostic(ErrorCode.ERR_AutoSetterCantBeReadOnly, "set").WithArguments("S.P7.set").WithLocation(10, 35),
                // (11,25): error CS8658: Auto-implemented property 'S.P8' cannot be marked 'readonly' because it has a 'set' accessor.
                //     public readonly int P8 { get; readonly set; }
                Diagnostic(ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly, "P8").WithArguments("S.P8").WithLocation(11, 25),
                // (11,44): error CS8659: Cannot specify 'readonly' modifiers on both property or indexer 'S.P8' and its accessor. Remove one of them.
                //     public readonly int P8 { get; readonly set; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyReadOnlyMods, "set").WithArguments("S.P8").WithLocation(11, 44));
        }

        [Fact]
        public void NetModule_ImplicitReadOnlyAutoProperty()
        {
            var csharp = @"
public struct S
{
    public int P1 { get; private set; }
}
";
            var moduleMetadata = CreateCompilation(csharp, options: TestOptions.DebugModule, targetFramework: TargetFramework.Mscorlib45).EmitToImageReference();
            var moduleComp = CreateCompilation("", new[] { moduleMetadata });
            var moduleGetter = moduleComp.GetMember<PropertySymbol>("S.P1").GetMethod;
            Assert.False(moduleGetter.IsDeclaredReadOnly);

            var dllMetadata = CreateCompilation(csharp, options: TestOptions.DebugDll, targetFramework: TargetFramework.Mscorlib45).EmitToImageReference();
            var dllComp = CreateCompilation("", new[] { dllMetadata });
            var dllGetter = dllComp.GetMember<PropertySymbol>("S.P1").GetMethod;
            Assert.True(dllGetter.IsDeclaredReadOnly);
        }

        [Fact]
        public void NetModule_ImplicitReadOnlyAutoProperty_MalformedAttribute()
        {
            var csharp = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute
    {
        public IsReadOnlyAttribute(int x) {}
    }
}

public struct S
{
    public int P1 { get; private set; }
}
";
            var moduleMetadata = CreateCompilation(csharp, options: TestOptions.DebugModule, targetFramework: TargetFramework.Mscorlib45).EmitToImageReference();
            var moduleComp = CreateCompilation("", new[] { moduleMetadata });
            var moduleGetter = moduleComp.GetMember<PropertySymbol>("S.P1").GetMethod;
            Assert.False(moduleGetter.IsDeclaredReadOnly);

            var dllMetadata = CreateCompilation(csharp, options: TestOptions.DebugDll, targetFramework: TargetFramework.Mscorlib45).EmitToImageReference();
            var dllComp = CreateCompilation("", new[] { dllMetadata });
            var dllGetter = dllComp.GetMember<PropertySymbol>("S.P1").GetMethod;
            Assert.False(dllGetter.IsDeclaredReadOnly);
        }

        [Fact]
        public void NetModule_ExplicitReadOnlyAutoProperty_MalformedAttribute()
        {
            var csharp = @"
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute
    {
        public IsReadOnlyAttribute(int x) {}
    }
}

public struct S
{
    public int P1 { readonly get; private set; }
}
";
            var moduleComp = CreateCompilation(csharp, options: TestOptions.DebugModule, targetFramework: TargetFramework.Mscorlib45);
            moduleComp.VerifyDiagnostics(
                // (12,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public int P1 { readonly get; private set; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "get").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(12, 30));

            var dllComp = CreateCompilation(csharp, options: TestOptions.DebugDll, targetFramework: TargetFramework.Mscorlib45);
            dllComp.VerifyDiagnostics(
                // (12,30): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.IsReadOnlyAttribute..ctor'
                //     public int P1 { readonly get; private set; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "get").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute", ".ctor").WithLocation(12, 30));
        }

        [Fact]
        public void NetModule_ExplicitReadOnlyAutoProperty()
        {
            var csharp = @"
public struct S
{
    public int P1 { readonly get; private set; }
}
";
            var moduleComp = CreateCompilation(csharp, options: TestOptions.DebugModule, targetFramework: TargetFramework.Mscorlib45);
            moduleComp.VerifyDiagnostics(
                // (4,30): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsReadOnlyAttribute' is not defined or imported
                //     public int P1 { readonly get; private set; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "get").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(4, 30));

            var dllComp = CreateCompilation(csharp, options: TestOptions.DebugDll, targetFramework: TargetFramework.Mscorlib45);
            dllComp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyProperty_RedundantReadOnlyAccessor()
        {
            var csharp = @"
public struct S
{
    public readonly int P { readonly get => 42; set {} }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,38): error CS8659: Cannot specify 'readonly' modifiers on both property or indexer 'S.P' and its accessor. Remove one of them.
                //     public readonly int P { readonly get => 42; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyReadOnlyMods, "get").WithArguments("S.P").WithLocation(4, 38));
        }

        [Fact]
        public void ReadOnlyStaticAutoProperty()
        {
            var csharp = @"
public struct S
{
    public static readonly int P1 { get; set; }
    public static int P2 { readonly get; set; }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (4,32): error CS8656: Static member 'S.P1' cannot be marked 'readonly'.
                //     public static readonly int P1 { get; set; }
                Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "P1").WithArguments("S.P1").WithLocation(4, 32),
                // (5,37): error CS8656: Static member 'S.P2.get' cannot be marked 'readonly'.
                //     public static int P2 { readonly get; }
                Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "get").WithArguments("S.P2.get").WithLocation(5, 37));
        }

        [Fact]
        public void RefReturningReadOnlyMethod()
        {
            var csharp = @"
public struct S
{
    private static int f1;
    public readonly ref int M1_1() => ref f1; // ok
    public readonly ref readonly int M1_2() => ref f1; // ok

    private static readonly int f2;
    public readonly ref int M2_1() => ref f2; // error
    public readonly ref readonly int M2_2() => ref f2; // ok

    private static readonly int f3;
    public ref readonly int M3()
    {
        f1++;
        return ref f3;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (9,43): error CS8161: A static readonly field cannot be returned by writable reference
                //     public readonly ref int M2_1() => ref f2; // error
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyStatic, "f2").WithLocation(9, 43));
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
        public void ReadOnlyStruct_Constructor()
        {
            var csharp = @"
public readonly struct S
{
    public readonly int i;
    public S(int i)
    {
        this.i = i; // ok
        M(ref this); // ok
    }
    public static void M(ref S s)
    {
        s.i = 42; // error
        s = default; // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (12,9): error CS0191: A readonly field cannot be assigned to (except in a constructor or a variable initializer)
                //         s.i = 42; // error
                Diagnostic(ErrorCode.ERR_AssgReadonly, "s.i").WithLocation(12, 9));
        }

        [Fact]
        public void ReadOnlyMethod_GetEnumerator()
        {
            var csharp = @"
using System.Collections;

public struct S1
{
    public IEnumerator GetEnumerator() => throw null;
    void M1()
    {
        foreach (var x in this) {} // ok
    }
    readonly void M2()
    {
        foreach (var x in this) {} // warning-- implicit copy
    }
}

public struct S2
{
    public readonly IEnumerator GetEnumerator() => throw null;
    void M1()
    {
        foreach (var x in this) {} // ok
    }
    readonly void M2()
    {
        foreach (var x in this) {} // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (13,27): warning CS8655: Call to non-readonly member 'S1.GetEnumerator()' from a 'readonly' member results in an implicit copy of 'this'.
                //         foreach (var x in this) {} // warning-- implicit copy
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S1.GetEnumerator()", "this").WithLocation(13, 27));
        }

        [Fact]
        public void ReadOnlyMethod_GetEnumerator_MethodMissing()
        {
            var csharp = @"
public struct S1
{
    readonly void M2()
    {
        foreach (var x in this) {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'S1' because 'S1' does not contain a public instance definition for 'GetEnumerator'
                //         foreach (var x in this) {}
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "this").WithArguments("S1", "GetEnumerator").WithLocation(6, 27));
        }

        [Fact]
        public void ReadOnlyMethod_AsyncStreams()
        {
            var csharp = @"
using System.Threading.Tasks;
using System.Collections.Generic;

public struct S1
{
    public IAsyncEnumerator<int> GetAsyncEnumerator() => throw null;

    public async Task M1()
    {
        await foreach (var x in this) {}
    }

    public readonly async Task M2()
    {
        await foreach (var x in this) {} // warn
    }
}

public struct S2
{
    public readonly IAsyncEnumerator<int> GetAsyncEnumerator() => throw null;

    public async Task M1()
    {
        await foreach (var x in this) {}
    }

    public readonly async Task M2()
    {
        await foreach (var x in this) {} // ok
    }
}
";
            var comp = CreateCompilationWithTasksExtensions(new[] { csharp, AsyncStreamsTypes });
            comp.VerifyDiagnostics(
                // (16,33): warning CS8655: Call to non-readonly member 'S1.GetAsyncEnumerator()' from a 'readonly' member results in an implicit copy of 'this'.
                //         await foreach (var x in this) {} // warn
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S1.GetAsyncEnumerator()", "this").WithLocation(16, 33));
        }

        [Fact]
        public void ReadOnlyMethod_Using()
        {
            // 'using' results in a boxing conversion when the struct implements 'IDisposable'.
            // Boxing conversions are out of scope of the implicit copy warning.
            // 'await using' can't be used with ref structs, so implicitly copy warnings can't be produced in that scenario.
            var csharp = @"
public ref struct S1
{
    public void Dispose() {}

    void M1()
    {
        using (this) { } // ok
    }

    readonly void M2()
    {
        using (this) { } // should warn
    }
}

public ref struct S2
{
    public readonly void Dispose() {}

    void M1()
    {
        using (this) { } // ok
    }

    readonly void M2()
    {
        using (this) { } // ok
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (13,16): warning CS8655: Call to non-readonly member 'S1.Dispose()' from a 'readonly' member results in an implicit copy of 'this'.
                //         using (this) { } // should warn
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S1.Dispose()", "this").WithLocation(13, 16));
        }

        [Fact]
        public void ReadOnlyMethod_Deconstruct()
        {
            var csharp = @"
public struct S1
{
    void M1()
    {
        var (x, y) = this; // ok
    }

    readonly void M2()
    {
        var (x, y) = this; // should warn
    }

    public void Deconstruct(out int x, out int y)
    {
        x = 42;
        y = 123;
    }
}

public struct S2
{
    void M1()
    {
        var (x, y) = this; // ok
    }

    readonly void M2()
    {
        var (x, y) = this; // ok
    }

    public readonly void Deconstruct(out int x, out int y)
    {
        x = 42;
        y = 123;
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (11,22): warning CS8655: Call to non-readonly member 'S1.Deconstruct(out int, out int)' from a 'readonly' member results in an implicit copy of 'this'.
                //         var (x, y) = this; // should warn
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S1.Deconstruct(out int, out int)", "this").WithLocation(11, 22));
        }

        [Fact]
        public void ReadOnlyMethod_Deconstruct_MethodMissing()
        {
            var csharp = @"
public struct S2
{
    readonly void M1()
    {
        var (x, y) = this; // error
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (6,14): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'x'.
                //         var (x, y) = this; // error
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "x").WithArguments("x").WithLocation(6, 14),
                // (6,17): error CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'y'.
                //         var (x, y) = this; // error
                Diagnostic(ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, "y").WithArguments("y").WithLocation(6, 17),
                // (6,22): error CS1061: 'S2' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'S2' could be found (are you missing a using directive or an assembly reference?)
                //         var (x, y) = this; // error
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "this").WithArguments("S2", "Deconstruct").WithLocation(6, 22),
                // (6,22): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'S2', with 2 out parameters and a void return type.
                //         var (x, y) = this; // error
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "this").WithArguments("S2", "2").WithLocation(6, 22));
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
        public void ReadOnlyLocalFunction()
        {
            var csharp = @"
public struct S
{
    void M1()
    {
        local();
        readonly void local() {}
    }
    readonly void M2()
    {
        local();
        readonly void local() {}
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (7,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly void local() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(7, 9),
                // (12,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly void local() {}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(12, 9));
        }

        [Fact]
        public void ReadOnlyLambda()
        {
            var csharp = @"
public struct S
{
    void M1()
    {
        M2(readonly () => 42);
    }
    void M2(System.Func<int> a)
    {
        _ = a();
    }
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (6,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'S.M2(Func<int>)'
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M2").WithArguments("a", "S.M2(System.Func<int>)").WithLocation(6, 9),
                // (6,12): error CS1026: ) expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(6, 12),
                // (6,12): error CS1002: ; expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(6, 12),
                // (6,12): error CS0106: The modifier 'readonly' is not valid for this item
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 12),
                // (6,22): error CS8124: Tuple must contain at least two elements.
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(6, 22),
                // (6,24): error CS1001: Identifier expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(6, 24),
                // (6,24): error CS1003: Syntax error, ',' expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(6, 24),
                // (6,27): error CS1002: ; expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "42").WithLocation(6, 27),
                // (6,29): error CS1002: ; expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 29),
                // (6,29): error CS1513: } expected
                //         M2(readonly () => 42);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 29));
        }

        [Fact]
        public void ReadOnlyIndexer()
        {
            var csharp = @"
public struct S1
{
    // ok
    public readonly int this[int i] => i;
}

public struct S2
{
    // ok
    public int this[int i]
    {
        readonly get => i;
        set {}
    }
}

public struct S3
{
    // error
    public int this[int i]
    {
        readonly get { return i; }
        readonly set {}
    }
}

public struct S4
{
    // error
    public int this[int i]
    {
        readonly get { return i; }
    }
}

public struct S5
{
    // error
    public readonly int this[int i]
    {
        readonly get { return i; }
        set { }
    }
}

public struct S6
{
    // error
    public static readonly int this[int i] => i;
}
";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (21,16): error CS8660: Cannot specify 'readonly' modifiers on both accessors of property or indexer 'S3.this[int]'. Instead, put a 'readonly' modifier on the property itself.
                //     public int this[int i]
                Diagnostic(ErrorCode.ERR_DuplicatePropertyReadOnlyMods, "this").WithArguments("S3.this[int]").WithLocation(21, 16),
                // (31,16): error CS8663: 'S4.this[int]': 'readonly' can only be used on accessors if the property or indexer has both a get and a set accessor
                //     public int this[int i]
                Diagnostic(ErrorCode.ERR_ReadOnlyModMissingAccessor, "this").WithArguments("S4.this[int]").WithLocation(31, 16),
                // (42,18): error CS8659: Cannot specify 'readonly' modifiers on both property or indexer 'S5.this[int]' and its accessor. Remove one of them.
                //         readonly get { return i; }
                Diagnostic(ErrorCode.ERR_InvalidPropertyReadOnlyMods, "get").WithArguments("S5.this[int]").WithLocation(42, 18),
                // (50,32): error CS0106: The modifier 'static' is not valid for this item
                //     public static readonly int this[int i] => i;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("static").WithLocation(50, 32));
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
                // (6,45): error CS8661: Field-like event 'S1.E' cannot be 'readonly'.
                //     public readonly event Action<EventArgs> E;
                Diagnostic(ErrorCode.ERR_FieldLikeEventCantBeReadOnly, "E").WithArguments("S1.E").WithLocation(6, 45));
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
                // (6,52): error CS8656: Static member 'S1.E' cannot be marked 'readonly'.
                //     public static readonly event Action<EventArgs> E
                Diagnostic(ErrorCode.ERR_StaticMemberCantBeReadOnly, "E").WithArguments("S1.E").WithLocation(6, 52));
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

        [Fact]
        public void ReadOnlyMembers_LangVersion()
        {
            var csharp = @"
using System;

public struct S
{
    public readonly void M() {}

    public readonly int P1 => 42;
    public int P2 { readonly get => 123; set {} }
    public int P3 { get => 123; readonly set {} }

    public readonly int this[int i] => i;
    public int this[int i, int j] { readonly get => i + j; set {} }

    public readonly event Action<EventArgs> E { add {} remove {} }
}
";
            var comp = CreateCompilation(csharp, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (6,12): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public readonly void M() {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(6, 12),
                // (8,12): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public readonly int P1 => 42;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(8, 12),
                // (9,21): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public int P2 { readonly get => 123; set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(9, 21),
                // (10,33): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public int P3 { get => 123; readonly set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(10, 33),
                // (12,12): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public readonly int this[int i] => i;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(12, 12),
                // (13,37): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public int this[int i, int j] { readonly get => i + j; set {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(13, 37),
                // (15,12): error CS8652: The feature 'readonly members' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     public readonly event Action<EventArgs> E { add {} remove {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "readonly").WithArguments("readonly members", "8.0").WithLocation(15, 12));

            comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyMethod_CompoundPropertyAssignment()
        {
            var csharp = @"
struct S
{
    int P1 { get => 123; set {} }
    int P2 { readonly get => 123; set {} }
    int P3 { get => 123; readonly set {} }
    readonly int P4 { get => 123; set {} }

    void M1()
    {
        // ok
        P1 += 1;
        P2 += 1;
        P3 += 1;
        P4 += 1;
    }

    readonly void M2()
    {
        P1 += 1; // error
        P2 += 1; // error
        P3 += 1; // warning
        P4 += 1; // ok
    }
}";
            var comp = CreateCompilation(csharp);
            comp.VerifyDiagnostics(
                // (20,9): error CS1604: Cannot assign to 'P1' because it is read-only
                //         P1 += 1; // error
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "P1").WithArguments("P1").WithLocation(20, 9),
                // (21,9): error CS1604: Cannot assign to 'P2' because it is read-only
                //         P2 += 1; // error
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "P2").WithArguments("P2").WithLocation(21, 9),
                // (22,9): warning CS8655: Call to non-readonly member 'S.P3.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         P3 += 1; // warning
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "P3").WithArguments("S.P3.get", "this").WithLocation(22, 9));
        }
    }
}
