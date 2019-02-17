// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        public static readonly ImmutableArray<string> LocalizableStringsForAllAllowedValues = CreateLocalizableStringsForAllAllowedValues();

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

        private static ImmutableArray<string> CreateLocalizableStringsForAllAllowedValues()
        {
            var valueUsageInfoStrings = Enum.GetValues(typeof(ValueUsageInfo))
                                        .Cast<ValueUsageInfo>()
                                        .Where(value => value.IsSingleBitSet())
                                        .Select(v => v.ToLocalizableString());
            var typeOrNamespaceUsageInfoStrings = Enum.GetValues(typeof(TypeOrNamespaceUsageInfo))
                                                  .Cast<TypeOrNamespaceUsageInfo>()
                                                  .Where(value => value.IsSingleBitSet())
                                                  .Select(v => v.ToLocalizableString());
            return valueUsageInfoStrings.Concat(typeOrNamespaceUsageInfoStrings).ToImmutableArray();
        }

        public bool IsReadFrom()
            => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsReadFrom();

        public bool IsWrittenTo()
            => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsWrittenTo();

        public bool IsNameOnly()
            => ValueUsageInfoOpt.HasValue && ValueUsageInfoOpt.Value.IsNameOnly();

        public string ToLocalizableString()
            => ValueUsageInfoOpt.HasValue ? ValueUsageInfoOpt.Value.ToLocalizableString() : TypeOrNamespaceUsageInfoOpt.Value.ToLocalizableString();

        public ImmutableArray<string> ToLocalizableValues()
            => ValueUsageInfoOpt.HasValue ? ValueUsageInfoOpt.Value.ToLocalizableValues() : TypeOrNamespaceUsageInfoOpt.Value.ToLocalizableValues();

        public override bool Equals(object obj)
            => obj is SymbolUsageInfo && Equals((SymbolUsageInfo)obj);

        public bool Equals(SymbolUsageInfo other)
        {
            if (ValueUsageInfoOpt.HasValue)
            {
                return other.ValueUsageInfoOpt.HasValue &&
                    ValueUsageInfoOpt.Value == other.ValueUsageInfoOpt.Value;
            }

            return other.TypeOrNamespaceUsageInfoOpt.HasValue &&
                TypeOrNamespaceUsageInfoOpt.Value == other.TypeOrNamespaceUsageInfoOpt.Value;
        }

        public override int GetHashCode()
            => Hash.Combine(ValueUsageInfoOpt?.GetHashCode() ?? 0, TypeOrNamespaceUsageInfoOpt?.GetHashCode() ?? 0);
    }
}
