// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
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

            // The common case is that one line has one member.
            using var _ = ArrayBuilder<InheritanceTargetItem>.GetInstance(out var allItems);
            foreach (var marginItem in membersOnLine)
                allItems.AddRange(marginItem.TargetItems);

            var relationship = allItems[0].RelationToMember;
            for (var i = 1; i < allItems.Count; i++)
                relationship |= allItems[i].RelationToMember;

            Moniker = InheritanceMarginHelpers.GetMoniker(relationship);
        }
    }
}
