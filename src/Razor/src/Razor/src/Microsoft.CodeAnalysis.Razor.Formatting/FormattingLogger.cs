// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

internal sealed class FormattingLogger(string logFolder, JsonSerializerOptions serializerOptions) : IFormattingLogger
{
    private readonly string _logFolder = logFolder;
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions;

    public void LogObject<T>(string name, T value)
    {
        Log($"{name}.json", writer => writer.Write(JsonSerializer.Serialize(value, _serializerOptions)));
    }

    public void LogSourceText(string name, SourceText sourceText)
    {
        Log($"{name}.txt", writer => sourceText.Write(writer));
    }

    public void LogMessage(string message)
    {
        Log("Messages.txt", writer => writer.WriteLine(message), FileMode.Append);
    }

    private void Log(string fileName, Action<TextWriter> writeFunc, FileMode fileMode = FileMode.CreateNew)
    {
        var filePath = Path.Combine(_logFolder, fileName);
        try
        {
            using var stream = new FileStream(filePath, fileMode);
            using var writer = new StreamWriter(stream);
            writeFunc(writer);
        }
        catch (IOException)
        {
            // Swallow IO exceptions, logging is best effort
        }
    }
}
