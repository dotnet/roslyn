// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides information about the way a particular symbol is being used at a symbol reference node.
    /// For namespaces and types, this corresponds to values from <see cref="TypeOrNamespaceUsageInfo"/>.
    /// For methods, fields, properties, events, locals and parameters, this corresponds to values from <see cref="ValueUsageInfo"/>.
    /// </summary>
    internal readonly struct SymbolUsageInfo : IEquatable<SymbolUsageInfo>
    {
        public static readonly SymbolUsageInfo None = Create(ValueUsageInfo.None);

        public ValueUsageInfo? ValueUsageInfoOpt { get; }
        public TypeOrNamespaceUsageInfo? TypeOrNamespaceUsageInfoOpt { get; }

        private SymbolUsageInfo(ValueUsageInfo? valueUsageInfoOpt, TypeOrNamespaceUsageInfo? typeOrNamespaceUsageInfoOpt)
        {
            Debug.Assert(valueUsageInfoOpt.HasValue ^ typeOrNamespaceUsageInfoOpt.HasValue);

            ValueUsageInfoOpt = valueUsageInfoOpt;
            TypeOrNamespaceUsageInfoOpt = typeOrNamespaceUsageInfoOpt;
        }

        public static SymbolUsageInfo Create(ValueUsageInfo valueUsageInfo)
            => new SymbolUsageInfo(valueUsageInfo, typeOrNamespaceUsageInfoOpt: null);

        public static SymbolUsageInfo Create(TypeOrNamespaceUsageInfo typeOrNamespaceUsageInfo)
            => new SymbolUsageInfo(valueUsageInfoOpt: null, typeOrNamespaceUsageInfo);

        public bool IsReadFrom()
            => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsReadFrom();

        public bool IsWrittenTo()
            => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsWrittenTo();

        public bool IsNameOnly()
            => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsNameOnly();

        public override bool Equals(object obj)
            => obj is SymbolUsageInfo && Equals((SymbolUsageInfo)obj);

        public bool Equals(SymbolUsageInfo other)
        {
            if (ValueUsageInfoOpt.HasValue)
            {
                return other is
                {
                    ValueUsageInfoOpt: { HasValue: true, Value: other.ValueUsageInfoOpt.Value }
                };
            }

            return other.TypeOrNamespaceUsageInfoOpt.HasValue &&
                TypeOrNamespaceUsageInfoOpt.Value == other.TypeOrNamespaceUsageInfoOpt.Value;
        }

        public override int GetHashCode()
            => Hash.Combine(ValueUsageInfoOpt?.GetHashCode() ?? 0, TypeOrNamespaceUsageInfoOpt?.GetHashCode() ?? 0);
    }
}
