// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
{
    public class VisualStudioActiveStatementProviderTests
    {
        [Fact]
        public void GroupActiveStatementsByInstructionId()
        {
            var module1 = new Guid("10000000-0000-0000-0000-000000000000");
            var module2 = new Guid("20000000-0000-0000-0000-000000000000");
            var thread1 = new Guid("01000000-0000-0000-0000-000000000000");
            var thread2 = new Guid("02000000-0000-0000-0000-000000000000");

            var map = new Dictionary<ActiveInstructionId, (DkmInstructionSymbol Symbol, ArrayBuilder<Guid> Threads, int Index, ActiveStatementFlags Flags)>();

            VisualStudioActiveStatementProvider.GroupActiveStatementsByInstructionId(map, new[]
            {
                // thread #1:
                new DkmActiveStatement(
                    thread1,
                    module1,
                    0x06000001,
                    methodVersion: 1,
                    ilOffset: 0,
                    DkmActiveStatementFlags.None | DkmActiveStatementFlags.MethodUpToDate),
                new DkmActiveStatement(
                    thread1,
                    module2,
                    0x06000001,
                    methodVersion: 1,
                    ilOffset: 0,
                    DkmActiveStatementFlags.Leaf | DkmActiveStatementFlags.MethodUpToDate),

                // thread #2:
                new DkmActiveStatement(
                    thread2,
                    module1,
                    0x06000001,
                    methodVersion: 1,
                    ilOffset: 0,
                    DkmActiveStatementFlags.MethodUpToDate),
                new DkmActiveStatement(
                    thread2,
                    module1,
                    0x06000002,
                    methodVersion: 1,
                    ilOffset: 2,
                    DkmActiveStatementFlags.MidStatement),
                new DkmActiveStatement(
                    thread2,
                    module1,
                    0x06000003,
                    methodVersion: 1,
                    ilOffset: 4,
                    DkmActiveStatementFlags.NonUser | DkmActiveStatementFlags.MethodUpToDate),
                new DkmActiveStatement(
                    thread2,
                    module1,
                    0x06000001,
                    methodVersion: 1,
                    ilOffset: 0,
                    DkmActiveStatementFlags.Leaf | DkmActiveStatementFlags.MethodUpToDate)
            });

            AssertEx.Equal(new[]
            {
                "0: mvid=10000000-0000-0000-0000-000000000000 0x06000001 v1 IL_0000 threads=[01000000-0000-0000-0000-000000000000,02000000-0000-0000-0000-000000000000,02000000-0000-0000-0000-000000000000] [IsLeafFrame, MethodUpToDate, IsNonLeafFrame]",
                "1: mvid=20000000-0000-0000-0000-000000000000 0x06000001 v1 IL_0000 threads=[01000000-0000-0000-0000-000000000000] [IsLeafFrame, MethodUpToDate]",
                "2: mvid=10000000-0000-0000-0000-000000000000 0x06000002 v1 IL_0002 threads=[02000000-0000-0000-0000-000000000000] [PartiallyExecuted, IsNonLeafFrame]",
                "3: mvid=10000000-0000-0000-0000-000000000000 0x06000003 v1 IL_0004 threads=[02000000-0000-0000-0000-000000000000] [NonUserCode, MethodUpToDate, IsNonLeafFrame]"
            }, map.OrderBy(e => e.Value.Index).Select(e => $"{e.Value.Index}: {e.Key.GetDebuggerDisplay()} threads=[{string.Join(",", e.Value.Threads)}] [{e.Value.Flags}]"));
        }

        [Fact]
        public void GroupActiveStatementsByInstructionId_InconsistentFlags()
        {
            var module1 = new Guid("10000000-0000-0000-0000-000000000000");
            var thread1 = new Guid("01000000-0000-0000-0000-000000000000");

            var map = new Dictionary<ActiveInstructionId, (DkmInstructionSymbol Symbol, ArrayBuilder<Guid> Threads, int Index, ActiveStatementFlags Flags)>();

            Assert.Throws<InvalidOperationException>(() => VisualStudioActiveStatementProvider.GroupActiveStatementsByInstructionId(map, new[]
            {
                // thread #1:
                new DkmActiveStatement(
                    thread1,
                    module1,
                    0x06000001,
                    methodVersion: 1,
                    ilOffset: 0,
                    DkmActiveStatementFlags.MethodUpToDate),

                // thread #2:
                new DkmActiveStatement(
                    thread1,
                    module1,
                    0x06000001,
                    methodVersion: 1,
                    ilOffset: 0,
                    DkmActiveStatementFlags.MidStatement)
            }));
        }

        [Theory]
        [InlineData(1, 2, 3, 4, 0, 1, 2, 3)]
        [InlineData(5, 0, 5, 0, 4, 0, 4, 0)]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(0, 2, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, 0, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, 0, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, 2, 0, 0, 0, 0, 0)]
        [InlineData(int.MinValue, 2, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, int.MinValue, 2, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, int.MinValue, 2, 0, 0, 0, 0)]
        [InlineData(2, 2, 2, int.MinValue, 0, 0, 0, 0)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue - 1, int.MaxValue - 1, int.MaxValue - 1, int.MaxValue - 1)]
        public void Span(int startLine, int startColumn, int endLine, int endColumn,
                         int expectedStartLine, int expectedStartColumn, int expectedEndLine, int expectedEndColumn)
        {
            var actual = VisualStudioActiveStatementProvider.ToLinePositionSpan(new DkmTextSpan(StartLine: startLine, EndLine: endLine, StartColumn: startColumn, EndColumn: endColumn));
            var expected = new LinePositionSpan(new LinePosition(expectedStartLine, expectedStartColumn), new LinePosition(expectedEndLine, expectedEndColumn));

            Assert.Equal(expected, actual);
        }
    }
}
