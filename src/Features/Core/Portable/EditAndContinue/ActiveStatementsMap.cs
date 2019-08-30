// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementsMap
    {
        /// <summary>
        /// Groups active statements by document. 
        /// Multiple documents point to the same set of active statements if they are linked to the same underlying source file.
        /// </summary>
        public readonly IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>> DocumentMap;

        /// <summary>
        /// Active statements by instruction id.
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
