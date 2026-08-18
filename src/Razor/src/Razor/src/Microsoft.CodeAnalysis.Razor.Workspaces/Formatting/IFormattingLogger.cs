// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal interface IFormattingLogger
{
    void LogMessage(string message);
    void LogObject<T>(string name, T value);
    void LogSourceText(string name, SourceText sourceText);
}
