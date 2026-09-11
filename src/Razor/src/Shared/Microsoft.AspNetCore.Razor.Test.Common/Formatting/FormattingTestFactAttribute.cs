// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

using System;
using System.Runtime.CompilerServices;
using XunitV3::Xunit.v3;

namespace Microsoft.AspNetCore.Razor.Test.Common;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer(typeof(FormattingFactDiscoverer))]
public sealed class FormattingTestFactAttribute(
    [CallerFilePath] string sourceFilePath = "",
    [CallerLineNumber] int sourceLineNumber = 0) : Attribute, IFactAttribute
{
    public string? DisplayName { get; set; }

    public bool DisableParallelization { get; set; }

    public bool Explicit { get; set; }

    public string? Skip { get; set; }

    public Type[]? SkipExceptions { get; set; }

    public Type? SkipType { get; set; }

    public string? SkipUnless { get; set; }

    public string? SkipWhen { get; set; }

    public string SourceFilePath { get; } = sourceFilePath;

    public int? SourceLineNumber { get; } = sourceLineNumber;

    public int Timeout { get; set; }
}
