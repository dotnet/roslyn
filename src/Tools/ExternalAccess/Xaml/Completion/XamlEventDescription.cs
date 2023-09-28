// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;

/// <summary>
/// Description of a XAML event handler.
/// </summary>
internal class XamlEventDescription(
    string? className,
    string? eventName,
    string? returnType,
    ImmutableArray<(string Name, string ParameterType, string Modifier)>? parameters)
{
    public string? ClassName { get; } = className;
    public string? EventName { get; } = eventName;
    public string? ReturnType { get; } = returnType;
    public ImmutableArray<(string Name, string ParameterType, string Modifier)>? Parameters { get; } = parameters;
}
