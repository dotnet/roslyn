// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Interactive;
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
