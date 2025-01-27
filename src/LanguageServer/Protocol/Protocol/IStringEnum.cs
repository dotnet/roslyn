// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Interface that describes a string-based enumeration.
/// String-based enumerations are serialized simply as their <see cref="Value"/>.
/// </summary>
/// <remarks>
/// When implementing this interface, a constructor that takes a single string as parameters is required by
/// <see cref="StringEnumConverter{TStringEnumType}"/>.
/// </remarks>
internal interface IStringEnum
{
    /// <summary>
    /// Gets the value of the enumeration.
    /// </summary>
    string Value { get; }
}
