// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.Interop;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess
{
    [TestService]
    internal partial class DebuggerInProcess
    {
        private async Task<EnvDTE100.Debugger5> GetDebuggerAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            return (EnvDTE100.Debugger5)dte.Debugger;
        }

        public Task SetBreakpointAsync(string fileName, string text, CancellationToken cancellationToken)
            => SetBreakpointAsync(fileName, text, charsOffset: 0, cancellationToken);

        public async Task SetBreakpointAsync(string fileName, string text, int charsOffset, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await TestServices.Editor.ActivateAsync(cancellationToken);
            await TestServices.Editor.SelectTextInCurrentDocumentAsync(text, cancellationToken);

            var caretPosition = await TestServices.Editor.GetCaretPositionAsync(cancellationToken);
            caretPosition.BufferPosition.GetLineAndCharacter(out var lineNumber, out var characterIndex);
            await SetBreakpointAsync(fileName, lineNumber, characterIndex + charsOffset, cancellationToken);
        }

        public async Task SetBreakpointAsync(string fileName, int lineNumber, int characterIndex, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var debugger = await GetDebuggerAsync(cancellationToken);
            // Need to increment the line number because editor line numbers starts from 0 but the debugger ones starts from 1.
            debugger.Breakpoints.Add(File: fileName, Line: lineNumber + 1, Column: characterIndex);
        }

        public async Task GoAsync(bool waitForBreakMode, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var debugger = await GetDebuggerAsync(cancellationToken);
            debugger.Go(waitForBreakMode);
        }
    }
}
