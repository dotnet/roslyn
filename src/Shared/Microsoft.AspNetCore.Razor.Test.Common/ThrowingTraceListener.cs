// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public sealed class ThrowingTraceListener : TraceListener
{
    private static readonly ConcurrentBag<string> _fails = [];

    public static string[] Fails => _fails.ToArray();

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ThrowingTraceListener());
    }

    public override void Fail(string? message, string? detailMessage)
    {
        var stackTrace = new StackTrace(fNeedFileInfo: true);
        var logMessage = (string.IsNullOrEmpty(message) ? "Assertion failed" : message) +
            (string.IsNullOrEmpty(detailMessage) ? "" : Environment.NewLine + detailMessage);

        _fails.Add($"{logMessage}{Environment.NewLine}{stackTrace}");

        throw new InvalidOperationException(logMessage);
    }

    public override void Write(object? o)
    {
    }

    public override void Write(object? o, string? category)
    {
    }

    public override void Write(string? message)
    {
    }

    public override void Write(string? message, string? category)
    {
    }

    public override void WriteLine(object? o)
    {
    }

    public override void WriteLine(object? o, string? category)
    {
    }

    public override void WriteLine(string? message)
    {
    }

    public override void WriteLine(string? message, string? category)
    {
    }
}
