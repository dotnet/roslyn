// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipes;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal enum StreamCopyCompletion
{
    SourceClosed,
    SourceException,
    DestinationException,
    Cancelled,
}

internal static class ProcessUtilities
{
    private const int BufferSize = 64 * 1024;

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/> byte for byte, flushing after every read so
    /// forwarded data (LSP messages in stdio transport, or diagnostics) is delivered promptly. Runs on a background
    /// thread so a blocking console read never stalls the caller. Returns when the source ends, either stream faults,
    /// or forwarding is cancelled.
    /// </summary>
    public static Task ForwardStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            _ = await CopyStreamAsync(source, destination, cancellationToken).ConfigureAwait(false);
        }, CancellationToken.None);
    }

    public static async Task<StreamCopyCompletion> CopyStreamAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];

        try
        {
            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                {
                    return StreamCopyCompletion.SourceException;
                }

                if (bytesRead == 0)
                    return StreamCopyCompletion.SourceClosed;

                try
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                {
                    return StreamCopyCompletion.DestinationException;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StreamCopyCompletion.Cancelled;
        }
    }

    public static string GetCommandLineForDisplay(ServerExecutable executable, IReadOnlyList<string> arguments)
    {
        var parts = new List<string>();
        parts.Add(executable.FileName);
        parts.AddRange(arguments);
        return string.Join(" ", parts.Select(QuoteForDisplay));
    }

    private static string QuoteForDisplay(string value)
    {
        if (value.Length == 0)
            return "\"\"";

        if (!value.Any(static c => char.IsWhiteSpace(c) || c == '"'))
            return value;

        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
