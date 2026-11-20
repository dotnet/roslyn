// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Daemon;

internal static class DaemonHandshake
{
    public const string CopilotSessionIdEnvironmentVariable = "COPILOT_AGENT_SESSION_ID";

    private const int MaxSessionIdByteCount = 1024;
    private static readonly UTF8Encoding s_encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static void Write(Stream stream, string? copilotSessionId)
    {
        var byteCount = string.IsNullOrWhiteSpace(copilotSessionId) ? 0 : s_encoding.GetByteCount(copilotSessionId);
        if (byteCount > MaxSessionIdByteCount)
            throw new InvalidDataException("The Copilot session ID exceeds the daemon handshake size limit.");

        var buffer = new byte[sizeof(int) + byteCount];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, byteCount);
        if (byteCount > 0)
            s_encoding.GetBytes(copilotSessionId.AsSpan(), buffer.AsSpan(sizeof(int)));

        stream.Write(buffer);
    }

    public static async Task<string?> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var byteCount = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (byteCount < 0 || byteCount > MaxSessionIdByteCount)
            throw new InvalidDataException("The daemon handshake contains an invalid session ID length.");

        if (byteCount == 0)
            return null;

        var buffer = new byte[byteCount];
        await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        return s_encoding.GetString(buffer);
    }
}
