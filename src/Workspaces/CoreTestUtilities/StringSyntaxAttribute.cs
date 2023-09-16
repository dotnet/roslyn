// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.CodeAnalysis;

internal sealed class StringSyntaxAttribute(string syntax) : Attribute
{
    public string Syntax { get; } = syntax;
}
