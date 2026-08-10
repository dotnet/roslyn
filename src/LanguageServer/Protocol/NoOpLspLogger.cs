// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class NoOpLspLogger : ILspLogger, ILspService
{
    public static readonly NoOpLspLogger Instance = new();

    private NoOpLspLogger() { }

    public IDisposable? CreateContext(string context) => null;
    public IDisposable? CreateLanguageContext(string? context) => null;

    public void LogDebug(string message, params object[] @params)
    {
    }

    public void LogException(Exception exception, string? message = null, params object[] @params)
    {
    }

    public void LogInformation(string message, params object[] @params)
    {
    }

    public void LogWarning(string message, params object[] @params)
    {
    }

    public void LogError(string message, params object[] @params)
    {
    }
}
