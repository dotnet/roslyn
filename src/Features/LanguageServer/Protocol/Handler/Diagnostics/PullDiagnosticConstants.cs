// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal static class PullDiagnosticConstants
{
    public const string TaskItemCustomTag = nameof(TaskItemCustomTag);

    public const string Priority = nameof(Priority);
    public const string Low = nameof(Low);
    public const string Medium = nameof(Medium);
    public const string High = nameof(High);
}
