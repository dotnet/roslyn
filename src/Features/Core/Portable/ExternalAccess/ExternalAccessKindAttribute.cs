// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess;

internal interface IExternalAccessKindMetadata
{
    string Kind { get; }
}

internal static class ExternalAccessKind
{
    public const string Individual = "Individual";
    public const string Unified = "Unified";
}

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal class ExternalAccessKindAttribute(string kind) : Attribute, IExternalAccessKindMetadata
{
    public string Kind { get; } = kind;
}
