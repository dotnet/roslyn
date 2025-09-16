// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SourceGeneration;

/// <summary>
/// Indicates the presence and types of source generators in a project.
/// </summary>
internal enum SourceGeneratorPresence
{
    /// <summary>
    /// The project contains no source generators.
    /// </summary>
    NoSourceGenerators,

    /// <summary>
    /// The project contains source generators, but none of them are considered "required".
    /// These generators may be skipped in certain scenarios for performance reasons.
    /// </summary>
    OnlyOptionalSourceGenerators,

    /// <summary>
    /// The project contains at least one required source generator that must always run.
    /// The project may also contain additional optional generators.
    /// </summary>
    ContainsRequiredSourceGenerators
}
