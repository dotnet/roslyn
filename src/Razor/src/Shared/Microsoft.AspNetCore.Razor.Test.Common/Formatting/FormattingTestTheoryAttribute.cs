// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Test.Common;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer(typeof(FormattingTheoryDiscoverer))]
public sealed class FormattingTestTheoryAttribute(
    [CallerFilePath] string sourceFilePath = "",
    [CallerLineNumber] int sourceLineNumber = 0) : Attribute, ITheoryAttribute
{
    public bool DisableDiscoveryEnumeration { get; set; }

    public bool DisableParallelization { get; set; }

    public bool IncludeTestCaseIndex { get; set; }

    public bool SkipTestWithoutData { get; set; }

    public string? DisplayName { get; set; }

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
