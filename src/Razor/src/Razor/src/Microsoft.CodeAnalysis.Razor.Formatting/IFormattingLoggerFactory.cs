// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.CodeAnalysis.Remote.Razor.Formatting;

internal interface IFormattingLoggerFactory
{
    void SetLogDirectory(string? logDirectory);

    IFormattingLogger? CreateLogger(string documentFilePath, string formattingType, JsonSerializerOptions serializerOptions);
}
