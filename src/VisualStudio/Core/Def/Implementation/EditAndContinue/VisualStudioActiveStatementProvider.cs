// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;
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
        /// Retrives active statements from the debuggee process.
        /// Shall only be called while in debug mode.
        /// Can be invoked on any thread.
        /// </summary>
        public Task<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
        {
            DkmComponentManager.InitializeThread(DebuggerComponentIds.ManagedEditAndContinueService);
            try
            {
                // TODO: report errors
                // TODO: return empty outside of debug session.
                // https://github.com/dotnet/roslyn/issues/24325

                var workList = DkmWorkList.Create(CompletionRoutine: null);
                var completion = new TaskCompletionSource<ImmutableArray<ActiveStatementDebugInfo>>();
                var builders = default(ArrayBuilder<ArrayBuilder<ActiveStatementDebugInfo>>);
                int pendingRuntimes = 0;
                int runtimeCount = 0;

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
                                    workList.Cancel();

                                    // make sure we cancel with the token we received from the caller:
                                    completion.TrySetCanceled(cancellationToken);
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
                                    symbol.GetSourcePosition(workList, DkmSourcePositionFlags.None, InspectionSession: null, sourcePositionResult =>
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            workList.Cancel();

                                            // make sure we cancel with the token we received from the caller:
                                            completion.TrySetCanceled(cancellationToken);
                                            return;
                                        }

                                        if (sourcePositionResult.ErrorCode == 0)
                                        {
                                            builders[runtimeIndex][index] = new ActiveStatementDebugInfo(
                                                instructionId,
                                                sourcePositionResult.SourcePosition.DocumentName,
                                                ToLinePositionSpan(sourcePositionResult.SourcePosition.TextSpan),
                                                threads.ToImmutableAndFree(),
                                                flags);
                                        }

                                        // the last active statement of the current runtime has been processed:
                                        if (Interlocked.Decrement(ref pendingStatements) == 0)
                                        {
                                            instructionMap.Free();

                                            // the last active statement of the last runtime has been processed:
                                            if (Interlocked.Decrement(ref pendingRuntimes) == 0)
                                            {
                                                completion.SetResult(builders.ToFlattenedImmutableArrayAndFree());
                                            }
                                        }
                                    });
                                }
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
            finally
            {
                // Since we don't own this thread we need to uninitialize the dispatcher. Otherwise if the thread is reused by another bit of code 
                // trying to call into the Concord API than its InitializeThread will fail.
                // The work items we queued to the work list above will be run on a dedicated thread maintained by the Concord dispatcher.
                DkmComponentManager.UninitializeThread(DebuggerComponentIds.ManagedEditAndContinueService);
            }
        }
    }
}
