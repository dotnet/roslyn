// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    internal class InheritanceMarginTag : IGlyphTag
    {
        /// <summary>
        /// Margin moniker.
        /// </summary>
        public ImageMoniker Moniker { get; }

        /// <summary>
        /// Members needs to be shown on this line. There might be multiple members.
        /// For example:
        /// interface IBar { void Foo1(); void Foo2(); }
        /// class Bar : IBar { void Foo1() { } void Foo2() { } }
        /// </summary>
        public readonly ImmutableArray<InheritanceMarginItem> MembersOnLine;

        /// <summary>
        /// Used for accessibility purpose.
        /// </summary>
        public readonly int LineNumber;

        public readonly Workspace Workspace;

        public InheritanceMarginTag(Workspace workspace, int lineNumber, ImmutableArray<InheritanceMarginItem> membersOnLine)
        {
            Contract.ThrowIfTrue(membersOnLine.IsEmpty);

            Workspace = workspace;
            LineNumber = lineNumber;
            MembersOnLine = membersOnLine;
            // The common case, one line has one member, avoid to use select & aggregate
            if (membersOnLine.Length == 1)
            {
                var member = membersOnLine[0];
                var targets = member.TargetItems;
                var relationship = targets[0].RelationToMember;
                foreach (var target in targets.Skip(1))
                {
                    relationship |= target.RelationToMember;
                }

                Moniker = InheritanceMarginHelpers.GetMoniker(relationship);
            }
            else
            {
                // Multiple members on same line.
                var aggregateRelationship = membersOnLine
                    .SelectMany(member => member.TargetItems.Select(target => target.RelationToMember))
                    .Aggregate((r1, r2) => r1 | r2);
                Moniker = InheritanceMarginHelpers.GetMoniker(aggregateRelationship);
            }
        }
    }
}
