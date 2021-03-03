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

    internal class InheritanceChainItem
    {
        public readonly string ItemKind;
        public readonly string Name;
        public readonly Relationship RelationToMember;
        public readonly ImmutableArray<InheritanceItemNavigator> Navigators;
    }

    internal class InheritanceItemNavigator
    {
        public readonly string TargetDescription;
        public readonly Func<Task> NavigationFunc;
    }
}
