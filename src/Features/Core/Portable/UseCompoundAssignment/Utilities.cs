// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCompoundAssignment
{
    internal static class Utilities
    {
        public static void GenerateMaps<TSyntaxKind>(
            ImmutableArray<(TSyntaxKind exprKind, TSyntaxKind assignmentKind, TSyntaxKind tokenKind)> kinds,
            out ImmutableDictionary<TSyntaxKind, TSyntaxKind> binaryToAssignmentMap,
            out ImmutableDictionary<TSyntaxKind, TSyntaxKind> assignmentToTokenMap)
        {
            var binaryToAssignmentBuilder = ImmutableDictionary.CreateBuilder<TSyntaxKind, TSyntaxKind>();
            var assignmentToTokenBuilder = ImmutableDictionary.CreateBuilder<TSyntaxKind, TSyntaxKind>(); ;

            foreach (var (exprKind, assignmentKind, tokenKind) in kinds)
            {
                binaryToAssignmentBuilder[exprKind] = assignmentKind;
                assignmentToTokenBuilder[assignmentKind] = tokenKind;
            }

            binaryToAssignmentMap = binaryToAssignmentBuilder.ToImmutable();
            assignmentToTokenMap = assignmentToTokenBuilder.ToImmutable();

            Debug.Assert(binaryToAssignmentMap.Count == assignmentToTokenMap.Count);
            Debug.Assert(binaryToAssignmentMap.Values.All(assignmentToTokenMap.ContainsKey));
        }
    }
}
