﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Represents the submission states and globals that get passed to a script entry point when run.
    /// </summary>
    internal sealed class ScriptExecutionState
    {
        private object[] _submissionStates;
        private int _count;
        private int _frozen;

        private ScriptExecutionState(object[] submissionStates, int count)
        {
            _submissionStates = submissionStates;
            _count = count;
        }

        public static ScriptExecutionState Create(object globals)
        {
            // first submission state is the globals.
            var submissionStates = new object[2];
            submissionStates[0] = globals;
            return new ScriptExecutionState(submissionStates, count: 1);
        }

        public ScriptExecutionState FreezeAndClone()
        {
            // freeze state so it can no longer be modified.
            var wasAlreadyFrozen = Interlocked.CompareExchange(ref _frozen, 1, 0) == 1;

            if (wasAlreadyFrozen)
            {
                // since only one state can add to the submissions, if its already been frozen and clone before
                // make a copy of the visible contents for the new state.
                var copy = new object[_count];
                Array.Copy(_submissionStates, copy, _count);
                return new ScriptExecutionState(copy, _count);
            }
            else
            {
                // cloned state can continue to add submission states
                return new ScriptExecutionState(_submissionStates, _count);
            }
        }

        public int SubmissionStateCount => _count;

        public object GetSubmissionState(int index)
        {
            Debug.Assert(index >= 0 && index < _count);
            return _submissionStates[index];
        }

        internal async Task<TResult> RunSubmissionsAsync<TResult>(
            ImmutableArray<Func<object[], Task>> precedingExecutors,
            Func<object[], Task> currentExecutor,
            StrongBox<Exception> exceptionHolderOpt,
            Func<Exception, bool> catchExceptionOpt,
            CancellationToken cancellationToken)
        {
            Debug.Assert(_frozen == 0);
            Debug.Assert((exceptionHolderOpt != null) == (catchExceptionOpt != null));

            // Each executor points to a <Factory> method of the Submission class.
            // The method creates an instance of the Submission class passing the submission states to its constructor.
            // The consturctor initializes the links between submissions and stores the Submission instance to 
            // a slot in submission states that corresponds to the submission.
            // The <Factory> method then calls the <Initialize> method that consists of top-level script code statements.

            int executorIndex = 0;
            try
            {
                while (executorIndex < precedingExecutors.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    EnsureStateCapacity();

                    try
                    {
                        await precedingExecutors[executorIndex++](_submissionStates).ConfigureAwait(continueOnCapturedContext: false);
                    }
                    finally
                    {
                        // The submission constructor always runs into completion (unless we emitted bad code).
                        // We need to advance the counter to reflect the updates to submission states done in the constructor.
                        AdvanceStateCounter();
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                TResult result;
                EnsureStateCapacity();

                try
                {
                    executorIndex++;
                    result = await ((Task<TResult>)currentExecutor(_submissionStates)).ConfigureAwait(continueOnCapturedContext: false);
                }
                finally
                {
                    // The submission constructor always runs into completion (unless we emitted bad code).
                    // We need to advance the counter to reflect the updates to submission states done in the constructor.
                    AdvanceStateCounter();
                }

                return result;
            }
            catch (Exception exception) when (catchExceptionOpt?.Invoke(exception) == true)
            {
                // The following code creates instances of all submissions without executing the user code.
                // The constructors don't contain any user code.
                var submissionCtorArgs = new object[] { null };

                while (executorIndex < precedingExecutors.Length)
                {
                    EnsureStateCapacity();

                    // update the value since the array might have been resized:
                    submissionCtorArgs[0] = _submissionStates;

                    Activator.CreateInstance(precedingExecutors[executorIndex++].GetMethodInfo().DeclaringType, submissionCtorArgs);
                    AdvanceStateCounter();
                }

                if (executorIndex == precedingExecutors.Length)
                {
                    EnsureStateCapacity();

                    // update the value since the array might have been resized:
                    submissionCtorArgs[0] = _submissionStates;

                    Activator.CreateInstance(currentExecutor.GetMethodInfo().DeclaringType, submissionCtorArgs);
                    AdvanceStateCounter();
                }

                exceptionHolderOpt.Value = exception;
                return default(TResult);
            }
        }

        private void EnsureStateCapacity()
        {
            // make sure there is enough free space for the submission to add its state
            if (_count >= _submissionStates.Length)
            {
                Array.Resize(ref _submissionStates, Math.Max(_count, _submissionStates.Length * 2));
            }
        }

        private void AdvanceStateCounter()
        {
            // check to see if state was added (submissions that don't make declarations don't add state)
            if (_submissionStates[_count] != null)
            {
                _count++;
            }
        }
    }
}
