// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive
{
    internal sealed class TestInteractiveEvaluator : IInteractiveEvaluator, IResettableInteractiveEvaluator
    {
        internal event EventHandler<string> OnExecute;

        public IInteractiveWindow CurrentWindow { get; set; }
        public InteractiveEvaluatorResetOptions ResetOptions { get; set; }

        public void Dispose()
        {
        }

        public Task<ExecutionResult> InitializeAsync()
            => Task.FromResult(ExecutionResult.Success);

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
            => Task.FromResult(ExecutionResult.Success);

        public bool CanExecuteCode(string text)
            => true;

        public Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            OnExecute?.Invoke(this, text);
            return Task.FromResult(ExecutionResult.Success);
        }

        public string FormatClipboard()
            => null;

        public void AbortExecution()
        {
        }

        public string GetConfiguration()
            => "config";

        public string GetPrompt()
            => "> ";

        public Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory)
            => Task.CompletedTask;
    }
}
