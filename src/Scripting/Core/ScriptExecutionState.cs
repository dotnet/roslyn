// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
            CancellationToken cancellationToken)
        {
            Debug.Assert(_frozen == 0);

            foreach (var executor in precedingExecutors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                EnsureStateCapacity();
                await executor(_submissionStates).ConfigureAwait(continueOnCapturedContext: false);
                AdvanceStateCounter();
            }

            cancellationToken.ThrowIfCancellationRequested();

            EnsureStateCapacity();
            TResult result = await ((Task<TResult>)currentExecutor(_submissionStates)).ConfigureAwait(continueOnCapturedContext: false);
            AdvanceStateCounter();

            return result;
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
