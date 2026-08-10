// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed class VoidSessionTracker : IEditAndContinueSessionTracker
{
    public static readonly VoidSessionTracker Instance = new();

    public bool IsSessionActive => false;
    public ImmutableArray<DiagnosticData> ApplyChangesDiagnostics => [];
}
