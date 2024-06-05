// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class SuppressionInfoExtensions
{
    internal static ImmutableArray<Suppression> ProgrammaticSuppressions(this SuppressionInfo suppressionInfo)
        => throw new NotImplementedException();
}
