// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class MemberDisplayRowViewModel
    {
        public ImageMoniker ImageMoniker { get; }
        public string DisplayName { get; }

        public ImmutableArray<TargetDisplayRowViewModel> Targets { get; }

        public MemberDisplayRowViewModel(InheritanceMemberItem member)
        {
            ImageMoniker = member.Glyph.GetImageMoniker();
            DisplayName = member.MemberDisplayName;
            Targets = member.TargetItems.SelectAsArray(target => new TargetDisplayRowViewModel(target));
        }
    }
}
