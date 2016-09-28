// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial class MethodDebugInfo<TTypeSymbol, TLocalSymbol>
        where TTypeSymbol : class, ITypeSymbol
        where TLocalSymbol : class
    {
        public static readonly MethodDebugInfo<TTypeSymbol, TLocalSymbol> None = new MethodDebugInfo<TTypeSymbol, TLocalSymbol>(
            ImmutableArray<HoistedLocalScopeRecord>.Empty,
            ImmutableArray<ImmutableArray<ImportRecord>>.Empty,
            ImmutableArray<ExternAliasRecord>.Empty,
            null,
            null,
            "",
            ImmutableArray<string>.Empty,
            ImmutableArray<TLocalSymbol>.Empty,
            ILSpan.MaxValue);

        public readonly ImmutableArray<HoistedLocalScopeRecord> HoistedLocalScopeRecords;
        public readonly ImmutableArray<ImmutableArray<ImportRecord>> ImportRecordGroups;
        public readonly ImmutableArray<ExternAliasRecord> ExternAliasRecords; // C# only.
        public readonly ImmutableDictionary<int, ImmutableArray<bool>> DynamicLocalMap; // C# only.
        public readonly ImmutableDictionary<int, ImmutableArray<string>> TupleLocalMap;
        public readonly string DefaultNamespaceName; // VB only.
        public readonly ImmutableArray<string> LocalVariableNames;
        public readonly ImmutableArray<TLocalSymbol> LocalConstants;
        public readonly ILSpan ReuseSpan;

        public MethodDebugInfo(
            ImmutableArray<HoistedLocalScopeRecord> hoistedLocalScopeRecords,
            ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups,
            ImmutableArray<ExternAliasRecord> externAliasRecords,
            ImmutableDictionary<int, ImmutableArray<bool>> dynamicLocalMap,
            ImmutableDictionary<int, ImmutableArray<string>> tupleLocalMap,
            string defaultNamespaceName,
            ImmutableArray<string> localVariableNames,
            ImmutableArray<TLocalSymbol> localConstants,
            ILSpan reuseSpan)
        {
            Debug.Assert(!importRecordGroups.IsDefault);
            Debug.Assert(!externAliasRecords.IsDefault);
            Debug.Assert(defaultNamespaceName != null);
            Debug.Assert(!hoistedLocalScopeRecords.IsDefault);

            HoistedLocalScopeRecords = hoistedLocalScopeRecords;
            ImportRecordGroups = importRecordGroups;

            ExternAliasRecords = externAliasRecords;
            DynamicLocalMap = dynamicLocalMap;
            TupleLocalMap = tupleLocalMap;

            DefaultNamespaceName = defaultNamespaceName;

            LocalVariableNames = localVariableNames;
            LocalConstants = localConstants;
            ReuseSpan = reuseSpan;
        }

        public ImmutableSortedSet<int> GetInScopeHoistedLocalIndices(int ilOffset, ref ILSpan methodContextReuseSpan)
        {
            if (this.HoistedLocalScopeRecords.IsEmpty)
            {
                return ImmutableSortedSet<int>.Empty;
            }

            methodContextReuseSpan = MethodContextReuseConstraints.CalculateReuseSpan(
                ilOffset,
                methodContextReuseSpan,
                HoistedLocalScopeRecords.Select(record => new ILSpan((uint)record.StartOffset, (uint)(record.StartOffset + record.Length))));

            var scopesBuilder = ArrayBuilder<int>.GetInstance();
            int i = 0;
            foreach (var record in this.HoistedLocalScopeRecords)
            {
                var delta = ilOffset - record.StartOffset;
                if (0 <= delta && delta < record.Length)
                {
                    scopesBuilder.Add(i);
                }

                i++;
            }

            var result = scopesBuilder.ToImmutableSortedSet();
            scopesBuilder.Free();
            return result;
        }
    }
}
