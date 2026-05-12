// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language;

public interface IRazorTargetExtensionFeature : IRazorEngineFeature
{
    ImmutableArray<ICodeTargetExtension>.Builder TargetExtensions { get; }
}
