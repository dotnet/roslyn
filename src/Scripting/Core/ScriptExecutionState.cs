// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Represents the submission states and globals that get passed to a script entry point when run.
    /// </summary>
    internal class ScriptExecutionState
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

        public int Count
        {
            get { return _count; }
        }

        public object this[int index]
        {
            get { return _submissionStates[index]; }
        }

        /// <summary>
        /// Run's the submission with this state. Submission's state get added to this as a side-effect.
        /// </summary>
        public T RunSubmission<T>(Func<object[], T> submissionRunner)
        {
            if (_frozen != 0)
            {
                throw new InvalidOperationException(ScriptingResources.ExecutionStateFrozen);
            }

            // make sure there is enough free space for the submission to add its state
            if (_count >= _submissionStates.Length)
            {
                Array.Resize(ref _submissionStates, Math.Max(_count + 1, _submissionStates.Length * 2));
            }

            var result = submissionRunner(_submissionStates);

            // check to see if state was added (submissions that don't make declarations don't add state)
            if (_submissionStates[_count] != null)
            {
                _count++;
            }

            return result;
        }
    }
}
