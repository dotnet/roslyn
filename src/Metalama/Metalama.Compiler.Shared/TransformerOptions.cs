// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !METALAMA_COMPILER_INTERFACE
#endif

#pragma warning disable CS8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler;

/// <summary>
///     Options of a <see cref="ISourceTransformer" />, exposed on <see cref="TransformerContext.Options" />.
/// </summary>
public sealed class TransformerOptions
{
    /// <summary>
    ///     Gets or sets a value indicating that transformers should annotate
    ///     the code with code coverage annotations from <see cref="MetalamaCompilerAnnotations" />.
    /// </summary>
    public bool RequiresCodeCoverageAnnotations { get; init; }

    public static TransformerOptions Default { get; } = new();
}
