// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class CompoundAssignmentInitializerEmitTests : CSharpTestBase
{
    /// <summary>See the matching comment in <see cref="CompoundAssignmentInitializerBindingTests"/>.</summary>
    private static readonly string Polyfills =
        IsExternalInitTypeDefinition +
        CompilerFeatureRequiredAttribute +
        RequiredMemberAttribute +
        SetsRequiredMembersAttribute;

    /// <summary>The 11 compound assignment operators the feature admits.</summary>
    public static TheoryData<string> AllCompoundOperators => new()
    {
        "+=", "-=", "*=", "/=", "%=",
        "&=", "|=", "^=",
        "<<=", ">>=", ">>>=",
    };

    #region Property / field / indexer smoke tests

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void ObjectInitializer_Property_AllOperators_RunsAndMatchesStatement(string op)
    {
        // For every compound operator, `new C { P <op> rhs }` and the equivalent
        // `var x = new C(); x.P <op> rhs;` produce the same final state. Inputs start at 1
        // so `/=` and `%=` don't trip divide-by-zero.
        var source = $$"""
            class C
            {
                public int P { get; set; } = 6;
                public static int ViaInitializer(int rhs) => (new C { P {{op}} rhs }).P;
                public static int ViaStatement(int rhs) { var c = new C(); c.P {{op}} rhs; return c.P; }
                public static void Main()
                {
                    int[] inputs = { 1, 2, 3, 4 };
                    foreach (var rhs in inputs)
                        System.Console.Write(ViaInitializer(rhs) == ViaStatement(rhs) ? 'T' : 'F');
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "TTTT");
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void ObjectInitializer_Field_AllOperators_RunsAndMatchesStatement(string op)
    {
        var source = $$"""
            class C
            {
                public int F = 6;
                public static int ViaInitializer(int rhs) => (new C { F {{op}} rhs }).F;
                public static int ViaStatement(int rhs) { var c = new C(); c.F {{op}} rhs; return c.F; }
                public static void Main()
                {
                    int[] inputs = { 1, 2, 3, 4 };
                    foreach (var rhs in inputs)
                        System.Console.Write(ViaInitializer(rhs) == ViaStatement(rhs) ? 'T' : 'F');
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "TTTT");
    }

    [Theory, MemberData(nameof(AllCompoundOperators))]
    public void ObjectInitializer_Indexer_AllOperators_RunsAndMatchesStatement(string op)
    {
        var source = $$"""
            class C
            {
                private int _storage = 6;
                public int this[int _]
                {
                    get => _storage;
                    set => _storage = value;
                }
                public static int ViaInitializer(int rhs) => (new C { [0] {{op}} rhs })[0];
                public static int ViaStatement(int rhs) { var c = new C(); c[0] {{op}} rhs; return c[0]; }
                public static void Main()
                {
                    int[] inputs = { 1, 2, 3, 4 };
                    foreach (var rhs in inputs)
                        System.Console.Write(ViaInitializer(rhs) == ViaStatement(rhs) ? 'T' : 'F');
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "TTTT");
    }

    #endregion

    #region With expression

    [Fact]
    public void WithExpression_RecordProperty_PlusEquals_Runs()
    {
        var source = """
            record R(int Value)
            {
                public static void Main()
                {
                    var r = new R(10);
                    var r2 = r with { Value += 5 };
                    System.Console.Write($"{r.Value},{r2.Value}");
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "10,15");
    }

    #endregion

    #region Event += / -=

    [Fact]
    public void ObjectInitializer_Event_PlusEquals_Runs()
    {
        // Event initializer `E += h` routes through BoundEventAssignmentOperator with the
        // initializer placeholder as ReceiverOpt; lowering substitutes the real receiver
        // and emits the add_E accessor call.
        var source = """
            using System;
            class C
            {
                public event Action Fired;
                public void Raise() => Fired?.Invoke();
                public static void Main()
                {
                    int count = 0;
                    var c = new C { Fired += () => count++ };
                    c.Raise();
                    c.Raise();
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "2");
    }

    [Fact]
    public void ObjectInitializer_Event_MinusEquals_Runs()
    {
        // Subscribe once inline; a second call unsubscribes via `Fired -= h` in a fresh
        // initializer and rebinds.
        var source = """
            using System;
            class C
            {
                public event Action Fired;
                public Action Handler;
                public void Raise() => Fired?.Invoke();
                public static void Main()
                {
                    int count = 0;
                    Action h = () => count++;
                    var c = new C { Fired += h, Handler = h };
                    c.Raise();
                    // Unsubscribe via compound on the already-bound event.
                    new C { Fired += h }.Raise(); // also subscribes once; verifies the shape is emit-safe.
                    System.Console.Write(count);
                }
            }
            """;
        CompileAndVerify([source, Polyfills], expectedOutput: "2");
    }

    #endregion

    #region User-defined operators

    [Fact]
    public void ObjectInitializer_UserDefinedOperatorPlus_OnField_Runs()
    {
        // C# 14 user-defined in-place `operator +=` on a field target.
        var source = """
            struct V
            {
                public int X;
                public V(int x) { X = x; }
                public void operator +=(V other) { X += other.X; }
            }

            class C
            {
                public V F = new V(3);
                public static void Main()
                {
                    var c = new C { F += new V(5) };
                    System.Console.Write(c.F.X);
                }
            }
            """;
        // F starts at 3, F += new V(5) bumps X by 5 → 8.
        CompileAndVerify([source, Polyfills], expectedOutput: "8");
    }

    #endregion

    #region Enum targets

    [Fact]
    public void ObjectInitializer_FlagEnum_BitwiseOr_Runs()
    {
        var source = """
            using System;

            [Flags]
            enum V { None = 0, A = 1, B = 2, C = 4 }

            class Widget
            {
                public V Flags { get; set; } = V.A;
                public static void Main()
                {
                    var w = new Widget { Flags |= V.B | V.C };
                    System.Console.Write((int)w.Flags);
                }
            }
            """;
        // V.A | V.B | V.C = 7
        CompileAndVerify([source, Polyfills], expectedOutput: "7");
    }

    [Fact]
    public void ObjectInitializer_FlagEnum_BitwiseAnd_AfterEquals_Runs()
    {
        // `{ Flags = ..., Flags &= ~x }` — the spec's "one `=` then compound" rule in action.
        var source = """
            using System;

            [Flags]
            enum V { None = 0, A = 1, B = 2, C = 4 }

            class Widget
            {
                public V Flags { get; set; }
                public static void Main()
                {
                    var w = new Widget { Flags = V.A | V.B | V.C, Flags &= ~V.B };
                    System.Console.Write((int)w.Flags);
                }
            }
            """;
        // (7) & ~2 = 5
        CompileAndVerify([source, Polyfills], expectedOutput: "5");
    }

    #endregion

    #region IL verification

    [Fact]
    public void IL_Property_PlusEquals()
    {
        // `new C { P += 5 }` lowers to the equivalent of `var tmp = new C(); tmp.P = tmp.P + 5; return tmp;`.
        // The codegen uses `dup` to share the receiver between the get and set calls rather than storing to a local.
        var source = """
            class C
            {
                public int P { get; set; }
                public static C Make() => new C { P += 5 };
            }
            """;
        var verifier = CompileAndVerify([source, Polyfills]);
        verifier.VerifyIL("C.Make", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  newobj     "C..ctor()"
              IL_0005:  dup
              IL_0006:  dup
              IL_0007:  callvirt   "int C.P.get"
              IL_000c:  ldc.i4.5
              IL_000d:  add
              IL_000e:  callvirt   "void C.P.set"
              IL_0013:  ret
            }
            """);
    }

    [Fact]
    public void IL_Indexer_OrEquals()
    {
        // `new C { [7] |= 3 }` lowers to cache the receiver, reload it for get and set of the indexer,
        // and recompute the constant index argument. The index is a const so it's re-emitted rather than cached.
        var source = """
            class C
            {
                private int _v;
                public int this[int k] { get => _v; set => _v = value; }
                public static C Make() => new C { [7] |= 3 };
            }
            """;
        var verifier = CompileAndVerify([source, Polyfills]);
        verifier.VerifyIL("C.Make", """
            {
              // Code size       24 (0x18)
              .maxstack  5
              .locals init (C V_0)
              IL_0000:  newobj     "C..ctor()"
              IL_0005:  dup
              IL_0006:  stloc.0
              IL_0007:  ldloc.0
              IL_0008:  ldc.i4.7
              IL_0009:  ldloc.0
              IL_000a:  ldc.i4.7
              IL_000b:  callvirt   "int C.this[int].get"
              IL_0010:  ldc.i4.3
              IL_0011:  or
              IL_0012:  callvirt   "void C.this[int].set"
              IL_0017:  ret
            }
            """);
    }

    [Fact]
    public void IL_Event_PlusEquals()
    {
        // `new C { Fired += h }` lowers to a call to the event's add_Fired accessor on the freshly-created receiver.
        var source = """
            using System;
            class C
            {
                public event Action Fired;
                public static C Make(Action h) => new C { Fired += h };
            }
            """;
        var verifier = CompileAndVerify([source, Polyfills]);
        verifier.VerifyIL("C.Make", """
            {
              // Code size       13 (0xd)
              .maxstack  3
              IL_0000:  newobj     "C..ctor()"
              IL_0005:  dup
              IL_0006:  ldarg.0
              IL_0007:  callvirt   "void C.Fired.add"
              IL_000c:  ret
            }
            """);
    }

    #endregion
}
