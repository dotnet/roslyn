// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

/// <summary>
/// Thrown when trying to evaluate within a module that was not loaded due to bad metadata.
/// </summary>
internal sealed class BadMetadataModuleException(ModuleId moduleId)
    : Exception($"Unable to evaluate within module '{moduleId.DisplayName}' ({moduleId.Id}): the module metadata is invalid.")
{
    public ModuleId ModuleId { get; } = moduleId;
}
