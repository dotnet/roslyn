// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Provides information about the way a particular symbol is being used at a symbol reference node.
/// For namespaces and types, this corresponds to values from <see cref="TypeOrNamespaceUsageInfo"/>.
/// For methods, fields, properties, events, locals and parameters, this corresponds to values from <see cref="ValueUsageInfo"/>.
/// </summary>
[DataContract]
internal readonly record struct SymbolUsageInfo
{
    public static readonly SymbolUsageInfo None = Create(ValueUsageInfo.None);

    [DataMember(Order = 0)]
    public ValueUsageInfo? ValueUsageInfoOpt { get; }

    [DataMember(Order = 1)]
    public TypeOrNamespaceUsageInfo? TypeOrNamespaceUsageInfoOpt { get; }

    // Must be public since it's used for deserialization.
    public SymbolUsageInfo(ValueUsageInfo? valueUsageInfoOpt, TypeOrNamespaceUsageInfo? typeOrNamespaceUsageInfoOpt)
    {
        Debug.Assert(valueUsageInfoOpt.HasValue ^ typeOrNamespaceUsageInfoOpt.HasValue);

        ValueUsageInfoOpt = valueUsageInfoOpt;
        TypeOrNamespaceUsageInfoOpt = typeOrNamespaceUsageInfoOpt;
    }

    public static SymbolUsageInfo Create(ValueUsageInfo valueUsageInfo)
        => new(valueUsageInfo, typeOrNamespaceUsageInfoOpt: null);

    public static SymbolUsageInfo Create(TypeOrNamespaceUsageInfo typeOrNamespaceUsageInfo)
        => new(valueUsageInfoOpt: null, typeOrNamespaceUsageInfo);

    public bool IsReadFrom()
        => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsReadFrom();

    public bool IsWrittenTo()
        => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsWrittenTo();
}
