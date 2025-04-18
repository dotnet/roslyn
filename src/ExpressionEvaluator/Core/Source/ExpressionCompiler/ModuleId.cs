// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal readonly struct ModuleId(Guid id, string displayName)
{
    public Guid Id { get; } = id;
    public string DisplayName { get; } = displayName;
}

internal static class Extensions
{
    public static ModuleId GetModuleId(this DkmClrModuleInstance module)
        => new(module.Mvid, module.FullName);
}
