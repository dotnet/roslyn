// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementsMap
    {
        public static readonly ActiveStatementsMap Empty =
            new(ImmutableDictionary<DocumentId, ImmutableArray<ActiveStatement>>.Empty, ImmutableDictionary<ManagedInstructionId, ActiveStatement>.Empty);

        /// <summary>
        /// Groups active statements by document. 
        /// Multiple documents point to the same set of active statements if they are linked to the same underlying source file.
        /// </summary>
        public readonly IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>> DocumentMap;

        /// <summary>
        /// Active statements by instruction id.
        /// </summary>
        public readonly IReadOnlyDictionary<ManagedInstructionId, ActiveStatement> InstructionMap;

        public ActiveStatementsMap(
            IReadOnlyDictionary<DocumentId, ImmutableArray<ActiveStatement>> documentMap,
            IReadOnlyDictionary<ManagedInstructionId, ActiveStatement> instructionMap)
        {
            DocumentMap = documentMap;
            InstructionMap = instructionMap;
        }
    }
}
