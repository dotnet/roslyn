// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal enum RelayEndpoint
{
    Editor,
    Server,
}

internal readonly struct RelayResult(RelayEndpoint closedEndpoint, bool bothSidesClosed)
{

    /// <summary>The endpoint whose stream closed first, ending the relay.</summary>
    public RelayEndpoint ClosedEndpoint { get; } = closedEndpoint;

    /// <summary>
    /// True when, shortly after the first side closed, the other side also closed on its own. A clean LSP
    /// shutdown closes both sides (the editor sends <c>exit</c> and closes; the server processes it and
    /// closes), whereas a crash leaves one side connected.
    /// </summary>
    public bool BothSidesClosed { get; } = bothSidesClosed;
}

internal static class LspRelay
{
    /// <summary>
    /// Grace period to wait for the second side to close after the first does, so a clean shutdown (which
    /// closes both) can be distinguished from a one-sided disconnect (a crash).
    /// </summary>
    private static readonly TimeSpan s_secondCloseGracePeriod = TimeSpan.FromSeconds(5);

    public static async Task<RelayResult> RelayAsync(
        Stream editorInput,
        Stream editorOutput,
        Stream serverInput,
        Stream serverOutput)
    {
        using var cancellationSource = new CancellationTokenSource();
        var editorToServer = CopyUntilClosedAsync(editorInput, serverOutput, RelayEndpoint.Editor, RelayEndpoint.Server, cancellationSource.Token);
        var serverToEditor = CopyUntilClosedAsync(serverInput, editorOutput, RelayEndpoint.Server, RelayEndpoint.Editor, cancellationSource.Token);
        var completedTask = await Task.WhenAny(editorToServer, serverToEditor).ConfigureAwait(false);

        // Give the other direction a brief window to finish on its own. If it does, both sides closed, which
        // indicates a clean shutdown rather than a crash on one side.
        var otherTask = completedTask == editorToServer ? serverToEditor : editorToServer;
        var bothSidesClosed = await Task.WhenAny(otherTask, Task.Delay(s_secondCloseGracePeriod)).ConfigureAwait(false) == otherTask;

        cancellationSource.Cancel();
        var result = await completedTask.ConfigureAwait(false);
        return new RelayResult(result, bothSidesClosed);
    }

    private static async Task<RelayEndpoint> CopyUntilClosedAsync(
        Stream input,
        Stream output,
        RelayEndpoint inputEndpoint,
        RelayEndpoint outputEndpoint,
        CancellationToken cancellationToken)
    {
        var result = await ProcessUtilities.CopyStreamAsync(input, output, cancellationToken).ConfigureAwait(false);
        return result switch
        {
            StreamCopyCompletion.SourceClosed or StreamCopyCompletion.SourceException or StreamCopyCompletion.Cancelled => inputEndpoint,
            StreamCopyCompletion.DestinationException => outputEndpoint,
            _ => throw new InvalidOperationException($"Unexpected stream copy completion kind: {result}"),
        };
    }
}
