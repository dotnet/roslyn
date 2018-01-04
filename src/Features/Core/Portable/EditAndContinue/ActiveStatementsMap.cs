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
        /// Maps active instruction ids to <see cref="ActiveStatement"/>. 
        /// </summary>
        public readonly IReadOnlyDictionary<ActiveInstructionId, ActiveStatement> InstructionMap;

        public ActiveStatementsMap(
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>> documentMap,
            IReadOnlyDictionary<ActiveInstructionId, ActiveStatement> instructionMap)
        {
            Debug.Assert(documentMap != null);
            Debug.Assert(instructionMap != null);

            DocumentMap = documentMap;
            InstructionMap = instructionMap;
        }
    }
}
