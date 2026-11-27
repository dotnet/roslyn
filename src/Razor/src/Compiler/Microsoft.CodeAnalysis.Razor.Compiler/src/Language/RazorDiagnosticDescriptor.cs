// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Language;

[DebuggerDisplay($"{{{nameof(DebuggerToString)}(),nq}}")]
public sealed record RazorDiagnosticDescriptor
{
    public string Id { get; }
    public string MessageFormat { get; }
    public RazorDiagnosticSeverity Severity { get; }

    /// <summary>
    /// The warning level at which this diagnostic is reported. Diagnostics with a warning level
    /// greater than the configured <c>RazorWarningLevel</c> are suppressed.
    /// A value of <c>0</c> means the diagnostic is always reported regardless of the configured level.
    /// </summary>
    public int WarningLevel { get; }

    public RazorDiagnosticDescriptor(string id, string messageFormat, RazorDiagnosticSeverity severity)
        : this(id, messageFormat, severity, warningLevel: 0)
    {
    }

    public RazorDiagnosticDescriptor(string id, string messageFormat, RazorDiagnosticSeverity severity, int warningLevel)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id), Resources.ArgumentCannotBeNullOrEmpty);
        }

        if (string.IsNullOrEmpty(messageFormat))
        {
            throw new ArgumentNullException(nameof(messageFormat), Resources.ArgumentCannotBeNullOrEmpty);
        }

        Id = id;
        MessageFormat = messageFormat;
        Severity = severity;
        WarningLevel = warningLevel;
    }

    private string DebuggerToString() => $"""
        Error "{Id}" (level {WarningLevel}): "{MessageFormat}"
        """;
}