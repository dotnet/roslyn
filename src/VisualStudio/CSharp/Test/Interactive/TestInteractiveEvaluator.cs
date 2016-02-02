// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    internal sealed class TestInteractiveEvaluator : IInteractiveEvaluator
    {
        internal event EventHandler<string> OnExecute;

        public IInteractiveWindow CurrentWindow { get; set; }

        public void Dispose()
        {
        }

        public Task<ExecutionResult> InitializeAsync()
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public bool CanExecuteCode(string text)
        {
            return true;
        }

        public Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            OnExecute?.Invoke(this, text);
            return Task.FromResult(ExecutionResult.Success);
        }

        public string FormatClipboard()
        {
            return null;
        }

        public void AbortExecution()
        {
        }

        public string GetConfiguration()
        {
            return "config";
        }

        public string GetPrompt()
        {
            return "> ";
        }
    }
}
