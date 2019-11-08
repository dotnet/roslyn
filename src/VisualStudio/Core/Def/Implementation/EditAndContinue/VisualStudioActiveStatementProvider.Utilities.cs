// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Utilities;

#if TESTS
namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
#else
namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
#endif
{
    // Utilities that are testable. Linked to test project to allow mocking.
    partial class VisualStudioActiveStatementProvider
    {
        // internal for testing
        internal static void GroupActiveStatementsByInstructionId(
            Dictionary<ActiveInstructionId, (DkmInstructionSymbol Symbol, ArrayBuilder<Guid> Threads, int Index, ActiveStatementFlags Flags)> instructionMap,
            IEnumerable<DkmActiveStatement> dkmStatements)
        {
            foreach (var dkmStatement in dkmStatements)
            {
                // flags whose value only depends on the active instruction:
                const DkmActiveStatementFlags instructionFlagsMask = DkmActiveStatementFlags.MethodUpToDate | DkmActiveStatementFlags.NonUser;
                var instructionFlags = (ActiveStatementFlags)(dkmStatement.Flags & instructionFlagsMask);

                var isLeaf = (dkmStatement.Flags & DkmActiveStatementFlags.Leaf) != 0;

                // MidStatement is set differently for leaf frames and non-leaf frames.
                // We aggregate it so that if any frame has MidStatement the ActiveStatement is considered partially executed.
                var frameFlags =
                    (isLeaf ? ActiveStatementFlags.IsLeafFrame : ActiveStatementFlags.IsNonLeafFrame) |
                    (ActiveStatementFlags)(dkmStatement.Flags & DkmActiveStatementFlags.MidStatement);

                var instruction = dkmStatement.InstructionAddress;

                var instructionId = new ActiveInstructionId(
                    instruction.ModuleInstance.Mvid,
                    instruction.MethodId.Token,
                    unchecked((int)instruction.MethodId.Version),
                    unchecked((int)instruction.ILOffset));

                if (instructionMap.TryGetValue(instructionId, out var entry))
                {
                    // all flags, except for LeafFrame should be the same for active statements whose instruction ids are the same:
                    Contract.ThrowIfFalse(instructionFlags == (entry.Flags & (ActiveStatementFlags)instructionFlagsMask), "Inconsistent active statement flags");

                    entry.Flags |= frameFlags;
                }
                else
                {
                    entry = (dkmStatement.InstructionSymbol, ArrayBuilder<Guid>.GetInstance(1), instructionMap.Count, instructionFlags | frameFlags);
                }

                instructionMap[instructionId] = entry;
                entry.Threads.Add(dkmStatement.Thread.UniqueId);
            }
        }

        // internal for testing
        internal static LinePositionSpan ToLinePositionSpan(DkmTextSpan span)
        {
            // ignore invalid/unsupported spans - they might come from stack frames of non-managed languages
            if (span.StartLine <= 0 || span.EndLine <= 0)
            {
                return default;
            }

            // C++ produces spans without columns
            if (span is { StartColumn: 0, EndColumn: 0 })
            {
                return new LinePositionSpan(new LinePosition(span.StartLine - 1, 0), new LinePosition(span.EndLine - 1, 0));
            }

            // ignore invalid/unsupported spans - they might come from stack frames of non-managed languages
            if (span.StartColumn <= 0 || span.EndColumn <= 0)
            {
                return default;
            }

            return new LinePositionSpan(new LinePosition(span.StartLine - 1, span.StartColumn - 1), new LinePosition(span.EndLine - 1, span.EndColumn - 1));
        }
    }
}
