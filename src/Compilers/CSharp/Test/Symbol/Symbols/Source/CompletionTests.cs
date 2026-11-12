// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CompletionTests : CSharpTestBase
    {
        [Fact]
        public void SpinWaitCompleteAlreadyComplete()
        {
            var compilation = CreateCompilation("");
            string initialReport = GetWaitReport(compilation);
            var state = new SymbolCompletionState();
            var parts = CompletionPart.FinishBaseType | CompletionPart.FinishInterfaces;
            Assert.True(state.NotePartComplete(parts));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            state.SpinWaitComplete(parts, compilation, cancellation.Token);
            Assert.True(state.HasComplete(parts));
            Assert.Equal(initialReport, GetWaitReport(compilation));
        }

        [Fact]
        public void SpinWaitCompleteCanceled()
        {
            var compilation = CreateCompilation("");
            var state = new SymbolCompletionState();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var exception = Assert.Throws<OperationCanceledException>(() =>
                state.SpinWaitComplete(CompletionPart.FinishBaseType, compilation, cancellation.Token));
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.False(state.HasComplete(CompletionPart.FinishBaseType));
            Assert.Contains("completed=0 canceled=1 timedOut=0 interrupted=0", GetWaitReport(compilation));
        }

        [Fact]
        public void SpinWaitCompleteTimesOut()
        {
            var compilation = CreateCompilation("");
            var state = new SymbolCompletionState();
            Assert.True(state.NotePartComplete(CompletionPart.StartBaseType));
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var startTicks = unchecked((uint)Environment.TickCount);

            const uint timeoutMilliseconds = 1000;
            var exception = Assert.Throws<InvalidOperationException>(() =>
                state.SpinWaitCompleteCore(CompletionPart.FinishBaseType, timeoutMilliseconds, compilation, cancellation.Token));

            Assert.Equal("Symbol completion timed out.", exception.Message);
            Assert.True(unchecked((uint)Environment.TickCount - startTicks) >= timeoutMilliseconds);
            Assert.False(state.HasComplete(CompletionPart.FinishBaseType));
            Assert.Contains("completed=0 canceled=0 timedOut=1 interrupted=0", GetWaitReport(compilation));
        }

        [Fact]
        public void SymbolCompletionWaitHistogramConcurrentAndIsolated()
        {
            var compilation = CreateCompilation("", assemblyName: "WaitMeasurements");
            uint[] durations = [0, 1, 9, 10, 99, 100, 999, 1000, 4999, 5000, 9999, 10000, 29999, 30000, 59999, 60000, uint.MaxValue];

            Parallel.For(0, durations.Length, i =>
                compilation.RecordSymbolCompletionWait(durations[i], CSharpCompilation.SymbolCompletionWaitOutcome.Completed));

            Assert.Equal(
                "SymbolCompletionWaits: assembly=\"WaitMeasurements\" completed=17 canceled=0 timedOut=0 interrupted=0 totalWaitMs=4295179509 maxWaitMs=4294967295 ms[0]=1 ms[1,10)=2 ms[10,100)=2 ms[100,1000)=2 ms[1000,5000)=2 ms[5000,10000)=2 ms[10000,30000)=2 ms[30000,60000)=2 ms[60000,inf)=2" + Environment.NewLine,
                GetWaitReport(compilation));
            Assert.Equal(
                "SymbolCompletionWaits: assembly=\"WaitMeasurements\" completed=0 canceled=0 timedOut=0 interrupted=0 totalWaitMs=0 maxWaitMs=0 ms[0]=0 ms[1,10)=0 ms[10,100)=0 ms[100,1000)=0 ms[1000,5000)=0 ms[5000,10000)=0 ms[10000,30000)=0 ms[30000,60000)=0 ms[60000,inf)=0" + Environment.NewLine,
                GetWaitReport(compilation.Clone()));
        }

        private static string GetWaitReport(CSharpCompilation compilation)
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            compilation.ReportSymbolCompletionWaits(writer);
            return writer.ToString();
        }

        [Fact]
        public void FieldSymbolsAreLazy()
        {
            var text =
@"
class A {
    int x;
    NotFound y;
    public int F(int x, int y) {}
}
";
            var comp = CreateEmptyCompilation(text, skipUsesIsNullable: true);
            var global = comp.GlobalNamespace;

            var a = global.GetMember<NamedTypeSymbol>("A");
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(a.HasComplete(CompletionPart.Members));

            var x = a.GetMember<FieldSymbol>("x");
            Assert.True(a.HasComplete(CompletionPart.Members)); // getting one member completes the whole set
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(x.HasComplete(CompletionPart.Type));

            var xType = x.TypeWithAnnotations;
            Assert.True(x.HasComplete(CompletionPart.Type));
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));

            var y = a.GetMember<FieldSymbol>("y");
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(y.HasComplete(CompletionPart.Type));

            var yType = y.TypeWithAnnotations;
            Assert.True(y.HasComplete(CompletionPart.Type));
            Assert.False(a.HasComplete(CompletionPart.StartBaseType)); // needed to look in A's base for y's type

            var f = a.GetMember<MethodSymbol>("F");
            Assert.False(f.HasComplete(CompletionPart.StartMethodChecks));
            Assert.False(f.ReturnsVoid);
            Assert.True(f.HasComplete(CompletionPart.StartMethodChecks));
            Assert.True(f.HasComplete(CompletionPart.FinishMethodChecks));
        }

        [Fact]
        public void PropertySymbolsAreLazy()
        {
            var text =
@"class A
{
    object P { get; set; }
    object this[object o] { get { return null; } set { } }
}";
            var comp = CreateCompilation(text, skipUsesIsNullable: true);
            var global = comp.GlobalNamespace;

            var a = global.GetMember<NamedTypeSymbol>("A");
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(a.HasComplete(CompletionPart.Members));

            var p = a.GetMember<PropertySymbol>("P");
            Assert.True(a.HasComplete(CompletionPart.Members)); // getting one member completes the whole set
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));

            var pType = p.TypeWithAnnotations;
            var pParameters = p.Parameters;
            Assert.False(p.HasComplete(CompletionPart.Type));
            Assert.False(p.HasComplete(CompletionPart.Parameters));

            p = a.GetMember<PropertySymbol>("this[]");
            pType = p.TypeWithAnnotations;
            pParameters = p.Parameters;
            Assert.False(p.HasComplete(CompletionPart.Type));
            Assert.False(p.HasComplete(CompletionPart.Parameters));

            a.ForceComplete(null, filter: null, CancellationToken.None);
            Assert.True(p.HasComplete(CompletionPart.Type));
            Assert.True(p.HasComplete(CompletionPart.Parameters));
        }

        /// <summary>
        /// We used to have a problem where Symbol.NextIncompletePart read from
        /// Symbol.incompleteParts twice, rather than copying the field value
        /// into the temp.  If the value changed in between the reads, NextIncompletePart
        /// would return more than one part, which caused a deadlock in
        /// SourceNamedTypeSymbol.ForceComplete.  This test sometimes, but not always,
        /// failed before the fix was applied.  Now it documents the former problem
        /// and gives us some level of confidence in the fix.
        /// </summary>
        [Fact, WorkItem(546196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546196"), WorkItem(546604, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546604")]
        public void TestNextCompletionPart()
        {
            SymbolCompletionState state = new SymbolCompletionState();

            Action reader = () =>
            {
                while (state.IncompleteParts != 0)
                {
                    Assert.True(SymbolCompletionState.HasAtMostOneBitSet((int)state.NextIncompletePart));
                }
            };

            Action writers = () =>
            {
                Parallel.For(0, Math.Max(1, Environment.ProcessorCount - 1), t =>
                {
                    Random r = new Random(t);
                    while (state.IncompleteParts != 0)
                    {
                        CompletionPart part = (CompletionPart)(1 << r.Next(8 * sizeof(CompletionPart)));
                        state.NotePartComplete(part);
                    }
                });
            };

            for (int i = 0; i < 1000; i++)
            {
                Parallel.Invoke(reader, writers);
            }
        }

        /// <summary>
        /// This test demonstrates the correctness of <see cref="SymbolCompletionState.HasAtMostOneBitSet"/>.
        /// </summary>
        [Fact]
        public void TestHasAtMostOneBitSet()
        {
            for (int i = sbyte.MinValue; i <= sbyte.MaxValue; i++)
            {
                sbyte b = (sbyte)i;
                Assert.Equal(HasAtMostOneBitSetSafe(b), HasAtMostOneBitSetFast(b));
            }
        }

        /// <summary>
        /// This is the simple implementation of the sbyte version of <see cref="SymbolCompletionState.HasAtMostOneBitSet"/>.
        /// Hopefully, it is obviously correct.
        /// </summary>
        private static bool HasAtMostOneBitSetSafe(sbyte bits)
        {
            bool seenOne = false;
            for (int i = 0; i < sizeof(sbyte) * 8; i++)
            {
                if ((bits & (1 << i)) != 0)
                {
                    if (seenOne)
                    {
                        return false;
                    }
                    seenOne = true;
                }
            }
            return true;
        }

        /// <summary>
        /// This is the sbyte version of <see cref="SymbolCompletionState.HasAtMostOneBitSet"/>.
        /// It can be exhaustively tested more quickly than the full version.
        /// </summary>
        private static bool HasAtMostOneBitSetFast(sbyte bits)
        {
            return unchecked((bits & (sbyte)(bits - 1)) == 0);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/83978")]
        public void Repro_IsMetadataVirtualLocked_Assertion()
        {
            var source1 = """
                public struct U : I1
                {
                    public object Value => throw null!;
                }

                public interface I1
                {
                    object Value { get; }
                }
                """;

            var source2 = """
                class Program
                {
                    object M1(U u)
                    {
                        return u.Value;
                    }
                }
                """;

            var comp0 = CreateCompilation(source1);
            var comp = CreateCompilation(source2, references: [comp0.ToMetadataReference()]);
            comp.VerifyEmitDiagnostics();
            comp0.VerifyEmitDiagnostics();
        }
    }
}
