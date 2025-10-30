// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal sealed partial class TestRunner
{
    private sealed class DebugTestHostLauncher(BufferedProgress<RunTestsPartialResult> progress, IClientLanguageServerManager clientLanguageServerManager) : ITestHostLauncher2, ITestHostLauncher3
    {
        public bool IsDebug => true;

        public bool AttachDebuggerToProcess(int pid)
        {
            return AttachDebugger(pid, CancellationToken.None);
        }

        public bool AttachDebuggerToProcess(int pid, CancellationToken cancellationToken)
        {
            return AttachDebugger(pid, cancellationToken);
        }

        public bool AttachDebuggerToProcess(AttachDebuggerInfo attachDebuggerInfo, CancellationToken cancellationToken)
        {
            return AttachDebugger(attachDebuggerInfo.ProcessId, cancellationToken);
        }

        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
        {
            // This is not called anymore in modern client and vstest.console.
            throw new NotImplementedException();
        }

        public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
        {
            // This is not called anymore in modern client and vstest.console.
            throw new NotImplementedException();
        }

        private bool AttachDebugger(int processId, CancellationToken cancellationToken)
        {
            progress.Report(new RunTestsPartialResult(LanguageServerResources.Debugging_tests, string.Format(LanguageServerResources.Attaching_debugger_to_process_0, processId), Progress: null));

            // Send an explicit request to the client to tell it to attach to the debugger and wait for the response.
            // We want to wait for the attach to complete before we continue.
            var task = Task.Run(async () => await AttachDebuggerAsync(processId, cancellationToken), cancellationToken);
            return task.WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }

        private async Task<bool> AttachDebuggerAsync(int processId, CancellationToken cancellationToken)
        {
            var request = new DebugAttachParams(processId);
            var result = await clientLanguageServerManager.SendRequestAsync<DebugAttachParams, DebugAttachResult>("workspace/attachDebugger", request, cancellationToken);
            if (!result.DidAttach)
            {
                progress.Report(new RunTestsPartialResult(LanguageServerResources.Debugging_tests, LanguageServerResources.Client_failed_to_attach_the_debugger, Progress: null));
            }

            return result.DidAttach;
        }
    }
}
