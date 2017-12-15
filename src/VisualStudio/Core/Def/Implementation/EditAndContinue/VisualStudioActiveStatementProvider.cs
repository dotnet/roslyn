// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    internal sealed class VisualStudioActiveStatementProvider : IActiveStatementProvider
    {
        public Task<ImmutableArray<ActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
        {
            // TODO: report errors
            // TODO: return empty outside of debug session.
            
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
                        clrRuntimeInstance.GetActiveStatements(workList, activeStatementsResult =>
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                workList.Cancel();
                                completion.SetCanceled();
                            }

                            int activeStatementCount = 0;
                            int pendingStatements = activeStatementsResult.ActiveStatements.Length;
                            builders[runtimeIndex] = ArrayBuilder<ActiveStatementDebugInfo>.GetInstance(pendingStatements);
                            builders[runtimeIndex].Count = pendingStatements;

                            foreach (var dkmStatement in activeStatementsResult.ActiveStatements)
                            {
                                int activeStatementIndex = activeStatementCount;
                                dkmStatement.InstructionSymbol.GetSourcePosition(workList, DkmSourcePositionFlags.None, InspectionSession: null, sourcePositionResult =>
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        workList.Cancel();
                                        completion.SetCanceled();
                                    }

                                    if (sourcePositionResult.ErrorCode == 0)
                                    {
                                        builders[runtimeIndex][activeStatementIndex] = new ActiveStatementDebugInfo(
                                            dkmStatement.Id,
                                            new ActiveInstructionId(
                                                dkmStatement.InstructionSymbol.Module.Id.Mvid,
                                                dkmStatement.InstructionAddress.MethodId.Token,
                                                dkmStatement.InstructionAddress.MethodId.Version,
                                                dkmStatement.InstructionAddress.ILOffset),
                                            sourcePositionResult.SourcePosition.DocumentName,
                                            ToLinePositionSpan(sourcePositionResult.SourcePosition.TextSpan),
                                            (ActiveStatementFlags)dkmStatement.Flags);
                                    }

                                    // the last active statement of the last runtime has been processed:
                                    if (Interlocked.Decrement(ref pendingStatements) == 0 && 
                                        Interlocked.Decrement(ref pendingRuntimes) == 0)
                                    {
                                        completion.SetResult(builders.ToImmutableArrayAndFree());
                                    }
                                });

                                activeStatementCount++;
                            }
                        });

                        runtimeCount++;
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

        private static LinePositionSpan ToLinePositionSpan(DkmTextSpan span)
            => new LinePositionSpan(new LinePosition(span.StartLine - 1, span.StartColumn - 1), new LinePosition(span.EndLine - 1, span.EndColumn - 1));
    }
}
