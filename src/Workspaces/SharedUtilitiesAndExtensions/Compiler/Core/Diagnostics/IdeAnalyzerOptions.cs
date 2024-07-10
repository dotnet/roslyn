// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeStyle;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// IDE specific options available to analyzers in a specific project (language).
/// </summary>
[DataContract]
internal sealed record class IdeAnalyzerOptions
{
    public static readonly IdeAnalyzerOptions CommonDefault = new();

    [DataMember] public bool CrashOnAnalyzerException { get; init; } = false;
}
