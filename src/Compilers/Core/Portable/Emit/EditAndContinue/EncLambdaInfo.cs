// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit;

internal readonly struct EncLambdaInfo(LambdaDebugInfo debugInfo, ImmutableArray<DebugId> structClosureIds)
{
    public readonly LambdaDebugInfo DebugInfo = debugInfo;
    public readonly ImmutableArray<DebugId> StructClosureIds = structClosureIds;
}
