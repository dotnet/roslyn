// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal sealed class EditorConnection : IDisposable
{
    private readonly IDisposable? _disposable;

    private EditorConnection(Stream input, Stream output, IDisposable? disposable)
    {
        Input = input;
        Output = output;
        _disposable = disposable;
    }

    public Stream Input { get; }
    public Stream Output { get; }

    public static async Task<EditorConnection> CreateAsync(ThinClientArguments arguments)
    {
        if (arguments.EditorTransportKind == EditorTransportKind.StandardInputOutput)
        {
            return new EditorConnection(
                Console.OpenStandardInput(),
                Console.OpenStandardOutput(),
                disposable: null);
        }

        var pipeName = NormalizeEditorPipeName(arguments.EditorPipeName!);
        var pipeClient = NamedPipeUtil.CreateClient(serverName: ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            await pipeClient.ConnectAsync().ConfigureAwait(false);
            return new EditorConnection(pipeClient, pipeClient, pipeClient);
        }
        catch
        {
            await pipeClient.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose()
        => _disposable?.Dispose();

    private static string NormalizeEditorPipeName(string pipeName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            pipeName.StartsWith(@"\\.\pipe\", StringComparison.OrdinalIgnoreCase))
        {
            return pipeName.Substring(@"\\.\pipe\".Length);
        }

        return pipeName;
    }
}
