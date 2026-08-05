// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal enum RelayEndpoint
{
    Editor,
    Server,
}

internal enum RelayCompletionKind
{
    CleanShutdown,
    EditorConnectionLost,
    ServerConnectionLost,
}

internal static class LspRelay
{
    /// <summary>
    /// Grace period to wait for the second side to close after the first does, so a clean shutdown (which
    /// closes both) can be distinguished from a one-sided disconnect (a crash).
    /// </summary>
    private static readonly TimeSpan s_secondCloseGracePeriod = TimeSpan.FromSeconds(5);

    public static async Task<RelayCompletionKind> RelayAsync(
        Stream fromEditor,
        Stream toEditor,
        Stream fromServer,
        Stream toServer)
    {
        using var cancellationSource = new CancellationTokenSource();
        var editorToServer = CopyUntilClosedAsync(fromEditor, toServer, RelayEndpoint.Editor, RelayEndpoint.Server, cancellationSource.Token);
        var serverToEditor = CopyUntilClosedAsync(fromServer, toEditor, RelayEndpoint.Server, RelayEndpoint.Editor, cancellationSource.Token);
        var completedTask = await Task.WhenAny(editorToServer, serverToEditor).ConfigureAwait(false);

        // Give the other direction a brief window to finish on its own. If both copies terminate at the server,
        // the server connection was lost and caused both directions to stop. Any other pair is a clean shutdown:
        // an editor closes its bidirectional transport after sending LSP 'exit', so both copies can terminate at
        // the editor before the server closes its side.
        var otherTask = completedTask == editorToServer ? serverToEditor : editorToServer;
        RelayEndpoint? otherClosedEndpoint = null;
        if (await Task.WhenAny(otherTask, Task.Delay(s_secondCloseGracePeriod)).ConfigureAwait(false) == otherTask)
            otherClosedEndpoint = await otherTask.ConfigureAwait(false);

        cancellationSource.Cancel();
        var closedEndpoint = await completedTask.ConfigureAwait(false);

        if (otherClosedEndpoint is not null)
        {
            return closedEndpoint == RelayEndpoint.Server && otherClosedEndpoint == RelayEndpoint.Server
                ? RelayCompletionKind.ServerConnectionLost
                : RelayCompletionKind.CleanShutdown;
        }

        return closedEndpoint == RelayEndpoint.Editor
            ? RelayCompletionKind.EditorConnectionLost
            : RelayCompletionKind.ServerConnectionLost;
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
