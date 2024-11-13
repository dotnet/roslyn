// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class CompilationEventTests : CompilingTestBase
    {
        internal static void VerifyEvents(AsyncQueue<CompilationEvent> queue, params string[] expectedEvents)
        {
            var expected = new HashSet<string>();
            foreach (var s in expectedEvents)
            {
                if (!expected.Add(s))
                {
                    Console.WriteLine("Expected duplicate " + s);
                }
            }

            var actual = ArrayBuilder<CompilationEvent>.GetInstance();
            while (queue.Count > 0 || !queue.IsCompleted)
            {
                var te = queue.DequeueAsync(CancellationToken.None);
                Assert.True(te.IsCompleted);
                actual.Add(te.Result);
            }
            bool unexpected = false;
            foreach (var a in actual)
            {
                var eventString = a.ToString();
                if (!expected.Remove(eventString))
                {
                    if (!unexpected)
                    {
                        Console.WriteLine("UNEXPECTED EVENTS:");
                        unexpected = true;
                    }
                    Console.WriteLine(eventString);
                }
            }
            if (expected.Count != 0)
            {
                Console.WriteLine("MISSING EVENTS:");
            }
            foreach (var e in expected)
            {
                Console.WriteLine(e);
            }
            if (unexpected || expected.Count != 0 || expectedEvents.Length != actual.Count)
            {
                bool first = true;
                Console.WriteLine("ACTUAL EVENTS:");
                foreach (var e in actual)
                {
                    if (!first)
                    {
                        Console.WriteLine(",");
                    }
                    first = false;
                    Console.Write("\"" + e.ToString() + "\"");
                }
                Console.WriteLine();
                Assert.True(false);
            }
        }

        [Fact]
        public void TestQueuedSymbols()
        {
            var source =
@"namespace N
{
  partial class C<T1>
  {
    partial void M(int x1);
    internal int P { get; private set; }
    int F = 12;
    void N<T2>(int y = 12) { F = F + 1; }
  }
  partial class C<T1>
  {
    partial void M(int x2) {}
  }
}";
            var q = new AsyncQueue<CompilationEvent>();
            CreateCompilationWithMscorlib461(source)
                .WithEventQueue(q)
                .VerifyDiagnostics(
                    // (12,18): warning CS8826: Partial method declarations 'void C<T1>.M(int x1)' and 'void C<T1>.M(int x2)' have signature differences.
                    //     partial void M(int x2) {}
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void C<T1>.M(int x1)", "void C<T1>.M(int x2)").WithLocation(12, 18)

                )  // force diagnostics twice
                .VerifyDiagnostics(
                    // (12,18): warning CS8826: Partial method declarations 'void C<T1>.M(int x1)' and 'void C<T1>.M(int x2)' have signature differences.
                    //     partial void M(int x2) {}
                    Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void C<T1>.M(int x1)", "void C<T1>.M(int x2)").WithLocation(12, 18)
                );
            VerifyEvents(q);
        }

        private static void VerifyEvents(AsyncQueue<CompilationEvent> q)
        {
            VerifyEvents(q,
                "CompilationStartedEvent",
                "SymbolDeclaredCompilationEvent(P int C<T1>.P @ : (5,4)-(5,40))",
                "SymbolDeclaredCompilationEvent(F int C<T1>.F @ : (6,8)-(6,14))",
                "SymbolDeclaredCompilationEvent(C C<T1> @ : (2,2)-(8,3), : (9,2)-(12,3))",
                "SymbolDeclaredCompilationEvent(M void C<T1>.M(int x1) @ : (4,4)-(4,27))",
                "SymbolDeclaredCompilationEvent(M void C<T1>.M(int x2) @ : (11,4)-(11,29))",
                "SymbolDeclaredCompilationEvent(N N @ : (0,0)-(13,1))",
                "SymbolDeclaredCompilationEvent(<empty>  @ : (0,0)-(13,1))",
                "SymbolDeclaredCompilationEvent(get_P int C<T1>.P.get @ : (5,21)-(5,25))",
                "SymbolDeclaredCompilationEvent(set_P void C<T1>.P.set @ : (5,26)-(5,38))",
                "SymbolDeclaredCompilationEvent(N void C<T1>.N<T2>(int y = 12) @ : (7,4)-(7,41))",
                "CompilationUnitCompletedEvent()",
                "CompilationCompletedEvent"
                );
        }

        [Fact]
        public void TestQueuedSymbolsAndGetUsedAssemblyReferences()
        {
            var source =
@"namespace N
{
  partial class C<T1>
  {
    partial void M(int x1);
    internal int P { get; private set; }
    int F = 12;
    void N<T2>(int y = 12) { F = F + 1; }
  }
  partial class C<T1>
  {
    partial void M(int x2) {}
  }
}";
            var q = new AsyncQueue<CompilationEvent>();
            var comp = CreateCompilationWithMscorlib461(source).WithEventQueue(q);
            comp.GetUsedAssemblyReferences();
            VerifyEvents(q);

            q = new AsyncQueue<CompilationEvent>();
            comp = CreateCompilationWithMscorlib461(source).WithEventQueue(q);
            comp.VerifyDiagnostics(
                // (12,18): warning CS8826: Partial method declarations 'void C<T1>.M(int x1)' and 'void C<T1>.M(int x2)' have signature differences.
                //     partial void M(int x2) {}
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void C<T1>.M(int x1)", "void C<T1>.M(int x2)").WithLocation(12, 18)
                );
            comp.GetUsedAssemblyReferences();
            VerifyEvents(q);

            q = new AsyncQueue<CompilationEvent>();
            comp = CreateCompilationWithMscorlib461(source).WithEventQueue(q);
            comp.GetUsedAssemblyReferences();
            comp.VerifyDiagnostics(
                // (12,18): warning CS8826: Partial method declarations 'void C<T1>.M(int x1)' and 'void C<T1>.M(int x2)' have signature differences.
                //     partial void M(int x2) {}
                Diagnostic(ErrorCode.WRN_PartialMethodTypeDifference, "M").WithArguments("void C<T1>.M(int x1)", "void C<T1>.M(int x2)").WithLocation(12, 18)
                );
            VerifyEvents(q);

            q = new AsyncQueue<CompilationEvent>();
            comp = CreateCompilationWithMscorlib461(source).WithEventQueue(q);
            comp.GetUsedAssemblyReferences();
            comp.GetUsedAssemblyReferences();
            VerifyEvents(q);
        }
    }
}
