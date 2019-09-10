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
        [ImportingConstructor]
        public VisualStudioActiveStatementProvider()
        {
        }

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

                var completion = new TaskCompletionSource<ImmutableArray<ActiveStatementDebugInfo>>();
                var builders = default(ArrayBuilder<ArrayBuilder<ActiveStatementDebugInfo>>);
                var pendingRuntimes = 0;
                var runtimeCount = 0;

                // No exception should be thrown in case of errors on the debugger side. 
                // The debugger is responsible to provide telemetry for error cases.
                // The callback should not be called, but it's there to guarantee that the task completes and a hang is avoided.
                var workList = DkmWorkList.Create(_ => { completion.TrySetResult(ImmutableArray<ActiveStatementDebugInfo>.Empty); });

                void CancelWork()
                {
                    if (builders != null)
                    {
                        FreeBuilders(builders);
                        builders = null;

                        workList.Cancel(blockOnCompletion: false);

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

                            var runtimeIndex = runtimeCount;
                            runtimeCount++;

                            clrRuntimeInstance.GetActiveStatements(workList, activeStatementsResult =>
                            {
                                try
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        CancelWork();
                                        return;
                                    }

                                    var localBuilders = builders;
                                    if (localBuilders == null) // e.g. cancelled
                                    {
                                        return;
                                    }

                                    if (activeStatementsResult.ErrorCode != 0)
                                    {
                                        localBuilders[runtimeIndex] = ArrayBuilder<ActiveStatementDebugInfo>.GetInstance(0);

                                        // the last active statement of the last runtime has been processed:
                                        if (Interlocked.Decrement(ref pendingRuntimes) == 0)
                                        {
                                            completion.TrySetResult(localBuilders.ToFlattenedImmutableArrayAndFree());
                                        }

                                        return;
                                    }

                                    // group active statement by instruction and aggregate flags and threads:
                                    var instructionMap = PooledDictionary<ActiveInstructionId, (DkmInstructionSymbol Symbol, ArrayBuilder<Guid> Threads, int Index, ActiveStatementFlags Flags)>.GetInstance();

                                    GroupActiveStatementsByInstructionId(instructionMap, activeStatementsResult.ActiveStatements);

                                    var pendingStatements = instructionMap.Count;
                                    localBuilders[runtimeIndex] = ArrayBuilder<ActiveStatementDebugInfo>.GetInstance(pendingStatements);
                                    localBuilders[runtimeIndex].Count = pendingStatements;

                                    if (instructionMap.Count == 0)
                                    {
                                        if (Interlocked.Decrement(ref pendingRuntimes) == 0)
                                        {
                                            completion.TrySetResult(localBuilders.ToFlattenedImmutableArrayAndFree());
                                        }

                                        return;
                                    }

                                    foreach (var (instructionId, (symbol, threads, index, flags)) in instructionMap)
                                    {
                                        var immutableThreads = threads.ToImmutableAndFree();

                                        symbol.GetSourcePosition(workList, DkmSourcePositionFlags.None, InspectionSession: null, sourcePositionResult =>
                                        {
                                            try
                                            {
                                                if (cancellationToken.IsCancellationRequested)
                                                {
                                                    CancelWork();
                                                    return;
                                                }

                                                DkmSourcePosition position;
                                                string documentNameOpt;
                                                LinePositionSpan span;
                                                if (sourcePositionResult.ErrorCode == 0 && (position = sourcePositionResult.SourcePosition) != null)
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

                                                localBuilders[runtimeIndex][index] = new ActiveStatementDebugInfo(
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
                                                        completion.TrySetResult(localBuilders.ToFlattenedImmutableArrayAndFree());
                                                    }
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                completion.TrySetException(e);
                                            }
                                        });
                                    }

                                    instructionMap.Free();
                                }
                                catch (Exception e)
                                {
                                    completion.TrySetException(e);
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
