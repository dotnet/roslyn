// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides information about the way a particular symbol is being used at a symbol reference node.
    /// For namespaces and types, this corresponds to values from <see cref="TypeOrNamespaceUsageInfo"/>.
    /// For methods, fields, properties, events, locals and parameters, this corresponds to values from <see cref="ValueUsageInfo"/>.
    /// </summary>
    internal readonly struct SymbolUsageInfo
    {
        public static readonly SymbolUsageInfo None = Create(ValueUsageInfo.None);
        public static readonly ImmutableArray<string> LocalizableStringsForAllAllowedValues = CreateLocalizableStringsForAllAllowedValues();

        private readonly ValueUsageInfo? _valueUsageInfoOpt;
        private readonly TypeOrNamespaceUsageInfo? _typeOrNamespaceUsageInfoOpt;

        private SymbolUsageInfo(ValueUsageInfo? valueUsageInfoOpt, TypeOrNamespaceUsageInfo? typeOrNamespaceUsageInfoOpt)
        {
            Debug.Assert(valueUsageInfoOpt.HasValue ^ typeOrNamespaceUsageInfoOpt.HasValue);

            _valueUsageInfoOpt = valueUsageInfoOpt;
            _typeOrNamespaceUsageInfoOpt = typeOrNamespaceUsageInfoOpt;
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
            => _valueUsageInfoOpt.HasValue && _valueUsageInfoOpt.Value.IsReadFrom();

        public bool IsWrittenTo()
            => _valueUsageInfoOpt.HasValue && _valueUsageInfoOpt.Value.IsWrittenTo();

        public bool IsNameOnly()
            => _valueUsageInfoOpt.HasValue && _valueUsageInfoOpt.Value.IsNameOnly();

        public string ToLocalizableString()
            => _valueUsageInfoOpt.HasValue ? _valueUsageInfoOpt.Value.ToLocalizableString() : _typeOrNamespaceUsageInfoOpt.Value.ToLocalizableString();

        public ImmutableArray<string> ToLocalizableValues()
            => _valueUsageInfoOpt.HasValue ? _valueUsageInfoOpt.Value.ToLocalizableValues() : _typeOrNamespaceUsageInfoOpt.Value.ToLocalizableValues();
    }
}
