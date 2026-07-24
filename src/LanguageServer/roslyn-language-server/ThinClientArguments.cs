// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal enum EditorTransportKind
{
    StandardInputOutput,
    Pipe,
}

internal sealed class ThinClientArguments
{
    public bool DaemonMode { get; }
    public EditorTransportKind EditorTransportKind { get; }
    public string? EditorPipeName { get; }
    public int? ClientProcessId { get; }
    public string[] ServerArguments { get; }

    private ThinClientArguments(
        bool daemonMode,
        EditorTransportKind editorTransportKind,
        string? editorPipeName,
        int? clientProcessId,
        string[] serverArguments)
    {
        DaemonMode = daemonMode;
        EditorTransportKind = editorTransportKind;
        EditorPipeName = editorPipeName;
        ClientProcessId = clientProcessId;
        ServerArguments = serverArguments;
    }

    public static ThinClientArguments Parse(string[] args)
    {
        var daemonMode = false;
        EditorTransportKind? editorTransportKind = null;
        string? editorPipeName = null;
        int? clientProcessId = null;
        var serverArguments = new List<string>(args.Length);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--daemon-mode")
            {
                daemonMode = true;
                continue;
            }

            if (arg == "--stdio")
            {
                SetEditorTransport(EditorTransportKind.StandardInputOutput, null, ref editorTransportKind, ref editorPipeName);
                continue;
            }

            if (TryGetInlineOptionValue(arg, "--pipe", out var inlinePipeName))
            {
                SetEditorTransport(EditorTransportKind.Pipe, inlinePipeName, ref editorTransportKind, ref editorPipeName);
                continue;
            }

            if (arg == "--pipe")
            {
                var pipeName = GetRequiredNextValue(args, ref i, "--pipe");
                SetEditorTransport(EditorTransportKind.Pipe, pipeName, ref editorTransportKind, ref editorPipeName);
                continue;
            }

            if (TryGetInlineOptionValue(arg, "--clientProcessId", out var inlineClientProcessId))
            {
                clientProcessId = ParseClientProcessId(inlineClientProcessId);
                continue;
            }

            if (arg == "--clientProcessId")
            {
                var value = GetRequiredNextValue(args, ref i, "--clientProcessId");
                clientProcessId = ParseClientProcessId(value);
                continue;
            }

            serverArguments.Add(arg);
        }

        if (editorTransportKind is null)
            throw new ArgumentException("Expected either --stdio or --pipe <name>.");

        return new ThinClientArguments(
            daemonMode,
            editorTransportKind.Value,
            editorPipeName,
            clientProcessId,
            serverArguments.ToArray());
    }

    private static void SetEditorTransport(
        EditorTransportKind transportKind,
        string? pipeName,
        ref EditorTransportKind? editorTransportKind,
        ref string? editorPipeName)
    {
        if (editorTransportKind is not null)
            throw new ArgumentException("Expected only one editor transport option (--stdio or --pipe).");

        if (transportKind == EditorTransportKind.Pipe && string.IsNullOrWhiteSpace(pipeName))
            throw new ArgumentException("Expected a non-empty value for --pipe.");

        editorTransportKind = transportKind;
        editorPipeName = pipeName;
    }

    private static bool TryGetInlineOptionValue(string arg, string optionName, out string value)
    {
        if (arg.StartsWith(optionName + "=", StringComparison.Ordinal))
        {
            value = arg.Substring(optionName.Length + 1);
            return true;
        }

        if (arg.StartsWith(optionName + ":", StringComparison.Ordinal))
        {
            value = arg.Substring(optionName.Length + 1);
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string GetRequiredNextValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Expected a value after {optionName}.");

        index++;
        return args[index];
    }

    private static int ParseClientProcessId(string value)
    {
        if (!int.TryParse(value, out var processId) || processId <= 0)
            throw new ArgumentException($"Expected a positive integer value for --clientProcessId, but got '{value}'.");

        return processId;
    }
}
