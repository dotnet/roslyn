// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

internal static class TestDebugger
{
    public static async Task<bool> AttachAsync(
        int processId,
        BufferedProgress<RunTestsPartialResult> progress,
        IClientLanguageServerManager clientLanguageServerManager,
        CancellationToken cancellationToken)
    {
        progress.Report(new RunTestsPartialResult(
            LanguageServerResources.Debugging_tests,
            string.Format(LanguageServerResources.Attaching_debugger_to_process_0, processId),
            Progress: null));

        var request = new DebugAttachParams(processId);
        var result = await clientLanguageServerManager.SendRequestAsync<DebugAttachParams, DebugAttachResult>(
            "workspace/attachDebugger",
            request,
            cancellationToken).ConfigureAwait(false);

        if (!result.DidAttach)
        {
            progress.Report(new RunTestsPartialResult(
                LanguageServerResources.Debugging_tests,
                LanguageServerResources.Client_failed_to_attach_the_debugger,
                Progress: null));
        }

        return result.DidAttach;
    }
}
