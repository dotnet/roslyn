// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class MemberDisplayViewModel
    {
        public ImageMoniker ImageMoniker { get; }
        public string DisplayName { get; }
        public string ToolTip { get; }
        public ImmutableArray<TargetDisplayViewModel> Targets { get; }
        public string AutomationName { get; }

        public MemberDisplayViewModel(InheritanceMarginItem member)
        {
            ImageMoniker = member.Glyph.GetImageMoniker();
            DisplayName = member.DisplayName;

            // TODO: Move this to resources.
            ToolTip = $"Click to look for '{DisplayName}'";
            AutomationName = ToolTip;

            Targets = member.TargetItems.SelectAsArray(item => new TargetDisplayViewModel(item));
        }
    }
}
