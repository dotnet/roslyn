// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue;

/// <summary>
/// Flags regarding Edit and Continue engine capabilities.
/// </summary>
[Flags]
internal enum ManagedEditAndContinueEngineCapabilities
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Whether we can replace methods while stopped.
    /// </summary>
    CanReplaceMethodsWhileStopped = 0x1,

    /// <summary>
    /// Whether the engine supports changes made in the current method.
    /// </summary>
    SupportsInMethodReplacements = 0x2,

    /// <summary>
    /// Whether it supports applying changes once a module has been loaded.
    /// </summary>
    SupportsEditAndContinueOnModuleLoad = 0x4
}
