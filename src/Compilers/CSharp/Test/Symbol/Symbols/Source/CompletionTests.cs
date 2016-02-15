// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;

            var a = global.GetMember<NamedTypeSymbol>("A");
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(a.HasComplete(CompletionPart.Members));

            var x = a.GetMember<FieldSymbol>("x");
            Assert.True(a.HasComplete(CompletionPart.Members)); // getting one member completes the whole set
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(x.HasComplete(CompletionPart.Type));

            var xType = x.Type;
            Assert.True(x.HasComplete(CompletionPart.Type));
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));

            var y = a.GetMember<FieldSymbol>("y");
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(y.HasComplete(CompletionPart.Type));

            var yType = y.Type;
            Assert.True(y.HasComplete(CompletionPart.Type));
            Assert.False(a.HasComplete(CompletionPart.StartBaseType)); // needed to look in A's base for y's type

            var f = a.GetMember<MethodSymbol>("F");
            Assert.False(f.HasComplete(CompletionPart.StartMethodChecks));
            Assert.Equal(false, f.ReturnsVoid);
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
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;

            var a = global.GetMember<NamedTypeSymbol>("A");
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));
            Assert.False(a.HasComplete(CompletionPart.Members));

            var p = a.GetMember<PropertySymbol>("P");
            Assert.True(a.HasComplete(CompletionPart.Members)); // getting one member completes the whole set
            Assert.False(a.HasComplete(CompletionPart.StartBaseType));

            var pType = p.Type;
            var pParameters = p.Parameters;
            Assert.False(p.HasComplete(CompletionPart.Type));
            Assert.False(p.HasComplete(CompletionPart.Parameters));

            p = a.GetMember<PropertySymbol>("this[]");
            pType = p.Type;
            pParameters = p.Parameters;
            Assert.False(p.HasComplete(CompletionPart.Type));
            Assert.False(p.HasComplete(CompletionPart.Parameters));

            a.ForceComplete(null, CancellationToken.None);
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
        /// This test demonstrates the correctness of <see cref="Microsoft.CodeAnalysis.CSharp.Symbol.HasAtMostOneBitSet"/>.
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
        /// This is the simple implementation of the sbyte version of <see cref="Microsoft.CodeAnalysis.CSharp.Symbol.HasAtMostOneBitSet"/>.
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
        /// This is the sbyte version of <see cref="Microsoft.CodeAnalysis.CSharp.Symbol.HasAtMostOneBitSet"/>.
        /// It can be exhaustively tested more quickly than the full version.
        /// </summary>
        private static bool HasAtMostOneBitSetFast(sbyte bits)
        {
            return unchecked((bits & (sbyte)(bits - 1)) == 0);
        }
    }
}
