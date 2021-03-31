// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to display a member in MenuItem. Only used when there are multiple members on the same line.
    /// </summary>
    internal class MemberDisplayViewModel : InheritanceContextMenuItemViewModel
    {
        /// <summary>
        /// Inheritance Targets for this member.
        /// </summary>
        public ImmutableArray<TargetDisplayViewModel> Targets { get; }

        public MemberDisplayViewModel(InheritanceMarginItem member)
            : base(member.DisplayName, member.Glyph.GetImageMoniker(), member.DisplayName)
        {
            Targets = member.TargetItems.SelectAsArray(item => TargetDisplayViewModel.Create(item));
        }
    }
}
