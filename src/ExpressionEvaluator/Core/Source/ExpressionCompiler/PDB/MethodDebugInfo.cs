// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct MethodDebugInfo
    {
        public readonly ImmutableArray<ImmutableArray<ImportRecord>> ImportRecordGroups;
        public readonly ImmutableArray<ExternAliasRecord> ExternAliasRecords;
        public readonly string DefaultNamespaceName;

        public MethodDebugInfo(
            ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups,
            ImmutableArray<ExternAliasRecord> externAliasRecords,
            string defaultNamespaceName)
        {
            Debug.Assert(!importRecordGroups.IsDefault);
            Debug.Assert(!externAliasRecords.IsDefault);
            Debug.Assert(defaultNamespaceName != null);

            ImportRecordGroups = importRecordGroups;
            ExternAliasRecords = externAliasRecords;
            DefaultNamespaceName = defaultNamespaceName;
        }
    }
}
