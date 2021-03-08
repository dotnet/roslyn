// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal enum Relationship
    {
        SubType,
        BaseType,
        Implement,
        Implemented,
        Override,
        Overriden,
    }

    internal class InheritanceMemberInfo
    {
        public readonly TaggedText MemberDescription;

        public readonly int LineNumber;

        public readonly ImmutableArray<InheritanceTargetItem> InheritanceItems;

        public InheritanceMemberInfo(
            TaggedText memberDescription,
            int lineNumber,
            ImmutableArray<InheritanceTargetItem> inheritanceItems)
        {
            MemberDescription = memberDescription;
            LineNumber = lineNumber;
            InheritanceItems = inheritanceItems;
        }
    }

    internal class InheritanceTargetItem
    {
        public readonly TaggedText TargetDescription;

        public readonly Relationship RelationToMember;

        public readonly ImmutableArray<DefinitionItem> TargetDefinitionItems;

        public InheritanceTargetItem(
            TaggedText targetDescription,
            Relationship relationToMember,
            ImmutableArray<DefinitionItem> targetDefinitionItems)
        {
            TargetDescription = targetDescription;
            RelationToMember = relationToMember;
            TargetDefinitionItems = targetDefinitionItems;
        }
    }
}
