// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementsMap
    {
        /// <summary>
        /// Groups active statement spans by document. 
        /// <see cref="ActiveStatementId"/> is used to identify span in this map.
        /// </summary>
        public readonly IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>> DocumentMap;

        /// <summary>
        /// Maps active statement debugger ids to <see cref="ActiveStatementId"/> and <see cref="ActiveInstructionId"/>,
        /// which is a document and an ordinal in the corresponding <see cref="DocumentMap"/> array.
        /// </summary>
        public readonly IReadOnlyDictionary<int, ActiveStatement> Ids;

        public ActiveStatementsMap(
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>> spans,
            IReadOnlyDictionary<int, ActiveStatement> ids)
        {
            Debug.Assert(spans != null);
            Debug.Assert(ids != null);

            DocumentMap = spans;
            Ids = ids;
        }
    }
}
