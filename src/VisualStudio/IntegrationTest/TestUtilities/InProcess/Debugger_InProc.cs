// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Debugger_InProc : InProcComponent
    {
        /// <summary>
        /// HResult for "Operation Not Supported" when raising commands. 
        /// </summary>
        private const uint OperationNotSupportedHResult = 0x8971003c;

        /// <summary>
        /// Time to wait between retries if "Operation Not Supported" is thrown when raising a debugger stepping command.
        /// </summary>
        private static readonly TimeSpan DebuggerCommandRetryTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Time to wait before re-polling a delegate.
        /// </summary>
        private static readonly TimeSpan DefaultPollingInterCallSleep = TimeSpan.FromMilliseconds(250);

        private readonly EnvDTE.Debugger _debugger;

        private Debugger_InProc()
        {
            _debugger = GetDTE().Debugger;
        }

        public static Debugger_InProc Create()
            => new Debugger_InProc();

        public void SetBreakPoint(string fileName, int lineNumber, int columnIndex)
        {
            // Need to increment the line number because editor line numbers starts from 0 but the debugger ones starts from 1.
            _debugger.Breakpoints.Add(File: fileName, Line: lineNumber + 1, Column: columnIndex);
        }

        public void Go(bool waitForBreakMode) => _debugger.Go(waitForBreakMode);

        public void StepOver(bool waitForBreakOrEnd) => this.WaitForRaiseDebuggerDteCommand(() => _debugger.StepOver(waitForBreakOrEnd));

        public void Stop(bool waitForDesignMode) => _debugger.Stop(WaitForDesignMode: waitForDesignMode);

        public void SetNextStatement() => _debugger.SetNextStatement();

        public void ExecuteStatement(string statement) => _debugger.ExecuteStatement(statement);

        public Common.Expression GetExpression(string expressionText) => new Common.Expression(_debugger.GetExpression(expressionText));

        /// <summary>
        /// Executes the specified action delegate and retries if Operation Not Supported is thrown.
        /// </summary>
        /// <param name="action">Action delegate to exectute.</param>
        private void WaitForRaiseDebuggerDteCommand(Action action)
        {
            var actionSucceeded = false;

            Func<bool> predicate = delegate
            {
                try
                {
                    action();
                    actionSucceeded = true;
                }
                catch (COMException ex)
                {
                    if ((uint)ex.ErrorCode != OperationNotSupportedHResult)
                    {
                        var message = string.Format(
                            CultureInfo.InvariantCulture,
                            "Failed to raise debugger command, an unexpected '{0}' was thrown with the HResult of '{1}'.",
                            typeof(COMException),
                            ex.ErrorCode);

                        throw new Exception(message, ex);
                    }

                    actionSucceeded = false;
                }

                return actionSucceeded;
            };

            // Repeat the command if "Operation Not Supported" is thrown.
            if (!TryWaitFor(DebuggerCommandRetryTimeout, predicate))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to raise debugger command within '{0}' seconds.",
                    DebuggerCommandRetryTimeout.TotalSeconds);

                throw new Exception(message);
            }
        }

        /// <summary>
        /// Polls for the specified delegate to return true for the given timeout.
        /// </summary>
        /// <param name="timeout">Timeout to keep polling.</param>
        /// <param name="predicate">Delegate to invoke.</param>
        /// <returns>True if the delegate returned true when polled, otherwise false.</returns>
        public static bool TryWaitFor(TimeSpan timeout, Func<bool> predicate)
        {
            return TryWaitFor(timeout, DefaultPollingInterCallSleep, predicate);
        }

        /// <summary>
        /// Polls for the specified delegate to return true for the given timeout.
        /// </summary>
        /// <param name="timeout">Timeout to keep polling.</param>
        /// <param name="interval">Time to wait between polling.</param>
        /// <param name="predicate">Delegate to invoke.</param>
        /// <returns>
        /// True if the delegate returned true when polled, otherwise false.
        /// </returns>
        private static bool TryWaitFor(TimeSpan timeout, TimeSpan interval, Func<bool> predicate)
        {
            var endTime = DateTime.UtcNow + timeout;
            var validationDelegateSuccess = false;

            while (DateTime.UtcNow < endTime)
            {
                // Note: we don't do this inline in the while() condition and return the result of 
                // (DateTime.Now < startTime + timeout) as this could lead to cases where the validation
                // delegate returned true, at the boundary of the valid time, and would return false
                // when hitting the return statement. 
                if (predicate())
                {
                    validationDelegateSuccess = true;
                    break;
                }

                System.Threading.Thread.Sleep(interval);
            }

            return validationDelegateSuccess;
        }
    }
}
