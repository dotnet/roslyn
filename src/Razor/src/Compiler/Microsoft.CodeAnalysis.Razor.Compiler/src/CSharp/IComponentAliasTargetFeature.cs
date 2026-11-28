// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Compiler.CSharp;

internal interface IComponentAliasTargetFeature : IRazorEngineFeature
{
    // Null means compilation information is unavailable, so discovery falls back to descriptors.
    // An available map is authoritative, even when it has no entry or the target couldn't be resolved.
    ComponentAliasTargetMap? GetAliasTargets(RazorCodeDocument document, CancellationToken cancellationToken);
}
