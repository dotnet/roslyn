// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin
{
    internal class InheritanceMarginTag : IGlyphTag
    {
        /// <summary>
        /// Margin moniker.
        /// </summary>
        public readonly ImageMoniker Moniker;

        /// <summary>
        /// Members needs to be shown on this line. There might be multiple members.
        /// For example:
        /// interface IBar { void Foo1(); void Foo2() }
        /// class Bar : IBar { void Foo1() { } void Foo2() { } }
        /// </summary>
        public readonly ImmutableArray<InheritanceMemberItem> MembersOnLine;

        public InheritanceMarginTag(ImmutableArray<InheritanceMemberItem> membersOnLine)
        {
            Debug.Assert(!membersOnLine.IsEmpty);

            MembersOnLine = membersOnLine;
            var aggregateRelationship = membersOnLine
                .SelectMany(member => member.TargetItems.Select(target => target.RelationToMember))
                .Aggregate((r1, r2) => r1 | r2);
            Moniker = GetMonikers(aggregateRelationship);
        }

        /// <summary>
        /// Decide which moniker should be shown.
        /// </summary>
        private static ImageMoniker GetMonikers(InheritanceRelationship inheritanceRelationship)
            => inheritanceRelationship switch
            {
                InheritanceRelationship.Implementing | InheritanceRelationship.Overriding => KnownMonikers.ImplementingOverriding,
                InheritanceRelationship.Implementing | InheritanceRelationship.Overriden => KnownMonikers.ImplementingOverridden,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Implemented) => KnownMonikers.Implemented,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Implementing) => KnownMonikers.Implementing,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Overriden) => KnownMonikers.Overridden,
                _ when inheritanceRelationship.HasFlag(InheritanceRelationship.Overriding) => KnownMonikers.Overriding,
                _ => throw ExceptionUtilities.Unreachable,
            };
    }
}
