// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.InheritanceChainMargin
{
    internal enum Relationship
    {
        SubType,
        BaseType,
        Implement,
        Override,
        Overriden,
    }

    internal class MemberInheritanceInfo
    {
        public readonly string MemberDisplayName;

        public readonly int MemberDeclarationLineNumber;

        public readonly ImmutableArray<InheritanceItem> InheritanceItems;

        public MemberInheritanceInfo(
            string memberDisplayName,
            int memberDeclarationLineNumber,
            ImmutableArray<InheritanceItem> inheritanceItems)
        {
            MemberDisplayName = memberDisplayName;
            MemberDeclarationLineNumber = memberDeclarationLineNumber;
            InheritanceItems = inheritanceItems;
        }
    }

    internal class InheritanceItem
    {
        public readonly string ItemKind;

        public readonly string Name;

        public readonly Relationship RelationToMember;

        public readonly ImmutableArray<InheritanceItemNavigator> Navigators;

        public InheritanceItem(
            string itemKind,
            string name,
            Relationship relationToMember,
            ImmutableArray<InheritanceItemNavigator> navigators)
        {
            ItemKind = itemKind;
            Name = name;
            RelationToMember = relationToMember;
            Navigators = navigators;
        }
    }

    internal class InheritanceItemNavigator
    {
        public readonly string TargetDescription;
        public readonly Func<Task> NavigationFunc;

        public InheritanceItemNavigator(string targetDescription, Func<Task> navigationFunc)
        {
            TargetDescription = targetDescription;
            NavigationFunc = navigationFunc;
        }
    }
}
