// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    [Export(typeof(IActiveStatementProvider)), Shared]
    internal sealed partial class VisualStudioActiveStatementProvider : IActiveStatementProvider
    {
        /// <summary>
        /// Retrieves active statements from the debuggee process.
        /// Shall only be called while in debug mode.
        /// Can be invoked on any thread.
        /// </summary>
        public Task<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
        {
            using (DebuggerComponent.ManagedEditAndContinueService())
            {
                // TODO: return empty outside of debug session.
                // https://github.com/dotnet/roslyn/issues/24325

                int unexpectedError = 0;
                var completion = new TaskCompletionSource<ImmutableArray<ActiveStatementDebugInfo>>();
                var builders = default(ArrayBuilder<ArrayBuilder<ActiveStatementDebugInfo>>);
                int pendingRuntimes = 0;
                int runtimeCount = 0;

                var workList = DkmWorkList.Create(CompletionRoutine: _ =>
                {
                    completion.TrySetException(new InvalidOperationException($"Unexpected error enumerating active statements: 0x{unexpectedError:X8}"));
                });

                void CancelWork()
                {
                    if (builders != null)
                    {
                        FreeBuilders(builders);
                        builders = null;

                        // TODO: DkmWorkList.Cancel doesn't currently work when invoked on the completion callback.
                        // We continue execute all the queued callbacks -- they will be no-ops.
                        // See https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/562781.
                        // 
                        // workList.Cancel();

                        // make sure we cancel with the token we received from the caller:
                        completion.TrySetCanceled(cancellationToken);
                    }
                }

                foreach (var process in DkmProcess.GetProcesses())
                {
                    foreach (var runtimeInstance in process.GetRuntimeInstances())
                    {
                        if (runtimeInstance.TagValue == DkmRuntimeInstance.Tag.ClrRuntimeInstance)
                        {
                            var clrRuntimeInstance = (DkmClrRuntimeInstance)runtimeInstance;

                            int runtimeIndex = runtimeCount;
                            runtimeCount++;

                            clrRuntimeInstance.GetActiveStatements(workList, activeStatementsResult =>
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    CancelWork();
                                    return;
                                }

                                if (activeStatementsResult.ErrorCode != 0)
                                {
                                    unexpectedError = activeStatementsResult.ErrorCode;
                                    return;
                                }

                                // group active statement by instruction and aggregate flags and threads:
                                var instructionMap = PooledDictionary<ActiveInstructionId, (DkmInstructionSymbol Symbol, ArrayBuilder<Guid> Threads, int Index, ActiveStatementFlags Flags)>.GetInstance();

                                GroupActiveStatementsByInstructionId(instructionMap, activeStatementsResult.ActiveStatements);

                                int pendingStatements = instructionMap.Count;
                                builders[runtimeIndex] = ArrayBuilder<ActiveStatementDebugInfo>.GetInstance(pendingStatements);
                                builders[runtimeIndex].Count = pendingStatements;

                                foreach (var (instructionId, (symbol, threads, index, flags)) in instructionMap)
                                {
                                    var immutableThreads = threads.ToImmutableAndFree();

                                    symbol.GetSourcePosition(workList, DkmSourcePositionFlags.None, InspectionSession: null, sourcePositionResult =>
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            CancelWork();
                                            return;
                                        }

                                        int errorCode = sourcePositionResult.ErrorCode;
                                        if (errorCode != 0)
                                        {
                                            unexpectedError = errorCode;
                                        }

                                        DkmSourcePosition position;
                                        string documentNameOpt;
                                        LinePositionSpan span;
                                        if (errorCode == 0 && (position = sourcePositionResult.SourcePosition) != null)
                                        {
                                            documentNameOpt = position.DocumentName;
                                            span = ToLinePositionSpan(position.TextSpan);
                                        }
                                        else
                                        {
                                            // The debugger can't determine source location for the active statement.
                                            // The PDB might not be available or the statement is in a method that doesn't have debug information.
                                            documentNameOpt = null;
                                            span = default;
                                        }

                                        builders[runtimeIndex][index] = new ActiveStatementDebugInfo(
                                            instructionId,
                                            documentNameOpt,
                                            span,
                                            immutableThreads,
                                            flags);

                                        // the last active statement of the current runtime has been processed:
                                        if (Interlocked.Decrement(ref pendingStatements) == 0)
                                        {
                                            // the last active statement of the last runtime has been processed:
                                            if (Interlocked.Decrement(ref pendingRuntimes) == 0)
                                            {
                                                completion.TrySetResult(builders.ToFlattenedImmutableArrayAndFree());
                                            }
                                        }
                                    });
                                }

                                instructionMap.Free();
                            });
                        }
                    }
                }

                pendingRuntimes = runtimeCount;
                builders = ArrayBuilder<ArrayBuilder<ActiveStatementDebugInfo>>.GetInstance(runtimeCount);
                builders.Count = runtimeCount;

                // Start execution of the Concord work items.
                workList.BeginExecution();

                return completion.Task;
            }
        }

        private static void FreeBuilders(ArrayBuilder<ArrayBuilder<ActiveStatementDebugInfo>> builders)
        {
            foreach (var builderArray in builders)
            {
                builderArray?.Free();
            }

            builders.Free();
        }
    }
}
