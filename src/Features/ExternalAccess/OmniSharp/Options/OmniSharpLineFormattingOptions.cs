// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;

internal sealed record class OmniSharpLineFormattingOptions
{
    public bool UseTabs { get; init; } = false;
    public int TabSize { get; init; } = 4;
    public int IndentationSize { get; init; } = 4;
    public string NewLine { get; init; } = Environment.NewLine;
}
