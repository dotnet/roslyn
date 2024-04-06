// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CodeGen;

internal readonly struct LambdaRuntimeRudeEditInfo(DebugId lambdaId, RuntimeRudeEdit rudeEdit)
{
    public DebugId LambdaId { get; } = lambdaId;
    public RuntimeRudeEdit RudeEdit { get; } = rudeEdit;
}
