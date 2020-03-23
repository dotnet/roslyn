// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal class MoveMembersAnalysisResult
    {
        public ImmutableArray<DestinationAnalysisResult> DestinationAnalysisResults { get; }
        public INamedTypeSymbol SelectedType { get; }
        public ISymbol? SelectedMember { get; }
        public SyntaxNode SelectedNode { get; }
        public ImmutableArray<ISymbol> ValidMembersInType { get; }
        public bool CanExtractToBaseClass => SelectedType.TypeKind == TypeKind.Class;

        public MoveMembersAnalysisResult(
            INamedTypeSymbol selectedType,
            ISymbol? selectedMember,
            SyntaxNode selectedNode,
            ImmutableArray<DestinationAnalysisResult> destinationAnalysisResults)
        {
            SelectedType = selectedType;
            SelectedMember = selectedMember;
            SelectedNode = selectedNode;
            DestinationAnalysisResults = destinationAnalysisResults;

            ValidMembersInType = selectedType.GetMembers().
                WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member));
        }
    }
}
