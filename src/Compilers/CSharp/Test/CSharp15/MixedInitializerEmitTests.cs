// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

// Emit and runtime tests for the mixed object/collection initializer feature
// (dotnet/csharplang#10185). Verifies that the lowered code produces the right side effects in
// the right order, and pins canonical IL for one shape.
public sealed class MixedInitializerEmitTests : CSharpTestBase
{
    #region Runtime ordering and behavior

    [Fact]
    public void Mixed_MemberAndElement_RunsInLexicalOrder()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) => Console.Write($"Add({item}) X={X};");
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var _ = new C { 10, X = 5, 20, X += 100 };
                }
            }
            """;
        // Add and the X assignment are processed in lexical order, so the second Add observes the
        // value the most recent X = ... assignment placed into the slot, and the final
        // compound assignment further accumulates.
        var verifier = CompileAndVerify(source, expectedOutput: "Add(10) X=0;Add(20) X=5;");
        verifier.VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_MemberWriteBetweenAdds_AffectsLaterAdds()
    {
        // The mixed form's ordering guarantee is the whole point of the feature: an assignment
        // between two Add() calls actually changes state that the later Add reads. This test
        // would not be expressible at all before the feature lands (the binder would reject the
        // mixed shape).
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int Prefix { get; set; }
                public void Add(int item) => Console.Write($"{Prefix}-{item};");
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var _ = new C { 1, Prefix = 100, 2, Prefix += 100, 3 };
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "0-1;100-2;200-3;").VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_CompoundMemberAndElements_RunsInLexicalOrder()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int Counter { get; set; }
                public void Add(int item) => Console.Write($"Counter={Counter} adding={item};");
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var _ = new C { Counter = 1, 10, Counter += 4, 20, Counter *= 2, 30 };
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "Counter=1 adding=10;Counter=5 adding=20;Counter=10 adding=30;")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_IndexerMemberAndElements_RunsInLexicalOrder()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                private readonly Dictionary<int, string> _map = new();
                public string this[int i] { get => _map[i]; set => _map[i] = value; }
                public void Add(int item) => Console.Write($"Add({item}); ");
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;

                public string Dump() => $"[0]={_map[0]} [1]={_map[1]}";
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { [0] = "a", 100, [1] = "b", 200 };
                    Console.Write(c.Dump());
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "Add(100); Add(200); [0]=a [1]=b")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_EventMemberAndElements_RunsInLexicalOrder()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public event Action<int> E;
                public void Raise(int n) => E?.Invoke(n);
                public void Add(int item) { Console.Write($"Add({item})"); Raise(item); Console.Write(";"); }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var c = new C { E += n => Console.Write($"->H1({n})"), 1, E += n => Console.Write($"->H2({n})"), 2 };
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "Add(1)->H1(1);Add(2)->H1(2)->H2(2);")
            .VerifyDiagnostics();
    }

    [Fact]
    public void Mixed_BraceListElement_RunsInLexicalOrder()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int a, int b) => Console.Write($"Add({a},{b}) X={X};");
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                static void Main()
                {
                    var _ = new C { X = 1, { 2, 3 }, X += 9, { 4, 5 } };
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "Add(2,3) X=1;Add(4,5) X=10;").VerifyDiagnostics();
    }

    #endregion

    #region IL pin

    [Fact]
    public void Mixed_MemberAndElement_IL_LowersToInterleavedStatementSequence()
    {
        var source = """
            using System.Collections;
            using System.Collections.Generic;

            class C : IEnumerable<int>
            {
                public int X { get; set; }
                public void Add(int item) { }
                public IEnumerator<int> GetEnumerator() { yield break; }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }

            class Program
            {
                public static C Make() => new C { X = 5, 10, 20 };
            }
            """;
        var verifier = CompileAndVerify(source);
        verifier.VerifyDiagnostics();
        // The lowered method allocates the receiver, then in lexical order: set X, Add(10),
        // Add(20), with `dup` carrying the receiver between the initializer statements and the
        // final return.
        verifier.VerifyIL("Program.Make", """
            {
              // Code size       29 (0x1d)
              .maxstack  3
              IL_0000:  newobj     "C..ctor()"
              IL_0005:  dup
              IL_0006:  ldc.i4.5
              IL_0007:  callvirt   "void C.X.set"
              IL_000c:  dup
              IL_000d:  ldc.i4.s   10
              IL_000f:  callvirt   "void C.Add(int)"
              IL_0014:  dup
              IL_0015:  ldc.i4.s   20
              IL_0017:  callvirt   "void C.Add(int)"
              IL_001c:  ret
            }
            """);
    }

    #endregion
}
