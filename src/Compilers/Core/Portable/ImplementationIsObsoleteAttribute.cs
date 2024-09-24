// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
internal sealed class ImplementationObsoleteAttribute(string url) : Attribute
{
    public string Url { get; } = url;
}
